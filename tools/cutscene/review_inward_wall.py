"""Review all thirteen Level 4 arrival shots, uninterrupted playback and real launch routes through Godot AI MCP.

Requires a stopped game and preserves the editor scene. An owned temporary save
is created and removed only for the saved-session loading check.
"""
import base64
import json
import os
import time
from pathlib import Path
from mcp_probe import GodotMcp
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'.validation/inward-wall/review'
SHOTS=[5,15,24,34,44,54,64,74,84,94,104,113,118]


def contact_sheets():
    """Reuse the Docker FFmpeg sheet builder with the thirteen-shot timeline."""
    import subprocess
    container=os.environ.get('INWARD_WALL_CONTAINER')
    if not container:
        running=subprocess.check_output(['docker','ps','--format','{{.Names}}'],text=True).splitlines()
        if 'axiom-inward-wall-production' in running: container='axiom-inward-wall-production'
    if container:
        subprocess.run(['docker','exec',container,'mkdir','-p','/workspace/.validation/inward-wall/review'],check=True)
        subprocess.run(['docker','cp',str(OUT)+'/.',container+':/workspace/.validation/inward-wall/review'],check=True)
    for ratio in ['1920x1080','1920x1200','2560x1080']:
        args=(['docker','exec',container,'ffmpeg'] if container else ['docker','compose','run','--rm','--entrypoint','ffmpeg','cutscene'])+['-v','error','-y']
        for t in SHOTS: args+=['-i',f'/workspace/.validation/inward-wall/review/{ratio}_{t}.png']
        filters=[f'[{i}:v]scale=480:270:force_original_aspect_ratio=decrease,pad=480:270:(ow-iw)/2:(oh-ih)/2[v{i}]' for i in range(13)]
        layout='|'.join(f'{i%4*480}_{i//4*270}' for i in range(13))
        filters.append(''.join(f'[v{i}]' for i in range(13))+f'xstack=inputs=13:layout={layout}:fill=black[out]')
        args+=['-filter_complex_threads','1','-filter_complex',';'.join(filters),'-map','[out]','-frames:v','1',f'/workspace/.validation/inward-wall/review/{ratio}_contact.png']
        subprocess.run(args,check=True)
        if container: subprocess.run(['docker','cp',f'{container}:/workspace/.validation/inward-wall/review/{ratio}_contact.png',str(OUT/f'{ratio}_contact.png')],check=True)


