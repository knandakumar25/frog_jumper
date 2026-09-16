extends Node3D
const SHADER = preload("res://assets/vfx/cooldown_ring.gdshader")
@export var radius := 0.75
@export var follow_offset := Vector3(0.0, 0.035, 0.0)
var _target: Node3D
var _material: ShaderMaterial
var progress := 1.0

func _ready() -> void:
	var mesh := MeshInstance3D.new()
	var plane := PlaneMesh.new()
	plane.size = Vector2.ONE * maxf(radius, 0.01) * 2.0
	mesh.mesh = plane
	mesh.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	_material = ShaderMaterial.new()
	_material.shader = SHADER
	mesh.material_override = _material
	add_child(mesh)
	set_progress(progress)

func follow(target: Node3D, offset: Vector3 = Vector3(0.0, 0.035, 0.0)) -> void:
	_target = target
	follow_offset = offset
	if is_instance_valid(_target):
		global_position = _target.global_position + follow_offset

func set_progress(value: float) -> void:
	progress = clampf(value, 0.0, 1.0)
	_material.set_shader_parameter("progress", progress)

func set_cooldown(remaining: float, total: float) -> void:
	set_progress(1.0 - maxf(remaining, 0.0) / total if total > 0.0 else 1.0)

func set_color(color: Color) -> void:
	_material.set_shader_parameter("ring_color", color)

func _process(_delta: float) -> void:
	if is_instance_valid(_target):
		global_position = _target.global_position + follow_offset
		global_basis = Basis.IDENTITY
