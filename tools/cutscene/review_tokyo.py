"""Capture six epilogue shots and three aspect ratios through Godot AI MCP.

Starts a separate Level 1 game with autosave=false; never changes the open editor scene.
Completes real objective prerequisites, freezes only the review clock and stores local evidence.
"""
import base64
import json
import time
import argparse
from pathlib import Path

from mcp_probe import GodotMcp

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / '.validation/tokyo/review'


def main():
    """Verify project identity, run extraction and capture deterministic compositions plus runtime diagnostics."""
    OUT.mkdir(parents=True, exist_ok=True)
    client = GodotMcp()
    records = []
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--reuse', action='store_true', help='Use this tool\'s already-running Level 1 game')
    args = parser.parse_args()

    def call(tool, args):
        """Keep remote errors visible and prevent subsequent captures from silently testing the wrong scene."""
        result = client.call(tool, args)['result']
        for _attempt in range(12):
            if 'EVAL_GAME_NOT_READY' not in json.dumps(result):
                break
            time.sleep(1)
            result = client.call(tool, args)['result']
        if result.get('isError'):
            raise RuntimeError(result)
        return result

    def evaluate(code):
        """Execute review code exclusively in the running game."""
        return call('editor_manage', {'op': 'game_eval', 'params': {'code': code}})

    identity = call('session_manage', {'op': 'list'})
    assert 'frog_jumper' in json.dumps(identity) and 'D:/Projects/frog_jumper' in json.dumps(identity)
    if not args.reuse:
        call('project_run', {'mode': 'custom', 'scene': 'res://scenes/gameplay/level1.tscn', 'autosave': False})
    # The game bridge becomes available after project_run; its eval operation waits for readiness.
    extraction_code = "var m = get_tree().current_scene\nvar p = m.get_node('Actors/CyberShogun')\nvar b = m.get_node('Actors/DemolitionBeacon')\nm.get_node('GameManager').set('Points',150)\np.emit_signal('ComboChanged',4)\np.global_position = b.global_position + Vector2(-200,-50)\np.emit_signal('ParryDeflected',p)\np.emit_signal('ParryDeflected',p)\nb.call('TakeDamage',1000.0)\np.global_position = m.get_node('Actors/DataBlade').global_position + Vector2(0,-60)\nm.call('CollectNearbyObjectives')\np.global_position = m.get_node('Actors/ExtractionGate').global_position + Vector2(0,-80)\nm.call('CollectNearbyObjectives')\nawait get_tree().process_frame\nvar c = m.get_node('TokyoRemembers')\nc.set_process(false)\nc.get_node('Speech').stop()\nc.get_node('Atmosphere').stop()\nreturn {'mission':m.scene_file_path,'complete':m.get('MissionComplete'),'player_processing':p.can_process(),'health':p.get('CurrentHealth'),'score':m.get_node('GameManager').get('Points')}"
    records.append({'extraction': evaluate(extraction_code)})
    for width, height in [(1920, 1080), (1920, 1200), (2560, 1080)]:
        for seconds in ([4.0, 11.0, 19.0, 26.0, 33.0, 38.5] if width == 1920 and height == 1080 else [11.0, 33.0]):
            evidence = evaluate(f"get_window().mode = Window.MODE_WINDOWED\nget_window().size = Vector2i({width},{height})\nvar c = get_tree().current_scene.get_node('TokyoRemembers')\nc.call('Seek',{seconds})\nawait get_tree().process_frame\nawait RenderingServer.frame_post_draw\nreturn {{'time':c.get('Elapsed'),'subtitle':c.get_node('UI/SubtitlePanel/Subtitle').get_global_rect(),'skip':c.get_node('UI/Skip').get_global_rect(),'viewport':get_viewport().get_visible_rect().size}}")
            name = f'{width}x{height}_{seconds:g}'
            capture = call('editor_screenshot', {'source': 'game', 'max_resolution': 0, 'include_image': True})
            for block in capture.get('content', []):
                if block.get('type') == 'image':
                    (OUT / f'{name}.png').write_bytes(base64.b64decode(block['data']))
            records.append({'name': name, 'evidence': evidence})
            print('CAPTURED', name, flush=True)
    records.append({'results': evaluate("var m = get_tree().current_scene\nm.get_node('TokyoRemembers').call('Finish')\nawait get_tree().process_frame\nreturn {'mission':m.scene_file_path,'results':m.get_node('CyberVictoryScorecard/RootPanel').visible,'continue':m.get_node('CyberVictoryScorecard/RootPanel/MainVBox/ContinueBtn').visible,'score':m.get_node('CyberVictoryScorecard/RootPanel/MainVBox/StatsGrid/ScoreLabel').text}")})
    records.append({'replay': evaluate("get_tree().current_scene.get_node('CyberVictoryScorecard/RootPanel/MainVBox/ButtonsHBox/ReplayBtn').emit_signal('pressed')\nawait get_tree().scene_changed\nreturn {'mission':get_tree().current_scene.scene_file_path,'player_processing':get_tree().current_scene.get_node('Actors/CyberShogun').can_process()}")})
    records.append({'replay_extraction': evaluate(extraction_code)})
    records.append({'continue': evaluate("var c = get_tree().current_scene.get_node('TokyoRemembers')\nc.call('SeekAudio',39.5)\nc.set_process(true)\nawait get_tree().create_timer(1.2).timeout\nget_tree().current_scene.get_node('CyberVictoryScorecard/RootPanel/MainVBox/ContinueBtn').emit_signal('pressed')\nawait get_tree().scene_changed\nvar catalog = load('res://scripts/managers/LevelCatalogBridge.cs').new()\nvar result = {'mission':get_tree().current_scene.scene_file_path,'squad_count':catalog.UnlockedCount(),'player_processing':get_tree().current_scene.get_node('Actors/CyberShogun').can_process()}\ncatalog = null\nreturn result")})
    records.append({'level_select': evaluate("var card = load('res://scenes/ui/cyber_victory_scorecard.tscn').instantiate()\nget_tree().current_scene.add_child(card)\ncard.call('ShowScorecard',0,1,'KAITO')\ncard.get_node('RootPanel/MainVBox/ButtonsHBox/LevelSelectBtn').emit_signal('pressed')\nawait get_tree().scene_changed\nreturn get_tree().current_scene.scene_file_path")})
    records.append({'main_menu': evaluate("var card = load('res://scenes/ui/cyber_victory_scorecard.tscn').instantiate()\nget_tree().current_scene.add_child(card)\ncard.call('ShowScorecard',0,1,'KAITO')\ncard.get_node('RootPanel/MainVBox/ButtonsHBox/MainMenuBtn').emit_signal('pressed')\nawait get_tree().scene_changed\nreturn get_tree().current_scene.scene_file_path")})
    records.append({'logs': call('logs_read', {'source': 'game', 'count': 100, 'include_details': True})})
    (OUT / 'evidence.json').write_text(json.dumps(records, indent=2))
    call('project_manage', {'op': 'stop'})
    client.close()


if __name__ == '__main__':
    main()
