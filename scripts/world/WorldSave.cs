using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>Versioned exploration snapshot, stored as a document of the account's save slot.</summary>
public sealed record WorldSave(int Version, string World, WorldPoint Position, WorldPoint? Waypoint,
    WorldPoint LastExtraction, string Resume, int Mission, string Operative,
    Dictionary<string, string?>? CompletedQuests = null,
    Dictionary<string, int>? ActiveQuests = null,
    Dictionary<int, string>? DistrictChoices = null,
    HashSet<string>? UnlockedVehicles = null,
    string? ActiveVehicle = null,
    double WorldTimeMinutes = 0.0,
    Dictionary<string, int>? FactionReputation = null,
    HashSet<string>? UnlockedHqAmenities = null,
    string? CurrentCity = null,
    Dictionary<string, int>? OperationClears = null) : IEquatable<WorldSave>
{
    /// <summary>
    /// A first arrival in a city. Used only for cities other than Los Angeles, each of which keeps its own
    /// record, so quest progress and region-numbered choices can't collide between cities.
    /// </summary>
    public static WorldSave ArrivalIn(WorldDefinition city, int mission, string operative) =>
        new(1, city.Identity, city.Skyport, null, city.Skyport, "world", LevelCatalog.Clamp(mission), OperativeRoster.Normalize(operative),
            new Dictionary<string, string?>(), new Dictionary<string, int>(), new Dictionary<int, string>(), new HashSet<string> { "veh_starter_bike" }, null, 0.0,
            new Dictionary<string, int>(), new HashSet<string>());

    /// <summary>Creates a safe post-mission arrival; completion, including Mission 12, always resumes exploration.</summary>
    public static WorldSave AfterMission(int mission, string operative=OperativeRoster.Starter)
    {
        var entrance=WorldCatalog.Entrances[LevelCatalog.Clamp(mission)-1].Position;
        var safe=WorldCatalog.NearestStation(entrance).Position;
        return new(1,WorldCatalog.Identity,safe,null,safe,"world",LevelCatalog.Clamp(mission),OperativeRoster.Normalize(operative),
            new Dictionary<string, string?>(), new Dictionary<string, int>(), new Dictionary<int, string>(), new HashSet<string> { "veh_starter_bike" }, null, 0.0,
            new Dictionary<string, int>(), new HashSet<string>());
    }

    /// <summary>Rejects unsupported identities, unsafe coordinates and unknown modes instead of overwriting future saves. Accepts both neo-los-angeles-v1 and legacy neo-kyoto-v1 saves.</summary>
    public bool Valid()
    {
        var city = CityRegistry.ForIdentity(World);
        return Version==1 && city!=null && city.InBounds(Position)
            && city.InBounds(LastExtraction) && (Waypoint==null || city.InBounds(Waypoint.Value))
            && Resume is "world" or "mission" && Mission>=1 && Mission<=LevelCatalog.Count
            && OperativeRoster.RosterOrder.Contains(Operative)
            && (CurrentCity==null || CityRegistry.ById(CurrentCity)!=null);
    }

    /// <summary>Returns a copy of the state with a quest marked as completed, updating any chosen branch.</summary>
    public WorldSave WithQuestCompleted(string questId, string? branchChoice)
    {
        var completed = CompletedQuests != null ? new Dictionary<string, string?>(CompletedQuests) : new();
        var active = ActiveQuests != null ? new Dictionary<string, int>(ActiveQuests) : new();
        completed[questId] = branchChoice;
        active.Remove(questId);

        var choices = DistrictChoices != null ? new Dictionary<int, string>(DistrictChoices) : new();
        if (!string.IsNullOrEmpty(branchChoice))
        {
            var quest = CityRegistry.FindQuest(questId);
            if (quest != null) choices[quest.RegionId] = branchChoice;
        }

        return this with { CompletedQuests = completed, ActiveQuests = active, DistrictChoices = choices };
    }

    /// <summary>Value-based equality for records with nested collections.</summary>
    public bool Equals(WorldSave? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        if (Version != other.Version || World != other.World || Position != other.Position ||
            Waypoint != other.Waypoint || LastExtraction != other.LastExtraction ||
            Resume != other.Resume || Mission != other.Mission || Operative != other.Operative ||
            Math.Abs(WorldTimeMinutes - other.WorldTimeMinutes) > 0.0001 || ActiveVehicle != other.ActiveVehicle ||
            CurrentCity != other.CurrentCity)
            return false;
        if (!DictEqual(OperationClears, other.OperationClears)) return false;

        if (!DictEqual(CompletedQuests, other.CompletedQuests)) return false;
        if (!DictEqual(ActiveQuests, other.ActiveQuests)) return false;
        if (!DictEqual(DistrictChoices, other.DistrictChoices)) return false;
        if (!SetEqual(UnlockedVehicles, other.UnlockedVehicles)) return false;
        if (!DictEqual(FactionReputation, other.FactionReputation)) return false;
        if (!SetEqual(UnlockedHqAmenities, other.UnlockedHqAmenities)) return false;

        return true;
    }

    /// <summary>Hashes the scalar identity fields only; collections are compared in Equals, so equal saves still hash alike.</summary>
    public override int GetHashCode() => HashCode.Combine(Version, World, Position, Waypoint, LastExtraction, Resume, Mission, Operative);

    /// <summary>Compares dictionaries by content, treating null and empty as the same.</summary>
    private static bool DictEqual<K, V>(Dictionary<K, V>? a, Dictionary<K, V>? b) where K : notnull
    {
        if (ReferenceEquals(a, b)) return true;
        if (a == null || b == null) return (a == null || a.Count == 0) && (b == null || b.Count == 0);
        if (a.Count != b.Count) return false;
        foreach (var kvp in a)
        {
            if (!b.TryGetValue(kvp.Key, out var bVal) || !EqualityComparer<V>.Default.Equals(kvp.Value, bVal)) return false;
        }
        return true;
    }

    /// <summary>Compares sets by content, treating null and empty as the same.</summary>
    private static bool SetEqual<T>(HashSet<T>? a, HashSet<T>? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a == null || b == null) return (a == null || a.Count == 0) && (b == null || b.Count == 0);
        return a.SetEquals(b);
    }
}

