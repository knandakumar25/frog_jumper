using System.Text.Json;
using Npgsql;

/// <summary>Engine-independent geographic, deterministic-generation and bounded-streaming acceptance tests.</summary>
public class WorldTests
{
    /// <summary>The promised initial region is finite, north-up, fully named and has exactly the announced destinations.</summary>
    [Test]
    public void GeographyAndDestinationsMatchTheWorldContract()
    {
        Assert.That((long)WorldCatalog.Size*WorldCatalog.Size/1000000,Is.EqualTo(16384));
        Assert.That(WorldCatalog.Regions.Count,Is.EqualTo(16));
        Assert.That(WorldCatalog.Landmarks.Count,Is.EqualTo(48));
        Assert.That(WorldCatalog.Landmarks.Select(l=>l.Name).Distinct().Count(),Is.EqualTo(48));
        Assert.That(WorldCatalog.Stations.Count,Is.EqualTo(256));
        Assert.That(WorldCatalog.Region(new(1,1)).Name,Is.EqualTo("Angeles Crest Highlands"));
        Assert.That(WorldCatalog.Region(new(127999,127999)).Name,Is.EqualTo("Ballona Reclamation Wetlands"));
        Assert.That(WorldCatalog.Entrances.Select(e=>WorldCatalog.Region(e.Position).Id),Is.EqualTo(new[]{5,9,13,10,6,6,14,10,10,10,10,10}));
    }
    /// <summary>All transit and mission arrivals lie on connected arterials with no water/prop blockage within a foot radius.</summary>
    [Test]
    public void ArrivalPositionsAreSafeAndNearTransit()
    {
        foreach(var station in WorldCatalog.Stations)
        {
            Assert.That(WorldCatalog.InBounds(station.Position)); Assert.That(WorldCatalog.Arterial(station.Position));
            Assert.That(WorldCatalog.Water(station.Position),Is.False);
        }
        foreach(var entrance in WorldCatalog.Entrances)
        {
            Assert.That(WorldCatalog.NearbyEntrance(entrance.Position)?.Mission,Is.EqualTo(entrance.Mission),"Closely spaced Spire terminals must select their own mission.");
            Assert.That(WorldCatalog.InBounds(entrance.Position),Is.True);
            Assert.That(WorldCatalog.NearestStation(entrance.Position).Position.Distance(entrance.Position with {Layer=0}),Is.LessThanOrEqualTo(64));
            var data=WorldGeneration.Generate(entrance.Position.Chunk);
            double x=entrance.Position.X%128,y=entrance.Position.Y%128;
            Assert.That(data.Props.Any(p=>Math.Abs(p.X-x)<p.Width/2+1 && Math.Abs(p.Y-y)<p.Depth/2+1),Is.False);
        }
        // Every row and column of the arterial grid is continuous, including bridge crossings.
        for(int i=0;i<16;i++) for(int step=0;step<1000;step++)
        {
            Assert.That(WorldCatalog.Route(new(4000+i*8000,step*128)),Is.True);
            Assert.That(WorldCatalog.Route(new(step*128,4000+i*8000)),Is.True);
        }
    }
    /// <summary>Generation does not depend on call order, process RNG or prior cache contents, and routes join at seams.</summary>
    [Test]
    public void GenerationIsStableAcrossOrderAndSeams()
    {
        Assert.That(WorldGeneration.Hash(1,2,0),Is.EqualTo(564085346u));
        foreach(var address in new[]{new ChunkAddress(0,0),new(343,343),new(999,999),new(531,343,1)})
        {
            var first=WorldGeneration.Generate(address);
            WorldGeneration.Generate(new(100,200));
            var second=WorldGeneration.Generate(address);
            Assert.That(second.Terrain,Is.EqualTo(first.Terrain)); Assert.That(second.Props,Is.EqualTo(first.Props));
            Assert.That(first.Props.All(p=>p.X-p.Width/2>=0 && p.X+p.Width/2<128 && p.Y-p.Depth/2>=0 && p.Y+p.Depth/2<128));
        }
        for(int c=0;c<1000;c+=17)
        {
            var left=WorldGeneration.Generate(new(c,343)); var right=WorldGeneration.Generate(new(c+1,343));
            // The y=32 local street crosses both outer cell columns identically.
            Assert.That(left.Terrain[4*16+15],Is.EqualTo(right.Terrain[4*16]));
        }
    }
    /// <summary>Large logical positions round-trip through bounded local coordinates, including future negative expansion.</summary>
    [TestCase(127999.99,127999.99)] [TestCase(-.25,-128.5)] [TestCase(128,256)]
    public void ChunkCoordinatesRoundTrip(double x,double y)
    {
        var p=new WorldPoint(x,y); var chunk=p.Chunk;
        double localX=x-chunk.X*128,localY=y-chunk.Y*128;
        Assert.That(localX,Is.InRange(0,127.999999)); Assert.That(localY,Is.InRange(0,127.999999));
        Assert.That(chunk.X*128+localX,Is.EqualTo(x).Within(.0000001));
        Assert.That(chunk.Y*128+localY,Is.EqualTo(y).Within(.0000001));
    }
    /// <summary>A sweep with teleports and layer changes cannot grow the cache above 25 or active address set above nine.</summary>
    [Test]
    public async Task StreamingCacheRemainsBoundedDuringSoak()
    {
        var stream=new WorldStream();
        for(int i=0;i<120;i++)
        {
            var center=new ChunkAddress(i*7%1000,i*13%1000,i%3);
            stream.Request(center);
            for(int wait=0;stream.Busy && wait<1000;wait++) { stream.Pump(center); await Task.Delay(1); }
            Assert.That(stream.Busy,Is.False); Assert.That(stream.Cache.Count,Is.EqualTo(25));
            Assert.That(stream.Cache.Keys,Is.EquivalentTo(WorldStream.Window(center,2)));
            Assert.That(WorldStream.Window(center,1).Count(),Is.EqualTo(9));
        }
    }
    /// <summary>Snapshots and finale returns survive JSON; unknown versions and invalid layers are rejected.</summary>
    [Test]
    public void SaveValidationAndFinaleRoutingAreVersioned()
    {
        for(int i=1;i<=12;i++)
        {
            var state=WorldSave.AfterMission(i,"Mirage Blade");
            Assert.That(state.Valid(),Is.True); Assert.That(state.Resume,Is.EqualTo("world"));
            Assert.That(JsonSerializer.Deserialize<WorldSave>(JsonSerializer.Serialize(state)),Is.EqualTo(state));
            Assert.That(WorldMissionRouting.LaunchPath(i),Does.EndWith(".tscn"));
        }
        Assert.That((WorldSave.AfterMission(1) with {Version=2}).Valid(),Is.False);
        Assert.That((WorldSave.AfterMission(1) with {World="neo-kyoto-v1"}).Valid(),Is.True);
        Assert.That((WorldSave.AfterMission(1) with {World="invalid-world"}).Valid(),Is.False);
        Assert.That((WorldSave.AfterMission(1) with {Position=new(0,0,3)}).Valid(),Is.False);
        Assert.That(WorldMissionRouting.LaunchPath(5),Does.EndWith("act_ii_opening.tscn"));
        Assert.That(WorldMissionRouting.LaunchPath(8),Does.EndWith("act_ii_end_screen.tscn"));
    }
}

