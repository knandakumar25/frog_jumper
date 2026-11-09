using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Map edge that a stretch of sea runs along.</summary>
public enum SeaSide { North, South, West, East }

/// <summary>Open sea along one map edge, reaching Depth metres inland.</summary>
public sealed record SeaEdge(SeaSide Side, double Depth);

/// <summary>An ellipse of open water that only applies inside one region. Radii are in metres.</summary>
public sealed record LakeFootprint(int RegionId, double CenterX, double CenterY, double RadiusX, double RadiusY);

/// <summary>
/// The authored people, places and activities of one city. Quest, resident, interior and
/// operation IDs must be unique across all cities, because saves and receipts store only the ID.
/// </summary>
public sealed record CityContent(
    IReadOnlyList<QuestDefinition> Quests,
    IReadOnlyList<NamedResident> Residents,
    IReadOnlyList<InteriorDefinition> Interiors,
    IReadOnlySet<string> ListedSafehouseIds,
    IReadOnlyList<SideOperationDefinition> Operations,
    IReadOnlyDictionary<string, WorldDialogueCatalog.Conversation>? Conversations = null);

/// <summary>
/// Everything the open-world systems need to know about one city. Instances are immutable and
/// safe to read from the chunk worker threads; the running scene passes its current definition
/// explicitly instead of keeping one in a changing static.
/// </summary>
public sealed class WorldDefinition
{
    /// <summary>Spacing and offset of the arterial road grid. Every city shares them, so stations always sit on crossings.</summary>
    public const int ArterialSpacing = 8000, ArterialOffset = 4000;

    private readonly Lazy<CityContent> _content;
    private readonly Lazy<IReadOnlyList<WorldPoint>> _clearAreas;

    /// <summary>Creates a definition. Content is supplied late so catalogs can't deadlock during static start-up.</summary>
    public WorldDefinition(Func<CityContent> content, Func<CityContent, IReadOnlyList<WorldPoint>>? clearAreas = null)
    {
        _content = new Lazy<CityContent>(content);
        _clearAreas = new Lazy<IReadOnlyList<WorldPoint>>(() => clearAreas?.Invoke(Content) ?? Array.Empty<WorldPoint>());
    }

    /// <summary>Stable short key stored in the save, such as "kyoto".</summary>
    public required string Id { get; init; }
    /// <summary>Versioned world identity written into this city's save record.</summary>
    public required string Identity { get; init; }
    /// <summary>English city name, translated at display time.</summary>
    public required string DisplayName { get; init; }
    /// <summary>English map heading, translated at display time.</summary>
    public required string MapTitle { get; init; }
    /// <summary>Width and height of the square surface, in metres.</summary>
    public required int Size { get; init; }
    /// <summary>Districts per side of the square district grid.</summary>
    public required int RegionColumns { get; init; }
    /// <summary>Width of one district in metres.</summary>
    public required int RegionSize { get; init; }
    /// <summary>Districts in north-up, row-major order; each Id equals its list index.</summary>
    public required IReadOnlyList<WorldRegion> Regions { get; init; }
    /// <summary>Free transit endpoints on the arterial grid.</summary>
    public required IReadOnlyList<WorldStation> Stations { get; init; }
    /// <summary>Campaign mission terminals. Only Los Angeles has them.</summary>
    public IReadOnlyList<WorldEntrance> Entrances { get; init; } = Array.Empty<WorldEntrance>();
    /// <summary>Named map destinations.</summary>
    public required IReadOnlyList<WorldLandmark> Landmarks { get; init; }
    /// <summary>Elevator positions; elevator N serves underground layer N+1.</summary>
    public IReadOnlyList<WorldPoint> Elevators { get; init; } = Array.Empty<WorldPoint>();
    /// <summary>English names of the map layers, surface first. There is one per elevator plus the surface.</summary>
    public IReadOnlyList<string> LayerNames { get; init; } = new[] { "Surface" };
    /// <summary>River centreline from north to south. Empty means no river.</summary>
    public IReadOnlyList<WorldPoint> River { get; init; } = Array.Empty<WorldPoint>();
    /// <summary>Half the river's width in metres.</summary>
    public double RiverHalfWidth { get; init; } = 180;
    /// <summary>Everything east of this X is sea. Null means no coast.</summary>
    public double? CoastStartX { get; init; }
    /// <summary>Open sea along map edges, for cities whose coast isn't on the east.</summary>
    public IReadOnlyList<SeaEdge> SeaEdges { get; init; } = Array.Empty<SeaEdge>();
    /// <summary>Lakes and reservoirs.</summary>
    public IReadOnlyList<LakeFootprint> Lakes { get; init; } = Array.Empty<LakeFootprint>();
    /// <summary>Y coordinate of the east-west rail corridor. Null means no railway.</summary>
    public double? RailY { get; init; }
    /// <summary>Districts built from dense architecture rather than natural scatter.</summary>
    public required IReadOnlySet<int> UrbanRegionIds { get; init; }
    /// <summary>Chooses the art module for a generated prop from its district, urban flag and stable hash.</summary>
    public required Func<int, bool, uint, int> PropKind { get; init; }
    /// <summary>Folder holding this city's module_N.png and landmark_N.png, ending in a slash.</summary>
    public required string ArtFolder { get; init; }
    /// <summary>Where travellers arrive and the Skyport terminal stands. It must be on an arterial road.</summary>
    public required WorldPoint Skyport { get; init; }
    /// <summary>Operative whose recruitment opens this city. Null means it is always open.</summary>
    public string? UnlockOperative { get; init; }

