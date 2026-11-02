"""Review standalone Act II playback, all shot/aspect layouts and real results navigation via local Godot MCP.

No saves are created or modified. Requires a stopped game and no active session;
keeps the user's editor scene intact. Diagnostic seeks are separate from the
uninterrupted 120-second playback measurement.
"""
import base64
import json
import sys
import time
from pathlib import Path
from mcp_probe import GodotMcp

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT/'.validation/unregistered/review'
SHOTS = [4,14,26,38,50,62,74,86,98,108,112,117]


def main():
    """Capture native viewport evidence, verify runtime isolation and exercise replay/menu/Skip input routes."""
    OUT.mkdir(parents=True,exist_ok=True)
    client = GodotMcp()
    records = []
    started = False

    def call(tool, params):
        """Allow bounded game startup; propagate tool failures into the review log."""
        for attempt in range(15):
            result = client.call(tool,params)['result']
            if 'EVAL_GAME_NOT_READY' not in json.dumps(result):
                break
            time.sleep(1)
        if result.get('isError'):
            raise RuntimeError(result)
        return result

    def evaluate(code):
        """Retain exact diagnostic code and structured state for reproducibility."""
        result = call('editor_manage',{'op':'game_eval','params':{'code':code}})
        records.append(dict(code=code,result=result))
        data = result.get('structuredContent',{})
        if 'result' not in data:
            data = json.loads(result['content'][0]['text'])
        return data.get('result')

    def capture(name):
        """Save full-resolution game viewport images without changing the open editor scene."""
        result = call('editor_screenshot',{'source':'game','max_resolution':0,'include_image':True})
        for block in result.get('content',[]):
            if block.get('type') == 'image':
                (OUT/f'{name}.png').write_bytes(base64.b64decode(block['data']))

    snapshot = """var b=load('res://scripts/managers/LevelCatalogBridge.cs').new()
var g=get_node('/root/Global')
var state=[b.Count(),b.UnlockedCount(),b.ActiveOperative(),g.unlocked_levels,g.selected_character,get_node('/root/SessionManager').get('CurrentSessionId')]
b=null
return state"""
    identity = call('session_manage',{'op':'list'})
    assert 'D:/Projects/frog_jumper' in json.dumps(identity)
    assert '"play_state": "stopped"' in json.dumps(identity), 'Preserve a running game'
    try:
        started = True
        try:
            call('project_run',{'mode':'custom','scene':'res://scenes/cutscenes/act_ii_opening.tscn','autosave':False})
        except RuntimeError as error:
            # The editor may finish launching after its eight-second command deadline.
            # Continue only when this owned launch is confirmed; never start a second game.
            if 'timed out' not in str(error):
                raise
            time.sleep(2)
            assert '"play_state": "playing"' in json.dumps(call('session_manage',{'op':'list'}))
        assert evaluate("return get_tree().current_scene.scene_file_path").endswith('act_ii_opening.tscn')
        before = evaluate(snapshot)
        assert before[0] == 4 and before[-1] <= 0, before
        evaluate("var c=get_tree().current_scene\nc.set_process(false)\nc.get_node('Speech').stop()\nc.get_node('Atmosphere').stop()\nreturn true")
        # Reuse already inspected captures when rechecking a review-harness failure.
        ratios = [] if '--reuse-captures' in sys.argv else [(1920,1080),(1920,1200),(2560,1080)]
        for width,height in ratios:
            for seconds in SHOTS:
                state = evaluate(f"""get_window().mode=Window.MODE_WINDOWED
get_window().size=Vector2i({width},{height})
var c=get_tree().current_scene
c.call('Seek',{seconds}.0)
await get_tree().process_frame
await RenderingServer.frame_post_draw
return {{'viewport':get_viewport().get_visible_rect().size,'subtitle':c.get_node('UI/SubtitlePanel/Subtitle').text,'skip':c.get_node('UI/Skip').get_global_rect()}}""")
                capture(f'{width}x{height}_{seconds}')
                print('Captured',width,height,seconds,flush=True)
        if '--navigation-only' not in sys.argv:
            evaluate("var c=get_tree().current_scene\nc.call('SeekAudio',0.0)\nc.set_process(true)\nreturn true")
            start = time.monotonic()
            for sample in range(12):
                time.sleep(9.8)
                state = evaluate("var c=get_tree().current_scene\nreturn {'elapsed':c.get('Elapsed'),'audio':c.get_node('Speech').get_playback_position(),'finished':c.get('HasFinished'),'subtitle':c.get_node('UI/SubtitlePanel/Subtitle').text}")
                if state['finished']:
                    # MCP calls contribute wall time; completion can occur before the twelfth poll.
                    assert time.monotonic()-start >= 119 and state['elapsed'] == 120, state
                    break
                assert abs(state['elapsed']-state['audio']) < .3, state
                assert evaluate(snapshot) == before
                print('Playback',state,flush=True)
            time.sleep(3)
            state = evaluate("var c=get_tree().current_scene\nreturn {'finished':c.get('HasFinished'),'complete':c.get_node('Complete').visible,'speech':c.get_node('Speech').playing,'atmosphere':c.get_node('Atmosphere').playing}")
            assert state == dict(finished=True,complete=True,speech=False,atmosphere=False), state
            records.append(dict(uninterrupted_wall_seconds=time.monotonic()-start))
            assert evaluate(snapshot) == before
        else:
            # Keep previously completed full-playback evidence when isolating destination checks.
            evaluate("get_tree().current_scene.call('Finish')\nawait get_tree().process_frame\nreturn true")
            records.append(dict(review='Navigation-only follow-up; full playback retained separately'))
        for width,height in [(1920,1080),(1920,1200),(2560,1080)]:
            evaluate(f'get_window().size=Vector2i({width},{height})\nawait get_tree().process_frame\nawait RenderingServer.frame_post_draw\nreturn true')
            capture(f'{width}x{height}_complete')
        # Replay's production callback creates a fresh preview without a gameplay/session call.
        evaluate("get_tree().current_scene.get_node('Complete/Replay').emit_signal('pressed')\nawait get_tree().scene_changed\nreturn get_tree().current_scene.scene_file_path")
        assert evaluate(snapshot) == before
        state = evaluate("var c=get_tree().current_scene\nc.call('SeekAudio',60.0)\nvar e=InputEventJoypadButton.new()\ne.pressed=true\ne.button_index=JOY_BUTTON_START\nc.call('_Input',e)\nc.call('_Input',e)\nawait get_tree().process_frame\nreturn c.get_node('Complete').visible")
        assert state
        # Use a real pointer event to leave completion through the production Main Menu route.
        state = evaluate("var c=get_tree().current_scene\nvar e=InputEventMouseButton.new()\ne.pressed=true\ne.button_index=MOUSE_BUTTON_LEFT\ne.position=c.get_node('Complete/MainMenu').get_global_rect().get_center()\nc.call('_Input',e)\nawait get_tree().scene_changed\nreturn get_tree().current_scene.scene_file_path")
        assert state.endswith('main_menu.tscn'),state
        # Build the existing final mission results; the diagnostic display does not complete/save the mission.
        evaluate("get_tree().change_scene_to_file('res://scenes/gameplay/level4.tscn')\nawait get_tree().scene_changed\nvar m=get_tree().current_scene\nm.get_node('ShatteredAegisIntro').call('Finish')\nawait get_tree().process_frame\nm.process_mode=Node.PROCESS_MODE_DISABLED\nvar card=m.get_node('CyberVictoryScorecard')\ncard.process_mode=Node.PROCESS_MODE_ALWAYS\ncard.call('ShowActScorecard',150,4,'KAITO')\nreturn card.get_node('RootPanel/MainVBox/StatsGrid/ScoreLabel').text")
        before_results = evaluate(snapshot)
        capture('act_i_results')
        state = evaluate("var card=get_tree().current_scene.get_node('CyberVictoryScorecard')\ncard.get_node('RootPanel/MainVBox/PreviewBtn').emit_signal('pressed')\nawait get_tree().scene_changed\nreturn get_tree().current_scene.scene_file_path")
        assert state.endswith('act_ii_opening.tscn'),state
        assert evaluate(snapshot) == before_results
        state = evaluate("var c=get_tree().current_scene\nvar e=InputEventKey.new()\ne.pressed=true\ne.keycode=KEY_ESCAPE\nc.call('_Input',e)\nc.call('Finish')\nawait get_tree().process_frame\nreturn {'complete':c.get_node('Complete').visible,'speech':c.get_node('Speech').playing}")
        assert state == dict(complete=True,speech=False),state
        assert evaluate(snapshot) == before_results
        records.append({'logs':call('logs_read',{'source':'game','count':300,'include_details':True})})
    finally:
        (OUT/'evidence.json').write_text(json.dumps(records,indent=2))
        if started:
            call('project_manage',{'op':'stop'})
        client.close()


if __name__ == '__main__':
    main()
