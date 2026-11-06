"""Read-only/unsaved-preview Godot MCP world inspection, captures and streaming tour.

Requires the existing project's editor playing Neo-Kyoto with no selected campaign.
Tour drives the real destination list and transit button; it never writes player
saves, starts/stops an editor, exports, or changes the main scene. Evidence goes
under the ignored .validation directory. Use --tour for a rendered 32-stop soak.
"""
import argparse
import base64
import json
from pathlib import Path
import sys

ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'tools/cutscene'))
from mcp_probe import GodotMcp


def call(client,name,arguments):
    """Unwrap the MCP structured response and fail clearly on runtime errors."""
    result=client.call(name,arguments)['result']
    if result.get('isError'): raise RuntimeError(result)
    data=result.get('structuredContent')
    if data is None:
        data=json.loads(next(item['text'] for item in result['content'] if item['type']=='text'))
    return data


def evaluate(client,code):
    """Run an inspection expression through the existing authenticated Godot game helper."""
    # The helper wraps code in a tab-indented function; normalize Python-embedded GDScript indentation first.
    code='\n'.join('\t'*((len(line)-len(line.lstrip(' ')))//4)+line.lstrip(' ') for line in code.splitlines())
    return call(client,'editor_manage',dict(op='game_eval',params=dict(code=code))).get('result')


def capture(client,name):
    """Save a native-scale game framebuffer as an ignored QA artifact without editing game assets."""
    result=client.call('editor_screenshot',dict(source='game',max_resolution=1920))['result']
    image=next(item for item in result['content'] if item['type']=='image')
    path=ROOT/'.validation'/f'world-{name}.png'
    path.write_bytes(base64.b64decode(image['data']))
    print(path.name,flush=True)


def main():
    """Verify editor/project/preview identity, then capture or drive free transit and sample frame/memory bounds."""
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--tour',action='store_true')
    parser.add_argument('--name',default='city')
    parser.add_argument('--station',type=int)
    parser.add_argument('--interact',action='store_true')
    args=parser.parse_args()
    client=GodotMcp()
    try:
        sessions=call(client,'session_manage',dict(op='list'))
        active=next(s for s in sessions['sessions'] if s['is_active'])
        if Path(active['project_path']).resolve()!=ROOT.resolve(): raise RuntimeError('Wrong editor project')
        safe=evaluate(client,'return get_tree().current_scene.name == "NeoKyoto" and get_node("/root/SessionManager").CurrentSessionId <= 0')
        if safe is not True: raise RuntimeError('QA requires an unsaved NeoKyoto preview')
        if args.station is not None:
            if not 0<=args.station<256: raise ValueError('Station must be 0..255')
            evaluate(client,'''
var world=get_tree().current_scene
if not world.call("InspectionState").map_open:
    var event=InputEventAction.new()
    event.action="world_map"
    event.pressed=true
    world.call("_UnhandledInput",event)
var list=world.find_children("WorldDestinations","ItemList",true,false)[0]
list.select(STATION_INDEX)
list.item_selected.emit(STATION_INDEX)
for button in world.find_children("*","Button",true,false):
    if button.text==tr("Free transit"):
        button.pressed.emit()
for frame in 40:
    await get_tree().process_frame
return world.call("InspectionState")
'''.replace('STATION_INDEX',str(60+args.station)))
        if args.interact:
            evaluate(client,'''
var event=InputEventAction.new()
event.action="world_interact"
event.pressed=true
get_tree().current_scene.call("_UnhandledInput",event)
for frame in 40:
    await get_tree().process_frame
return get_tree().current_scene.call("InspectionState")
''')
        if not args.tour:
            capture(client,args.name)
            print(json.dumps(evaluate(client,'return get_tree().current_scene.InspectionState()')))
            return
        code='''
var world = get_tree().current_scene
var destinations = world.find_children("WorldDestinations", "ItemList", true, false)[0]
var button: Button
for candidate in world.find_children("*", "Button", true, false):
    if candidate.text == tr("Free transit"):
        button = candidate
var snapshots = []
var frames = []
var frame_intervals = []
var peaks = []
for stop in 1:
    var event = InputEventAction.new()
    event.action = "world_map"
    event.pressed = true
    world.call("_UnhandledInput", event)
    var index = 60 + STOP_INDEX
    destinations.select(index)
    destinations.item_selected.emit(index)
    button.pressed.emit()
    for settle in 60:
        await get_tree().process_frame
        if world.call("InspectionState").resident == 9 and not world.call("InspectionState").busy:
            break
    var snapshot = world.call("InspectionState")
    snapshot["memory_bytes"] = Performance.get_monitor(Performance.MEMORY_STATIC)
    snapshots.append(snapshot)
    var previous_usec = Time.get_ticks_usec()
    for sample in 120:
        await get_tree().process_frame
        frames.append(Performance.get_monitor(Performance.TIME_PROCESS) * 1000.0)
        var now_usec = Time.get_ticks_usec()
        frame_intervals.append((now_usec - previous_usec) / 1000.0)
        previous_usec = now_usec
    peaks.append(Performance.get_monitor(Performance.MEMORY_STATIC))
return {"stops":snapshots,"process_ms":frames,"frame_intervals_ms":frame_intervals,"memory_bytes":peaks,"window":DisplayServer.window_get_size(),"renderer":RenderingServer.get_video_adapter_name()}
'''
        data=None
        # MCP eval has an eight-second watchdog. Each bounded stop finishes in roughly two seconds.
        for stop in range(32):
            part=evaluate(client,code.replace('STOP_INDEX',str((stop*47)%256)))
            if not isinstance(part,dict) or not part.get('stops'): raise RuntimeError(part)
            if data is None: data=part
            else:
                for key in ('stops','process_ms','frame_intervals_ms','memory_bytes'): data[key].extend(part[key])
            if stop%8==7: print('Completed stops:',stop+1,flush=True)
        (ROOT/'.validation/world-soak.json').write_text(json.dumps(data,indent=2)+'\n')
        times=sorted(data['process_ms'])
        print(json.dumps(dict(stops=len(data['stops']),median_ms=times[len(times)//2],p95_ms=times[int(len(times)*.95)],max_ms=max(times),max_resident=max(s['resident'] for s in data['stops']),max_cache=max(s['cached'] for s in data['stops']),memory_min=min(data['memory_bytes']),memory_max=max(data['memory_bytes']),window=data['window'],renderer=data['renderer'])),flush=True)
        capture(client,'tour-final')
    finally:
        client.close()


if __name__=='__main__':
    main()
