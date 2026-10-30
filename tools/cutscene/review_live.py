"""Capture the four live shots and aspect ratios through Godot AI's running-game bridge.

Requires a running cutscene (project_run custom, autosave=false). Never changes the
open editor scene. Writes PNGs and structured runtime evidence under .validation.
"""
import base64
import json
from pathlib import Path
from mcp_probe import GodotMcp

OUT = Path(__file__).resolve().parents[2] / '.validation/deployment/review'


def main():
    """Freeze the runtime clock, inspect deterministic shots and record layout evidence."""
    OUT.mkdir(parents=True, exist_ok=True)
    client = GodotMcp()
    records = []
    def call(tool, arguments):
        """Fail quickly on remote errors while retaining the reviewable payload."""
        result = client.call(tool, arguments)['result']
        if result.get('isError'):
            raise RuntimeError(result)
        return result
    def evaluate(code):
        """Evaluate only in the running game; the editor's open scene is unaffected."""
        return call('editor_manage', {'op': 'game_eval', 'params': {'code': code}})
    # Reload only the running scene if natural completion already reached the briefing.
    evaluate("var tree = get_tree()\nif tree.current_scene.scene_file_path != 'res://scenes/main_menu/deployment_cutscene.tscn':\n\ttree.change_scene_to_file('res://scenes/main_menu/deployment_cutscene.tscn')\n\tawait tree.scene_changed\nvar scene = tree.current_scene\nscene.set_process(false)\nscene.narrator.stop()\nscene.transition_to_briefing = false\nreturn scene.name")
    for width, height in [(1920,1080), (1920,1200), (2560,1080)]:
        for seconds in ([5.0,15.0,25.4,35.0] if height == 1080 and width == 1920 else [15.0,35.0]):
            evidence = evaluate(f"var scene = get_tree().current_scene\nget_window().mode = Window.MODE_WINDOWED\nget_window().size = Vector2i({width},{height})\nawait get_tree().process_frame\nscene.seek_visuals({seconds})\nawait get_tree().process_frame\nawait RenderingServer.frame_post_draw\nreturn {{'time': scene.elapsed, 'root': scene.size, 'stage': scene.stage.get_viewport_rect().size, 'subtitle': scene.subtitle.get_global_rect(), 'skip': scene.get_node('UI/Skip').get_global_rect(), 'fps': Performance.get_monitor(Performance.TIME_FPS), 'draw_calls': Performance.get_monitor(Performance.RENDER_TOTAL_DRAW_CALLS_IN_FRAME)}}")
            name = f'{width}x{height}_{seconds:g}'
            result = call('editor_screenshot', {'source':'game','max_resolution':0,'include_image':True})
            for content in result.get('content', []):
                if content.get('type') == 'image':
                    (OUT / f'{name}.png').write_bytes(base64.b64decode(content['data']))
            records.append({'name':name, 'evidence':evidence, 'capture': [x for x in result.get('content', []) if x.get('type') != 'image']})
            print('CAPTURED', name, flush=True)
    records.append({'logs':call('logs_read', {'source':'game','include_details':True,'count':50})})
    (OUT/'evidence.json').write_text(json.dumps(records, indent=2))
    client.close()
    print('Review evidence saved', flush=True)


if __name__ == '__main__':
    main()
