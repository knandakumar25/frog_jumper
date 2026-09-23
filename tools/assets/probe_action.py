import bpy
action = bpy.data.actions.new(name="TestAction")
if hasattr(action, 'slots'):
    try:
        slot = action.slots.new('OBJECT', name="Slot")
        print(f"Created slot: {slot}")
    except Exception as e:
        print(f"Failed to create slot: {e}")
