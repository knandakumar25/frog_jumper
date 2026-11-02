"""Capture Levels 1–3 guidance through Godot AI MCP without editing the user's scene or saves.

Diagnostic placement and damage isolate visual states; this is not a human pacing run.
Evidence includes actual viewport sizes, visible instructions and per-run diagnostics.
"""
import base64
import argparse
import json
import sys
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'tools/cutscene'))
from mcp_probe import GodotMcp

PREFIX = "var m=get_tree().current_scene\nvar p=m.get_node('Actors/CyberShogun')\n"
CASES = {
    1: [
        ('lantern', 2300, 354, ''),
        ('archive-roof', 5910, -686, ''),
        ('archive-descent', 6700, -206, ''),
        ('beacon-shielded', 8320, -106, ''),
        ('beacon-exposed', 8320, -106,
         "m.get_node('Actors/DemolitionBeacon').call('HitShield')\nm.get_node('Actors/DemolitionBeacon').call('HitShield')\n"),
        ('blade-accessible', 8850, -46,
         "m.get_node('Actors/DemolitionBeacon').call('TakeDamage',1000.0)\n"),
    ],
    2: [
        ('rescue-defenders', 9500, 554, ''),
        ('rescue-fire', 9500, 554,
         "var e=m.get_node('Actors/Encounter_residential-route')\ne.call('Activate')\nfor n in e.get_children():\n\tif n.has_method('TakeDamage'): n.call('TakeDamage',1000.0)\n"),
        ('rescue-moving', 9500, 554,
         "var fire=m.get_node('Actors/ResidentialRouteFire')\nfire.call('ReceiveAbility','douse',fire.position+Vector2(-180,-50),1.0)\nm.get_node('Actors/Rescue_residential').call('_PhysicsProcess',0.1)\n"),
        ('rescue-arrived', 9500, 554,
         "m.get_node('Actors/Rescue_residential').call('_PhysicsProcess',6.0)\n"),
        ('spire-defense', 17400, 554, ''),
        ('final-barrier', 33300, 554,
         "var e=m.get_node('Actors/Encounter_extraction-guard')\ne.call('Activate')\nfor n in e.get_children():\n\tif n.has_method('TakeDamage'): n.call('TakeDamage',1000.0)\n"),
    ],
    3: [
        ('optional-patrol', 820, 554, ''),
        ('freight-defenders', 10880, 554, "m.call('CompleteCombatEvent','barrier:insertion')\n"),
        ('freight-target', 11200, 554, ''),
        ('west-coupler', 36700, -546,
         "for id in ['encounter:s2-4','shipment:freight','encounter:s3-3','shipment:runoff','recruit:aura','lesson:aura','encounter:s5-4','shipment:harmonic','uplink:west','encounter:s6-5','uplink:east']: m.call('CompleteCombatEvent',id)\n"),
        ('escape', 43500, 554,
         "for id in ['barrier:insertion','encounter:s2-4','shipment:freight','encounter:s3-3','shipment:runoff','recruit:aura','lesson:aura','encounter:s5-4','shipment:harmonic','uplink:west','encounter:s6-5','uplink:east','encounter:s7-1','encounter:s7-2','coupler:west','encounter:s7-3','coupler:east','encounter:s7-4','encounter:s7-5','relay:central','recruit:mirage','lesson:mirage']: m.call('CompleteCombatEvent',id)\n"),
    ],
}


