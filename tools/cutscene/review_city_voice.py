"""Inspect all eleven aftermath shots and real results navigation through Godot AI MCP.

Preserves the user's open editor scene; only launches a separate stopped game.
Diagnostic objective seeding isolates cinematic acceptance from combat pacing.
"""
import base64
import json
import os
import subprocess
import time
from pathlib import Path
from mcp_probe import GodotMcp

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'.validation/city-voice/review'
SHOTS=[4,13,22,31,40,49,59,69,78,85,89]
SEED="""var m=get_tree().current_scene
assert(get_node('/root/SessionManager').get('CurrentSessionId')<=0)
m.get_node('NeonRustIntro').call('Finish')
await get_tree().process_frame
for s in range(1,9):
	for g in range(1,[3,4,3,2,4,5,5,4][s-1]+1): m.call('CompleteCombatEvent','encounter:s%d-%d'%[s,g])
for id in ['barrier:insertion','shipment:freight','shipment:runoff','recruit:aura','lesson:aura','shipment:harmonic','uplink:west','uplink:east','coupler:west','coupler:east','encounter:s7-4','encounter:s7-5','relay:central','recruit:mirage','lesson:mirage']: m.call('CompleteCombatEvent',id)
var p=m.get_node('Actors/CyberShogun')
p.position=Vector2(47700,554)
m.call('_PhysicsProcess',0.016)
await get_tree().process_frame
assert(m.has_node('NeonRustAftermath'))
return {'position':p.position,'health':p.get('CurrentHealth'),'score':m.get_node('GameManager').get('Points')}
"""


def contact_sheets():
    """Assemble eleven captures into a 4-by-3 sheet, retaining each viewport's aspect ratio."""
    container=os.environ.get('CITY_VOICE_CONTAINER')
    if container:
        # Copy contents on reruns too; copying the directory itself nests review/review
        # and would assemble stale captures left by a previous visual iteration.
        subprocess.run(['docker','cp',str(OUT)+'/.' ,container+':/workspace/.validation/city-voice/review'],check=True)
    for ratio in ['1920x1080','1920x1200','2560x1080']:
        command=(['docker','exec',container,'ffmpeg'] if container else ['docker','compose','run','--rm','--entrypoint','ffmpeg','cutscene'])+['-v','error','-y']
        for seconds in SHOTS: command+=['-i',f'/workspace/.validation/city-voice/review/{ratio}_{seconds}.png']
        filters=[f'[{i}:v]scale=480:270:force_original_aspect_ratio=decrease,pad=480:270:(ow-iw)/2:(oh-ih)/2[v{i}]' for i in range(11)]
        layout='|'.join(f'{i%4*480}_{i//4*270}' for i in range(11))
        filters.append(''.join(f'[v{i}]' for i in range(11))+f'xstack=inputs=11:layout={layout}:fill=black[out]')
        command+=['-filter_complex_threads','1','-filter_complex',';'.join(filters),'-map','[out]','-frames:v','1',f'/workspace/.validation/city-voice/review/{ratio}_contact.png']
        subprocess.run(command,check=True,cwd=ROOT)
        if container:
            subprocess.run(['docker','cp',f'{container}:/workspace/.validation/city-voice/review/{ratio}_contact.png',str(OUT/f'{ratio}_contact.png')],check=True)


