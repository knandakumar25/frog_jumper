import bpy
print("NLA Operators:")
for op in dir(bpy.ops.nla):
    print(op)
