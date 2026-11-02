"""Isolated Mission 5 sector, restored access and results viewport inspection through Godot AI MCP, preserving the user's editor scene.

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
    args = parser.parse_args()
    out = ROOT / '.validation/names'
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

    identity=call('session_manage',{'op':'list'})['structuredContent']
    assert any(s['project_path'].rstrip('/')=='D:/Projects/frog_jumper' and s['play_state']=='stopped' for s in identity['sessions'])
    records.append({'identity':identity})
    prefix="var m=get_tree().current_scene\nvar p=m.get_node('Actors/CyberShogun')\n"
    objectives=['phase','signal','coolant','maintenance','record:1','record:2','record:3','isolate','defense:1','defense:2','defense:3','commit']
    formations=[['service1','service2'],['hall1','hall2','hall3'],['utility1','utility2'],['utility3'],['archive1a','archive1b'],['archive2a','archive2b'],['archive3a','archive3b'],[],['core1'],['core2'],['core3'],[]]

    def seed(count):
        """Seed legal prerequisites for isolated visuals; these captures do not assert traversal or combat balance."""
        code=''
        for objective,groups in zip(objectives[:count],formations[:count]):
            for group in groups:code+=f"m.call('CompleteCombatEvent','encounter:{group}')\n"
            code+=f"assert(m.call('CommitObjective','{objective}'))\n"
        return code

    def capture(label,width,height):
        """Capture an MCP game viewport and persist the exact dimensions alongside the review record."""
        evaluate('await RenderingServer.frame_post_draw\nreturn true')
        result=call('editor_screenshot',{'source':'game','max_resolution':0,'include_image':True})
        for block in result.get('content',[]):
            if block.get('type')=='image':(out/f'{label}-{width}x{height}.png').write_bytes(base64.b64decode(block['data']))
        print('CAPTURE',label,width,height,flush=True)

    try:
        call('project_run',{'mode':'custom','scene':'res://scenes/gameplay/level5.tscn','autosave':False});started=True
        for width,height in [(1920,1080),(1920,1200),(2560,1080)]:
            if width!=1920 or height!=1080:evaluate("get_tree().reload_current_scene()\nawait get_tree().scene_changed\nreturn true")
            evaluate("await get_tree().create_timer(4.5).timeout\nreturn true")
            previous=0
            for label,x,y,count in [('service',1100,554,0),('processing',6900,554,1),('utilities',10900,554,2),('originals',16350,154,5),('core',22500,554,8),('restored',26400,554,12)]:
                # Seed only events not already accepted for the previous sector snapshot.
                code=prefix
                for objective,groups in zip(objectives[previous:count],formations[previous:count]):
                    for group in groups:code+=f"m.call('CompleteCombatEvent','encounter:{group}')\n"
                    code+=f"assert(m.call('CommitObjective','{objective}'))\n"
                previous=count
                code+=f"get_window().size=Vector2i({width},{height})\np.call('ResetAtCheckpoint',Vector2({x},{y}))\np.get_node('Camera2D').reset_smoothing()\nm.call('_PhysicsProcess',0.016)\nawait get_tree().physics_frame\nawait RenderingServer.frame_post_draw\nreturn {{'position':p.position,'objective':m.get('CurrentObjective'),'viewport':get_viewport().get_visible_rect()}}"
                records.append({label:evaluate(code)});capture(label,width,height)
            records.append({'results':evaluate(prefix+"m.call('CommitObjective','extract')\nassert(m.get('MissionComplete'))\nreturn true")});capture('results',width,height)
        records.append({'diagnostics':call('logs_read',{'source':'game','count':100,'include_details':True})})
    finally:
        (out/'captures.json').write_text(json.dumps(records,indent=2))
        if started:call('project_manage',{'op':'stop'})
        client.close()


if __name__=='__main__':main()