def main():
    """Capture aspect samples, run uninterrupted playback, then verify four production navigation handlers."""
    OUT.mkdir(parents=True,exist_ok=True)
    client=GodotMcp()
    records=[]
    started=False

    def call(tool,params):
        """Bound startup waits and reject remote failures instead of silently accepting absent screenshots."""
        for attempt in range(15):
            result=client.call(tool,params)['result']
            if 'EVAL_GAME_NOT_READY' not in json.dumps(result): break
            time.sleep(1)
        if result.get('isError'): raise RuntimeError(result)
        return result

    def evaluate(code):
        """Keep diagnostic input and structured output beside the captured review evidence."""
        result=call('editor_manage',{'op':'game_eval','params':{'code':code}})
        records.append(dict(code=code,result=result))
        data=result.get('structuredContent',{})
        if 'result' not in data: data=json.loads(result['content'][0]['text'])
        return data.get('result')

    identity=call('session_manage',{'op':'list'})
    assert 'D:/Projects/frog_jumper' in json.dumps(identity) and '"play_state": "stopped"' in json.dumps(identity)
    try:
        call('project_run',{'mode':'custom','scene':'res://scenes/gameplay/level3.tscn','autosave':False})
        started=True
        initial=evaluate(SEED)
        evaluate("var c=get_tree().current_scene.get_node('NeonRustAftermath')\nc.set_process(false)\nc.get_node('Speech').stop()\nc.get_node('Atmosphere').stop()\nreturn true")
        for width,height in [(1920,1080),(1920,1200),(2560,1080)]:
            for seconds in SHOTS:
                state=evaluate(f"get_window().mode=Window.MODE_WINDOWED\nget_window().size=Vector2i({width},{height})\nvar c=get_tree().current_scene.get_node('NeonRustAftermath')\nc.call('Seek',{seconds}.0)\nawait get_tree().process_frame\nawait RenderingServer.frame_post_draw\nreturn {{'elapsed':c.get('Elapsed'),'viewport':get_viewport().get_visible_rect().size}}")
                capture=call('editor_screenshot',{'source':'game','max_resolution':0,'include_image':True})
                images=[b for b in capture.get('content',[]) if b.get('type')=='image']
                assert images, capture
                (OUT/f'{width}x{height}_{seconds}.png').write_bytes(base64.b64decode(images[0]['data']))
                print('Captured',width,height,seconds,flush=True)
        contact_sheets()
        evaluate("var c=get_tree().current_scene.get_node('NeonRustAftermath')\nc.call('SeekAudio',0.0)\nc.set_process(true)\nreturn true")
        for sample in range(10):
            time.sleep(8)
            state=evaluate("""var m=get_tree().current_scene
var c=m.get_node('NeonRustAftermath')
var p=m.get_node('Actors/CyberShogun')
var frozen=true
for n in m.find_children('*','',true,false):
	if n==c or c.is_ancestor_of(n) or n.name=='CyberVictoryScorecard' or m.get_node('CyberVictoryScorecard').is_ancestor_of(n): continue
	if n.can_process(): frozen=false
return {'elapsed':c.get('Elapsed'),'audio':c.get_node('Speech').get_playback_position(),'position':p.position,'health':p.get('CurrentHealth'),'score':m.get_node('GameManager').get('Points'),'frozen':frozen,'results':m.get_node('CyberVictoryScorecard/RootPanel').visible}
""")
            assert all(state[k]==initial[k] for k in initial) and state['frozen'] and not state['results'],state
            assert abs(state['elapsed']-state['audio'])<.3,state
            print('Playback',state,flush=True)
        time.sleep(11)
        state=evaluate("var m=get_tree().current_scene\nreturn {'overlay':m.has_node('NeonRustAftermath'),'results':m.get_node('CyberVictoryScorecard/RootPanel').visible,'jump':Input.is_action_pressed('jump')}")
        assert not state['overlay'] and state['results'] and not state['jump'],state
        for label,button,expected in [('continue','ContinueBtn','level4'),('replay','ButtonsHBox/ReplayBtn','level3'),('select','ButtonsHBox/LevelSelectBtn','level_selection'),('menu','ButtonsHBox/MainMenuBtn','main_menu')]:
            if label!='continue':
                evaluate("get_tree().change_scene_to_file('res://scenes/gameplay/level3.tscn')\nawait get_tree().scene_changed\nreturn true")
                evaluate(SEED)
                evaluate("get_tree().current_scene.get_node('NeonRustAftermath').call('Finish')\nawait get_tree().process_frame\nreturn true")
            state=evaluate(f"get_tree().current_scene.get_node('CyberVictoryScorecard/RootPanel/MainVBox/{button}').emit_signal('pressed')\nawait get_tree().scene_changed\nvar m=get_tree().current_scene\nvar b=load('res://scripts/managers/LevelCatalogBridge.cs').new()\nreturn {{'scene':m.scene_file_path,'squad':b.UnlockedCount(),'arrival':m.get('ArrivalActive') if m.has_method('CompleteCombatEvent') else false}}")
            assert state['scene'].endswith(expected+'.tscn'),state
            if label=='continue': assert state['squad']==5,state
            if label=='replay': assert state['squad']==3 and state['arrival'],state
            print('Navigation',label,state,flush=True)
        records.append({'logs':call('logs_read',{'source':'game','count':500,'include_details':True})})
    finally:
        (OUT/'evidence.json').write_text(json.dumps(records,indent=2)+'\n')
        if started: call('project_manage',{'op':'stop'})
        client.close()


if __name__=='__main__':
    main()
