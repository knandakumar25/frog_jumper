"""Assisted Shattered Aegis blockout/objective traversal through Godot AI MCP, preserving the user's editor scene.

Uses actual movement, jump collision, operative swaps and signature abilities. All damage uses existing primary attacks and abilities;
these timings cannot establish first-time player pacing. Each eval is bounded below MCP's eight-second limit.
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
    """Run isolated route or aspect-ratio captures, store evidence, and stop only the game this script started."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--resume', action='store_true', help='Resume this tool-owned Level 4 runtime without reloading its ledger')
    parser.add_argument('--navigation', action='store_true')
    args = parser.parse_args()
    out = ROOT / '.validation/aegis'
    out.mkdir(parents=True, exist_ok=True)
    client = GodotMcp()
    records = []
    started = False

    def call(tool, params):
        """Reject transport/remote errors; caller assertions validate actual gameplay outcomes."""
        result = client.call(tool, params)['result']
        if result.get('isError'):
            raise RuntimeError(result)
        return result

    def evaluate(code):
        """Wait for game bootstrap only; keep all gameplay batches individually bounded."""
        for _ in range(12):
            code = '\n'.join('\t' * ((len(line)-len(line.lstrip(' ')))//4) + line.lstrip(' ') for line in code.splitlines())
            result = client.call('editor_manage', {'op': 'game_eval', 'params': {'code': code}})['result']
            if 'EVAL_GAME_NOT_READY' not in json.dumps(result):
                if result.get('isError'):
                    raise RuntimeError(result)
                return result.get('structuredContent', result)
            time.sleep(1)
        raise RuntimeError('Game bootstrap timed out')

    identity = call('session_manage', {'op':'list'})['structuredContent']
    assert any(s['project_path'].rstrip('/')=='D:/Projects/frog_jumper' and (args.resume or s['play_state']=='stopped') for s in identity['sessions'])
    records.append({'identity':identity})
    prefix="var m=get_tree().current_scene\nvar p=m.get_node('Actors/CyberShogun')\n"
    try:
        if not args.resume:
            call('project_run', {'mode':'custom','scene':'res://scenes/gameplay/level4.tscn','autosave':False})
            records.append({'arrival':evaluate(prefix+"assert(m.get('ArrivalActive'))\nassert(p.position.x==200)\nm.set_meta('aegis_combat_review',true)\nm.get_node('ShatteredAegisIntro').call('Finish')\nawait get_tree().process_frame\nreturn true")})
        else:
            records.append({'resume':evaluate(prefix+"assert(m.scene_file_path.ends_with('level4.tscn'))\nInput.action_release('right')\nInput.action_release('left')\nreturn p.position")})
        started=True
        code=prefix+'''
var recoveries=0
var previous=p.position.x
var max_alive=0
for frame in range(240):
    var living=0
    var combat_target:Node2D=null
    var nearest_enemy=800.0
    for e in m.get_node('Actors').get_children():
        if e.name.begins_with('Encounter_'):
            living+=int(e.get('LivingMembers'))
            for enemy in e.get_children():
                if enemy.has_method('TakeDamage') and not enemy.is_queued_for_deletion():
                    var distance=p.position.distance_to(enemy.global_position)
                    if distance<nearest_enemy:
                        nearest_enemy=distance
                        combat_target=enemy
    max_alive=maxi(max_alive,living)
    assert(living<=4)
    var waiting=false
    var ability_zone=false
    for device_name in ['ApproachControl','CrossingControl']:
        var device=m.get_node('Actors/'+device_name)
        if not device.get('IsOffline') and absf(device.position.x-p.position.x)<95:
            Input.action_release('right')
            p.call('PerformPrimaryAttack')
            waiting=true
    for gate_name in ['CoolantIntake','MaintenanceBreach','InnerController']:
        var gate=m.get_node_or_null('Actors/'+gate_name)
        if gate==null: continue
        if not gate.get('Cleared') and absf(gate.position.x-p.position.x)<210:
            var who={'CoolantIntake':'Oceanus','MaintenanceBreach':'Apex Titan','InnerController':'Aura Phoenicia'}[gate_name]
            p.call('SwapOperative',who)
            p.call('TriggerSignatureAbility')
    for vent_x in [6900,7450,8650]:
        if p.position.x>vent_x-190 and p.position.x<vent_x+120:
            ability_zone=true
            p.call('SwapOperative','Surya Prime')
            if p.position.x<vent_x-110:
                if p.get('AbilityCooldownTimer')>0: waiting=true
                else: p.call('TriggerSignatureAbility')
    for beam_x in [17400,18300,19300,19950]:
        if p.position.x>beam_x-170 and p.position.x<beam_x+100:
            ability_zone=true
        if p.position.x>beam_x-130 and p.position.x<beam_x-50:
            p.call('SwapOperative','Mirage Blade')
            if not p.is_on_floor() or p.get('AbilityCooldownTimer')>0: waiting=true
            elif p.get('FacingSign')>0: p.call('TriggerSignatureAbility')
    if ability_zone:combat_target=null
    var boss=m.get('Boss')
    var combat_direction=0
    var jump_combat=false
    if is_instance_valid(boss) and not boss.is_queued_for_deletion():
        var fortress=m.get('Fortress')
        if fortress.get('Shielded'):
            for device in m.get_node('Actors').get_children():
                if device.name.begins_with('ArenaPylon') and not device.get('IsOffline'):
                    if combat_target==null or p.position.distance_to(device.position)<p.position.distance_to(combat_target.position):combat_target=device
        elif fortress.get('Exposed'):combat_target=boss
        else:
            if p.position.x>23900:m.set_meta('boss_direction',-1)
            elif p.position.x<22200:m.set_meta('boss_direction',1)
            combat_direction=int(m.get_meta('boss_direction',1))
            p.call('SwapOperative','Cyber Shogun')
            p.call('TriggerSignatureAbility')
    if combat_target!=null:
        var dx=combat_target.global_position.x-p.position.x
        combat_direction=1 if dx>65 else -1 if dx < -65 else 0
        jump_combat=combat_target.global_position.y<p.position.y-75
        p.call('SwapOperative','Cyber Shogun')
        p.call('TriggerSignatureAbility')
        if absf(dx)<110:
            p.call('PerformPrimaryAttack')
        waiting=combat_direction==0
    if not m.call('HasCombatEvent','lesson:blink') and p.position.x>17340 and p.position.x<17600:
        combat_direction=-1
        ability_zone=true
        Input.action_press('left');Input.action_release('right')
    elif combat_target!=null or is_instance_valid(boss):
        if combat_direction>0:Input.action_press('right');Input.action_release('left')
        elif combat_direction<0:Input.action_press('left');Input.action_release('right')
        else:Input.action_release('right');Input.action_release('left')
    elif waiting:Input.action_release('right');Input.action_release('left')
    else:Input.action_press('right');Input.action_release('left')
    if p.is_on_floor() and (p.is_on_wall() or jump_combat):Input.action_press('jump')
    else:Input.action_release('jump')
    await get_tree().physics_frame
    if previous-p.position.x>1000: recoveries+=1
    previous=p.position.x
    if m.get('MissionComplete'): break
Input.action_release('right')
Input.action_release('left')
Input.action_release('jump')
return {'position':p.position,'health':p.get('CurrentHealth'),'objective':m.get('CurrentObjective'),'complete':m.get('MissionComplete'),'recoveries':recoveries,'max_alive':max_alive}
'''
        for batch in range(160):
            result=evaluate(code)
            records.append({'batch':batch,'route':result})
            print('ROUTE',batch,json.dumps(result),flush=True)
            if '"complete": true' in json.dumps(result):break
        else:raise RuntimeError('Traversal did not finish')
        records.append({'diagnostics':call('logs_read',{'source':'game','count':300,'include_details':True})})
    finally:
        (out/('real-combat-resume.json' if args.resume else 'real-combat.json')).write_text(json.dumps(records,indent=2))
        if started:call('project_manage',{'op':'stop'})
        client.close()


if __name__=='__main__':
    main()