def main():
    """Capture thirteen shots at three ratios, sample uninterrupted playback and exercise launch/navigation routes."""
    OUT.mkdir(parents=True, exist_ok=True)
    client = GodotMcp()
    records = []
    temporary_session = None
    started = False

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
        call('project_run', {'mode':'custom','scene':'res://scenes/gameplay/level4.tscn','autosave':False})
        started = True
        state = evaluate("""var m=get_tree().current_scene
assert(get_node('/root/SessionManager').get('CurrentSessionId')<=0)
var c=m.get_node('ShatteredAegisIntro')
c.set_process(false)
c.get_node('Speech').stop()
c.get_node('Atmosphere').stop()
var p=m.get_node('Actors/CyberShogun')
var b=load('res://scripts/managers/LevelCatalogBridge.cs').new()
var result={'arrival':m.get('ArrivalActive'),'position':p.position,'health':p.get('CurrentHealth'),'score':m.get_node('GameManager').get('Points'),'squad':b.UnlockedCount()}
b=null
return result""")
        assert state['arrival'] and state['squad'] == 5, state
        initial = {key:state[key] for key in ['position','health','score']}
        for width, height in [(1920,1080),(1920,1200),(2560,1080)]:
            for seconds in SHOTS:
                evaluate(f"get_window().mode=Window.MODE_WINDOWED\nget_window().size=Vector2i({width},{height})\nvar c=get_tree().current_scene.get_node('ShatteredAegisIntro')\nc.call('Seek',{seconds}.0)\nawait get_tree().process_frame\nawait RenderingServer.frame_post_draw\nreturn {{'elapsed':c.get('Elapsed'),'viewport':get_viewport().get_visible_rect().size,'subtitle':c.get_node('UI/SubtitlePanel/Subtitle').get_global_rect(),'skip':c.get_node('UI/Skip').get_global_rect()}}")
                capture = call('editor_screenshot', {'source':'game','max_resolution':0,'include_image':True})
                for block in capture.get('content', []):
                    if block.get('type') == 'image':
                        (OUT/f'{width}x{height}_{seconds}.png').write_bytes(base64.b64decode(block['data']))
                print('Captured', width, height, seconds, flush=True)
        contact_sheets()
        evaluate("var c=get_tree().current_scene.get_node('ShatteredAegisIntro')\nc.call('SeekAudio',0.0)\nc.set_process(true)\nreturn true")
        for shot in range(12):
            time.sleep(9.5)
            state = evaluate("""var m=get_tree().current_scene
var c=m.get_node('ShatteredAegisIntro')
var p=m.get_node('Actors/CyberShogun')
var frozen=true
for n in m.find_children('*','',true,false):
	if n==c or c.is_ancestor_of(n): continue
	if n.can_process(): frozen=false
return {'elapsed':c.get('Elapsed'),'audio':c.get_node('Speech').get_playback_position(),'position':p.position,'health':p.get('CurrentHealth'),'score':m.get_node('GameManager').get('Points'),'frozen':frozen,'boss':m.get('Boss')!=null,'recruited':m.get_node('Actors/SuryaJoins').get('Triggered'),'wall':m.get_node('PalaceGeometry/BorderWall').get('IsOpen'),'fragments':m.get('MemoryFragmentsFound')}""")
            assert abs(state['elapsed'] - state['audio']) < .3 and state['frozen'], state
            assert all(state[key] == initial[key] for key in initial), state
            assert not state['boss'] and not state['recruited'] and not state['wall'] and state['fragments'] == 0, state
            print('Playback', state, flush=True)
        time.sleep(7)
        state = evaluate("var m=get_tree().current_scene\nreturn {'arrival':m.get('ArrivalActive'),'overlay':m.has_node('ShatteredAegisIntro'),'live':m.get_node('Actors/CyberShogun').can_process(),'hud':m.get_node('MissionHUD').visible,'music':m.get_node('AudioStreamPlayer').stream_paused,'jump':Input.is_action_pressed('jump')}")
        assert not state['arrival'] and not state['overlay'] and state['live'] and state['hud'] and not state['music'] and not state['jump'], state
        # The shared results Continue loads the actual Level 4 scene; no campaign completion/save is fabricated.
        result = evaluate("get_tree().change_scene_to_file('res://scenes/gameplay/level3.tscn')\nawait get_tree().scene_changed\nvar m=get_tree().current_scene\nm.get_node('NeonRustIntro').call('Finish')\nawait get_tree().process_frame\nvar card=m.get_node('CyberVictoryScorecard')\ncard.call('ShowMissionScorecard',150,4,'KAITO',4)\ncard.get_node('RootPanel/MainVBox/ContinueBtn').emit_signal('pressed')\nawait get_tree().scene_changed\nreturn {'scene':get_tree().current_scene.scene_file_path,'arrival':get_tree().current_scene.get('ArrivalActive')}")
        assert result['scene'].endswith('level4.tscn') and result['arrival'], result
        # Exercise mission selection through its production loading screen and catalog path.
        result = evaluate("get_tree().change_scene_to_file('res://scenes/main_menu/level_selection.tscn')\nawait get_tree().scene_changed\nget_tree().current_scene.call('_start_level',4)\nawait get_tree().scene_changed\nreturn {'scene':get_tree().current_scene.scene_file_path,'arrival':get_tree().current_scene.get('ArrivalActive')}")
        assert result['scene'].endswith('level4.tscn') and result['arrival'], result
        # Use an owned temporary PostgreSQL save, never a user's existing campaign, for the actual menu load route.
        temporary_session = evaluate("var s=get_node('/root/SessionManager')\nvar id=s.create_new_session('Cyber Shogun')\nassert(id>0)\ns.update_session(id,4,0,0)\nreturn id")
        result = evaluate(f"get_tree().change_scene_to_file('res://scenes/main_menu/main_menu.tscn')\nawait get_tree().scene_changed\nvar m=get_tree().current_scene\nvar popup=m.get_node('ConfirmPopup')\npopup.set_meta('delete_mode','none')\npopup.set_meta('session_id',{temporary_session})\npopup.set_meta('session_data',get_node('/root/SessionManager').get_session({temporary_session}))\nm.get_node('MenuSessionUI').call('ConfirmAction')\nawait get_tree().scene_changed\nreturn {{'scene':get_tree().current_scene.scene_file_path,'arrival':get_tree().current_scene.get('ArrivalActive')}}")
        assert result['scene'].endswith('level4.tscn') and result['arrival'], result
        # Replay uses the production scorecard handler, reloads mission 4, then reconstructs the arrival.
        result = evaluate("var m=get_tree().current_scene\nm.get_node('ShatteredAegisIntro').call('Finish')\nawait get_tree().process_frame\nvar card=load('res://scenes/ui/cyber_victory_scorecard.tscn').instantiate()\nm.add_child(card)\ncard.call('ShowScorecard',150,4,'KAITO')\ncard.get_node('RootPanel/MainVBox/ButtonsHBox/ReplayBtn').emit_signal('pressed')\nawait get_tree().scene_changed\nreturn {'scene':get_tree().current_scene.scene_file_path,'arrival':get_tree().current_scene.get('ArrivalActive')}")
        assert result['scene'].endswith('level4.tscn') and result['arrival'], result
        records.append({'logs':call('logs_read', {'source':'game','count':300,'include_details':True})})
    finally:
        try:
            if temporary_session is not None:
                deleted = evaluate(f"get_node('/root/SessionManager').delete_session({temporary_session})\nreturn get_node('/root/SessionManager').get_session({temporary_session}).is_empty()")
                assert deleted, 'Owned temporary save was not removed'
        finally:
            (OUT/'evidence.json').write_text(json.dumps(records, indent=2))
            if started: call('project_manage', {'op':'stop'})
            client.close()


if __name__ == '__main__':
    main()