    /// <summary>People, places and activities of this city, built on first use.</summary>
    public CityContent Content => _content.Value;
    /// <summary>Points where no solid prop may be generated, so authored places stay reachable.</summary>
    public IReadOnlyList<WorldPoint> ClearAreas => _clearAreas.Value;

    /// <summary>Returns the containing district, clamped so the outer edge still has one.</summary>
    public WorldRegion Region(WorldPoint p) => Regions[Math.Clamp((int)(p.Y / RegionSize), 0, RegionColumns - 1) * RegionColumns + Math.Clamp((int)(p.X / RegionSize), 0, RegionColumns - 1)];

    /// <summary>Nearest surface transit endpoint, also used for safe extraction after underground missions.</summary>
    public WorldStation NearestStation(WorldPoint p) => Stations.MinBy(s => s.Position.Distance(p with { Layer = 0 }))!;

    /// <summary>Chooses the closest terminal within reach, so adjacent terminals can't select the wrong mission.</summary>
    public WorldEntrance? NearbyEntrance(WorldPoint p, double reach = 8) => Entrances.Where(e => e.Position.Distance(p) < reach).MinBy(e => e.Position.Distance(p));

    /// <summary>Shared main road lattice. Every station lies on it.</summary>
    public bool Arterial(WorldPoint p, double halfWidth = 8) => GridDistance(p.X, ArterialSpacing, ArterialOffset) < halfWidth || GridDistance(p.Y, ArterialSpacing, ArterialOffset) < halfWidth;

    /// <summary>Local streets join the arterial network.</summary>
    public bool Route(WorldPoint p) => Arterial(p) || GridDistance(p.X, 128, 32) < 5 || GridDistance(p.Y, 128, 32) < 5;

    /// <summary>Finite surface bounds, plus one 2 km square per underground network.</summary>
    public bool InBounds(WorldPoint p) => double.IsFinite(p.X) && double.IsFinite(p.Y) && (p.Layer == 0
        ? p.X >= 0 && p.Y >= 0 && p.X < Size && p.Y < Size
        : p.Layer >= 1 && p.Layer <= Elevators.Count && Math.Abs(p.X - Elevators[p.Layer - 1].X) < 1024 && Math.Abs(p.Y - Elevators[p.Layer - 1].Y) < 1024);

    /// <summary>Water is physical except where a route crosses it as a bridge.</summary>
    public bool Water(WorldPoint p)
    {
        if (p.Layer != 0 || Route(p)) return false;
        if (River.Count > 0)
        {
            double riverX = River[^1].X;
            for (int i = 1; i < River.Count; i++)
            {
                if (p.Y > River[i].Y) continue;
                double t = (p.Y - River[i - 1].Y) / (River[i].Y - River[i - 1].Y);
                riverX = River[i - 1].X + (River[i].X - River[i - 1].X) * t;
                break;
            }
            if (Math.Abs(p.X - riverX) < RiverHalfWidth) return true;
        }
        if (CoastStartX is double coast && p.X > coast) return true;
        foreach (var edge in SeaEdges)
        {
            bool inSea = edge.Side switch
            {
                SeaSide.North => p.Y < edge.Depth,
                SeaSide.South => p.Y > Size - edge.Depth,
                SeaSide.West => p.X < edge.Depth,
                _ => p.X > Size - edge.Depth
            };
            if (inSea) return true;
        }
        int region = Region(p).Id;
        return Lakes.Any(l => l.RegionId == region && Math.Pow((p.X - l.CenterX) / l.RadiusX, 2) + Math.Pow((p.Y - l.CenterY) / l.RadiusY, 2) < 1);
    }