def main():
    """Review all changed states at three viewport sizes, stopping only the game launched by this helper."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--level', type=int, choices=[1, 2, 3], help='Capture only one mission after a focused visual change.')
    parser.add_argument('--navigation', action='store_true', help='Diagnostically seed Levels 1–2 to verify aftermath, results and all four destinations.')
    args = parser.parse_args()
    out = ROOT / '.validation/mission-readability/visual'
    out.mkdir(parents=True, exist_ok=True)
    client = GodotMcp()
    records = []
    started = False

    def call(tool, arguments):
        """Reject MCP errors; return full metadata while keeping image bytes out of console logs."""
        result = client.call(tool, arguments)['result']
        if result.get('isError'):
            raise RuntimeError(result)
        return result

    def evaluate(code):
        """Wait only for bootstrap; individual evaluations stay below the MCP execution deadline."""
        for _ in range(15):
            result = client.call('editor_manage', {'op': 'game_eval', 'params': {'code': code}})['result']
            if 'EVAL_GAME_NOT_READY' not in json.dumps(result):
                if result.get('isError'):
                    raise RuntimeError(result)
                return result.get('structuredContent', result)
            time.sleep(1)
        raise RuntimeError('Game bootstrap timed out')

    identity = call('session_manage', {'op': 'list'})['structuredContent']
    session = next(s for s in identity['sessions'] if s['project_path'].rstrip('/') == 'D:/Projects/frog_jumper')
    assert session['play_state'] == 'stopped', 'Preserve any running user game'
    original_scene = session['current_scene']
    try:
        if args.navigation:
            call('project_run', {'mode': 'custom', 'scene': 'res://scenes/gameplay/level1.tscn', 'autosave': False})
            started = True
            for level in [1, 2]:
                for action, button, destination in [
                    ('replay', 'ButtonsHBox/ReplayBtn', f'level{level}'),
                    ('continue', 'ContinueBtn', f'level{level+1}'),
                    ('select', 'ButtonsHBox/LevelSelectBtn', 'level_selection'),
                    ('menu', 'ButtonsHBox/MainMenuBtn', 'main_menu'),
                ]:
                    code = f"get_tree().change_scene_to_file('res://scenes/gameplay/level{level}.tscn')\nawait get_tree().scene_changed\n"
                    code += PREFIX + "assert(get_node('/root/SessionManager').get('CurrentSessionId')<=0)\n"
                    if level == 1:
                        code += "var b=m.get_node('Actors/DemolitionBeacon')\nb.call('HitShield')\nb.call('HitShield')\nb.call('TakeDamage',1000.0)\np.position=Vector2(9080,-60)\nm.call('CollectNearbyObjectives')\np.position=Vector2(9250,-80)\nm.call('CollectNearbyObjectives')\n"
                    else:
                        code += "m.get_node('ConcreteWildfireIntro').call('Finish')\nawait get_tree().process_frame\nm.get_node('Actors/OceanusJoins').call('Trigger')\nm.get_node('Actors/TitanJoins').call('Trigger')\n"
                        code += "for id in ['encounter:residential-route','encounter:overpass-exit','encounter:extraction-guard','encounter:spire-street','encounter:spire-tower','encounter:spire-courtyard','relay:west','relay:east','battery:core','barrier:final','route:residential','route:overpass','route:extraction','rescue:residential','rescue:overpass','rescue:extraction','spire:street','spire:tower','spire:courtyard']: m.call('CompleteCombatEvent',id)\np.position=Vector2(35100,554)\nm.call('_PhysicsProcess',0.016)\n"
                    aftermath = 'TokyoRemembers' if level == 1 else 'WildfireAftermath'
                    code += f"assert(m.get('MissionComplete'))\nawait get_tree().process_frame\nassert(not m.get_node('CyberVictoryScorecard/RootPanel').visible)\nm.get_node('{aftermath}').call('Finish')\nawait get_tree().process_frame\nassert(m.get_node('CyberVictoryScorecard/RootPanel').visible)\n"
                    code += f"m.get_node('CyberVictoryScorecard/RootPanel/MainVBox/{button}').emit_signal('pressed')\nawait get_tree().scene_changed\nvar next=get_tree().current_scene\nassert(next.scene_file_path.ends_with('{destination}.tscn'))\n"
                    if action in ('replay', 'continue'):
                        expected_roster = 3 if level == 2 and action == 'continue' else 1
                        code += f"var bridge=load('res://scripts/managers/LevelCatalogBridge.cs').new()\nassert(bridge.UnlockedCount()=={expected_roster})\n"
                        if action == 'replay':
                            code += "assert(not next.get('BeaconNeutralized'))\n" if level == 1 else "assert(next.get('CivilianGroupsEvacuated')==0)\n"
                        if level == 2 or action == 'continue':
                            code += "assert(next.get('ArrivalActive'))\n"
                    code += "return {'scene':next.scene_file_path,'navigation':true}"
                    records.append({'level': level, 'action': action, 'result': evaluate(code)})
                    print('NAVIGATION', level, action, flush=True)
                records.append({'level': level, 'navigation_logs': call('logs_read', {'source': 'game', 'count': 500, 'include_details': True})})
            return
        for level, cases in CASES.items():
            if args.level and args.level != level:
                continue
            for width, height in [(1920, 1080), (1920, 1200), (2560, 1080)]:
                call('project_run', {'mode': 'custom', 'scene': f'res://scenes/gameplay/level{level}.tscn', 'autosave': False})
                started = True
                intro = {2: 'ConcreteWildfireIntro', 3: 'NeonRustIntro'}.get(level)
                setup = PREFIX + "assert(get_node('/root/SessionManager').get('CurrentSessionId')<=0)\n"
                if intro:
                    setup += f"m.get_node('{intro}').call('Finish')\n"
                setup += f"get_window().size=Vector2i({width},{height})\n"
                # Disable autonomous damage only for still captures, retaining manual production callbacks.
                setup += "p.set_physics_process(false)\nvar title=m.get_node_or_null('MissionHUD/HUDRoot/TitleCard')\nif title!=null: title.hide()\nfor n in m.get_node('Actors').get_children():\n\tif n!=p: n.set_physics_process(false)\nreturn true"
                evaluate(setup)
                for label, x, y, mutation in cases:
                    filename = f'l{level}-{label}-{width}x{height}.png'
                    code = PREFIX + mutation + f"p.call('ResetAtCheckpoint',Vector2({x},{y}))\n"
                    code += "m.call('_PhysicsProcess',0.016)\nm.call('_Process',0.016)\n"
                    if level == 2:
                        code += "for n in m.get_node('Actors').get_children():\n\tif n.name.begins_with('Rescue_'): n.call('_PhysicsProcess',0.0)\n"
                    code += "for gate in get_tree().get_nodes_in_group('ability_gates'): gate.queue_redraw()\n"
                    # Write full pixels in the game process: some 2560px PNG responses exceed the MCP 4 MiB transport cap.
                    destination = (out / filename).as_posix()
                    code += "p.get_node('Camera2D').reset_smoothing()\nawait get_tree().process_frame\nawait RenderingServer.frame_post_draw\n"
                    code += f"assert(get_viewport().get_texture().get_image().save_png('{destination}')==OK)\n"
                    code += "return {'viewport':get_viewport().get_visible_rect().size,'objective':m.get('CurrentObjective'),'position':p.position}"
                    state = evaluate(code)
                    result = call('editor_screenshot', {'source': 'game', 'max_resolution': 1280, 'include_image': True})
                    images = [b for b in result.get('content', []) if b.get('type') == 'image']
                    assert len(images) == 1, result
                    (out / filename.replace('.png', '-preview.png')).write_bytes(base64.b64decode(images[0]['data']))
                    records.append({'capture': filename, 'state': state})
                    print('CAPTURE', filename, flush=True)
                records.append({'level': level, 'ratio': [width, height], 'logs': call('logs_read', {'source': 'game', 'count': 300, 'include_details': True})})
                call('project_manage', {'op': 'stop'})
                started = False
        final = call('session_manage', {'op': 'list'})['structuredContent']
        assert next(s for s in final['sessions'] if s['session_id'] == session['session_id'])['current_scene'] == original_scene
        records.append({'editor_scene_preserved': original_scene})
    finally:
        evidence_name = 'navigation.json' if args.navigation else f'evidence-l{args.level}.json' if args.level else 'evidence.json'
        (out / evidence_name).write_text(json.dumps(records, indent=2), encoding='utf-8')
        if started:
            call('project_manage', {'op': 'stop'})
        client.close()


if __name__ == '__main__':
    main()
