"""Godot AI MCP route/capture review in a separate game; preserves the user's open editor scene.

Blockout review is an assisted geometry check, not a first-playthrough pacing measurement.
The captures mode records dressed sectors at three aspect ratios and the actual runtime diagnostics.
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
    """Check project identity, launch an isolated review, collect evidence and stop only that review game."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--blockout', action='store_true')
    args = parser.parse_args()
    out = ROOT / '.validation/wildfire-gameplay'
    out.mkdir(parents=True, exist_ok=True)
    client = GodotMcp()
    evidence = []

    def call(tool, params):
        """Reject remote failures, including boot-time parse errors, instead of recording an empty success."""
        result = client.call(tool, params)['result']
        if result.get('isError'):
            raise RuntimeError(result)
        return result

    def evaluate(code):
        """Execute only in our running game; never mutate the editor scene."""
        for attempt in range(10):
            result = client.call('editor_manage', {'op': 'game_eval', 'params': {'code': code}})['result']
            if 'EVAL_GAME_NOT_READY' not in json.dumps(result):
                if result.get('isError'):
                    raise RuntimeError(result)
                return result
            time.sleep(1)
        raise RuntimeError('Review game did not become ready')

    identity = call('session_manage', {'op': 'list'})
    assert 'D:/Projects/frog_jumper' in json.dumps(identity)
    assert '"play_state": "stopped"' in json.dumps(identity), 'Refusing to replace an already running user game'
    evidence.append({'identity': identity})
    try:
        evidence.append({'launch': call('project_run', {'mode': 'custom', 'scene': 'res://scenes/gameplay/level2.tscn', 'autosave': False})})
        evidence.append({'arrival': evaluate("var m = get_tree().current_scene\nvar frozen = m.get('ArrivalActive')\nm.get_node('ConcreteWildfireIntro').call('Finish')\nreturn {'initially_locked':frozen,'restored':not m.get('ArrivalActive'),'scene':m.scene_file_path}")})
        if args.blockout:
            evidence.append({'setup': evaluate("var m = get_tree().current_scene\nfor n in m.get_node('PalaceGeometry').get_children():\n\tif n.name.begins_with('DamagedFacade'): n.hide()\nm.get_node('Actors/Artillery').set_physics_process(false)\nreturn {'width':35800,'purpose':'assisted blockout, not pacing'}")})
            # Each section starts at its preceding stable landing. Actual movement, gravity and collision remain active.
            # Combat is cleared diagnostically to isolate geometry; objective/ability/recovery semantics have unit coverage.
            for operative, begin, end in [('Cyber Shogun', 200, 5550), ('Oceanus', 5800, 11000),
                                          ('Oceanus', 11000, 16100), ('Oceanus', 16200, 24200),
                                          ('Apex Titan', 24850, 28100), ('Apex Titan', 28400, 35000)]:
                code = f'''var m = get_tree().current_scene
var p = m.get_node('Actors/CyberShogun')
var bridge = load('res://scripts/managers/LevelCatalogBridge.cs').new()
bridge.UnlockOperative('{operative}')
p.call('ResetAtCheckpoint', Vector2({begin},554))
p.call('SwapOperative','{operative}')
var start = Time.get_ticks_msec()
var peak_y = p.position.y
var falls = 0
for frame in range(7200):
\tfor encounter in m.get_node('Actors').get_children():
\t\tif encounter.name.begins_with('Encounter_'):
\t\t\tfor enemy in encounter.get_children():
\t\t\t\tif enemy.has_method('TakeDamage'): enemy.call('TakeDamage',1000.0)
\tfor gate in get_tree().get_nodes_in_group('ability_gates'):
\t\tif gate.get('Kind') == 0: gate.call('ReceiveAbility','douse',p.position,1.0)
\t\telif gate.get('Kind') == 1 and '{operative}' == 'Apex Titan': gate.call('ReceiveAbility','slam',p.position,1.0)
\tvar space = p.get_world_2d().direct_space_state
\tvar ahead = p.position + Vector2(75,46)
\tvar ground = space.intersect_ray(PhysicsRayQueryParameters2D.create(ahead+Vector2(0,-5),ahead+Vector2(0,140),1))
\tvar obstacle = p.is_on_wall()
\tvar can_jump_gap = false
\tif ground.is_empty() and p.is_on_floor():
\t\tfor deck in m.get_node('PalaceGeometry').get_children():
\t\t\tif deck.get('Size') == null: continue
\t\t\tvar left = deck.position.x
\t\t\tif left > p.position.x and left < p.position.x + 235 and absf(deck.position.y - (p.position.y + 46)) < 160: can_jump_gap = true
\tif p.is_on_floor() and (obstacle or can_jump_gap): Input.action_press('jump')
\telse: Input.action_release('jump')
\tif ground.is_empty() and p.is_on_floor() and not can_jump_gap: Input.action_release('right')
\telse: Input.action_press('right')
\tawait get_tree().physics_frame
\tpeak_y = minf(peak_y,p.position.y)
\tif p.position.y > 1100: falls += 1
\tif p.position.x >= {end}: break
Input.action_release('right')
Input.action_release('jump')
return {{'operative':'{operative}','from':{begin},'target':{end},'position':p.position,'reached':p.position.x >= {end},'seconds':(Time.get_ticks_msec()-start)/1000.0,'peak_y':peak_y,'fall_frames':falls}}'''
                setup, tick = code.split('var start =', 1)
                evaluate(setup + '\nreturn true')
                # MCP deliberately caps evals at eight seconds; bound each batch to five seconds of physics.
                tick = "var m=get_tree().current_scene\nvar p=m.get_node('Actors/CyberShogun')\nvar start =" + tick.replace('range(7200)', 'range(300)')
                records = []
                for batch in range(32):
                    record = evaluate(tick)
                    records.append(record)
                    payload = json.dumps(record)
                    if '"reached": true' in payload or '\\"reached\\":true' in payload or '\\"reached\\": true' in payload:
                        break
                evidence.append({'route': records})
                print('ROUTE', operative, begin, end, json.dumps(records[-1]), flush=True)
        else:
            evaluate("for frame in range(300):\n\tawait get_tree().physics_frame\nreturn true")
            for width, height in [(1920, 1080), (1920, 1200), (2560, 1080)]:
                for sector, x, y in [('road', 2900, 554), ('shelter', 9820, 554), ('overpass', 13400, 400),
                                      ('spires', 20500, 224), ('breach', 25400, 554), ('battery', 30500, 554)]:
                    evidence.append({'view': evaluate(f"get_window().size=Vector2i({width},{height})\nvar p=get_tree().current_scene.get_node('Actors/CyberShogun')\np.call('ResetAtCheckpoint',Vector2({x},{y}))\np.get_node('Camera2D').reset_smoothing()\nawait get_tree().physics_frame\nawait RenderingServer.frame_post_draw\nreturn {{'viewport':get_viewport().get_visible_rect().size,'position':p.position}}")})
                    result = call('editor_screenshot', {'source': 'game', 'max_resolution': 0, 'include_image': True})
                    for block in result.get('content', []):
                        if block.get('type') == 'image':
                            (out / f'{sector}-{width}x{height}.png').write_bytes(base64.b64decode(block['data']))
                    print('CAPTURE', sector, width, height, flush=True)
        evidence.append({'logs': call('logs_read', {'source': 'game', 'count': 200, 'include_details': True})})
    finally:
        (out / ('blockout.json' if args.blockout else 'captures.json')).write_text(json.dumps(evidence, indent=2))
        call('project_manage', {'op': 'stop'})
        client.close()


if __name__ == '__main__':
    main()