/// <summary>
/// World persistence on the account's save slot (2026-11-18; previously direct PostgreSQL).
/// Each city's record is one slot document: Los Angeles is <c>world</c>, any other city
/// <c>world:&lt;cityId&gt;</c>. Writes go through one all-or-nothing commit per action
/// (<see cref="SlotTransaction"/>), so travel stores the departure, the arrival and Los Angeles'
/// current-city marker together, and a conflicting save from elsewhere is recomputed, not lost.
/// The pure helpers (<see cref="TravelArrival"/>, <see cref="SettleReturn"/>, <see cref="Read"/>)
/// hold the rules and are shared with the settlement and quest code.
/// </summary>
public static class WorldStorage
{
    /// <summary>Slot document key of a city's record: <c>world</c> for Los Angeles, <c>world:&lt;id&gt;</c> otherwise.</summary>
    public static string DocKey(WorldDefinition? city) => city==null || city==WorldCatalog.Definition ? "world" : $"world:{city.Id}";

    /// <summary>
    /// The old database key of a city's record (before saves moved to accounts). Kept as a pure
    /// helper for tests and tools that still reason about per-campaign keys.
    /// </summary>
    public static string StateKey(int session,WorldDefinition? city) => city==null || city==WorldCatalog.Definition ? $"world:{session}" : $"world:{session}:{city.Id}";

