"""Review every aftermath shot at three ratios and uninterrupted playback via Godot AI MCP.

Runs a separate Level 2 game without saving the editor. Diagnostic damage resolves
combat; real objective callbacks gate extraction. Does not create or modify a saved session.
"""
import base64
import json
import time
from pathlib import Path
from mcp_probe import GodotMcp

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / '.validation/breach/review'


def main():
    """Capture shots, check clock/freeze invariants, then exercise real results navigation."""
    OUT.mkdir(parents=True,exist_ok=True)
    client = GodotMcp()
    records = []

    def call(tool,params):
        """Require a successful MCP result, allowing the bounded game startup delay."""
        for attempt in range(15):
            result = client.call(tool,params)['result']
            if 'EVAL_GAME_NOT_READY' not in json.dumps(result):
                break
            time.sleep(1)
        if result.get('isError'):
            raise RuntimeError(result)
        return result

    def evaluate(code):
        """Evaluate only in the running game and preserve the complete structured evidence."""
        result=call('editor_manage',{'op':'game_eval','params':{'code':code}})
        records.append(dict(code=code,result=result))
        data=result.get('structuredContent',{})
        if 'result' not in data:
            data=json.loads(result['content'][0]['text'])
        return data.get('result')

    identity=call('session_manage',{'op':'list'})
    assert 'D:/Projects/frog_jumper' in json.dumps(identity)
    assert '"play_state": "stopped"' in json.dumps(identity), 'Preserve an existing running game'
    try:
        call('project_run',{'mode':'custom','scene':'res://scenes/gameplay/level2.tscn','autosave':False})
        # Combat shortcuts still invoke actual encounter and objective damage callbacks.
        result=evaluate("""var m=get_tree().current_scene
assert(get_node('/root/SessionManager').get('CurrentSessionId')<=0)
m.get_node('ConcreteWildfireIntro').call('Finish')
var p=m.get_node('Actors/CyberShogun')
m.get_node('Actors/OceanusJoins').call('Trigger')
for id in ['residential-route','overpass-exit','spire-street','spire-tower','spire-courtyard','extraction-guard']:
	var encounter=m.get_node('Actors/Encounter_'+id)
	encounter.call('Activate')
	for enemy in encounter.get_children():
		if enemy.has_method('TakeDamage'): enemy.call('TakeDamage',1000.0)
for name in ['ResidentialRouteFire','OverpassRouteFire']:
	var fire=m.get_node('Actors/'+name)
	fire.call('ReceiveAbility','douse',fire.position+Vector2(-180,-50),1.0)
for i in range(1,4):
	m.call('_PhysicsProcess',0.01)
	m.get_node('Actors/BiometricSpire%d'%i).call('TakeDamage',1000.0)
m.get_node('Actors/TitanJoins').call('Trigger')
for name in ['TitanPracticeRubble','DefendedRubble','MembraneRubble']:
	var wall=m.get_node('Actors/'+name)
	wall.call('ReceiveAbility','slam',wall.position+Vector2(-120,-50),1.0)
for name in ['Relay_west','Relay_east','Core']:
	m.get_node('Actors/SiegeBattery/'+name).call('TakeDamage',1000.0)
for name in ['residential','overpass','extraction']:
	m.get_node('Actors/Rescue_'+name).call('_PhysicsProcess',6.0)
assert(m.get('ExtractionReady'))
m.get_node('GameManager').set('Points',150)
p.emit_signal('ComboChanged',4)
p.position=Vector2(35100,554)
m.call('_PhysicsProcess',0.01)
m.call('_PhysicsProcess',0.01)
await get_tree().process_frame
var c=m.get_node('WildfireAftermath')
c.set_process(false)
c.get_node('Speech').stop()
c.get_node('Atmosphere').stop()
return {'complete':m.get('MissionComplete'),'instances':m.find_children('WildfireAftermath','',false,false).size(),'frozen':not p.can_process()}
""")
        assert result['complete'] and result['instances']==1 and result['frozen'],result
        for width,height in [(1920,1080),(1920,1200),(2560,1080)]:
            for seconds in [4,12,21,30,38,48,59,68,73]:
                evaluate(f"get_window().mode=Window.MODE_WINDOWED\nget_window().size=Vector2i({width},{height})\nvar c=get_tree().current_scene.get_node('WildfireAftermath')\nc.call('Seek',{seconds}.0)\nawait get_tree().process_frame\nawait RenderingServer.frame_post_draw\nreturn {{'elapsed':c.get('Elapsed'),'viewport':get_viewport().get_visible_rect().size,'subtitle':c.get_node('UI/SubtitlePanel/Subtitle').get_global_rect(),'skip':c.get_node('UI/Skip').get_global_rect()}}")
                capture=call('editor_screenshot',{'source':'game','max_resolution':0,'include_image':True})
                for block in capture.get('content',[]):
                    if block.get('type')=='image':
                        (OUT/f'{width}x{height}_{seconds}.png').write_bytes(base64.b64decode(block['data']))
                print('Captured',width,height,seconds,flush=True)
        evaluate("var c=get_tree().current_scene.get_node('WildfireAftermath')\nc.call('SeekAudio',0.0)\nc.set_process(true)\nreturn true")
        for shot in range(8):
            time.sleep(9)
            state=evaluate("var m=get_tree().current_scene\nvar c=m.get_node('WildfireAftermath')\nvar p=m.get_node('Actors/CyberShogun')\nreturn {'elapsed':c.get('Elapsed'),'audio':c.get_node('Speech').get_playback_position(),'score':m.get_node('GameManager').get('Points'),'position':p.position,'health':p.get('CurrentHealth'),'frozen':not p.can_process()}")
            assert abs(state['elapsed']-state['audio'])<.3 and state['score']==150 and state['frozen'],state
            print('Playback',state,flush=True)
        time.sleep(4)
        result=evaluate("var m=get_tree().current_scene\nreturn {'scene':m.scene_file_path,'overlay':m.has_node('WildfireAftermath'),'results':m.get_node('CyberVictoryScorecard/RootPanel').visible,'frozen':not m.get_node('Actors/CyberShogun').can_process()}")
        assert not result['overlay'] and result['results'] and result['frozen'],result
        result=evaluate("get_tree().current_scene.get_node('CyberVictoryScorecard/RootPanel/MainVBox/ContinueBtn').emit_signal('pressed')\nawait get_tree().scene_changed\nvar b=load('res://scripts/managers/LevelCatalogBridge.cs').new()\nvar r={'scene':get_tree().current_scene.scene_file_path,'squad':b.UnlockedCount()}\nb=null\nreturn r")
        assert result['scene'].endswith('level3.tscn') and result['squad']==3,result
        # Scorecard navigation is shared; retaining Level 2 ensures Replay includes the arrival.
        result=evaluate("get_tree().change_scene_to_file('res://scenes/gameplay/level2.tscn')\nawait get_tree().scene_changed\nvar m=get_tree().current_scene\nm.get_node('ConcreteWildfireIntro').call('Finish')\nvar card=m.get_node('CyberVictoryScorecard')\ncard.call('ShowMissionScorecard',150,4,'KAITO',3)\ncard.get_node('RootPanel/MainVBox/ButtonsHBox/ReplayBtn').emit_signal('pressed')\nawait get_tree().scene_changed\nreturn {'scene':get_tree().current_scene.scene_file_path,'arrival':get_tree().current_scene.has_node('ConcreteWildfireIntro')}")
        assert result['scene'].endswith('level2.tscn') and result['arrival'],result
        for button,target in [('LevelSelectBtn','level_selection.tscn'),('MainMenuBtn','main_menu.tscn')]:
            result=evaluate(f"var card=load('res://scenes/ui/cyber_victory_scorecard.tscn').instantiate()\nget_tree().current_scene.add_child(card)\ncard.call('ShowScorecard',150,4,'KAITO')\ncard.get_node('RootPanel/MainVBox/ButtonsHBox/{button}').emit_signal('pressed')\nawait get_tree().scene_changed\nreturn get_tree().current_scene.scene_file_path")
            assert result.endswith(target),result
        records.append({'logs':call('logs_read',{'source':'game','count':300,'include_details':True})})
    finally:
        (OUT/'evidence.json').write_text(json.dumps(records,indent=2))
        call('project_manage',{'op':'stop'})
        client.close()


if __name__=='__main__':
    main()
