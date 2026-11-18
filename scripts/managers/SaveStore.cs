using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

/// <summary>One campaign save slot as the API returns it (2026-11-18: saves belong to accounts).</summary>
public sealed record SaveSlot(int Id, string Character, int CurrentLevel, long TotalScore, int Collectables,
    string CreatedAt, string LastPlayed, long Revision);

/// <summary>One stored document of a slot: its schema version and JSON text.</summary>
public sealed record SlotDoc(int SchemaVersion, string Json);

/// <summary>A slot with every document it holds (keys <c>world</c>, <c>world:&lt;city&gt;</c>, <c>progression</c>).</summary>
public sealed record SlotSnapshot(SaveSlot Slot, IReadOnlyDictionary<string, SlotDoc> Docs)
{
    /// <summary>The JSON of one document, or null when the slot has none under that key.</summary>
    public string? Json(string key) => Docs.TryGetValue(key, out var doc) ? doc.Json : null;
}

/// <summary>One document to write in a commit.</summary>
public sealed record SlotDocWrite(string Key, string Json, int SchemaVersion = 1);

/// <summary>
/// One all-or-nothing write to a slot. <see cref="ExpectRevision"/> is the revision that was read;
/// a <see cref="Token"/> with its receipt makes the commit safe to retry (see <c>backend/src/lib/gameSaves.js</c>).
/// </summary>
public sealed record SlotCommit(long ExpectRevision, string? Token = null, string? ReceiptKind = null, string? ReceiptJson = null,
    string? Character = null, int? LevelAtLeast = null, long? ScoreAdd = null, int? CollectablesAdd = null,
    IReadOnlyList<SlotDocWrite>? Docs = null);

/// <summary>What happened to a commit.</summary>
public enum CommitOutcome { Committed, AlreadyCommitted, RevisionConflict, ReceiptMismatch, SchemaNewer, NotFound, NotSignedIn, Failed }

/// <summary>A commit's outcome and, where the server sent one, the slot as it now stands.</summary>
public sealed record CommitResult(CommitOutcome Outcome, SlotSnapshot? Snapshot = null, string Error = "");

/// <summary>Raised when the save service cannot be reached or refuses a read, so callers can show Retry.</summary>
public sealed class SaveStoreException : Exception
{
    /// <summary>True when the player is not signed in (or the session ended).</summary>
    public bool NotSignedIn { get; }
    /// <summary>Creates the exception with a player-safe message.</summary>
    public SaveStoreException(string message, bool notSignedIn = false) : base(message) { NotSignedIn = notSignedIn; }
}

/// <summary>
/// Per-account save storage. The game calculates rewards and world state; the store keeps them per
/// account with all-or-nothing commits, retry-safe receipts and revision conflicts. Reads throw
/// <see cref="SaveStoreException"/> on failure; commits report failure through <see cref="CommitResult"/>.
/// </summary>
public interface ISaveStore
{
    /// <summary>The signed-in player's slots, most recently played first.</summary>
    Task<IReadOnlyList<SaveSlot>> ListSlotsAsync();
    /// <summary>Creates a slot starting with the given operative.</summary>
    Task<SaveSlot> CreateSlotAsync(string character);
    /// <summary>A slot with its documents, or null when it does not exist for this player.</summary>
    Task<SlotSnapshot?> GetSlotAsync(int slotId);
    /// <summary>Applies one all-or-nothing commit.</summary>
    Task<CommitResult> CommitAsync(int slotId, SlotCommit commit);
    /// <summary>Deletes one slot; false when it did not exist.</summary>
    Task<bool> DeleteSlotAsync(int slotId);
    /// <summary>Deletes all of the player's slots and returns how many.</summary>
    Task<int> DeleteAllSlotsAsync();
    /// <summary>The player's account documents (settings, key bindings, prompt style, operative progression) as key to JSON.</summary>
    Task<Dictionary<string, string>> GetAccountDocsAsync();
    /// <summary>Replaces one account document.</summary>
    Task PutAccountDocAsync(string key, string json);
}