    /// <summary>
    /// A city's record from a slot snapshot. Null only when the slot has none; a damaged record or
    /// one saved by a newer game version throws, so callers can't overwrite it with defaults.
    /// </summary>
    public static WorldSave? Read(SlotSnapshot snapshot,WorldDefinition? city=null)
    {
        if(!snapshot.Docs.TryGetValue(DocKey(city),out var doc)) return null;
        if(doc.SchemaVersion!=1) throw new InvalidOperationException("Unsupported world version.");
        var state=JsonSerializer.Deserialize<WorldSave>(doc.Json);
        if(state?.Valid()!=true) throw new InvalidOperationException("Invalid world state.");
        return state;
    }

    /// <summary>The document write for a record. The record's own identity picks the key, so a city can never overwrite Los Angeles.</summary>
    public static SlotDocWrite Write(WorldSave state)
    {
        if(!state.Valid()) throw new InvalidOperationException("Invalid world state.");
        return new SlotDocWrite(DocKey(CityRegistry.ForIdentity(state.World)),JsonSerializer.Serialize(state));
    }

    /// <summary>
    /// The safe post-mission arrival in Los Angeles, merged into the existing record so quests,
    /// choices, vehicles, the waypoint and the clock survive a mission. An absent or invalid record
    /// falls back to fresh defaults. Campaign missions always finish in Los Angeles.
    /// </summary>
    public static WorldSave SettleReturn(SlotSnapshot snapshot,int mission,string operative)
    {
        var state=WorldSave.AfterMission(mission,operative);
        WorldSave? existing=null;
        try { existing=Read(snapshot); } catch(InvalidOperationException) { existing=null; }
        return existing!=null
            ? existing with { World=state.World,Position=state.Position,LastExtraction=state.LastExtraction,Resume="world",Mission=state.Mission,Operative=state.Operative,ActiveVehicle=null,CurrentCity=null }
            : state;
    }

    /// <summary>Reads one campaign's Los Angeles record, filling unvisited campaigns at their latest safe mission terminal.</summary>
    public static async Task<WorldSave?> LoadAsync(int session,int progress,ISaveStore? store=null,string? preferredOperative=null)
    {
        if(session<=0) return null;
        var snapshot=await (store??SaveStore.Current).GetSlotAsync(session).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Campaign no longer exists.");
        return Read(snapshot) ?? (progress>1?WorldSave.AfterMission(progress-1,preferredOperative??OperativeRoster.Starter):null);
    }

    /// <summary>
    /// Loads the record of the city the player is currently in. Los Angeles stores which city that
    /// is. A missing city record falls back to Los Angeles rather than stranding the player.
    /// </summary>
    public static async Task<WorldSave?> LoadActiveAsync(int session,int progress,ISaveStore? store=null,string? preferredOperative=null)
    {
        if(session<=0) return null;
        var snapshot=await (store??SaveStore.Current).GetSlotAsync(session).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Campaign no longer exists.");
        var home=Read(snapshot) ?? (progress>1?WorldSave.AfterMission(progress-1,preferredOperative??OperativeRoster.Starter):null);
        var city=CityRegistry.ById(home?.CurrentCity);
        if(home==null || city==null || city==WorldCatalog.Definition) return home;
        return Read(snapshot,city) ?? home with { CurrentCity=null };
    }

    /// <summary>Saves one record and the slot's current operative in one commit; a conflict is retried on the newer slot.</summary>
    public static async Task SaveAsync(int session,WorldSave state,ISaveStore? store=null)
    {
        if(session<=0 || !state.Valid()) throw new InvalidOperationException("An active campaign and valid world state are required.");
        var write=Write(state);
        var result=await SlotTransaction.RunAsync(store??SaveStore.Current,session,
            snapshot=>new SlotCommit(snapshot.Slot.Revision,Character:state.Operative,Docs:new[]{ write })).ConfigureAwait(false);
        ThrowUnlessSaved(result);
    }

