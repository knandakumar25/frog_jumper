"""Capture six arrival shots and three aspect ratios through Godot AI MCP.

Starts a separate Level 2 game with autosave=false; never changes the open editor scene.
Freezes only the cinematic review clock and stores shot/layout/handoff evidence.
"""
import base64
import json
import time
import argparse
from pathlib import Path

from mcp_probe import GodotMcp

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / '.validation/wildfire/review'


def main():
    """Verify project identity, run extraction and capture deterministic compositions plus runtime diagnostics."""
    OUT.mkdir(parents=True, exist_ok=True)
    client = GodotMcp()
    records = []
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--reuse', action='store_true', help='Use an already-running Level 2 review game')
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
        call('project_run', {'mode': 'custom', 'scene': 'res://scenes/gameplay/level2.tscn', 'autosave': False})
    # The game bridge becomes available after project_run; its eval operation waits for readiness.
    records.append({'initial': evaluate("var m = get_tree().current_scene\nvar c = m.get_node('ConcreteWildfireIntro')\nc.set_process(false)\nc.get_node('Speech').stop()\nc.get_node('Atmosphere').stop()\nreturn {'mission':m.scene_file_path,'locked':m.get('ArrivalActive'),'player_processing':m.get_node('Actors/CyberShogun').can_process(),'hud':m.get_node('MissionHUD').visible}")})
    for width, height in [(1920, 1080), (1920, 1200), (2560, 1080)]:
        for seconds in ([4.0, 14.0, 24.0, 34.0, 44.0, 54.0] if width == 1920 and height == 1080 else [14.0, 34.0]):
            evidence = evaluate(f"get_window().mode = Window.MODE_WINDOWED\nget_window().size = Vector2i({width},{height})\nvar c = get_tree().current_scene.get_node('ConcreteWildfireIntro')\nc.call('Seek',{seconds})\nawait get_tree().process_frame\nawait RenderingServer.frame_post_draw\nreturn {{'time':c.get('Elapsed'),'subtitle':c.get_node('UI/SubtitlePanel/Subtitle').get_global_rect(),'skip':c.get_node('UI/Skip').get_global_rect(),'viewport':get_viewport().get_visible_rect().size}}")
            name = f'{width}x{height}_{seconds:g}'
            capture = call('editor_screenshot', {'source': 'game', 'max_resolution': 0, 'include_image': True})
            for block in capture.get('content', []):
                if block.get('type') == 'image':
                    (OUT / f'{name}.png').write_bytes(base64.b64decode(block['data']))
            records.append({'name': name, 'evidence': evidence})
            print('CAPTURED', name, flush=True)
    records.append({'handoff': evaluate("var m = get_tree().current_scene\nvar c = m.get_node('ConcreteWildfireIntro')\nc.call('Finish')\nc.call('Finish')\nreturn {'mission':m.scene_file_path,'locked':m.get('ArrivalActive'),'player_processing':m.get_node('Actors/CyberShogun').can_process(),'hud':m.get_node('MissionHUD').visible,'pause':m.get_node('CanvasLayer/PauseMenu').can_process()}")})
    records.append({'logs': call('logs_read', {'source': 'game', 'count': 100, 'include_details': True})})
    (OUT / 'evidence.json').write_text(json.dumps(records, indent=2))
    call('project_manage', {'op': 'stop'})
    client.close()


if __name__ == '__main__':
    main()