/// <summary>The store the game uses. Tests replace it with an <see cref="InMemorySaveStore"/>.</summary>
public static class SaveStore
{
    private static ISaveStore? _current;
    /// <summary>The active store; the HTTPS store unless a test installed another.</summary>
    public static ISaveStore Current { get => _current ??= new HttpSaveStore(); set => _current = value; }
}

/// <summary>
/// Runs "read the slot, compute, commit" with the conflict rule: if another commit landed first,
/// the server returns the current slot and the computation runs again on it (at most 3 times).
/// </summary>
public static class SlotTransaction
{
    /// <summary>How many times a conflicted commit is recomputed before giving up.</summary>
    public const int MaxAttempts = 3;

    /// <summary>
    /// Loads <paramref name="slotId"/>, asks <paramref name="plan"/> for a commit (or a final answer
    /// without writing), commits, and repeats on a revision conflict. A missing slot returns
    /// <see cref="CommitOutcome.NotFound"/>; any other failure is returned as is.
    /// </summary>
    /// <param name="plan">Given the current slot, returns the commit to send, or null to stop without writing.</param>
    public static async Task<CommitResult> RunAsync(ISaveStore store, int slotId, Func<SlotSnapshot, SlotCommit?> plan)
    {
        SlotSnapshot? snapshot;
        try { snapshot = await store.GetSlotAsync(slotId).ConfigureAwait(false); }
        catch (SaveStoreException error) { return new CommitResult(error.NotSignedIn ? CommitOutcome.NotSignedIn : CommitOutcome.Failed, null, error.Message); }
        if (snapshot == null) return new CommitResult(CommitOutcome.NotFound, null, "The selected campaign no longer exists.");
        for (int attempt = 1; ; attempt++)
        {
            var commit = plan(snapshot);
            if (commit == null) return new CommitResult(CommitOutcome.Committed, snapshot);
            var result = await store.CommitAsync(slotId, commit).ConfigureAwait(false);
            if (result.Outcome != CommitOutcome.RevisionConflict || attempt >= MaxAttempts || result.Snapshot == null) return result;
            snapshot = result.Snapshot;
        }
    }
}

/// <summary>
/// In-memory store with the same commit rules as the server (ownership is implicit: one player).
/// Used by tests and by direct scene play in the editor. Thread-safe; commits run one at a time,
/// like the server's slot row lock.
/// </summary>
public sealed class InMemorySaveStore : ISaveStore
{
    private sealed class Slot
    {
        public SaveSlot Row = null!;
        public Dictionary<string, SlotDoc> Docs = new();
        public Dictionary<string, (string Kind, string Json)> Receipts = new();
    }
    private readonly Dictionary<int, Slot> _slots = new();
    private readonly Dictionary<string, string> _account = new();
    private readonly SemaphoreSlim _lock = new(1, 1);
    private int _nextId = 1;
    /// <summary>When true every call fails as if the service were unreachable (outage tests).</summary>
    public bool Offline { get; set; }

    private void ThrowIfOffline() { if (Offline) throw new SaveStoreException("The save service could not be reached."); }
    private static string Now() => DateTime.UtcNow.ToString("O");
    private static SlotSnapshot Snap(Slot s) => new(s.Row, new Dictionary<string, SlotDoc>(s.Docs));

    /// <inheritdoc/>
    public Task<IReadOnlyList<SaveSlot>> ListSlotsAsync()
    {
        ThrowIfOffline();
        lock (_slots) return Task.FromResult<IReadOnlyList<SaveSlot>>(_slots.Values.Select(s => s.Row).OrderByDescending(r => r.LastPlayed).ThenByDescending(r => r.Id).ToList());
    }

    /// <inheritdoc/>
    public Task<SaveSlot> CreateSlotAsync(string character)
    {
        ThrowIfOffline();
        lock (_slots)
        {
            var row = new SaveSlot(_nextId++, character, 1, 0, 0, Now(), Now(), 0);
            _slots[row.Id] = new Slot { Row = row };
            return Task.FromResult(row);
        }
    }

    /// <inheritdoc/>
    public Task<SlotSnapshot?> GetSlotAsync(int slotId)
    {
        ThrowIfOffline();
        lock (_slots) return Task.FromResult(_slots.TryGetValue(slotId, out var s) ? Snap(s) : null);
    }

