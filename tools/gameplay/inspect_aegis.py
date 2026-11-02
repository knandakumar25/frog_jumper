"""Isolated Shattered Aegis section and boss viewport inspection through Godot AI MCP, preserving the user's editor scene.

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

    identity=call('session_manage',{'op':'list'})['structuredContent']
    assert any(s['project_path'].rstrip('/')=='D:/Projects/frog_jumper' and s['play_state']=='stopped' for s in identity['sessions'])
    records.append({'identity':identity})
    prefix="var m=get_tree().current_scene\nvar p=m.get_node('Actors/CyberShogun')\n"
    events=['encounter:s1-3','interlock:approach','encounter:s2-1','encounter:s2-2','recruit:surya','lesson:bulwark','crossing:bulwark','encounter:s3-3','coolant:intake','encounter:s4-3','guns:western','lesson:blink','encounter:s5-3','curtain:inner']

    def seed(count):
        """Seed exact objective prerequisites for visual isolation; these snapshots do not claim normal traversal."""
        return "\n".join("m.call('CompleteCombatEvent',%s)"%json.dumps(event) for event in events[:count])+"\n"

    def capture(label,width,height):
        """Capture an MCP game viewport and persist the exact dimensions alongside the review record."""
        evaluate('await RenderingServer.frame_post_draw\nreturn true')
        result=call('editor_screenshot',{'source':'game','max_resolution':0,'include_image':True})
        for block in result.get('content',[]):
            if block.get('type')=='image':(out/f'{label}-{width}x{height}.png').write_bytes(base64.b64decode(block['data']))
        print('CAPTURE',label,width,height,flush=True)

    try:
        call('project_run',{'mode':'custom','scene':'res://scenes/gameplay/level4.tscn','autosave':False})
        started=True
        for width,height in [(1920,1080),(1920,1200),(2560,1080)]:
            if width!=1920 or height!=1080:
                evaluate("get_tree().reload_current_scene()\nawait get_tree().scene_changed\nreturn true")
            evaluate(prefix+"assert(m.get('ArrivalActive'))\nassert(p.position.x==200)\nm.get_node('ShatteredAegisIntro').call('Finish')\nawait get_tree().process_frame\nawait get_tree().create_timer(4.5).timeout\nreturn true")
            for label,x,y,count in [('road',2800,554,0),('surya',6400,554,4),('thermal',10100,554,7),('gun-deck',15200,-46,9),('curtain',19100,454,12),('command',22700,554,14)]:
                code=prefix+seed(count)
                if label=='surya':code+="m.get_node('Actors/SuryaJoins').call('Trigger')\n"
                code+=f"get_window().size=Vector2i({width},{height})\np.call('ResetAtCheckpoint',Vector2({x},{y}))\np.get_node('Camera2D').reset_smoothing()\nm.call('_PhysicsProcess',0.016)\nawait get_tree().physics_frame\nawait RenderingServer.frame_post_draw\nreturn {{'position':p.position,'objective':m.get('CurrentObjective')}}"
                records.append({label:evaluate(code)})
                capture(label,width,height)
            for phase in [2,3]:
                code=prefix+"var b=m.get('Boss')\nvar c=m.get('Fortress')\nb.set_physics_process(false)\n"
                if phase==2:code+="c.call('Tick',1.2)\nc.call('Tick',1.8)\nc.call('Tick',1.1)\nc.call('Tick',0.31)\nb.call('TakeDamage',10000.0)\n"
                else:code+="for n in m.get_node('Actors').get_children():\n\tif n.name.begins_with('ArenaPylon'):n.call('TakeDamage',10000.0)\nc.call('Tick',0.01)\nb.call('TakeDamage',10000.0)\nc.call('Tick',1.2)\nc.call('Tick',1.8)\n"
                code+=f"assert(b.get('CurrentPhase')=={phase})\nreturn {{'phase':b.get('CurrentPhase'),'stage':c.get('Stage')}}"
                records.append({'boss'+str(phase):evaluate(code)})
                capture('boss'+str(phase),width,height)
            code=prefix+"var b=m.get('Boss')\nvar c=m.get('Fortress')\nc.call('Tick',1.1)\nc.call('Tick',0.31)\nb.call('TakeDamage',10000.0)\nassert(m.get('BossDefeated'))\nm.get_node('Actors/CrossingControl').call('TakeDamage',1.0)\np.call('ResetAtCheckpoint',Vector2(26200,554))\np.get_node('Camera2D').reset_smoothing()\nm.call('_PhysicsProcess',0.016)\nawait get_tree().physics_frame\nreturn true"
            records.append({'crossing':evaluate(code)})
            capture('crossing',width,height)
            records.append({'results':evaluate(prefix+"p.position=Vector2(27700,554)\nm.call('_PhysicsProcess',0.016)\nassert(m.get('MissionComplete'))\nreturn true")})
            capture('results',width,height)
        records.append({'diagnostics':call('logs_read',{'source':'game','count':300,'include_details':True})})
        records.append({'editor_diagnostics':call('logs_read',{'source':'editor','count':40,'include_details':True})})
    finally:
        (out/'captures.json').write_text(json.dumps(records,indent=2))
        if started:call('project_manage',{'op':'stop'})
        client.close()


if __name__=='__main__':main()
