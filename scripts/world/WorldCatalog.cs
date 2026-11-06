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

/// <summary>Version-one authored geography. IDs and seed math remain stable when outer territory is added.</summary>
public static class WorldCatalog
{
    public const int Size = 128000, RegionSize = 32000, ChunkSize = 128;
    public const string Identity = "neo-kyoto-v1";
    public const string Scene = "res://scenes/world/neo_kyoto.tscn";
    public static readonly IReadOnlyList<WorldRegion> Regions = new[]
    {
        new WorldRegion(0,"Cedarcrest Highlands","highland"), new(1,"Cloudstep Reservoir","reservoir"), new(2,"Stormbreak Peaks","mountain"), new(3,"Northlight Coast","coast"),
        new(4,"Cedarwild Preserve","forest"), new(5,"Old Kyoto","palace"), new(6,"Downtown Core","tower"), new(7,"Eastlight Harbor","harbor"),
        new(8,"Solar Belt","solar"), new(9,"Concrete Wards","concrete"), new(10,"Citadel Ring","citadel"), new(11,"Tideglass Coast","coast"),
        new(12,"Kurogane Escarpment","mountain"), new(13,"Rustrail Belt","rail"), new(14,"Foundry Basin","foundry"), new(15,"Reclamation Wetlands","wetland")
    };
    public static readonly IReadOnlyList<WorldStation> Stations = Enumerable.Range(0,256)
        .Select(i => new WorldStation(i, new(4000+(i%16)*8000,4000+(i/16)*8000))).ToArray();
    public static readonly IReadOnlyList<WorldEntrance> Entrances = BuildEntrances();
    public static readonly IReadOnlyList<WorldLandmark> Landmarks = BuildLandmarks();
    /// <summary>Downtown and Foundry elevators share surface and underground logical X/Y positions.</summary>
    public static readonly WorldPoint[] Elevators = { Stations[88].Position, Stations[234].Position };
    /// <summary>Authored river spine, reused by terrain sampling and both map views.</summary>
    public static readonly WorldPoint[] River = { new(48000,0),new(52000,24000),new(60000,40000),new(62000,64000),new(72000,80000),new(90000,104000),new(116000,128000) };

    /// <summary>Returns one of sixteen original districts without renumbering at boundaries.</summary>
    public static WorldRegion Region(WorldPoint p) => Regions[Math.Clamp((int)(p.Y/RegionSize),0,3)*4+Math.Clamp((int)(p.X/RegionSize),0,3)];
    /// <summary>Nearest surface endpoint, also used for safe extraction after underground missions.</summary>
    public static WorldStation NearestStation(WorldPoint p) => Stations.MinBy(s => s.Position.Distance(p with { Layer=0 }))!;
    /// <summary>Chooses the closest terminal within reach. Adjacent Spire terminals must not select an earlier mission's overlapping radius.</summary>
    public static WorldEntrance? NearbyEntrance(WorldPoint p,double reach=8) => Entrances.Where(e=>e.Position.Distance(p)<reach).MinBy(e=>e.Position.Distance(p));
    /// <summary>Shared main road/rail/trail lattice. Every endpoint lies on its connected arterial network.</summary>
    public static bool Arterial(WorldPoint p, double halfWidth = 8) => GridDistance(p.X,8000,4000)<halfWidth || GridDistance(p.Y,8000,4000)<halfWidth;
    /// <summary>Local streets join the arterial network; rural versions are narrower mountain/forest trails.</summary>
    public static bool Route(WorldPoint p) => Arterial(p) || GridDistance(p.X,128,32)<5 || GridDistance(p.Y,128,32)<5;
    /// <summary>Surface/underground finite bounds. Underground networks are two distinct 2 km squares.</summary>
    public static bool InBounds(WorldPoint p) => double.IsFinite(p.X) && double.IsFinite(p.Y) && (p.Layer == 0
        ? p.X>=0 && p.Y>=0 && p.X<Size && p.Y<Size
        : p.Layer is 1 or 2 && Math.Abs(p.X-Elevators[p.Layer-1].X)<1024 && Math.Abs(p.Y-Elevators[p.Layer-1].Y)<1024);
    /// <summary>Terrain water is physical except where the same route definition supplies a bridge.</summary>
    public static bool Water(WorldPoint p)
    {
        if (p.Layer!=0 || Route(p)) return false;
        double riverX=River[^1].X;
        for(int i=1;i<River.Length;i++) if(p.Y<=River[i].Y) { double t=(p.Y-River[i-1].Y)/(River[i].Y-River[i-1].Y); riverX=River[i-1].X+(River[i].X-River[i-1].X)*t; break; }
        return Math.Abs(p.X-riverX)<180 || (p.X>124000) || (Region(p).Id==1 && Math.Pow((p.X-48000)/7000,2)+Math.Pow((p.Y-16000)/9000,2)<1);
    }
    /// <summary>Dense architecture families, distinct from natural terrain scatter.</summary>
    public static bool Urban(WorldPoint p) => p.Layer>0 || Region(p).Id is 5 or 6 or 7 or 9 or 10 or 13 or 14;
    /// <summary>Distance to a periodic line, continuous across chunk seams.</summary>
    private static double GridDistance(double value, double spacing, double offset) { double r=((value-offset)%spacing+spacing)%spacing; return Math.Min(r,spacing-r); }
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