    /// <inheritdoc/>
    public async Task<CommitResult> CommitAsync(int slotId, SlotCommit commit)
    {
        if (Offline) return new CommitResult(CommitOutcome.Failed, null, "The save service could not be reached.");
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            Slot? s;
            lock (_slots) _slots.TryGetValue(slotId, out s);
            if (s == null) return new CommitResult(CommitOutcome.NotFound);
            // Receipts are checked before the revision, as on the server.
            if (commit.Token != null && s.Receipts.TryGetValue(commit.Token, out var receipt))
                return receipt.Kind == commit.ReceiptKind && JsonEqual(receipt.Json, commit.ReceiptJson)
                    ? new CommitResult(CommitOutcome.AlreadyCommitted, Snap(s))
                    : new CommitResult(CommitOutcome.ReceiptMismatch, null, "This token was already used for a different result.");
            if (s.Row.Revision != commit.ExpectRevision) return new CommitResult(CommitOutcome.RevisionConflict, Snap(s));
            foreach (var doc in commit.Docs ?? Array.Empty<SlotDocWrite>())
                if (s.Docs.TryGetValue(doc.Key, out var old) && old.SchemaVersion > doc.SchemaVersion)
                    return new CommitResult(CommitOutcome.SchemaNewer, null, $"Document {doc.Key} was saved by a newer game version.");
            foreach (var doc in commit.Docs ?? Array.Empty<SlotDocWrite>()) s.Docs[doc.Key] = new SlotDoc(doc.SchemaVersion, doc.Json);
            s.Row = s.Row with
            {
                Character = commit.Character ?? s.Row.Character,
                CurrentLevel = Math.Max(s.Row.CurrentLevel, commit.LevelAtLeast ?? s.Row.CurrentLevel),
                TotalScore = s.Row.TotalScore + (commit.ScoreAdd ?? 0),
                Collectables = s.Row.Collectables + (commit.CollectablesAdd ?? 0),
                LastPlayed = Now(), Revision = s.Row.Revision + 1,
            };
            if (commit.Token != null) s.Receipts[commit.Token] = (commit.ReceiptKind ?? "", commit.ReceiptJson ?? "null");
            return new CommitResult(CommitOutcome.Committed, Snap(s));
        }
        finally { _lock.Release(); }
    }

    /// <summary>JSON equality by content (property order and spacing ignored), like PostgreSQL jsonb.</summary>
    private static bool JsonEqual(string? a, string? b) =>
        JsonNode.DeepEquals(JsonNode.Parse(a ?? "null"), JsonNode.Parse(b ?? "null"));

    /// <inheritdoc/>
    public Task<bool> DeleteSlotAsync(int slotId) { ThrowIfOffline(); lock (_slots) return Task.FromResult(_slots.Remove(slotId)); }

    /// <inheritdoc/>
    public Task<int> DeleteAllSlotsAsync() { ThrowIfOffline(); lock (_slots) { int n = _slots.Count; _slots.Clear(); return Task.FromResult(n); } }

    /// <inheritdoc/>
    public Task<Dictionary<string, string>> GetAccountDocsAsync() { ThrowIfOffline(); lock (_account) return Task.FromResult(new Dictionary<string, string>(_account)); }

    /// <inheritdoc/>
    public Task PutAccountDocAsync(string key, string json) { ThrowIfOffline(); lock (_account) _account[key] = json; return Task.CompletedTask; }
}

