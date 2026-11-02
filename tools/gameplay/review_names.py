"""Assisted real-input blockout traversal with diagnostic enemy clearance. Preserves the user's editor scene; timings are not first-play pacing."""
import json
import sys
import time
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'tools/cutscene'))
from mcp_probe import GodotMcp


def main():
    """Walk every mandatory route with actual movement/ability inputs; write evidence and stop only this tool's game."""
    client=GodotMcp()
    out=ROOT/'.validation/names';out.mkdir(parents=True,exist_ok=True)
    records=[];started=False

    def call(name,args):
        """Reject remote errors before interpreting the response."""
        result=client.call(name,args)['result']
        if result.get('isError'):raise RuntimeError(result)
        return result.get('structuredContent',result)

    def evaluate(code):
        """Run bounded eval batches below the MCP time limit."""
        code='\n'.join('\t'*((len(line)-len(line.lstrip(' ')))//4)+line.lstrip(' ') for line in code.splitlines())
        for _ in range(15):
            result=client.call('editor_manage',{'op':'game_eval','params':{'code':code}})['result']
            if 'EVAL_GAME_NOT_READY' not in json.dumps(result):
                if result.get('isError'):raise RuntimeError(result)
                return result.get('structuredContent',result)
            time.sleep(1)
        raise RuntimeError('Game bootstrap timed out')

    identity=call('session_manage',{'op':'list'})
    assert any(s['project_path'].rstrip('/')=='D:/Projects/frog_jumper' and s['play_state']=='stopped' for s in identity['sessions'])
    try:
        call('project_run',{'mode':'custom','scene':'res://scenes/gameplay/level5.tscn','autosave':False});started=True
        code="""
var m=get_tree().current_scene
var p=m.get_node('Actors/CyberShogun')
for frame in range(180):
    for formation in m.get_node('Actors').get_children():
        if formation.name.begins_with('Formation'):
            for enemy in formation.get_children():
                if enemy.has_method('TakeDamage') and not enemy.is_queued_for_deletion():enemy.call('TakeDamage',1000.0)
    var waiting=false
    for spec in [['Devicesignal','Aura Phoenicia'],['Devicecoolant','Oceanus'],['Devicemaintenance','Apex Titan']]:
        var gate=m.get_node('Actors/'+spec[0])
        if not gate.get('Cleared') and absf(gate.position.x-p.position.x)<220:
            p.call('SwapOperative',spec[1]);p.call('TriggerSignatureAbility')
    if p.position.x>3790 and p.position.x<3930:
        p.call('SwapOperative','Mirage Blade')
        if p.get('AbilityCooldownTimer')<=0 and p.is_on_floor():p.call('TriggerSignatureAbility')
        else:waiting=true
    var terminal:Node2D=null
    for child in m.get_node('Actors').get_children():
        if child.name.begins_with('Terminal') and not m.call('HasObjective',child.get('Objective')):
            terminal=child;break
    if terminal!=null and p.position.x>terminal.position.x-70:
        if p.position.x>terminal.position.x+45:Input.action_press('left');waiting=true
        else:Input.action_release('left');waiting=true
        if terminal.get('InRange'):terminal.call('TryInteract')
        elif p.is_on_floor():Input.action_press('jump')
    else:Input.action_release('left')
    if not waiting:Input.action_press('right')
    else:Input.action_release('right')
    if p.is_on_floor() and p.is_on_wall():Input.action_press('jump')
    elif not p.is_on_floor():Input.action_release('jump')
    elif terminal==null or p.position.x<terminal.position.x-70:Input.action_release('jump')
    await get_tree().physics_frame
    if m.get('MissionComplete'):break
Input.action_release('right');Input.action_release('left');Input.action_release('jump')
return {'position':p.position,'objective':m.get('CurrentObjective'),'complete':m.get('MissionComplete'),'health':p.get('CurrentHealth')}
"""
        for batch in range(120):
            result=evaluate(code);records.append(dict(batch=batch,state=result));print(batch,json.dumps(result),flush=True)
            if '"complete": true' in json.dumps(result):break
        else:raise RuntimeError('Assisted traversal did not complete')
        records.append(dict(logs=call('logs_read',{'source':'game','count':100,'include_details':True})))
    finally:
        (out/'blockout.json').write_text(json.dumps(records,indent=2)+'\n')
        if started:call('project_manage',{'op':'stop'})
        client.close()


if __name__=='__main__':main()
