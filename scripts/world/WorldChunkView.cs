using Godot;
using System;
using System.Collections.Generic;

/// <summary>Main-thread chunk presentation: local terrain, foot-based solid props and fading roofs.</summary>
public partial class WorldChunkView : Node2D
{
    private WorldChunkData _data=null!;
    private readonly List<Sprite2D> _roofs=new();
    private Color _ground;
    private Color _route;
    private WorldDefinition _world=WorldCatalog.Definition;
    /// <summary>Applies one immutable worker payload. The owning world budgets chunk creation across frames.</summary>
    public void Initialize(WorldChunkData data,WorldDefinition world)
    {
        _data=data; _world=world;
        var region=world.Region(new(data.Address.X*128,data.Address.Y*128));
        _ground=data.Address.Layer>0?new("171d2b"):region.Biome switch
        {
            "desert"=>new("4f4331"),"forest"=>new("18392f"),"highland"=>new("34443b"),"mountain"=>new("3e4654"),
            "wetland"=>new("254c45"),"coast"=>new("3e585b"),"solar"=>new("41463d"),
            "palace"=>new("273a42"),"foundry" or "rail"=>new("3c3038"),_=>new("202d3c")
        };
        _route=world.Urban(new(data.Address.X*128,data.Address.Y*128,data.Address.Layer))?new("465867"):new("657266");
        ZIndex=-10;
        var props=new Node2D { YSortEnabled=true,ZIndex=10 }; AddChild(props);
        foreach(var prop in data.Props)
        {
            Vector2 at=ExplorationPlayer.Project(prop.X,prop.Y);
            var body=new StaticBody2D { Position=at,CollisionLayer=1,CollisionMask=2 }; props.AddChild(body);
            if(prop.Solid) body.AddChild(new CollisionShape2D { Shape=new RectangleShape2D { Size=ExplorationPlayer.Project(prop.Width,prop.Depth) } });
            string path=$"{world.ArtFolder}module_{prop.Kind}.png";
            if(ResourceLoader.Exists(path))
            {
                var roof=new Sprite2D { Texture=GD.Load<Texture2D>(path) };
                body.AddChild(roof); _roofs.Add(roof);
            }
        }
        // Water and outer bounds use the exact same cells drawn below. Routes through water are bridge decks.
        for(int y=0;y<16;y++) for(int x=0;x<16;x++) if(data.Terrain[y*16+x]>=2)
        {
            var body=new StaticBody2D { Position=ExplorationPlayer.Project(x*8+4,y*8+4),CollisionLayer=1,CollisionMask=2 };
            body.AddChild(new CollisionShape2D { Shape=new RectangleShape2D { Size=ExplorationPlayer.Project(8,8) } }); AddChild(body);
        }
        QueueRedraw();
    }
    /// <summary>Draws bounded local cells and path trim; no huge textures or global TileMap coordinates.</summary>
    public override void _Draw()
    {
        if(_data==null) return;
        for(int y=0;y<16;y++) for(int x=0;x<16;x++)
        {
            byte cell=_data.Terrain[y*16+x];
            var rect=new Rect2(ExplorationPlayer.Project(x*8,y*8),ExplorationPlayer.Project(8,8));
            DrawRect(rect,cell switch {1=>_route,2=>new Color("17384e"),3=>new Color("0b1320"),_=>_ground});
            if(cell==1) DrawLine(rect.Position,rect.Position+new Vector2(rect.Size.X,0),new Color(.4f,.7f,.75f,.2f),1);
            else if(cell==0 && WorldGeneration.Hash(_data.Address.X*16+x,_data.Address.Y*16+y,0)%3==0)
                DrawLine(rect.Position+new Vector2(12,13),rect.Position+new Vector2(17,13),new Color(.5f,.65f,.65f,.12f),1);
        }
        // The Rustrail corridor is physical geography shared with the map, with sleepers and two continuous rails.
        // Cities without a railway leave RailY unset, so the corridor is skipped.
        double railY=(_world.RailY??double.NegativeInfinity)-_data.Address.Y*128;
        if(_data.Address.Layer==0 && railY>=0 && railY<128)
        {
            for(int x=0;x<128;x+=4) DrawLine(ExplorationPlayer.Project(x,railY-2),ExplorationPlayer.Project(x,railY+2),new Color("866d52"),3);
            foreach(double offset in new[]{-1.2,1.2}) DrawLine(ExplorationPlayer.Project(0,railY+offset),ExplorationPlayer.Project(128,railY+offset),new Color("b0b8bd"),2);
        }
    }
    /// <summary>Fades occluding upper sprites near the player's feet, retaining the visible collision footprint.</summary>
    public void UpdateRoofs(Vector2 player)
    {
        foreach(var roof in _roofs)
        {
            Vector2 relative=player-roof.GlobalPosition;
            float target=Math.Abs(relative.X)<130 && relative.Y<40 && relative.Y> -170?.3f:1;
            roof.Modulate=new Color(1,1,1,Mathf.Lerp(roof.Modulate.A,target,.18f));
        }
    }
}
