using Godot;
using System;
using System.Threading.Tasks;

/// <summary>Campaign menu/world handoff. All transitions are stored before leaving; errors keep the current screen open.</summary>
public static class WorldNavigation
{
    /// <summary>Restores an interrupted mission through its story entry, or opens the committed/legacy exploration state.</summary>
    public static async Task<string> ResumePath(int session,int progress,string operative)
    {
        var state=await WorldStorage.LoadAsync(session,progress,preferredOperative:operative);
        if(state==null) return LevelCatalog.ScenePath(1);
        if(state.Resume=="mission") { CampaignProgress.PrepareMission(state.Mission,state.Operative); return WorldMissionRouting.LaunchPath(state.Mission); }
        await WorldStorage.SaveAsync(session,state);
        return WorldCatalog.Scene;
    }
    /// <summary>Mission selection sets a waypoint to the entrance; launching still requires walking to and interacting with it.</summary>
    public static async Task SelectEntrance(Node owner,int mission)
    {
        if(!GodotObject.IsInstanceValid(owner) || !owner.IsInsideTree()) return;
        var global=owner.GetNodeOrNull<Node>("/root/Global");
        int progress=global?.Get("unlocked_levels").AsInt32()??1;
        if(mission<1 || mission>progress || mission>LevelCatalog.Count) return;
        if(progress==1 && mission==1) { CampaignProgress.PrepareMission(1); LoadingScreen.LoadScene(owner.GetTree(),WorldMissionRouting.LaunchPath(1)); return; }
        int session=SessionManager.Instance?.CurrentSessionId??-1;
        try
        {
            var state=await WorldStorage.LoadAsync(session,progress)??WorldSave.AfterMission(Math.Max(1,progress-1));
            // Mission terminals are all in Los Angeles, so choosing one brings the player back from any city.
            state=state with {Resume="world",Waypoint=WorldCatalog.Entrances[mission-1].Position,CurrentCity=null};
            if(session>0) await WorldStorage.SaveAsync(session,state);
            if(GodotObject.IsInstanceValid(owner) && owner.IsInsideTree()) LoadingScreen.LoadScene(owner.GetTree(),WorldCatalog.Scene);
        }
        catch(Exception) { ShowSaveError(owner); }
    }
    /// <summary>Shows a dismissible error; repeating the original selection retries without discarding the committed state.</summary>
    public static void ShowSaveError(Node owner)
    {
        if(!GodotObject.IsInstanceValid(owner) || !owner.IsInsideTree()) return;
        var dialog=new AcceptDialog {DialogText=WorldText.Get("Save failed. Retry to continue.")}; owner.AddChild(dialog);
        dialog.Confirmed+=dialog.QueueFree; dialog.Canceled+=dialog.QueueFree; dialog.PopupCentered();
    }
}
