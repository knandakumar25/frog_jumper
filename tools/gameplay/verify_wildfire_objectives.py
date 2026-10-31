"""Verify the complete live Level 2 objective chain and campaign handoff through Godot AI MCP.

Combat is resolved diagnostically through the real damage callbacks; abilities, civilian movement and campaign
transition run in the real game. This is a functional integration check, not human playthrough/pacing evidence.
"""
import json
import sys
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'tools/cutscene'))
from mcp_probe import GodotMcp


def main():
    """Run one separate review game, require each milestone and preserve the user's editor scene and save selection."""
    client = GodotMcp()
    records = []
    out = ROOT / '.validation/wildfire-gameplay/objectives.json'
    out.parent.mkdir(parents=True, exist_ok=True)

    def call(tool, params):
        """Fail on remote errors and retain structured integration evidence."""
        result = client.call(tool, params)['result']
        if result.get('isError'):
            raise RuntimeError(result)
        return result

    def evaluate(label, code):
        """Run a bounded milestone, recording its actual result rather than an assumed pass."""
        for attempt in range(12):
            result = client.call('editor_manage', {'op': 'game_eval', 'params': {'code': code}})['result']
            if 'EVAL_GAME_NOT_READY' not in json.dumps(result):
                break
            time.sleep(1)
        if result.get('isError'):
            raise RuntimeError(result)
        records.append({label: result})
        print(label, json.dumps(result.get('structuredContent', {})), flush=True)

    def defeat(id):
        """Produce a small diagnostic step which uses the actual group's spawn and damage callbacks."""
        return f"var e=m.get_node('Actors/Encounter_{id}')\ne.call('Activate')\nfor enemy in e.get_children():\n\tif enemy.has_method('TakeDamage'): enemy.call('TakeDamage',1000.0)\n"

    prefix = "var m=get_tree().current_scene\nvar p=m.get_node('Actors/CyberShogun')\n"
    identity = call('session_manage', {'op': 'list'})
    assert 'D:/Projects/frog_jumper' in json.dumps(identity)
    assert '"play_state": "stopped"' in json.dumps(identity)
    records.append({'identity': identity})
    try:
        call('project_run', {'mode': 'custom', 'scene': 'res://scenes/gameplay/level2.tscn', 'autosave': False})
        evaluate('arrival', prefix + "assert(get_node('/root/SessionManager').get('CurrentSessionId') <= 0)\nassert(m.get('ArrivalActive'))\nm.get_node('ConcreteWildfireIntro').call('Finish')\nreturn {'restored':not m.get('ArrivalActive')}")
        evaluate('oceanus', prefix + "p.call('ResetAtCheckpoint',Vector2(5700,554))\nfor i in range(3): await get_tree().physics_frame\nassert(p.call('SwapOperative','Oceanus'))\nreturn {'recruited':m.get_node('Actors/OceanusJoins').get('Triggered')}")
        for group, encounter, fire_name in [('residential', 'residential-route', 'ResidentialRouteFire'), ('overpass', 'overpass-exit', 'OverpassRouteFire')]:
            evaluate(group, prefix + defeat(encounter) + f"var fire=m.get_node('Actors/{fire_name}')\np.call('ResetAtCheckpoint',fire.position+Vector2(-240,-46))\nfor i in range(5): await get_tree().physics_frame\np.call('TriggerSignatureAbility')\nfor i in range(350): await get_tree().physics_frame\nassert(m.get_node('Actors/Rescue_{group}').get('Evacuated'))\nreturn {{'evacuated':m.get('CivilianGroupsEvacuated'),'permanent_fire_safety':fire.get('PermanentlySafe')}}")
        for index, id in enumerate(['street', 'tower', 'courtyard'], 1):
            evaluate('spire-' + id, prefix + defeat('spire-' + id) + f"await get_tree().physics_frame\nvar s=m.get_node('Actors/BiometricSpire{index}')\ns.call('TakeDamage',1000.0)\ns.emit_signal('Neutralized')\nassert(m.get('SpiresDisabled')=={index})\nreturn {{'spires':m.get('SpiresDisabled'),'membrane':m.get('MembraneOpen')}}")
        evaluate('titan', prefix + "assert(m.get('MembraneOpen'))\np.call('ResetAtCheckpoint',Vector2(25000,554))\nfor i in range(70): await get_tree().physics_frame\nassert(p.call('SwapOperative','Apex Titan'))\nreturn {'recruited':m.get_node('Actors/TitanJoins').get('Triggered')}")
        for name in ['TitanPracticeRubble', 'DefendedRubble']:
            evaluate(name, prefix + f"var wall=m.get_node('Actors/{name}')\np.call('ResetAtCheckpoint',wall.position+Vector2(-160,-46))\nfor i in range(155): await get_tree().physics_frame\np.call('TriggerSignatureAbility')\nassert(wall.get('Cleared'))\nreturn {{'wall_cleared':wall.get('Cleared')}}")
        for name in ['Relay_west', 'Relay_east', 'Core']:
            evaluate(name, prefix + f"var b=m.get_node('Actors/SiegeBattery')\nb.get_node('{name}').call('TakeDamage',1000.0)\nreturn {{'shielded':b.get_node('Core').get('Shielded'),'destroyed':b.get('Destroyed'),'artillery_stopped':m.get_node('Actors/Artillery').get('Stopped')}}")
        evaluate('final-wall', prefix + "var wall=m.get_node('Actors/MembraneRubble')\np.call('ResetAtCheckpoint',wall.position+Vector2(-160,-46))\nfor i in range(155): await get_tree().physics_frame\np.call('TriggerSignatureAbility')\nassert(wall.get('Cleared'))\nreturn {'final_wall':true}")
        evaluate('final-rescue', prefix + defeat('extraction-guard') + "for i in range(350): await get_tree().physics_frame\nassert(m.get('CivilianGroupsEvacuated')==3)\nassert(m.get('ExtractionReady'))\np.call('ResetAtCheckpoint',Vector2(35100,554))\nawait get_tree().physics_frame\nassert(m.get('MissionComplete'))\nreturn {'extraction_ready':true,'completed':m.get('MissionComplete')}")
        evaluate('campaign-transition', "await get_tree().process_frame\nget_tree().current_scene.get_node('WildfireAftermath').call('Finish')\nawait get_tree().process_frame\nget_tree().current_scene.get_node('CyberVictoryScorecard/RootPanel/MainVBox/ContinueBtn').emit_signal('pressed')\nawait get_tree().scene_changed\nvar path=get_tree().current_scene.scene_file_path\nassert(path=='res://scenes/gameplay/level3.tscn')\nreturn {'next_scene':path}")
        records.append({'diagnostics': call('logs_read', {'source': 'game', 'count': 500, 'include_details': True})})
    finally:
        out.write_text(json.dumps(records, indent=2))
        call('project_manage', {'op': 'stop'})
        client.close()


if __name__ == '__main__':
    main()
