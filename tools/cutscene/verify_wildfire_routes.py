"""Verify arrival launch routes and uninterrupted playback through the local Godot AI MCP.

Uses only an explicitly created temporary save and deletes that record before stopping
its separate test game. The open editor scene and existing saved sessions are preserved.
"""
import json
import time
from pathlib import Path
from mcp_probe import GodotMcp

OUT = Path(__file__).resolve().parents[2] / '.validation/wildfire'


def main():
    """Exercise real scorecard/menu/save routes, then measure all six real-time audio/visual windows."""
    client = GodotMcp()
    records = []
    temporary_id = None

    def call(tool, args):
        """Expose remote errors and tolerate only the bridge's documented startup delay."""
        for attempt in range(15):
            result = client.call(tool, args)['result']
            if 'EVAL_GAME_NOT_READY' not in json.dumps(result):
                break
            time.sleep(1)
        if result.get('isError'):
            raise RuntimeError(result)
        return result

    def evaluate(code):
        """Evaluate exclusively in the separate running game and extract its structured result."""
        result = call('editor_manage', {'op':'game_eval','params':{'code':code}})
        data = result.get('structuredContent', {})
        if 'result' not in data:
            data = json.loads(result['content'][0]['text'])
        records.append({'code':code,'response':data})
        return data.get('result')

    def assert_arrival():
        """Require the current mission, independent intro, frozen player and starting solo roster."""
        result = evaluate("var m = get_tree().current_scene\nvar bridge = load('res://scripts/managers/LevelCatalogBridge.cs').new()\nvar r = {'scene':m.scene_file_path,'intro':m.has_node('ConcreteWildfireIntro'),'locked':m.get('ArrivalActive'),'player':m.get_node('Actors/CyberShogun').can_process(),'squad':bridge.UnlockedCount()}\nbridge = null\nreturn r")
        assert result['scene'].endswith('level2.tscn') and result['intro'] and result['locked'] and not result['player'] and result['squad']==1, result

    identity = call('session_manage', {'op':'list'})
    assert 'D:/Projects/frog_jumper' in json.dumps(identity)
    try:
        call('project_run', {'mode':'custom','scene':'res://scenes/gameplay/level1.tscn','autosave':False})
        # Use real extraction prerequisites and the existing Continue button.
        evaluate("var m = get_tree().current_scene\nvar p = m.get_node('Actors/CyberShogun')\nvar b = m.get_node('Actors/DemolitionBeacon')\np.global_position = b.global_position + Vector2(-200,-50)\np.emit_signal('ParryDeflected',p)\np.emit_signal('ParryDeflected',p)\nb.call('TakeDamage',1000.0)\np.global_position = m.get_node('Actors/DataBlade').global_position + Vector2(0,-60)\nm.call('CollectNearbyObjectives')\np.global_position = m.get_node('Actors/ExtractionGate').global_position + Vector2(0,-80)\nm.call('CollectNearbyObjectives')\nawait get_tree().process_frame\nm.get_node('TokyoRemembers').call('Finish')\nm.get_node('CyberVictoryScorecard/RootPanel/MainVBox/ContinueBtn').emit_signal('pressed')\nawait get_tree().scene_changed\nreturn get_tree().current_scene.scene_file_path")
        assert_arrival()
        print('Verified Level 1 Continue',flush=True)
        evaluate("get_tree().change_scene_to_file('res://scenes/main_menu/level_selection.tscn')\nawait get_tree().scene_changed\nget_tree().current_scene.call('_on_level_2_pressed')\nawait get_tree().scene_changed\nreturn get_tree().current_scene.scene_file_path")
        assert_arrival()
        print('Verified Level Select',flush=True)
        # A temporary save exercises the actual menu confirmation loader without modifying an existing record.
        temporary_id = evaluate("var sm = get_node('/root/SessionManager')\nvar id = sm.call('create_new_session','Cyber Shogun')\nsm.call('update_session',id,2,0,0)\nreturn id")
        evaluate("get_tree().change_scene_to_file('res://scenes/main_menu/main_menu.tscn')\nawait get_tree().scene_changed\nreturn get_tree().current_scene.scene_file_path")
        evaluate(f"var m = get_tree().current_scene\nvar popup = m.get_node('ConfirmPopup')\npopup.set_meta('delete_mode','none')\npopup.set_meta('session_id',{temporary_id})\npopup.set_meta('session_data',get_node('/root/SessionManager').call('get_session',{temporary_id}))\nm.get_node('MenuSessionUI').call('ConfirmAction')\nawait get_tree().scene_changed\nreturn get_tree().current_scene.scene_file_path")
        assert_arrival()
        evaluate(f"get_node('/root/SessionManager').call('delete_session',{temporary_id})\nreturn true")
        temporary_id = None
        print('Verified saved-session loading',flush=True)
        evaluate("get_tree().reload_current_scene()\nawait get_tree().scene_changed\nreturn true")
        assert_arrival()
        # Restart only the review clock/audio; playback thereafter is uninterrupted until natural completion.
        evaluate("var c = get_tree().current_scene.get_node('ConcreteWildfireIntro')\nc.call('SeekAudio',0.0)\nreturn true")
        for _shot in range(6):
            time.sleep(9.7)
            result = evaluate("var m = get_tree().current_scene\nvar c = m.get_node_or_null('ConcreteWildfireIntro')\nvar p = m.get_node('Actors/CyberShogun')\nreturn {'time':c.get('Elapsed') if c else 60.0,'audio':c.get_node('Speech').get_playback_position() if c else 60.0,'health':p.get('CurrentHealth'),'score':m.get_node('GameManager').get('Points'),'position':p.position,'locked':m.get('ArrivalActive')}")
            assert result['health']==100 and result['score']==0, result
            assert abs(result['time']-result['audio']) < .3, result
            print('Playback checkpoint',result,flush=True)
        time.sleep(3)
        result = evaluate("var m = get_tree().current_scene\nreturn {'scene':m.scene_file_path,'intro':m.has_node('ConcreteWildfireIntro'),'locked':m.get('ArrivalActive'),'player':m.get_node('Actors/CyberShogun').can_process(),'hud':m.get_node('MissionHUD').visible,'pause':m.get_node('CanvasLayer/PauseMenu').can_process()}")
        assert not result['intro'] and not result['locked'] and result['player'] and result['hud'] and result['pause'], result
        records.append({'logs':call('logs_read',{'source':'game','count':100,'include_details':True})})
        print('Verified natural completion',flush=True)
    finally:
        if temporary_id is not None:
            evaluate(f"get_node('/root/SessionManager').call('delete_session',{temporary_id})\nreturn true")
        (OUT / 'routes-playback.json').write_text(json.dumps(records,indent=2))
        call('project_manage',{'op':'stop'})
        client.close()


if __name__ == '__main__':
    main()
