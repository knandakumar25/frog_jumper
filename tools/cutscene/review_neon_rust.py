"""Review Silent Freight through Godot AI MCP without changing the user's editor scene or saved sessions."""
import base64
import json
import subprocess
import time
from pathlib import Path
from mcp_probe import GodotMcp

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / '.validation/neon-rust/review'


def contact_sheets():
    """Use Docker FFmpeg to assemble ordered nine-shot QA sheets with preserved aspect ratios; no extra host dependencies."""
    for ratio in ['1920x1080','1920x1200','2560x1080']:
        command = ['docker','compose','run','--rm','--entrypoint','ffmpeg','cutscene','-v','error','-y']
        for seconds in [4,13,22,31,40,50,59,68,73]:
            command += ['-i',f'/workspace/.validation/neon-rust/review/{ratio}_{seconds}.png']
        filters = [f'[{i}:v]scale=480:270:force_original_aspect_ratio=decrease,pad=480:270:(ow-iw)/2:(oh-ih)/2[v{i}]' for i in range(9)]
        layout = '|'.join(f'{i%3*480}_{i//3*270}' for i in range(9))
        filters.append(''.join(f'[v{i}]' for i in range(9)) + f'xstack=inputs=9:layout={layout}[out]')
        command += ['-filter_complex_threads','1','-filter_complex',';'.join(filters),'-map','[out]','-frames:v','1',
                    f'/workspace/.validation/neon-rust/review/{ratio}_contact.png']
        subprocess.run(command, check=True, cwd=ROOT)


