using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>Immutable generated footprint in local metres. Kind identifies the original modular asset.</summary>
public sealed record WorldProp(double X, double Y, double Width, double Depth, int Kind, bool Solid);
/// <summary>Thread-safe chunk payload; terrain cells cover exactly 128m with eight-metre cells.</summary>
public sealed record WorldChunkData(ChunkAddress Address, byte[] Terrain, IReadOnlyList<WorldProp> Props);

/// <summary>Pure stable generator; shared geographic sampling prevents road/water discontinuities at chunk seams.</summary>
public static class WorldGeneration
{
    /// <summary>Small fixed integer hash avoids runtime-dependent System.Random and string hashing.</summary>
    public static uint Hash(int x,int y,int layer) { unchecked { uint n=(uint)x*374761393u+(uint)y*668265263u+(uint)layer*2246822519u+0x4e4b7631; n=(n^(n>>13))*1274126177u; return n^(n>>16); } }
    /// <summary>Builds data only, safe on a worker thread. Protected endpoints and trails cannot receive solid props.</summary>
    public static WorldChunkData Generate(ChunkAddress a)
    {
        byte[] terrain=new byte[256]; var props=new List<WorldProp>();
        for(int y=0;y<16;y++) for(int x=0;x<16;x++)
        {
            var p=new WorldPoint(a.X*128+x*8+4,a.Y*128+y*8+4,a.Layer);
            terrain[y*16+x]=!WorldCatalog.InBounds(p)?(byte)3:WorldCatalog.Water(p)?(byte)2:WorldCatalog.Route(p)?(byte)1:(byte)0;
        }
        for(int y=0;y<4;y++) for(int x=0;x<4;x++)
        {
            uint h=Hash(a.X*4+x,a.Y*4+y,a.Layer); double px=x*32+9+(h%5),py=y*32+9+((h>>4)%5);
            var p=new WorldPoint(a.X*128+px,a.Y*128+py,a.Layer);
            bool urban=WorldCatalog.Urban(p); double w=urban?12:5,d=urban?10:4;
            if(!WorldCatalog.InBounds(p) || WorldCatalog.Water(p) || WorldCatalog.Route(p) || WorldCatalog.Arterial(p,20)) continue;
            if(WorldCatalog.Entrances.Any(e=>e.Position.Distance(p)<48) || WorldCatalog.Landmarks.Any(e=>e.Position.Distance(p)<24) || WorldCatalog.Elevators.Any(e=>e.Distance(p with {Layer=0})<32)) continue;
            int region=WorldCatalog.Region(p).Id;
            int kind=a.Layer>0?3:urban?(region==5?1:region is 13 or 14?3:region==10?4:0):region is 0 or 2 or 12?6:region==8?7:5;
            props.Add(new(px,py,w,d,kind,true));
        }
        return new(a,terrain,props);
    }
}

/// <summary>Bounded 5x5 data/prefetch window. At most one 25-chunk worker batch exists; stale results are discarded.</summary>
public sealed class WorldStream
{
    private Task<WorldChunkData[]>? _pending;
    private ChunkAddress _requested;
    private readonly Dictionary<ChunkAddress,WorldChunkData> _cache=new();
    public IReadOnlyDictionary<ChunkAddress,WorldChunkData> Cache => _cache;
    public bool Busy => _pending!=null;
    /// <summary>Starts a worker batch without any Godot API calls; repeated requests wait for the previous bounded batch.</summary>
    public void Request(ChunkAddress center)
    {
        if(_pending!=null) return;
        _requested=center;
        var missing=Window(center,2).Where(a=>!_cache.ContainsKey(a)).ToArray();
        _pending=Task.Run(()=>missing.Select(WorldGeneration.Generate).ToArray());
    }
    /// <summary>Accepts completed data on the main thread, evicts outside the current window and bounds cache at 25.</summary>
    public bool Pump(ChunkAddress center)
    {
        if(_pending is not { IsCompleted:true }) return false;
        var result=_pending.GetAwaiter().GetResult(); _pending=null;
        var wanted=Window(center,2).ToHashSet();
        foreach(var key in _cache.Keys.Where(k=>!wanted.Contains(k)).ToArray()) _cache.Remove(key);
        foreach(var data in result) if(wanted.Contains(data.Address)) _cache[data.Address]=data;
        return _requested==center;
    }
    /// <summary>Enumerates a fixed square ordered by distance so active center chunks apply before their neighbors.</summary>
    public static IEnumerable<ChunkAddress> Window(ChunkAddress center,int radius) =>
        Enumerable.Range(-radius,2*radius+1).SelectMany(y=>Enumerable.Range(-radius,2*radius+1).Select(x=>new ChunkAddress(center.X+x,center.Y+y,center.Layer)))
        .OrderBy(a=>Math.Abs(a.X-center.X)+Math.Abs(a.Y-center.Y));
}
