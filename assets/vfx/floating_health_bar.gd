extends Node3D
const SHADER = preload("res://assets/vfx/floating_health_bar.gdshader")
@export var bar_width := 1.35
@export var follow_offset := Vector3(0.0, 2.3, 0.0)
@export var healthy_color := Color(0.02, 0.85, 1.0)
var _target: Node3D
var _visuals: Node3D
var _material: ShaderMaterial
var _label: Label3D
var _ratio := 1.0
var _trail := 1.0
var _glitch_time := 0.0

func _ready() -> void:
	_visuals = Node3D.new()
	add_child(_visuals)
	var mesh := MeshInstance3D.new()
	var quad := QuadMesh.new()
	quad.size = Vector2(bar_width, 0.14)
	mesh.mesh = quad
	mesh.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	_material = ShaderMaterial.new()
	_material.shader = SHADER
	mesh.material_override = _material
	_visuals.add_child(mesh)
	_label = Label3D.new()
	_label.font_size = 32
	_label.pixel_size = 0.0035
	_label.outline_size = 5
	_label.position.y = 0.16
	_visuals.add_child(_label)
	set_health(100.0, 100.0)

func follow(target: Node3D, offset: Vector3 = Vector3(0.0, 2.3, 0.0)) -> void:
	_target = target
	follow_offset = offset
	if is_instance_valid(_target):
		global_position = _target.global_position + follow_offset

func set_health(current: float, maximum: float) -> void:
	var safe_maximum := maxf(maximum, 0.0)
	var safe_current := clampf(current, 0.0, safe_maximum)
	var next_ratio := safe_current / safe_maximum if safe_maximum > 0.0 else 0.0
	if next_ratio < _ratio:
		_glitch_time = 0.18
	_ratio = next_ratio
	_trail = maxf(_trail, _ratio)
	var color := healthy_color
	if _ratio < 0.3:
		color = Color(1.0, 0.06, 0.14)
	elif _ratio < 0.6:
		color = Color(1.0, 0.61, 0.06)
	_material.set_shader_parameter("progress", _ratio)
	_material.set_shader_parameter("health_color", color)
	_label.modulate = color
	_label.text = "HP  %d / %d" % [ceili(safe_current), ceili(safe_maximum)]

func _process(delta: float) -> void:
	if is_instance_valid(_target):
		global_position = _target.global_position + follow_offset
	var camera := get_viewport().get_camera_3d()
	if camera != null:
		_visuals.global_basis = camera.global_basis.orthonormalized()
	_glitch_time = maxf(0.0, _glitch_time - delta)
	_visuals.position.x = sin(_glitch_time * 240.0) * 0.025 if _glitch_time > 0.0 else 0.0
	_trail = move_toward(_trail, _ratio, delta * 0.7)
	_material.set_shader_parameter("trail_progress", _trail)
