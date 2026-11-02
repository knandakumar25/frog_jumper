"""Assisted Neon Rust blockout/objective traversal through Godot AI MCP, preserving the user's editor scene.

Uses actual movement, jump collision, operative swaps and signature abilities. Enemy damage is diagnostic;
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
    parser.add_argument('--captures', action='store_true')
    parser.add_argument('--navigation', action='store_true')
    parser.add_argument('--required-only', action='store_true', help='Diagnostically defeat only the nine objective formations; bypass every optional patrol.')
    args = parser.parse_args()
    out = ROOT / ('.validation/mission-readability/neon-route' if args.required_only else '.validation/neon-rust-gameplay')
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
            result = client.call('editor_manage', {'op': 'game_eval', 'params': {'code': code}})['result']
            if 'EVAL_GAME_NOT_READY' not in json.dumps(result):
                if result.get('isError'):
                    raise RuntimeError(result)
                return result.get('structuredContent', result)
            time.sleep(1)
        raise RuntimeError('Game bootstrap timed out')

    identity = call('session_manage', {'op': 'list'})['structuredContent']
    assert any(s['project_path'].rstrip('/') == 'D:/Projects/frog_jumper' and s['play_state'] == 'stopped' for s in identity['sessions'])
    records.append({'identity': identity})
    prefix = "var m=get_tree().current_scene\nvar p=m.get_node('Actors/CyberShogun')\n"
    try:
        call('project_run', {'mode': 'custom', 'scene': 'res://scenes/gameplay/level3.tscn', 'autosave': False})
        started = True
        records.append({'arrival': evaluate(prefix + "assert(get_node('/root/SessionManager').get('CurrentSessionId')<=0)\nassert(m.get('ArrivalActive'))\nm.get_node('NeonRustIntro').call('Finish')\nawait get_tree().process_frame\nreturn {'arrival_restored':not m.get('ArrivalActive')}")})
        if args.navigation:
            seed = prefix + "\nfor s in range(1,9):\n\tfor g in range(1,[3,4,3,2,4,5,5,4][s-1]+1): m.call('CompleteCombatEvent','encounter:s%d-%d'%[s,g])\n"
            seed += "for id in ['barrier:insertion','shipment:freight','shipment:runoff','recruit:aura','lesson:aura','shipment:harmonic','uplink:west','uplink:east','coupler:west','coupler:east','encounter:s7-4','encounter:s7-5','relay:central','recruit:mirage','lesson:mirage']: m.call('CompleteCombatEvent',id)\np.position=Vector2(47700,554)\nm.call('_PhysicsProcess',0.016)\nassert(m.get('MissionComplete'))\nawait get_tree().process_frame\nm.get_node('NeonRustAftermath').call('Finish')\nawait get_tree().process_frame\nreturn {'results':m.get_node('CyberVictoryScorecard/RootPanel').visible}"
            for label, button, expected in [('replay','ButtonsHBox/ReplayBtn','level3'),
                ('continue','ContinueBtn','level4'), ('select','ButtonsHBox/LevelSelectBtn','level_selection'),
                ('menu','ButtonsHBox/MainMenuBtn','main_menu')]:
                if label in ('select','menu'):
                    records.append({'return_to_test':evaluate("get_tree().change_scene_to_file('res://scenes/gameplay/level3.tscn')\nawait get_tree().scene_changed\nget_tree().current_scene.get_node('NeonRustIntro').call('Finish')\nawait get_tree().process_frame\nreturn true")})
                records.append({'seed_for_'+label:evaluate(seed)})
                nav = f"get_tree().current_scene.get_node('CyberVictoryScorecard/RootPanel/MainVBox/{button}').emit_signal('pressed')\nawait get_tree().scene_changed\nvar m=get_tree().current_scene\nassert(m.scene_file_path.ends_with('{expected}.tscn'))\n"
                if label in ('replay','continue'):
                    nav += "var bridge=load('res://scripts/managers/LevelCatalogBridge.cs').new()\n"
                    nav += f"assert(bridge.UnlockedCount()=={3 if label=='replay' else 5})\n"
                if label == 'replay':
                    nav += "assert(m.get('ArrivalActive'))\nassert(not m.get('CommsBlackedOut'))\nm.get_node('NeonRustIntro').call('Finish')\nawait get_tree().process_frame\n"
                nav += "return {'scene':m.scene_file_path}"
                records.append({label:evaluate(nav)})
                print('NAVIGATION',label,flush=True)
        elif args.captures:
            for width, height in [(1920,1080), (1920,1200), (2560,1080)]:
                for sector, x, y in [('insertion',3000,554),('freight',9600,554),('runoff',14500,554),
                                     ('harmonic',19000,554),('west',25500,554),('tower',32400,-146),
                                     ('relay',38000,-546),('escape',45100,554)]:
                    records.append({'view': evaluate(prefix + f"get_window().size=Vector2i({width},{height})\np.call('ResetAtCheckpoint',Vector2({x},{y}))\np.get_node('Camera2D').reset_smoothing()\nawait get_tree().physics_frame\nawait RenderingServer.frame_post_draw\nreturn {{'viewport':get_viewport().get_visible_rect().size,'position':p.position}}")})
                    result = call('editor_screenshot', {'source':'game','max_resolution':0,'include_image':True})
                    for block in result.get('content', []):
                        if block.get('type') == 'image':
                            (out / f'{sector}-{width}x{height}.png').write_bytes(base64.b64decode(block['data']))
                    print('CAPTURE', sector, width, height, flush=True)
        else:
            code = prefix + '''
var start=Time.get_ticks_msec()
var max_alive=0
var previous_x=p.position.x
var recoveries=0
if not m.has_meta('qa_sector'):
	m.set_meta('qa_sector',0)
	m.set_meta('qa_sector_start',start)
	m.set_meta('qa_timings',[])
for frame in range(240):
	# If an automated attempt missed the practice window, physically return to its near side and try again.
	var retry_practice=bool(m.get_meta('qa_retry_practice',false))
	if not m.call('HasCombatEvent','lesson:mirage') and p.position.x>40528 and p.position.x<40800: retry_practice=true
	if p.position.x<=40340 or m.call('HasCombatEvent','lesson:mirage'): retry_practice=false
	m.set_meta('qa_retry_practice',retry_practice)
	var alive=0
	for encounter in m.get_node('Actors').get_children():
		if encounter.name.begins_with('Encounter_'):
			alive+=int(encounter.get('LivingMembers'))
			for enemy in encounter.get_children():
				if enemy.has_method('TakeDamage') and not enemy.is_queued_for_deletion(): enemy.call('TakeDamage',1000.0)
	max_alive=maxi(max_alive,alive)
	assert(alive<=6)
	var desired=''
	var nearest=10000.0
	for gate in get_tree().get_nodes_in_group('ability_gates'):
		if gate.get('Cleared'): continue
		var dx=gate.position.x-p.position.x
		if dx >= -30 and dx < 300 and dx < nearest:
			nearest=dx
			desired=gate.get('RequiredTag')
	var who={'slam':'Apex Titan','douse':'Oceanus','resonate':'Aura Phoenicia'}.get(desired,'')
	if who!='':
		p.call('SwapOperative',who)
		p.call('TriggerSignatureAbility')
	if p.position.x>40000:
		p.call('SwapOperative','Mirage Blade')
		for beam_x in [40500,43800,45000,46600]:
			if not retry_practice and p.position.x>=beam_x-130 and p.position.x<beam_x-40 and p.is_on_floor():
				p.call('TriggerSignatureAbility')
	var waiting=false
	# A harmless beam can be walked through while Blink cools down; wait on its near side for the actual lesson action.
	if not m.call('HasCombatEvent','lesson:mirage') and p.position.x>=40320 and p.position.x<40460:
		waiting=float(p.get('AbilityCooldownTimer'))>0.0
	for beam in m.get_node('Actors').get_children():
		if beam.get('AlwaysActive')==false and beam.get('IsLaserActive')==true and beam.get('BeamLength')!=null:
			if beam.position.x-p.position.x>45 and beam.position.x-p.position.x<130: waiting=true
	if retry_practice:
		Input.action_release('right')
		Input.action_press('left')
	else:
		Input.action_release('left')
		if waiting: Input.action_release('right')
		else: Input.action_press('right')
	if p.is_on_floor() and p.is_on_wall(): Input.action_press('jump')
	else: Input.action_release('jump')
	await get_tree().physics_frame
	if previous_x-p.position.x>1000: recoveries+=1
	previous_x=p.position.x
	var sector=int(m.get_meta('qa_sector'))
	if sector<8 and (p.position.x>=[6000,12000,18000,22000,28000,35000,40000,48000][sector] or m.get('MissionComplete')):
		var now=Time.get_ticks_msec()
		var timings=m.get_meta('qa_timings')
		timings.append({'sector':sector+1,'traversal_and_objectives_ms':now-int(m.get_meta('qa_sector_start')),'combat':'diagnostic damage; not measured','recovery_count':recoveries})
		m.set_meta('qa_timings',timings)
		m.set_meta('qa_sector_start',now)
		m.set_meta('qa_sector',sector+1)
	if m.get('MissionComplete'): break
Input.action_release('right')
Input.action_release('left')
Input.action_release('jump')
return {'position':p.position,'health':p.get('CurrentHealth'),'objective':m.get('CurrentObjective'),'blackout':m.get('CommsBlackedOut'),'complete':m.get('MissionComplete'),'recoveries':recoveries,'ms':Time.get_ticks_msec()-start,'max_alive':max_alive,'sector_times':m.get_meta('qa_timings')}
'''
            if args.required_only:
                # Keep authentic movement/abilities and enemy pressure; only mandatory damage is diagnostic.
                code = code.replace("if enemy.has_method('TakeDamage') and not enemy.is_queued_for_deletion():",
                    "if encounter.name in ['Encounter_s2-4','Encounter_s3-3','Encounter_s5-4','Encounter_s6-5','Encounter_s7-1','Encounter_s7-2','Encounter_s7-3','Encounter_s7-4','Encounter_s7-5'] and enemy.has_method('TakeDamage') and not enemy.is_queued_for_deletion():")
            for batch in range(180):
                result = evaluate(code)
                records.append({'batch':batch,'route':result})
                print('ROUTE', batch, json.dumps(result), flush=True)
                if '"complete": true' in json.dumps(result) or '"complete":true' in json.dumps(result):
                    break
            else:
                raise RuntimeError('Route did not finish within bounded batches')
            records.append({'results': evaluate("var m=get_tree().current_scene\nassert(m.get('MissionComplete'))\nawait get_tree().process_frame\nm.get_node('NeonRustAftermath').call('Finish')\nawait get_tree().process_frame\nvar b=m.get_node('CyberVictoryScorecard/RootPanel/MainVBox/ContinueBtn')\nassert(b.visible)\nassert(b.text=='CONTINUE TO LEVEL 4')\nreturn {'results':true,'continue':b.text}")})
        records.append({'diagnostics':call('logs_read', {'source':'game','count':300,'include_details':True})})
    finally:
        (out / ('navigation.json' if args.navigation else 'captures.json' if args.captures else 'blockout.json')).write_text(json.dumps(records,indent=2))
        if started:
            call('project_manage', {'op':'stop'})
        client.close()


if __name__ == '__main__':
    main()
