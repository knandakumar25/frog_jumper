extends Node
## One reusable helper per entity; overlays all descendant meshes then restores them.
const SHADER = preload("res://assets/vfx/hit_flash.gdshader")
var _meshes: Array[MeshInstance3D] = []
var _previous: Array[Material] = []
var _material: ShaderMaterial
var _elapsed := 0.0
var _duration := 0.12

func _ready() -> void:
	_material = ShaderMaterial.new()
	_material.shader = SHADER
	set_process(false)

func play(target: Node3D, duration: float = 0.12, energy: float = 8.0) -> void:
	restore()
	if not is_instance_valid(target):
		return
	_duration = maxf(duration, 0.01)
	_elapsed = 0.0
	_material.set_shader_parameter("strength", 1.0)
	_material.set_shader_parameter("energy", maxf(energy, 0.0))
	_collect(target)
	set_process(not _meshes.is_empty())

func _collect(node: Node) -> void:
	if node is MeshInstance3D:
		_meshes.append(node)
		_previous.append(node.material_overlay)
		node.material_overlay = _material
	for child in node.get_children():
		_collect(child)

func restore() -> void:
	for index in _meshes.size():
		var mesh := _meshes[index]
		if is_instance_valid(mesh) and mesh.material_overlay == _material:
			mesh.material_overlay = _previous[index]
	_meshes.clear()
	_previous.clear()
	set_process(false)

func _process(delta: float) -> void:
	_elapsed += delta
	var progress := clampf(_elapsed / _duration, 0.0, 1.0)
	_material.set_shader_parameter("strength", 1.0 - smoothstep(0.3, 1.0, progress))
	if progress >= 1.0:
		restore()

func _exit_tree() -> void:
	restore()