    /// <summary>Dense architecture families, distinct from natural terrain scatter.</summary>
    public bool Urban(WorldPoint p) => p.Layer > 0 || UrbanRegionIds.Contains(Region(p).Id);

    /// <summary>Distance to a periodic line, continuous across chunk seams.</summary>
    private static double GridDistance(double value, double spacing, double offset)
    {
        double r = ((value - offset) % spacing + spacing) % spacing;
        return Math.Min(r, spacing - r);
    }
}

/// <summary>Finds cities and their authored content. IDs are global, so a save can name them directly.</summary>
public static class CityRegistry
{
    /// <summary>Every open world, Los Angeles first.</summary>
    public static readonly IReadOnlyList<WorldDefinition> All = new[] { WorldCatalog.Definition, KyotoCity.Definition, MumbaiCity.Definition, HonoluluCity.Definition, DubaiCity.Definition, LuandaCity.Definition, BeirutCity.Definition };

    /// <summary>Whether a city can be flown to. Opens once its operative is on the squad; never gates the campaign.</summary>
    public static bool IsOpen(WorldDefinition city, Func<string, bool> isRecruited) => city.UnlockOperative == null || isRecruited(city.UnlockOperative);

    /// <summary>Finds a city's written conversation for an objective, or null if none exists.</summary>
    public static WorldDialogueCatalog.Conversation? FindConversation(string objectiveId)
    {
        foreach (var city in All)
            if (city.Content.Conversations?.TryGetValue(objectiveId, out var conversation) == true) return conversation;
        return null;
    }

    /// <summary>Returns a city by short key, or null if unknown.</summary>
    public static WorldDefinition? ById(string? id) => All.FirstOrDefault(c => c.Id == id);

    /// <summary>Returns the city a save identity belongs to. Legacy "neo-kyoto-v1" records are Los Angeles.</summary>
    public static WorldDefinition? ForIdentity(string? identity) =>
        identity == WorldCatalog.LegacyIdentity ? WorldCatalog.Definition : All.FirstOrDefault(c => c.Identity == identity);

    /// <summary>Whether a save identity is the Los Angeles world, including its legacy name.</summary>
    public static bool IsLosAngeles(string? identity) => identity == WorldCatalog.Identity || identity == WorldCatalog.LegacyIdentity;

    /// <summary>Finds a quest in any city, including Los Angeles quests that aren't playable yet.</summary>
    public static QuestDefinition? FindQuest(string id) =>
        WorldContentCatalog.Quests.FirstOrDefault(q => q.Id == id) ?? All.SelectMany(c => c.Content.Quests).FirstOrDefault(q => q.Id == id);

    /// <summary>Returns the city that owns a quest. Quests outside the city lists belong to Los Angeles.</summary>
    public static WorldDefinition CityOfQuest(string questId) =>
        All.FirstOrDefault(c => c != WorldCatalog.Definition && c.Content.Quests.Any(q => q.Id == questId)) ?? WorldCatalog.Definition;

    /// <summary>Finds a resident in any city.</summary>
    public static NamedResident? FindResident(string id) =>
        WorldContentCatalog.Residents.FirstOrDefault(r => r.Id == id) ?? All.SelectMany(c => c.Content.Residents).FirstOrDefault(r => r.Id == id);

    /// <summary>Finds a playable city operation and the city that owns it.</summary>
    public static (WorldDefinition City, SideOperationDefinition Operation)? FindOperation(string id)
    {
        foreach (var city in All)
        {
            var operation = city.Content.Operations.FirstOrDefault(o => o.Id == id);
            if (operation != null) return (city, operation);
        }
        return null;
    }
}
