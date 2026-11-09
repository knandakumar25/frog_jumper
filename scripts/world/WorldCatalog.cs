using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Stable metre coordinates. Only chunk-relative values are converted to Godot floats.</summary>
public readonly record struct WorldPoint(double X, double Y, int Layer = 0)
{
    /// <summary>Containing 128-metre chunk, using floor so negative expansion also works.</summary>
    public ChunkAddress Chunk => new((int)Math.Floor(X / 128), (int)Math.Floor(Y / 128), Layer);
    /// <summary>Euclidean distance on one layer; different layers are never adjacent interactables.</summary>
    public double Distance(WorldPoint other) => Layer == other.Layer ? Math.Sqrt(Math.Pow(X-other.X, 2)+Math.Pow(Y-other.Y, 2)) : double.PositiveInfinity;
}

/// <summary>Integer address independent of the rendering origin and loading order.</summary>
public readonly record struct ChunkAddress(int X, int Y, int Layer = 0);
/// <summary>Authored district identity and terrain family in north-up row-major order.</summary>
public sealed record WorldRegion(int Id, string Name, string Biome);
/// <summary>Named, permanently visible destination; kind is plaza, architecture or scenic.</summary>
public sealed record WorldLandmark(int Id, string Name, string Kind, WorldPoint Position);
/// <summary>Free surface transit endpoint. Two stations also serve underground elevator lobbies.</summary>
public sealed record WorldStation(int Id, WorldPoint Position);
/// <summary>Story-gated mission terminal, adjacent to its extraction and transit endpoint.</summary>
public sealed record WorldEntrance(int Mission, WorldPoint Position);

/// <summary>
/// The Los Angeles world: version-one authored geography, exposed as <see cref="Definition"/>.
/// IDs and seed math remain stable. The static members forward to that definition, so code and
/// tests that only ever concern Los Angeles keep working unchanged.
/// </summary>
public static class WorldCatalog
{
    public const int Size = 128000, RegionSize = 32000, ChunkSize = 128;
    public const string Identity = "neo-los-angeles-v1";
    /// <summary>Earlier name of this same Los Angeles world, still present in existing saves. It does not mean the Kyoto city.</summary>
    public const string LegacyIdentity = "neo-kyoto-v1";
    public const string Scene = "res://scenes/world/neo_kyoto.tscn";
    public static readonly IReadOnlyList<WorldRegion> Regions = new[]
    {
        new WorldRegion(0,"Angeles Crest Highlands","highland"), new(1,"Castaic Aqueduct Reservoir","reservoir"), new(2,"San Gabriel Peaks","mountain"), new(3,"Malibu Coast","coast"),
        new(4,"Topanga Canyon Preserve","forest"), new(5,"Historic Core & Little Tokyo","palace"), new(6,"Downtown LA Core","tower"), new(7,"San Pedro Harbor","harbor"),
        new(8,"Mojave Solar Belt","solar"), new(9,"Concrete Wards & LA River","concrete"), new(10,"Century Citadel Ring","citadel"), new(11,"Santa Monica Coast","coast"),
        new(12,"Kurogane Ridge Escarpment","mountain"), new(13,"Alameda Rustrail Belt","rail"), new(14,"Vernon Foundry Basin","foundry"), new(15,"Ballona Reclamation Wetlands","wetland")
    };
    public static readonly IReadOnlyList<WorldStation> Stations = Enumerable.Range(0,256)
        .Select(i => new WorldStation(i, new(4000+(i%16)*8000,4000+(i/16)*8000))).ToArray();
    public static readonly IReadOnlyList<WorldEntrance> Entrances = BuildEntrances();
    public static readonly IReadOnlyList<WorldLandmark> Landmarks = BuildLandmarks();
    /// <summary>Downtown and Foundry elevators share surface and underground logical X/Y positions.</summary>
    public static readonly WorldPoint[] Elevators = { Stations[88].Position, Stations[234].Position };
    /// <summary>Authored river spine, reused by terrain sampling and both map views.</summary>
    public static readonly WorldPoint[] River = { new(48000,0),new(52000,24000),new(60000,40000),new(62000,64000),new(72000,80000),new(90000,104000),new(116000,128000) };

