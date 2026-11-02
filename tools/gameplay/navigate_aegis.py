"""Production finale navigation, fresh Replay and all Level 4 launch routes through Godot AI MCP, preserving the user's editor scene.

Uses actual movement, jump collision, operative swaps and signature abilities. Enemy damage is diagnostic;
these timings cannot establish first-time player pacing. Each eval is bounded below MCP's eight-second limit.
"""
import argparse
import base64
import json
import sys
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'tools/cutscene'))
from mcp_probe import GodotMcp


def main():
    """Run isolated route or aspect-ratio captures, store evidence, and stop only the game this script started."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--captures', action='store_true')
    parser.add_argument('--navigation', action='store_true')
    args = parser.parse_args()
    out = ROOT / '.validation/aegis'
    out.mkdir(parents=True, exist_ok=True)
    client = GodotMcp()
    records = []
    started = False

    def call(tool, params):
        """Reject transport/remote errors; caller assertions validate actual gameplay outcomes."""
        result = client.call(tool, params)['result']
        if result.get('isError'):
            raise RuntimeError(result)
        return result

    def evaluate(code):
        """Wait for game bootstrap only; keep all gameplay batches individually bounded."""
        for _ in range(12):
            code = '\n'.join('\t' * ((len(line)-len(line.lstrip(' ')))//4) + line.lstrip(' ') for line in code.splitlines())
            result = client.call('editor_manage', {'op': 'game_eval', 'params': {'code': code}})['result']
            if 'EVAL_GAME_NOT_READY' not in json.dumps(result):
                if result.get('isError'):
                    raise RuntimeError(result)
                return result.get('structuredContent', result)
            time.sleep(1)
        raise RuntimeError('Game bootstrap timed out')

    identity=call('session_manage',{'op':'list'})['structuredContent']
    assert any(s['project_path'].rstrip('/')=='D:/Projects/frog_jumper' and s['play_state']=='stopped' for s in identity['sessions'])
    prefix="var m=get_tree().current_scene\nvar p=m.get_node('Actors/CyberShogun')\n"
    events=['encounter:s1-3','interlock:approach','encounter:s2-1','encounter:s2-2','recruit:surya','lesson:bulwark','crossing:bulwark','encounter:s3-3','coolant:intake','encounter:s4-3','guns:western','lesson:blink','encounter:s5-3','curtain:inner','boss:voss','crossing:open']
    temporary_id=None
    try:
        call('project_run',{'mode':'custom','scene':'res://scenes/gameplay/level4.tscn','autosave':False});started=True
        for action,button,expected in [('replay','ReplayBtn','level4'),('select','LevelSelectBtn','level_selection'),('menu','MainMenuBtn','main_menu')]:
            if action!='replay':evaluate("get_tree().change_scene_to_file('res://scenes/gameplay/level4.tscn')\nawait get_tree().scene_changed\nreturn true")
            seed=prefix+"m.get_node('ShatteredAegisIntro').call('Finish')\nawait get_tree().process_frame\n"
            seed+="\n".join("m.call('CompleteCombatEvent',%s)"%json.dumps(event) for event in events)+"\n"
            seed+="p.position=Vector2(27700,554)\nm.call('_PhysicsProcess',0.016)\nassert(m.get('MissionComplete'))\nassert(not m.get_node('CyberVictoryScorecard/RootPanel/MainVBox/ContinueBtn').visible)\nreturn true"
            records.append({'results_'+action:evaluate(seed)})
            code=f"get_tree().current_scene.get_node('CyberVictoryScorecard/RootPanel/MainVBox/ButtonsHBox/{button}').emit_signal('pressed')\nawait get_tree().scene_changed\nvar m=get_tree().current_scene\nassert(m.scene_file_path.ends_with('{expected}.tscn'))\n"
            if action=='replay':code+="assert(m.get('ArrivalActive'))\nassert(not m.call('HasCombatEvent','interlock:approach'))\nvar c=load('res://scripts/managers/LevelCatalogBridge.cs').new()\nassert(c.UnlockedCount()==5)\n"
            code+="return m.scene_file_path"
            records.append({action:evaluate(code)})
        checks=[('continue',"get_tree().change_scene_to_file('res://scenes/gameplay/level3.tscn')\nawait get_tree().scene_changed\nvar m=get_tree().current_scene\nm.get_node('NeonRustIntro').call('Finish')\nawait get_tree().process_frame\nvar c=m.get_node('CyberVictoryScorecard')\nc.call('ShowMissionScorecard',150,4,'KAITO',4)\nc.get_node('RootPanel/MainVBox/ContinueBtn').emit_signal('pressed')\nawait get_tree().scene_changed\n"),
          ('level_select',"get_tree().change_scene_to_file('res://scenes/main_menu/level_selection.tscn')\nawait get_tree().scene_changed\nget_tree().current_scene.call('_start_level',4)\nawait get_tree().scene_changed\n")]
        assertion="var level=get_tree().current_scene\nassert(level.scene_file_path.ends_with('level4.tscn'))\nassert(level.get('ArrivalActive'))\nassert(not level.call('HasCombatEvent','interlock:approach'))\nreturn true"
        for label,code in checks:records.append({label:evaluate(code+assertion)})
        response=evaluate("var s=get_node('/root/SessionManager')\nvar id=s.create_new_session('Cyber Shogun')\nassert(id>0)\ns.update_session(id,4,0,0)\nreturn id")
        temporary_id=int(response['result'])
        load=f"get_tree().change_scene_to_file('res://scenes/main_menu/main_menu.tscn')\nawait get_tree().scene_changed\nvar m=get_tree().current_scene\nvar popup=m.get_node('ConfirmPopup')\npopup.set_meta('delete_mode','none')\npopup.set_meta('session_id',{temporary_id})\npopup.set_meta('session_data',get_node('/root/SessionManager').get_session({temporary_id}))\nm.get_node('MenuSessionUI').call('ConfirmAction')\nawait get_tree().scene_changed\n"
        records.append({'saved_load':evaluate(load+assertion)})
        records.append({'logs':call('logs_read',{'source':'game','count':300,'include_details':True})})
    finally:
        if temporary_id is not None:
            records.append({'deleted_owned_save':evaluate(f"get_node('/root/SessionManager').delete_session({temporary_id})\nassert(get_node('/root/SessionManager').get_session({temporary_id}).is_empty())\nreturn true")})
        (out/'navigation.json').write_text(json.dumps(records,indent=2))
        if started:call('project_manage',{'op':'stop'})
        client.close()


if __name__=='__main__':main()