/// <summary>Real PostgreSQL world tests use only newly owned campaigns; failures cannot alter player saves.</summary>
[NonParallelizable]
public class WorldStorageTests
{
    private readonly List<int> _owned=new();
    private string _connection="";
    /// <summary>Reads the existing configured store from the repository, without logging connection credentials.</summary>
    [SetUp]
    public void Configure()
    {
        var root=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while(root!=null && !File.Exists(Path.Combine(root.FullName,"project.godot"))) root=root.Parent;
        string previous=Directory.GetCurrentDirectory();
        try { Directory.SetCurrentDirectory(root!.FullName); _connection=PostgresStorage.ConnectionString; }
        finally { Directory.SetCurrentDirectory(previous); }
    }
    /// <summary>Creates a uniquely owned temporary campaign in the existing game save schema.</summary>
    private async Task<int> Campaign()
    {
        await using var c=new NpgsqlConnection(_connection); await c.OpenAsync();
        await using(var schema=new NpgsqlCommand("CREATE TABLE IF NOT EXISTS game_sessions (id SERIAL PRIMARY KEY,character VARCHAR(100) NOT NULL,current_level INT NOT NULL DEFAULT 1,total_score INT NOT NULL DEFAULT 0,collectables INT NOT NULL DEFAULT 0,created_at VARCHAR(100) NOT NULL,last_played VARCHAR(100) NOT NULL)",c)) await schema.ExecuteNonQueryAsync();
        await using var cmd=new NpgsqlCommand("INSERT INTO game_sessions(character,created_at,last_played) VALUES ('world-fixture','fixture','fixture') RETURNING id",c);
        int id=Convert.ToInt32(await cmd.ExecuteScalarAsync()); _owned.Add(id); return id;
    }
    /// <summary>Removes only exact IDs allocated by this fixture, including their namespaced receipts and world snapshots.</summary>
    [TearDown]
    public async Task Cleanup()
    {
        await using var c=new NpgsqlConnection(_connection); await c.OpenAsync();
        foreach(int id in _owned)
        {
            await using var cmd=new NpgsqlCommand("DELETE FROM game_state WHERE state_key=@key OR state_key LIKE @prefix; DELETE FROM game_sessions WHERE id=@id AND created_at='fixture'",c);
            cmd.Parameters.AddWithValue("key",$"world:{id}"); cmd.Parameters.AddWithValue("prefix",$"mission-completion:{id}:%"); cmd.Parameters.AddWithValue("id",id);
            await cmd.ExecuteNonQueryAsync();
        }
        _owned.Clear();
    }
    /// <summary>Every mission commits its extraction alongside rewards; receipt replay cannot overwrite later free transit.</summary>
    [Test]
    public async Task AllTwelveReturnsAreAtomicAndRetriesPreserveNewerWorldState()
    {
        int id=await Campaign();
        for(int mission=1;mission<=12;mission++)
        {
            string token=Guid.NewGuid().ToString();
            var result=await CampaignSettlement.SaveAsync(id,mission,10,token,_connection,"Oceanus");
            Assert.That(result.Status,Is.EqualTo(CompletionSaveStatus.Saved));
            var state=await WorldStorage.LoadAsync(id,result.CurrentLevel,_connection);
            Assert.That(state,Is.EqualTo(WorldSave.AfterMission(mission,"Oceanus")));
            var transit=state! with {Position=WorldCatalog.Stations[255].Position,Waypoint=new(1000,1000),Resume="mission",Mission=mission};
            await WorldStorage.SaveAsync(id,transit,_connection);
            await CampaignSettlement.SaveAsync(id,mission,10,token,_connection,"Oceanus");
            Assert.That(await WorldStorage.LoadAsync(id,result.CurrentLevel,_connection),Is.EqualTo(transit));
            // The next settlement preserves the waypoint, so clear it before comparing the next default arrival.
            await WorldStorage.SaveAsync(id,transit with {Waypoint=null},_connection);
        }
    }
    /// <summary>Legacy campaigns migrate only after Mission 1; independent campaigns never share exploration positions.</summary>
    [Test]
    public async Task LegacyAndIndependentCampaignsRemainIsolated()
    {
        int first=await Campaign(),second=await Campaign();
        Assert.That(await WorldStorage.LoadAsync(first,1,_connection),Is.Null);
        Assert.That(await WorldStorage.LoadAsync(first,5,_connection),Is.EqualTo(WorldSave.AfterMission(4)));
        var state=WorldSave.AfterMission(3) with {Position=WorldCatalog.Stations[0].Position};
        await WorldStorage.SaveAsync(first,state,_connection);
        Assert.That(await WorldStorage.LoadAsync(second,5,_connection),Is.EqualTo(WorldSave.AfterMission(4)));
        Assert.That(await WorldStorage.LoadAsync(first,5,_connection),Is.EqualTo(state));
        var unavailable=new NpgsqlConnectionStringBuilder(_connection) {Port=1,Timeout=1};
        Assert.ThrowsAsync<NpgsqlException>(async()=>await WorldStorage.SaveAsync(first,state with {Position=new(100,100)},unavailable.ConnectionString));
        Assert.That(await WorldStorage.LoadAsync(first,5,_connection),Is.EqualTo(state));
    }

