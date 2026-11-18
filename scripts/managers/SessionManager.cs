using Godot;
using Godot.Collections;
using System;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// SessionManager is the autoload for campaign save slots and account settings.
///
/// Since 2026-11-18 saves belong to the signed-in account and live behind the accounts API
/// (<see cref="SaveStore"/>); the game no longer opens a database. Play requires sign-in. When
/// the account signs in, its settings documents and slot list are loaded into memory
/// (<see cref="AccountDocs"/>), and <see cref="AccountLoadedEventHandler"/> fires so settings,
/// key bindings, prompt style and operative progression can re-apply. Failures are reported in
/// <see cref="LastStorageError"/>; nothing is written locally.
/// </summary>
public partial class SessionManager : Node
{
	/// <summary>The process-wide autoload.</summary>
	public static SessionManager Instance { get; private set; }

	/// <summary>Raised after the signed-in account's data loaded (true) or after sign-out (false).</summary>
	[Signal] public delegate void AccountLoadedEventHandler(bool signedIn);

	/// <summary>Most recent save failure, in player-safe words; empty after a success.</summary>
	public string LastStorageError { get; private set; } = "";

	/// <summary>The selected campaign slot id, or -1 for none (direct scene play saves nothing).</summary>
	[Export] public int CurrentSessionId { get; set; } = -1;
	/// <summary>The selected slot's summary in the old dictionary shape (id, character, current_level, ...).</summary>
	public Dictionary CurrentSessionData { get; set; } = new Dictionary();

	/// <summary>True when a player is signed in and their account data has loaded.</summary>
	public bool AccountReady => AuthManager.Instance?.CurrentUser != null && AccountDocs.Loaded;

	// Slot summaries from the last load, for the synchronous Continue check.
	private System.Collections.Generic.List<SaveSlot> _slots = new();

	// Blocking GDScript aliases call these async methods on the main thread, so every await below
	// (except in LoadAccountAsync, which emits a signal) continues off the main thread to avoid deadlock.

	/// <summary>Registers the singleton and follows sign-in changes once AuthManager exists (it loads later).</summary>
	public override void _Ready()
	{
		Instance = this;
		Callable.From(() =>
		{
			if (AuthManager.Instance != null) AuthManager.Instance.OnProfileUpdated += OnAuthChanged;
		}).CallDeferred();
	}

	/// <summary>Stops following sign-in changes.</summary>
	public override void _ExitTree()
	{
		if (AuthManager.Instance != null) AuthManager.Instance.OnProfileUpdated -= OnAuthChanged;
		if (Instance == this) Instance = null;
	}

	/// <summary>Loads the account after sign-in; clears everything after sign-out.</summary>
	private void OnAuthChanged()
	{
		if (AuthManager.Instance?.CurrentUser != null) _ = LoadAccountAsync();
		else
		{
			AccountDocs.Clear();
			_slots = new();
			CurrentSessionId = -1;
			CurrentSessionData = new Dictionary();
			if (IsInsideTree()) EmitSignal(SignalName.AccountLoaded, false);
		}
	}

	/// <summary>
	/// Fetches the account's documents and slot list. Returns false (and sets
	/// <see cref="LastStorageError"/>) when the service cannot be reached; the menu shows Retry.
	/// </summary>
	public async Task<bool> LoadAccountAsync()
	{
		try
		{
			var docs = await SaveStore.Current.GetAccountDocsAsync();
			var slots = await SaveStore.Current.ListSlotsAsync();
			AccountDocs.ReplaceAll(docs);
			_slots = slots.ToList();
			LastStorageError = "";
			if (IsInsideTree()) EmitSignal(SignalName.AccountLoaded, true);
			return true;
		}
		catch (SaveStoreException error)
		{
			LastStorageError = error.Message;
			GD.PrintErr($"SessionManager: account load failed ({error.Message}).");
			return false;
		}
	}

	/// <summary>The old dictionary shape the menus and GDScript use for one slot.</summary>
	private static Dictionary RowOf(SaveSlot slot) => new()
	{
		{ "id", slot.Id }, { "character", slot.Character }, { "current_level", slot.CurrentLevel },
		{ "total_score", (int)Math.Min(int.MaxValue, slot.TotalScore) }, { "collectables", slot.Collectables },
		{ "created_at", slot.CreatedAt }, { "last_played", slot.LastPlayed },
	};

