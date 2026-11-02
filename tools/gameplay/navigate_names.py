"""Exercise Mission 5 campaign handoffs in an isolated Godot MCP game, with synthetic keyboard/controller/touch events."""
import json
import sys
import time
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'tools/cutscene'))
from mcp_probe import GodotMcp


def main():
    """Verify Level 4 captured results, actual opening destinations, Replay and direct Level Select without mutating user saves."""
    client=GodotMcp();records=[];started=False
    out=ROOT/'.validation/names';out.mkdir(parents=True,exist_ok=True)

    def call(name,args):
        """Reject remote failures instead of treating a returned JSON object as successful navigation."""
        result=client.call(name,args)['result']
        if result.get('isError'):raise RuntimeError(result)
        return result.get('structuredContent',result)

    def evaluate(code):
        """Bound evaluation and readiness polling to the MCP timeout."""
        code='\n'.join('\t'*((len(line)-len(line.lstrip(' ')))//4)+line.lstrip(' ') for line in code.splitlines())
        for _ in range(15):
            result=client.call('editor_manage',{'op':'game_eval','params':{'code':code}})['result']
            if 'EVAL_GAME_NOT_READY' not in json.dumps(result):
                if result.get('isError'):raise RuntimeError(result)
                return result.get('structuredContent',result)
            time.sleep(1)
        raise RuntimeError('Gameplay bootstrap timed out')

    identity=call('session_manage',{'op':'list'})
    assert any(s['project_path'].rstrip('/')=='D:/Projects/frog_jumper' and s['play_state']=='stopped' for s in identity['sessions'])
    try:
        call('project_run',{'mode':'custom','scene':'res://scenes/gameplay/level4.tscn','autosave':False});started=True
        records.append({'act_i_results':evaluate("""
var m=get_tree().current_scene
m.get_node('ShatteredAegisIntro').call('Finish')
var card=m.get_node('CyberVictoryScorecard')
card.call('ShowActScorecard',150,4,'KAITO')
assert(m.scene_file_path.ends_with('level4.tscn'))
assert(card.get_node('RootPanel/MainVBox/TitleLabel').text=='ACT I COMPLETE')
assert(card.get_node('RootPanel/MainVBox/PreviewBtn').text=='CONTINUE TO ACT II')
assert(card.get_node('RootPanel/MainVBox/StatsGrid/ScoreLabel').text.contains('150'))
assert(card.get_node('RootPanel/MainVBox/StatsGrid/ComboLabel').text.contains('4'))
card.get_node('RootPanel/MainVBox/PreviewBtn').emit_signal('pressed')
await get_tree().scene_changed
assert(get_tree().current_scene.scene_file_path.ends_with('act_ii_opening.tscn'))
return true
""")})
        for route in ['keyboard','controller','touch','natural','missing_art']:
            if route!='keyboard':
                records.append({'load_'+route:evaluate("""
var opening=load('res://scenes/cutscenes/act_ii_opening.tscn').instantiate()
opening.set('SpeechPath','')
"""+("opening.set('ArtPath','res://missing-essential-art/')\n" if route=='missing_art' else '')+"""
get_tree().change_scene_to_node(opening)
await get_tree().scene_changed
return true
""")})
            action={
                'keyboard':"var input=InputEventKey.new()\ninput.keycode=KEY_ESCAPE\ninput.pressed=true\no.call('_Input',input)\no.call('_Input',input)",
                'controller':"var input=InputEventJoypadButton.new()\ninput.button_index=JOY_BUTTON_START\ninput.pressed=true\no.call('_Input',input)\no.call('_Input',input)",
                'touch':"var input=InputEventScreenTouch.new()\ninput.position=o.get_node('UI/Skip').get_global_rect().get_center()\ninput.pressed=true\no.call('_Input',input)\no.call('_Input',input)",
                'natural':"o.get_node('Speech').stop()\no.call('Seek',119.95)\no.call('_Process',0.1)",
                'missing_art':"o.call('Finish')"}[route]
            # Missing-art fallback may have completed before a second eval; verify destination without accessing the old scene.
            if route=='missing_art':code="await get_tree().create_timer(0.2).timeout\nassert(get_tree().current_scene.scene_file_path.ends_with('level5.tscn'))\nreturn true"
            else:code="var o=get_tree().current_scene\n"+action+"\nassert(not o.get_node('Speech').playing)\nassert(not o.get_node('Atmosphere').playing)\nawait get_tree().scene_changed\nassert(get_tree().current_scene.scene_file_path.ends_with('level5.tscn'))\nreturn true"
            records.append({route:evaluate(code)});print('HANDOFF',route,flush=True)
        records.append({'replay':evaluate("""
var m=get_tree().current_scene
assert(m.scene_file_path.ends_with('level5.tscn'))
var card=m.get_node('CyberVictoryScorecard')
card.call('ShowScorecard',51,3,'KAITO')
card.get_node('RootPanel/MainVBox/ButtonsHBox/ReplayBtn').emit_signal('pressed')
await get_tree().scene_changed
var replay=get_tree().current_scene
assert(replay.scene_file_path.ends_with('level5.tscn'))
assert(not replay.call('HasObjective','phase'))
assert(not replay.has_node('ActIIOpening'))
return true
""")})
        records.append({'level_select':evaluate("""
get_tree().change_scene_to_file('res://scenes/main_menu/level_selection.tscn')
await get_tree().scene_changed
var menu=get_tree().current_scene
get_node('/root/Global').set('unlocked_levels',5)
menu.call('_update_level_buttons')
var button=menu.call('find_level_button','Level5')
assert(not button.disabled)
button.emit_signal('pressed')
for frame in range(120):
    await get_tree().process_frame
    if get_tree().current_scene!=null and get_tree().current_scene.scene_file_path.ends_with('level5.tscn'):break
assert(get_tree().current_scene.scene_file_path.ends_with('level5.tscn'))
return true
""")})
        records.append({'save_at_four':evaluate("""
var sessions=get_node('/root/SessionManager')
sessions.set_meta('names_before_hash',JSON.stringify(sessions.call('get_all_sessions')).sha256_text())
var id=sessions.call('create_new_session','Cyber Shogun')
assert(id>0)
sessions.set_meta('names_test_id',id)
sessions.call('update_session',id,4,0,0)
assert(sessions.call('get_session',id).current_level==4)
var bridge=load('res://scripts/managers/LevelCatalogBridge.cs').new()
bridge.call('PrepareMission',4)
get_tree().change_scene_to_file(bridge.call('ScenePath',4))
await get_tree().scene_changed
assert(get_tree().current_scene.scene_file_path.ends_with('level4.tscn'))
assert(sessions.call('get_session',id).current_level==4)
return true
""")})
        records.append({'complete_four_and_resume_five':evaluate("""
var sessions=get_node('/root/SessionManager')
var id=int(sessions.get_meta('names_test_id'))
var m=get_tree().current_scene
m.get_node('ShatteredAegisIntro').call('Finish')
m.call('CompleteMission')
for attempt in range(60):
    if sessions.call('get_session',id).current_level==5:break
    await get_tree().create_timer(0.05).timeout
assert(sessions.call('get_session',id).current_level==5)
assert(m.get_node('CyberVictoryScorecard/RootPanel/MainVBox/TitleLabel').text=='ACT I COMPLETE')
assert(sessions.call('load_session',id))
var data=sessions.call('get_session',id)
var bridge=load('res://scripts/managers/LevelCatalogBridge.cs').new()
get_node('/root/Global').set('selected_character','Surya Prime')
bridge.call('PrepareMission',int(data.current_level))
assert(bridge.call('ActiveOperative')=='Cyber Shogun')
get_tree().change_scene_to_file(bridge.call('ScenePath',int(data.current_level)))
await get_tree().scene_changed
assert(get_tree().current_scene.scene_file_path.ends_with('level5.tscn'))
assert(not get_tree().current_scene.has_node('ActIIOpening'))
return true
""")})
        records.append({'diagnostics':call('logs_read',{'source':'game','count':100,'include_details':True})})
    finally:
        if started:
            records.append({'fixture_cleanup':evaluate("""
var sessions=get_node('/root/SessionManager')
if sessions.has_meta('names_test_id'):
    sessions.call('delete_session',int(sessions.get_meta('names_test_id')))
    sessions.remove_meta('names_test_id')
    assert(JSON.stringify(sessions.call('get_all_sessions')).sha256_text()==sessions.get_meta('names_before_hash'))
return true
""")})
        (out/'navigation.json').write_text(json.dumps(records,indent=2)+'\n')
        if started:call('project_manage',{'op':'stop'})
        client.close()


if __name__=='__main__':main()