    /// <summary>An unsupported world document rolls back receipt, reward and campaign progress together.</summary>
    [Test]
    public async Task UnsupportedWorldVersionCannotPartiallySettleOrBeDowngraded()
    {
        int id=await Campaign();
        await WorldStorage.SaveAsync(id,WorldSave.AfterMission(1),_connection);
        await using var c=new NpgsqlConnection(_connection); await c.OpenAsync();
        await using(var change=new NpgsqlCommand("UPDATE game_state SET value=jsonb_set(value,'{Version}','2') WHERE state_key=@key",c))
        {
            change.Parameters.AddWithValue("key",$"world:{id}"); await change.ExecuteNonQueryAsync();
        }
        string token=Guid.NewGuid().ToString();
        Assert.That((await CampaignSettlement.SaveAsync(id,2,40,token,_connection)).Status,Is.EqualTo(CompletionSaveStatus.Failed));
        Assert.ThrowsAsync<InvalidOperationException>(async()=>await WorldStorage.SaveAsync(id,WorldSave.AfterMission(2),_connection));
        await using var verify=new NpgsqlCommand("SELECT current_level,total_score,(SELECT count(*) FROM game_state WHERE state_key=@receipt) FROM game_sessions WHERE id=@id",c);
        verify.Parameters.AddWithValue("receipt",$"mission-completion:{id}:{token}"); verify.Parameters.AddWithValue("id",id);
        await using var reader=await verify.ExecuteReaderAsync(); await reader.ReadAsync();
        Assert.That(reader.GetInt32(0),Is.EqualTo(1)); Assert.That(reader.GetInt32(1),Is.EqualTo(0)); Assert.That(reader.GetInt64(2),Is.EqualTo(0));
    }
}
