"""Capture every later cinematic cue at the required aspect ratios through Godot AI MCP.

Uses a separate review game, leaves the user's editor scene intact, and records
actual viewport/layout data beside lossless native screenshots. Seeking verifies
composition only; engine lifecycle tests and audio decoding cover other concerns.
"""
import json
import argparse
import time
from pathlib import Path
from mcp_probe import GodotMcp

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / '.validation/later-campaign/review'
FAMILIES = dict(CyberGrave='cyber_grave', FoundryForge='foundry_forge',
                StratosDocks='stratos_docks', SpireOuterHull='spire_outer_hull',
                CyberNexus='cyber_nexus', ApexBastion='apex_bastion', JudgmentDay='judgment_day')


def main():
    """Verify identity before launching and always stop only the owned review game."""
    OUT.mkdir(parents=True, exist_ok=True)
    client = GodotMcp()
    records = []
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--reuse', action='store_true', help='Resume the already launched owned review game')
    parser.add_argument('--only', nargs='*', help='Recheck specified overlays after a focused visual fix')
    arguments = parser.parse_args()

    def call(name, arguments):
        """Fail visibly on MCP errors; permit the bridge's bounded startup delay."""
        for attempt in range(15):
            result = client.call(name, arguments)['result']
            if 'EVAL_GAME_NOT_READY' not in json.dumps(result):
                if result.get('isError'):
                    raise RuntimeError(result)
                return result
            time.sleep(1)
        raise RuntimeError('Review game bridge did not become ready')

    def evaluate(code):
        """Execute review-only GDScript in the owned game, never in the editor."""
        return call('editor_manage', {'op': 'game_eval', 'params': {'code': code}})

    identity = call('session_manage', {'op': 'list'})
    assert 'D:/Projects/frog_jumper' in json.dumps(identity)
    if not arguments.reuse:
        call('project_run', {'mode': 'custom', 'scene': 'res://scenes/gameplay/level1.tscn', 'autosave': False})
    try:
        evaluate("var old = get_tree().current_scene\nvar host = Node2D.new()\nhost.name = 'CinematicReview'\nget_tree().root.add_child(host)\nget_tree().current_scene = host\nold.queue_free()\nawait get_tree().process_frame\nreturn {'review':host.name}")
        for prefix, family in FAMILIES.items():
            for suffix in ['Intro', 'Aftermath']:
                name = prefix + suffix
                if arguments.only and name not in arguments.only:
                    continue
                scene = family + ('_intro' if suffix == 'Intro' else '_aftermath')
                cues = json.loads((ROOT / 'assets/cutscenes' / scene / 'cues.json').read_text())
                if isinstance(cues, dict):
                    cues = cues['cues']
                times = [round(float(c['start']) + 1.5, 2) for c in cues]
                evaluate(f"var c = load('res://scripts/ui/{name}.cs').new()\nc.name = 'ReviewOverlay'\nc.set('SpeechPath','')\nif '{suffix}' == 'Intro': c.set('EnterMissionOnFinish',false)\nget_tree().current_scene.add_child(c)\nc.set_process(false)\nfor child in c.get_children():\n\tif child is AudioStreamPlayer: child.stop()\nreturn true")
                captures = []
                for width, height in [(1920,1080),(1920,1200),(2560,1080),(800,600)]:
                    evaluate(f"get_window().mode = Window.MODE_WINDOWED\nget_window().size = Vector2i({width},{height})\nawait get_tree().process_frame\nawait get_tree().process_frame\nreturn true")
                    for seconds in times:
                        filename = OUT / f'{name}_{width}x{height}_{seconds:05.1f}.png'
                        code = f"""var c = get_tree().current_scene.get_node('ReviewOverlay')
c.call('Seek', {seconds})
await get_tree().process_frame
await RenderingServer.frame_post_draw
var error = get_viewport().get_texture().get_image().save_png('{filename.as_posix()}')
var label = c.find_child('Subtitle',true,false)
var skip = c.find_child('Skip',true,false)
if skip == null: skip = c.find_child('SkipButton',true,false)
return {{'seconds':{seconds}, 'viewport':get_viewport().get_visible_rect().size,'subtitle':str(label.get_global_rect()) if label else 'missing','text':label.text if label else '', 'skip':str(skip.get_global_rect()) if skip else 'missing','saved':error}}"""
                        captures.append({'file':filename.name,'evidence':evaluate(code)})
                evaluate("get_tree().current_scene.get_node('ReviewOverlay').call('Finish')\nawait get_tree().process_frame\nawait get_tree().process_frame\nreturn true")
                records.append({'overlay':name,'captures':captures})
                (OUT / ('evidence-recheck.json' if arguments.only else 'evidence.json')).write_text(json.dumps(records, indent=2))
                print('CAPTURED', name, len(captures), flush=True)
        records.append({'logs': call('logs_read', {'source': 'game', 'count': 100, 'include_details': True})})
        (OUT / ('evidence-recheck.json' if arguments.only else 'evidence.json')).write_text(json.dumps(records, indent=2))
    finally:
        call('project_manage', {'op': 'stop'})
        client.close()


if __name__ == '__main__':
    main()