	/// <summary>Records a failure message and logs it without details that could leak tokens.</summary>
	private void Fail(Exception error, string action)
	{
		LastStorageError = error is SaveStoreException ? error.Message : "The save service could not be reached. Retry.";
		GD.PrintErr($"SessionManager: {action} failed ({error.GetType().Name}).");
	}

	/// <summary>Creates a campaign slot on the account and selects it; returns -1 on failure.</summary>
	public async Task<int> CreateNewSessionAsync(string characterName)
	{
		try
		{
			var slot = await SaveStore.Current.CreateSlotAsync(characterName).ConfigureAwait(false);
			_slots.Insert(0, slot);
			CurrentSessionId = slot.Id;
			CurrentSessionData = RowOf(slot);
			LastStorageError = "";
			return slot.Id;
		}
		catch (SaveStoreException error) { Fail(error, "creating a campaign"); return -1; }
	}

	/// <summary>Lists the account's campaigns, newest first; returns an empty array on failure.</summary>
	public async Task<Godot.Collections.Array> GetAllSessionsAsync()
	{
		var result = new Godot.Collections.Array();
		try
		{
			_slots = (await SaveStore.Current.ListSlotsAsync().ConfigureAwait(false)).ToList();
			foreach (var slot in _slots) result.Add(RowOf(slot));
			LastStorageError = "";
		}
		catch (SaveStoreException error) { Fail(error, "listing campaigns"); }
		return result;
	}

	/// <summary>Loads one campaign summary by id; returns an empty dictionary when absent or unavailable.</summary>
	public async Task<Dictionary> GetSessionAsync(int sessionId)
	{
		try
		{
			var snapshot = await SaveStore.Current.GetSlotAsync(sessionId).ConfigureAwait(false);
			return snapshot == null ? new Dictionary() : RowOf(snapshot.Slot);
		}
		catch (SaveStoreException error) { Fail(error, "reading a campaign"); return new Dictionary(); }
	}

	/// <summary>Selects an existing campaign. Its last-played time updates with its next save.</summary>
	public async Task<bool> LoadSessionAsync(int sessionId)
	{
		var data = await GetSessionAsync(sessionId).ConfigureAwait(false);
		if (data.Count == 0) return false;
		CurrentSessionId = sessionId;
		CurrentSessionData = data;
		return true;
	}

	/// <summary>Adds score and collectibles while advancing the saved mission (never lowers it), as one commit.</summary>
	public async Task UpdateSessionAsync(int sessionId, int level, int scoreIncrement, int collectablesIncrement)
	{
		var result = await SlotTransaction.RunAsync(SaveStore.Current, sessionId, snapshot => new SlotCommit(snapshot.Slot.Revision,
			LevelAtLeast: Math.Clamp(level, 1, 99), ScoreAdd: Math.Max(0, scoreIncrement), CollectablesAdd: Math.Max(0, collectablesIncrement))).ConfigureAwait(false);
		if (result.Outcome is CommitOutcome.Committed or CommitOutcome.AlreadyCommitted)
		{
			LastStorageError = "";
			if (sessionId == CurrentSessionId && result.Snapshot != null) CurrentSessionData = RowOf(result.Snapshot.Slot);
		}
		else LastStorageError = string.IsNullOrEmpty(result.Error) ? "The campaign could not be saved. Retry." : result.Error;
	}

	/// <summary>Kept for older callers: a slot's last-played time now updates with every save.</summary>
	public Task UpdateLastPlayedAsync(int sessionId) => Task.CompletedTask;

	/// <summary>Deletes one of the account's campaigns and clears the selection if it was selected.</summary>
	public async Task DeleteSessionAsync(int sessionId)
	{
		try
		{
			await SaveStore.Current.DeleteSlotAsync(sessionId).ConfigureAwait(false);
			_slots.RemoveAll(s => s.Id == sessionId);
			if (sessionId == CurrentSessionId) { CurrentSessionId = -1; CurrentSessionData = new Dictionary(); }
			LastStorageError = "";
		}
		catch (SaveStoreException error) { Fail(error, "deleting a campaign"); }
	}