    /// <summary>
    /// This world as data. The prop mapping and water values reproduce the original hand-written
    /// rules exactly; WorldFingerprintTests fails if any of them drift. The Skyport stands on the
    /// north-south arterial just south of the Little Tokyo station, where props never generate.
    /// </summary>
    public static readonly WorldDefinition Definition = new(BuildContent)
    {
        Id = "los-angeles",
        Identity = Identity,
        DisplayName = "Neo-Los Angeles",
        MapTitle = "Neo-Los Angeles 2157 · 128 × 128 km",
        Size = Size,
        RegionColumns = 4,
        RegionSize = RegionSize,
        Regions = Regions,
        Stations = Stations,
        Entrances = Entrances,
        Landmarks = Landmarks,
        Elevators = Elevators,
        LayerNames = new[] { "Surface", "Downtown underground", "Foundry underworks" },
        River = River,
        CoastStartX = 124000,
        Lakes = new[] { new LakeFootprint(1, 48000, 16000, 7000, 9000) },
        RailY = 108000,
        UrbanRegionIds = new HashSet<int> { 5, 6, 7, 9, 10, 13, 14 },
        PropKind = (region, urban, _) => urban ? (region == 5 ? 1 : region is 13 or 14 ? 3 : region == 10 ? 4 : 0) : region is 0 or 2 or 12 ? 6 : region == 8 ? 7 : 5,
        ArtFolder = "res://assets/world/",
        Skyport = new WorldPoint(44000, 44060)
    };

    /// <summary>Returns one of sixteen original districts without renumbering at boundaries.</summary>
    public static WorldRegion Region(WorldPoint p) => Definition.Region(p);
    /// <summary>Nearest surface endpoint, also used for safe extraction after underground missions.</summary>
    public static WorldStation NearestStation(WorldPoint p) => Definition.NearestStation(p);
    /// <summary>Chooses the closest terminal within reach. Adjacent Spire terminals must not select an earlier mission's overlapping radius.</summary>
    public static WorldEntrance? NearbyEntrance(WorldPoint p,double reach=8) => Definition.NearbyEntrance(p,reach);
    /// <summary>Shared main road/rail/trail lattice. Every endpoint lies on its connected arterial network.</summary>
    public static bool Arterial(WorldPoint p, double halfWidth = 8) => Definition.Arterial(p,halfWidth);
    /// <summary>Local streets join the arterial network; rural versions are narrower mountain/forest trails.</summary>
    public static bool Route(WorldPoint p) => Definition.Route(p);
    /// <summary>Surface/underground finite bounds. Underground networks are two distinct 2 km squares.</summary>
    public static bool InBounds(WorldPoint p) => Definition.InBounds(p);
    /// <summary>Terrain water is physical except where the same route definition supplies a bridge.</summary>
    public static bool Water(WorldPoint p) => Definition.Water(p);
    /// <summary>Dense architecture families, distinct from natural terrain scatter.</summary>
    public static bool Urban(WorldPoint p) => Definition.Urban(p);

    /// <summary>Gathers the Los Angeles content: only the slice quests and listed safehouses are in play.</summary>
    private static CityContent BuildContent() => new(
        WorldQuestRules.PlayableQuests,
        WorldContentCatalog.Residents,
        WorldContentCatalog.Interiors,
        new HashSet<string> { "safehouse_old_kyoto", "safehouse_concrete_wards" },
        Array.Empty<SideOperationDefinition>());

    /// <summary>Original grid addresses remain fixed; entrances occupy cleared road shoulders within 24m of transit.</summary>
    private static WorldEntrance[] BuildEntrances()
    {
        int[] ids={85,149,213,154,88,88,234,155,155,155,155,155};
        return ids.Select((id,i)=>new WorldEntrance(i+1, Stations[id].Position with { X=Stations[id].Position.X+20+(i>=7?(i-7)*6:0), Layer=i==5?1:i==6?2:0 })).ToArray();
    }
    /// <summary>Forty-eight authored destinations, each placed on an existing station plaza or local route.</summary>
    private static WorldLandmark[] BuildLandmarks()
    {
        string[] names={"Cedar Gate","Skywatch Monastery","Eagle Overlook","Cloudstep Landing","Floodgate Cathedral","Mirrorwater Promenade","Stormbreak Camp","Thunder Observatory","Lightning Saddle","Northlight Quay","Beacon of Dawn","Aurora Strand","Cedarwild Lodge","Rootglass Conservatory","Firefly Glade","Lantern Plaza","Kurogane Palace Walls","Moonbridge Garden","Meridian Square","Registry Needle","Skyline Terrace","Eastlight Exchange","Tidal Customs House","Crane Sunset Pier","Sunwheel Plaza","Helios Array","Golden Orchard","Concrete Junction","The Suspended Forum","Rainline Viaduct","Citadel Forecourt","Spire of Axiom","Silent Ramparts","Tideglass Landing","Sea Prism Lighthouse","Blueglass Strand","Kurogane Trailhead","Blackstone Shrine","Windcut Ridge","Rustrail Concourse","Iron Memory Depot","Ember Railcut","Foundry Exchange","Vulcan Furnace","Cinderwater Basin","Reclamation Landing","Reedglass Observatory","Heron Sanctuary"};
        return Enumerable.Range(0,48).Select(i=> { int r=i/3,c=r%4,row=r/4; int station=(row*4+1)*16+c*4+1; var p=Stations[station].Position; return new WorldLandmark(i,names[i],new[]{"plaza","architecture","scenic"}[i%3],p with { X=p.X+(i%3)*128 }); }).ToArray();
    }
}