def main():
    """Capture nine shots at three ratios, sample uninterrupted playback and exercise launch/navigation routes."""
    OUT.mkdir(parents=True, exist_ok=True)
    client = GodotMcp()
    records = []
    temporary_session = None

    def call(tool, params):
        """Wait through bounded startup and fail on MCP tool errors."""
        for attempt in range(15):
            result = client.call(tool, params)['result']
            if 'EVAL_GAME_NOT_READY' not in json.dumps(result):
                break
            time.sleep(1)
        if result.get('isError'):
            raise RuntimeError(result)
        return result

    def evaluate(code):
        """Keep every diagnostic expression and structured result in the review evidence."""
        result = call('editor_manage', {'op':'game_eval','params':{'code':code}})
        records.append(dict(code=code, result=result))
        data = result.get('structuredContent', {})
        if 'result' not in data:
            data = json.loads(result['content'][0]['text'])
        return data.get('result')

    identity = call('session_manage', {'op':'list'})
    assert 'D:/Projects/frog_jumper' in json.dumps(identity)
    assert '"play_state": "stopped"' in json.dumps(identity), 'Preserve an existing running game'
    try:
        call('project_run', {'mode':'custom','scene':'res://scenes/gameplay/level3.tscn','autosave':False})
        state = evaluate("""var m=get_tree().current_scene
assert(get_node('/root/SessionManager').get('CurrentSessionId')<=0)
var c=m.get_node('NeonRustIntro')
c.set_process(false)
c.get_node('Speech').stop()
c.get_node('Atmosphere').stop()
var p=m.get_node('Actors/CyberShogun')
var b=load('res://scripts/managers/LevelCatalogBridge.cs').new()
var result={'arrival':m.get('ArrivalActive'),'position':p.position,'health':p.get('CurrentHealth'),'score':m.get_node('GameManager').get('Points'),'squad':b.UnlockedCount()}
b=null
return result""")
        assert state['arrival'] and state['squad'] == 3, state
        initial = {key:state[key] for key in ['position','health','score']}
        for width, height in [(1920,1080),(1920,1200),(2560,1080)]:
            for seconds in [4,13,22,31,40,50,59,68,73]:
                evaluate(f"get_window().mode=Window.MODE_WINDOWED\nget_window().size=Vector2i({width},{height})\nvar c=get_tree().current_scene.get_node('NeonRustIntro')\nc.call('Seek',{seconds}.0)\nawait get_tree().process_frame\nawait RenderingServer.frame_post_draw\nreturn {{'elapsed':c.get('Elapsed'),'viewport':get_viewport().get_visible_rect().size,'subtitle':c.get_node('UI/SubtitlePanel/Subtitle').get_global_rect(),'skip':c.get_node('UI/Skip').get_global_rect()}}")
                capture = call('editor_screenshot', {'source':'game','max_resolution':0,'include_image':True})
                for block in capture.get('content', []):
                    if block.get('type') == 'image':
                        (OUT/f'{width}x{height}_{seconds}.png').write_bytes(base64.b64decode(block['data']))
                print('Captured', width, height, seconds, flush=True)
        contact_sheets()
        evaluate("var c=get_tree().current_scene.get_node('NeonRustIntro')\nc.call('SeekAudio',0.0)\nc.set_process(true)\nreturn true")
        for shot in range(8):
            time.sleep(9)
            state = evaluate("""var m=get_tree().current_scene
var c=m.get_node('NeonRustIntro')
var p=m.get_node('Actors/CyberShogun')
var frozen=true
for n in m.find_children('*','',true,false):
	if n==c or c.is_ancestor_of(n): continue
	if n.can_process(): frozen=false
return {'elapsed':c.get('Elapsed'),'audio':c.get_node('Speech').get_playback_position(),'position':p.position,'health':p.get('CurrentHealth'),'score':m.get_node('GameManager').get('Points'),'frozen':frozen,'relay':m.get('CommsBlackedOut'),'fragments':m.get('MemoryFragmentsFound')}""")
            assert abs(state['elapsed'] - state['audio']) < .3 and state['frozen'], state
            assert all(state[key] == initial[key] for key in initial), state
            assert not state['relay'] and state['fragments'] == 0, state
            print('Playback', state, flush=True)
        time.sleep(4)
        state = evaluate("var m=get_tree().current_scene\nreturn {'arrival':m.get('ArrivalActive'),'overlay':m.has_node('NeonRustIntro'),'live':m.get_node('Actors/CyberShogun').can_process(),'hud':m.get_node('MissionHUD').visible,'music':m.get_node('AudioStreamPlayer').stream_paused,'jump':Input.is_action_pressed('jump')}")
        assert not state['arrival'] and not state['overlay'] and state['live'] and state['hud'] and not state['music'] and not state['jump'], state
        # The shared results Continue loads the actual Level 3 scene; no campaign completion/save is fabricated.
        result = evaluate("get_tree().change_scene_to_file('res://scenes/gameplay/level2.tscn')\nawait get_tree().scene_changed\nvar m=get_tree().current_scene\nm.get_node('ConcreteWildfireIntro').call('Finish')\nawait get_tree().process_frame\nvar card=m.get_node('CyberVictoryScorecard')\ncard.call('ShowMissionScorecard',150,4,'KAITO',3)\ncard.get_node('RootPanel/MainVBox/ContinueBtn').emit_signal('pressed')\nawait get_tree().scene_changed\nreturn {'scene':get_tree().current_scene.scene_file_path,'arrival':get_tree().current_scene.get('ArrivalActive')}")
        assert result['scene'].endswith('level3.tscn') and result['arrival'], result
        # Exercise mission selection through its production loading screen and catalog path.
        result = evaluate("get_tree().change_scene_to_file('res://scenes/main_menu/level_selection.tscn')\nawait get_tree().scene_changed\nget_tree().current_scene.call('_start_level',3)\nawait get_tree().scene_changed\nreturn {'scene':get_tree().current_scene.scene_file_path,'arrival':get_tree().current_scene.get('ArrivalActive')}")
        assert result['scene'].endswith('level3.tscn') and result['arrival'], result
        # Use an owned temporary PostgreSQL save, never a user's existing campaign, for the actual menu load route.
        temporary_session = evaluate("var s=get_node('/root/SessionManager')\nvar id=s.create_new_session('Cyber Shogun')\nassert(id>0)\ns.update_session(id,3,0,0)\nreturn id")
        result = evaluate(f"get_tree().change_scene_to_file('res://scenes/main_menu/main_menu.tscn')\nawait get_tree().scene_changed\nvar m=get_tree().current_scene\nvar popup=m.get_node('ConfirmPopup')\npopup.set_meta('delete_mode','none')\npopup.set_meta('session_id',{temporary_session})\npopup.set_meta('session_data',get_node('/root/SessionManager').get_session({temporary_session}))\nm.get_node('MenuSessionUI').call('ConfirmAction')\nawait get_tree().scene_changed\nreturn {{'scene':get_tree().current_scene.scene_file_path,'arrival':get_tree().current_scene.get('ArrivalActive')}}")
        assert result['scene'].endswith('level3.tscn') and result['arrival'], result
        # Replay uses the production scorecard handler, reloads mission 3, then reconstructs the arrival.
        result = evaluate("var m=get_tree().current_scene\nm.get_node('NeonRustIntro').call('Finish')\nawait get_tree().process_frame\nvar card=load('res://scenes/ui/cyber_victory_scorecard.tscn').instantiate()\nm.add_child(card)\ncard.call('ShowScorecard',150,4,'KAITO')\ncard.get_node('RootPanel/MainVBox/ButtonsHBox/ReplayBtn').emit_signal('pressed')\nawait get_tree().scene_changed\nreturn {'scene':get_tree().current_scene.scene_file_path,'arrival':get_tree().current_scene.get('ArrivalActive')}")
        assert result['scene'].endswith('level3.tscn') and result['arrival'], result
        records.append({'logs':call('logs_read', {'source':'game','count':300,'include_details':True})})
    finally:
        if temporary_session is not None:
            evaluate(f"get_node('/root/SessionManager').delete_session({temporary_session})\nreturn get_node('/root/SessionManager').get_session({temporary_session}).is_empty()")
        (OUT/'evidence.json').write_text(json.dumps(records, indent=2))
        call('project_manage', {'op':'stop'})
        client.close()


if __name__ == '__main__':
    main()