	/// <summary>Deletes all of the account's campaigns (no one else's) and clears the selection.</summary>
	public async Task DeleteAllSessionsAsync()
	{
		try
		{
			await SaveStore.Current.DeleteAllSlotsAsync().ConfigureAwait(false);
			_slots = new();
			CurrentSessionId = -1;
			CurrentSessionData = new Dictionary();
			LastStorageError = "";
		}
		catch (SaveStoreException error) { Fail(error, "deleting campaigns"); }
	}

	/// <summary>True when the signed-in account has campaigns (from the last load; no network call).</summary>
	public bool HasSessions() => AccountReady && _slots.Count > 0;

	/// <summary>Formats a saved ISO timestamp to minute precision for the menu.</summary>
	public string FormatDatetime(string datetimeStr)
	{
		if (string.IsNullOrEmpty(datetimeStr)) return "Unknown";
		var formatted = datetimeStr.Replace("T", " ");
		var parts = formatted.Split(":");
		return parts.Length >= 2 ? parts[0] + ":" + parts[1] : formatted;
	}

	/// <summary>An account settings document for GDScript and C#; empty before the account loads.</summary>
	public Dictionary ReadState(string key)
	{
		try { return Json.ParseString(AccountDocs.Get(key) ?? "{}").AsGodotDictionary(); }
		catch (Exception ex) { GD.PrintErr($"Account settings could not be read ({ex.GetType().Name})."); return new Dictionary(); }
	}

	/// <summary>
	/// Saves an account settings document: the cache updates at once and the account copy is sent in
	/// the background. Returns false when no account is loaded (nothing is written anywhere).
	/// </summary>
	public bool WriteState(string key, Dictionary value) => WriteStateJson(key, Json.Stringify(value));

	/// <summary>Like <see cref="WriteState"/>, for callers that already hold JSON text.</summary>
	public bool WriteStateJson(string key, string json)
	{
		if (!AccountReady) return false;
		AccountDocs.Set(key, json);
		_ = PutInBackgroundAsync(key, json);
		return true;
	}

	/// <summary>Sends one account document; a failure is recorded so the menu can show it.</summary>
	private async Task PutInBackgroundAsync(string key, string json)
	{
		try { await SaveStore.Current.PutAccountDocAsync(key, json).ConfigureAwait(false); LastStorageError = ""; }
		catch (SaveStoreException error) { Fail(error, $"saving {key}"); }
	}

	/// <summary>
	/// Test hook: routes every save through a fresh in-memory store with the server's commit rules,
	/// so GDScript tests never reach the network or a real account. Pair with <see cref="UseServiceStore"/>.
	/// </summary>
	public void UseInMemoryStoreForTests() => SaveStore.Current = new InMemorySaveStore();

	/// <summary>Test hook: restores the real HTTPS save service.</summary>
	public void UseServiceStore() => SaveStore.Current = new HttpSaveStore();

	// ─── GDScript snake_case aliases (blocking; HTTP calls do not need the main thread) ───
	/// <summary>GDScript entry point: create a campaign.</summary>
	public int create_new_session(string character_name) => CreateNewSessionAsync(character_name).GetAwaiter().GetResult();
	/// <summary>GDScript entry point: list campaigns.</summary>
	public Godot.Collections.Array get_all_sessions() => GetAllSessionsAsync().GetAwaiter().GetResult();
	/// <summary>GDScript entry point: read a campaign.</summary>
	public Dictionary get_session(int session_id) => GetSessionAsync(session_id).GetAwaiter().GetResult();
	/// <summary>GDScript entry point: select a campaign.</summary>
	public bool load_session(int session_id) => LoadSessionAsync(session_id).GetAwaiter().GetResult();
	/// <summary>GDScript entry point: update a campaign's progress.</summary>
	public void update_session(int session_id, int level, int score, int collectables)
		=> UpdateSessionAsync(session_id, level, score, collectables).Wait();
	/// <summary>GDScript entry point: delete a campaign.</summary>
	public void delete_session(int session_id) => DeleteSessionAsync(session_id).Wait();
	/// <summary>GDScript entry point: delete all of the account's campaigns.</summary>
	public void delete_all_sessions() => DeleteAllSessionsAsync().Wait();
	/// <summary>GDScript entry point: whether the account has campaigns.</summary>
	public bool has_sessions() => HasSessions();
	/// <summary>GDScript entry point: format a timestamp.</summary>
	public string format_datetime(string datetime_str) => FormatDatetime(datetime_str);
}