    /// <summary>
    /// Moves the player between cities as one commit: the departure is stored, the arrival record is
    /// created or updated, and Los Angeles records the new location. A failed commit changes nothing.
    /// Returns the arrival record the scene should now use.
    /// </summary>
    public static async Task<WorldSave> TravelAsync(int session,WorldSave departing,WorldDefinition destination,ISaveStore? store=null)
    {
        if(session<=0 || !departing.Valid()) throw new InvalidOperationException("An active campaign and valid world state are required.");
        WorldSave? arrival=null;
        var result=await SlotTransaction.RunAsync(store??SaveStore.Current,session,snapshot=>
        {
            bool leavingHome=CityRegistry.IsLosAngeles(departing.World);
            var home=leavingHome?departing:Read(snapshot)??throw new InvalidOperationException("Campaign has no Los Angeles record.");
            arrival=TravelArrival(home,departing,destination,Read(snapshot,destination));
            var docs=new List<SlotDocWrite>();
            if(!leavingHome) docs.Add(Write(departing));
            if(destination==WorldCatalog.Definition) docs.Add(Write(arrival));
            else { docs.Add(Write(home with { CurrentCity=destination.Id })); docs.Add(Write(arrival)); }
            return new SlotCommit(snapshot.Slot.Revision,Character:departing.Operative,Docs:docs);
        }).ConfigureAwait(false);
        ThrowUnlessSaved(result);
        return arrival!;
    }

    /// <summary>
    /// Builds the record for arriving in a city, keeping that city's own quests. Pure, so tests and
    /// the unsaved preview use exactly the same rule as a real campaign.
    /// </summary>
    public static WorldSave TravelArrival(WorldSave home,WorldSave departing,WorldDefinition destination,WorldSave? existing)
    {
        var baseState=destination==WorldCatalog.Definition?home with { CurrentCity=null }:existing??WorldSave.ArrivalIn(destination,departing.Mission,departing.Operative);
        // LastExtraction is left alone: a safehouse rested at in this city stays the defeat return point.
        return baseState with { Position=destination.Skyport,Resume="world",Operative=departing.Operative,WorldTimeMinutes=departing.WorldTimeMinutes,ActiveVehicle=null,Waypoint=null };
    }

    /// <summary>Turns a failed commit into an exception with a player-safe message (callers show Retry).</summary>
    private static void ThrowUnlessSaved(CommitResult result)
    {
        if(result.Outcome is CommitOutcome.Committed or CommitOutcome.AlreadyCommitted) return;
        throw result.Outcome switch
        {
            CommitOutcome.NotFound => new InvalidOperationException("Campaign no longer exists."),
            CommitOutcome.NotSignedIn => new SaveStoreException("Sign in to save.",notSignedIn:true),
            CommitOutcome.SchemaNewer => new InvalidOperationException("Unsupported world save version."),
            _ => new SaveStoreException(string.IsNullOrEmpty(result.Error)?"The campaign could not be saved. Retry.":result.Error),
        };
    }
}

/// <summary>Single launch catalog preserves mission-owned arrivals and the existing Act II/III introduction chain.</summary>
public static class WorldMissionRouting
{
    /// <summary>Returns the story entry scene. Act III end/start screens themselves continue into Stratos Docks intro.</summary>
    public static string LaunchPath(int mission) => mission switch
    {
        5=>"res://scenes/cutscenes/act_ii_opening.tscn",
        6=>"res://scenes/cutscenes/cyber_grave_intro.tscn",
        7=>"res://scenes/cutscenes/foundry_forge_intro.tscn",
        8=>"res://scenes/ui/act_ii_end_screen.tscn",
        9=>"res://scenes/cutscenes/spire_outer_hull_intro.tscn",
        10=>"res://scenes/cutscenes/cyber_nexus_intro.tscn",
        11=>"res://scenes/cutscenes/apex_bastion_intro.tscn",
        12=>"res://scenes/cutscenes/judgment_day_intro.tscn",
        _=>LevelCatalog.ScenePath(mission)
    };
}