/// <summary>
/// The real store: HTTPS calls to <c>/api/game/</c> with the player's bearer token, through
/// <see cref="AuthManager.SendAuthorizedAsync"/> (which refreshes an expired token once and retries).
/// </summary>
public sealed class HttpSaveStore : ISaveStore
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Sends one authorized request and returns status plus body text.</summary>
    private static async Task<(HttpStatusCode Status, string Body)> SendAsync(HttpMethod method, string path, JsonNode? body = null)
    {
        var auth = AuthManager.Instance ?? throw new SaveStoreException("Sign in to save.", notSignedIn: true);
        try
        {
            using var response = await auth.SendAuthorizedAsync(() =>
            {
                var request = new HttpRequestMessage(method, AuthManager.ApiBaseUrl + "/game" + path);
                if (body != null) request.Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
                return request;
            }).ConfigureAwait(false);
            if (response == null) throw new SaveStoreException("Sign in to save.", notSignedIn: true);
            return (response.StatusCode, await response.Content.ReadAsStringAsync().ConfigureAwait(false));
        }
        catch (HttpRequestException) { throw new SaveStoreException("The save service could not be reached. Check your connection and retry."); }
        catch (TaskCanceledException) { throw new SaveStoreException("The save service did not answer in time. Retry."); }
    }

    /// <summary>Throws a player-safe exception for any non-success status.</summary>
    private static void Ensure(HttpStatusCode status)
    {
        if (status == HttpStatusCode.Unauthorized) throw new SaveStoreException("Sign in to save.", notSignedIn: true);
        if ((int)status >= 400) throw new SaveStoreException($"The save service refused the request ({(int)status}).");
    }

    /// <summary>Parses a slot object from the API.</summary>
    private static SaveSlot SlotFrom(JsonNode n) => new(
        (int)n["id"]!.GetValue<long>(), n["character"]!.GetValue<string>(), n["current_level"]!.GetValue<int>(),
        n["total_score"]!.GetValue<long>(), n["collectables"]!.GetValue<int>(), n["created_at"]!.ToString(),
        n["last_played"]!.ToString(), n["revision"]!.GetValue<long>());

    /// <summary>Parses `{slot, docs}` from the API.</summary>
    private static SlotSnapshot SnapshotFrom(JsonNode n)
    {
        var docs = new Dictionary<string, SlotDoc>();
        if (n["docs"] is JsonObject map)
            foreach (var (key, value) in map)
                docs[key] = new SlotDoc(value!["schemaVersion"]!.GetValue<int>(), value["value"]!.ToJsonString());
        return new SlotSnapshot(SlotFrom(n["slot"]!), docs);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SaveSlot>> ListSlotsAsync()
    {
        var (status, body) = await SendAsync(HttpMethod.Get, "/slots/").ConfigureAwait(false);
        Ensure(status);
        return JsonNode.Parse(body)!.AsArray().Select(n => SlotFrom(n!)).ToList();
    }

    /// <inheritdoc/>
    public async Task<SaveSlot> CreateSlotAsync(string character)
    {
        var (status, body) = await SendAsync(HttpMethod.Post, "/slots/", new JsonObject { ["character"] = character }).ConfigureAwait(false);
        Ensure(status);
        return SlotFrom(JsonNode.Parse(body)!);
    }

    /// <inheritdoc/>
    public async Task<SlotSnapshot?> GetSlotAsync(int slotId)
    {
        var (status, body) = await SendAsync(HttpMethod.Get, $"/slots/{slotId}/").ConfigureAwait(false);
        if (status == HttpStatusCode.NotFound) return null;
        Ensure(status);
        return SnapshotFrom(JsonNode.Parse(body)!);
    }

    /// <inheritdoc/>
    public async Task<CommitResult> CommitAsync(int slotId, SlotCommit commit)
    {
        var slot = new JsonObject();
        if (commit.Character != null) slot["character"] = commit.Character;
        if (commit.LevelAtLeast != null) slot["levelAtLeast"] = commit.LevelAtLeast;
        if (commit.ScoreAdd != null) slot["scoreAdd"] = commit.ScoreAdd;
        if (commit.CollectablesAdd != null) slot["collectablesAdd"] = commit.CollectablesAdd;
        var docs = new JsonArray();
        foreach (var doc in commit.Docs ?? Array.Empty<SlotDocWrite>())
            docs.Add(new JsonObject { ["key"] = doc.Key, ["schemaVersion"] = doc.SchemaVersion, ["value"] = JsonNode.Parse(doc.Json) });
        var body = new JsonObject { ["expectRevision"] = commit.ExpectRevision, ["slot"] = slot, ["docs"] = docs };
        if (commit.Token != null)
        {
            body["token"] = commit.Token;
            body["receipt"] = new JsonObject { ["kind"] = commit.ReceiptKind, ["value"] = JsonNode.Parse(commit.ReceiptJson ?? "null") };
        }
        try
        {
            var (status, text) = await SendAsync(HttpMethod.Post, $"/slots/{slotId}/commit/", body).ConfigureAwait(false);
            var json = string.IsNullOrWhiteSpace(text) ? null : JsonNode.Parse(text);
            if (status == HttpStatusCode.OK)
                return new CommitResult(json?["status"]?.GetValue<string>() == "alreadyCommitted" ? CommitOutcome.AlreadyCommitted : CommitOutcome.Committed, SnapshotFrom(json!));
            if (status == HttpStatusCode.NotFound) return new CommitResult(CommitOutcome.NotFound, null, "The selected campaign no longer exists.");
            if (status == HttpStatusCode.Unauthorized) return new CommitResult(CommitOutcome.NotSignedIn, null, "Sign in to save.");
            if (status == HttpStatusCode.Conflict)
            {
                string error = json?["error"]?.GetValue<string>() ?? "";
                return error switch
                {
                    "revision_conflict" => new CommitResult(CommitOutcome.RevisionConflict, SnapshotFrom(json!)),
                    "receipt_mismatch" => new CommitResult(CommitOutcome.ReceiptMismatch, null, "This token was already used for a different result."),
                    _ => new CommitResult(CommitOutcome.SchemaNewer, null, "This save was made by a newer game version."),
                };
            }
            return new CommitResult(CommitOutcome.Failed, null, $"The save service refused the request ({(int)status}).");
        }
        catch (SaveStoreException error) { return new CommitResult(error.NotSignedIn ? CommitOutcome.NotSignedIn : CommitOutcome.Failed, null, error.Message); }
        catch (JsonException) { return new CommitResult(CommitOutcome.Failed, null, "The save service sent an unreadable answer."); }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteSlotAsync(int slotId)
    {
        var (status, _) = await SendAsync(HttpMethod.Delete, $"/slots/{slotId}/").ConfigureAwait(false);
        if (status == HttpStatusCode.NotFound) return false;
        Ensure(status);
        return true;
    }

    /// <inheritdoc/>
    public async Task<int> DeleteAllSlotsAsync()
    {
        var (status, body) = await SendAsync(HttpMethod.Delete, "/slots/").ConfigureAwait(false);
        Ensure(status);
        return JsonNode.Parse(body)?["deleted"]?.GetValue<int>() ?? 0;
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, string>> GetAccountDocsAsync()
    {
        var (status, body) = await SendAsync(HttpMethod.Get, "/account-docs/").ConfigureAwait(false);
        Ensure(status);
        var result = new Dictionary<string, string>();
        if (JsonNode.Parse(body) is JsonObject map)
            foreach (var (key, value) in map) result[key] = value?.ToJsonString() ?? "{}";
        return result;
    }

    /// <inheritdoc/>
    public async Task PutAccountDocAsync(string key, string json)
    {
        var (status, _) = await SendAsync(HttpMethod.Put, $"/account-docs/{Uri.EscapeDataString(key)}/", JsonNode.Parse(json)).ConfigureAwait(false);
        Ensure(status);
    }
}

/// <summary>
/// The signed-in account's documents (settings, key bindings, prompt style, operative progression),
/// cached in memory after sign-in so synchronous callers can read them. Engine-free, so tests can
/// fill it directly. <see cref="SessionManager"/> loads and saves it through the store.
/// </summary>
public static class AccountDocs
{
    private static readonly Dictionary<string, string> Docs = new();
    /// <summary>True once the signed-in account's documents have been loaded.</summary>
    public static bool Loaded { get; private set; }

    /// <summary>The JSON of one document, or null when the account has none.</summary>
    public static string? Get(string key) { lock (Docs) return Docs.TryGetValue(key, out var json) ? json : null; }

    /// <summary>Stores one document in the cache (the caller sends it to the store).</summary>
    public static void Set(string key, string json) { lock (Docs) Docs[key] = json; }

    /// <summary>Replaces the whole cache with a freshly loaded account and marks it loaded.</summary>
    public static void ReplaceAll(IDictionary<string, string> docs)
    {
        lock (Docs) { Docs.Clear(); foreach (var (k, v) in docs) Docs[k] = v; Loaded = true; }
    }

    /// <summary>Forgets everything (sign-out).</summary>
    public static void Clear() { lock (Docs) { Docs.Clear(); Loaded = false; } }
}
