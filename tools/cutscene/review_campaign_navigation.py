"""Exercise real scene navigation through Godot AI MCP using one owned temporary PostgreSQL campaign.

Results are shown diagnostically to isolate buttons from combat. No objective
events are injected and this is not a mission-traversal acceptance run. Only the
temporary resume fixture is written; existing campaigns are never selected.
"""
import json
import subprocess
import time
import uuid
from pathlib import Path
from mcp_probe import GodotMcp

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / '.validation/later-campaign/navigation.json'


def fixture(character, remove=False):
    """Create/remove an exactly named owned fixture using the backend's existing database connection."""
    code = "import os;os.environ.setdefault('DJANGO_SETTINGS_MODULE','core.settings');import django;django.setup();from django.db import connection;import sys;c=connection.cursor();"
    if remove:
        code += "c.execute('DELETE FROM game_sessions WHERE character=%s',[sys.argv[1]]);print(c.rowcount)"
    else:
        code += "c.execute(\"INSERT INTO game_sessions(character,current_level,total_score,collectables,created_at,last_played) VALUES (%s,9,0,0,'2026-11-05T09:00:00','2026-11-05T09:00:00') RETURNING id\",[sys.argv[1]]);print(c.fetchone()[0])"
    output = subprocess.check_output(['docker', 'compose', 'exec', '-T', 'backend', 'python', '-c', code, character], cwd=ROOT, text=True)
    return int(output.strip().splitlines()[-1])


def main():
    """Review Continue, both acts, all prologues, Replay, Level Select, final menu and fixture Resume."""
    client = GodotMcp()
    records = []
    character = 'repair-navigation:' + str(uuid.uuid4())
    session_id = fixture(character)

    def call(name, arguments):
        """Keep runtime tool errors visible; allow a bounded initial game-bridge startup delay."""
        for attempt in range(15):
            result = client.call(name, arguments)['result']
            if 'EVAL_GAME_NOT_READY' in json.dumps(result):
                time.sleep(1)
                continue
            if result.get('isError'):
                raise RuntimeError(result)
            return result.get('structuredContent', {}).get('result', result)
        raise RuntimeError('Game bridge startup timed out')

    def evaluate(code):
        """Run diagnostics in the separately launched game only."""
        return call('editor_manage', {'op': 'game_eval', 'params': {'code': code}})

    def wait_scene(expected):
        """Assert actual scene replacement after deferred input/navigation work finishes."""
        for attempt in range(30):
            current = evaluate("return get_tree().current_scene.scene_file_path")
            if current == expected:
                records.append({'scene': current})
                OUT.write_text(json.dumps(records, indent=2))
                print('NAVIGATED', expected, flush=True)
                return
            time.sleep(.2)
        raise AssertionError((expected, current))

    def card(method):
        """Show the mission-owned results UI without falsely claiming gameplay completion."""
        return evaluate("var c = get_tree().current_scene.get_node('CyberVictoryScorecard')\nc.call(" + method + ")\nreturn true")

    def press(path):
        """Emit the real button signal and let its production navigation handler run."""
        return evaluate("get_tree().current_scene.get_node(" + repr(path) + ").emit_signal('pressed')\nreturn true")

    def finish_intro():
        """Invoke the cinematic's normal completion boundary, then verify the gameplay scene independently."""
        evaluate("get_tree().current_scene.call('Finish')\nreturn true")

    try:
        assert 'D:/Projects/frog_jumper' in json.dumps(call('session_manage', {'op': 'list'}))
        try:
            call('project_run', {'mode': 'custom', 'scene': 'res://scenes/gameplay/level4.tscn', 'autosave': False})
        except RuntimeError as error:
            if 'timed out' not in str(error):
                raise
        wait_scene('res://scenes/gameplay/level4.tscn')
        assert evaluate("return get_node('/root/SessionManager').get('CurrentSessionId')") <= 0
        card("'ShowActScorecard', 100, 2, 'KAITO'")
        press('CyberVictoryScorecard/RootPanel/MainVBox/PreviewBtn')
        wait_scene('res://scenes/cutscenes/act_ii_opening.tscn')
        finish_intro()
        wait_scene('res://scenes/gameplay/level5.tscn')
        for number, intro in [(6, 'cyber_grave'), (7, 'foundry_forge')]:
            card(f"'ShowMissionScorecard', 100, 2, 'KAITO', {number}")
            press('CyberVictoryScorecard/RootPanel/MainVBox/ContinueBtn')
            wait_scene(f'res://scenes/cutscenes/{intro}_intro.tscn')
            finish_intro()
            wait_scene(f'res://scenes/gameplay/level{number}.tscn')
            if number == 6:
                card("'ShowMissionScorecard',100,2,'KAITO',7")
                press('CyberVictoryScorecard/RootPanel/MainVBox/ButtonsHBox/ReplayBtn')
                wait_scene('res://scenes/gameplay/level6.tscn')
        card("'ShowActIIScorecard',100,2,'KAITO'")
        press('CyberVictoryScorecard/RootPanel/MainVBox/PreviewBtn')
        wait_scene('res://scenes/ui/act_ii_end_screen.tscn')
        evaluate("get_tree().current_scene.find_child('ProceedButton',true,false).emit_signal('pressed')\nreturn true")
        wait_scene('res://scenes/ui/act_iii_start_screen.tscn')
        evaluate("get_tree().current_scene.find_child('StartButton',true,false).emit_signal('pressed')\nreturn true")
        families = ['stratos_docks', 'spire_outer_hull', 'cyber_nexus', 'apex_bastion', 'judgment_day']
        for number, family in zip(range(8, 13), families):
            wait_scene(f'res://scenes/cutscenes/{family}_intro.tscn')
            finish_intro()
            wait_scene(f'res://scenes/gameplay/level{number}.tscn')
            if number < 12:
                card(f"'ShowMissionScorecard',100,2,'KAITO',{number + 1}")
                press('CyberVictoryScorecard/RootPanel/MainVBox/ContinueBtn')
        card("'ShowCampaignCompleteScorecard',100,2,'KAITO'")
        assert evaluate("return get_tree().current_scene.get_node('CyberVictoryScorecard/RootPanel/MainVBox/PreviewBtn').text") == 'RETURN TO MAIN MENU'
        press('CyberVictoryScorecard/RootPanel/MainVBox/PreviewBtn')
        wait_scene('res://scenes/main_menu/main_menu.tscn')
        # Actual Resume confirmation uses only the owned campaign; its metadata matches the stored row.
        evaluate(f"var m = get_tree().current_scene\nvar p = m.get_node('ConfirmPopup')\np.set_meta('delete_mode','none')\np.set_meta('session_id',{session_id})\np.set_meta('session_data',{{'current_level':9,'character':'Cyber Shogun'}})\nm.get_node('MenuSessionUI').call('ConfirmAction')\nreturn true")
        wait_scene('res://scenes/gameplay/level9.tscn')
        records.append({'resume_id': session_id, 'direct_entry': True})
        card("'ShowMissionScorecard',100,2,'KAITO',10")
        press('CyberVictoryScorecard/RootPanel/MainVBox/ButtonsHBox/LevelSelectBtn')
        wait_scene('res://scenes/main_menu/level_selection.tscn')
        evaluate("get_tree().current_scene.call('_on_level_9_pressed')\nreturn true")
        wait_scene('res://scenes/gameplay/level9.tscn')
        records.append({'level_select_direct_entry': True, 'mission_13': False})
        OUT.write_text(json.dumps(records, indent=2))
    finally:
        call('project_manage', {'op': 'stop'})
        client.close()
        fixture(character, remove=True)


if __name__ == '__main__':
    main()
