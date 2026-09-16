extends Node3D
## Instantiate under a world-space VFX container, then call play().
const PALETTE = preload("res://assets/vfx/operative_palette.gd")
const SHADER = preload("res://assets/vfx/ability_pulse.gdshader")

@export var free_when_finished := true
var _shell: MeshInstance3D
var _material: ShaderMaterial
var _elapsed := 0.0
var _duration := 0.6
var _radius := 3.0
var _playing := false

func _ready() -> void:
	_shell = MeshInstance3D.new()
	var sphere := SphereMesh.new()
	sphere.radius = 1.0
	sphere.height = 2.0
	sphere.radial_segments = 64
	sphere.rings = 32
	_shell.mesh = sphere
	_shell.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	_material = ShaderMaterial.new()
	_material.shader = SHADER
	_shell.material_override = _material
	add_child(_shell)
	visible = false
	set_process(false)

func play(color: Color, radius: float = 3.0, duration: float = 0.6) -> void:
	_radius = maxf(radius, 0.01)
	_duration = maxf(duration, 0.01)
	_elapsed = 0.0
	_material.set_shader_parameter("pulse_color", color)
	_material.set_shader_parameter("opacity", 1.0)
	_shell.scale = Vector3.ONE * _radius * 0.04
	visible = true
	_playing = true
	set_process(true)

func play_for_operative(operative: String, radius: float = 3.0, duration: float = 0.6) -> void:
	play(PALETTE.color_for(operative), radius, duration)

func _process(delta: float) -> void:
	if not _playing:
		return
	_elapsed += delta
	var progress := clampf(_elapsed / _duration, 0.0, 1.0)
	var expansion := 1.0 - pow(1.0 - progress, 2.0)
	_shell.scale = Vector3.ONE * _radius * lerpf(0.04, 1.0, expansion)
	_material.set_shader_parameter("opacity", pow(1.0 - progress, 1.25))
	if progress >= 1.0:
		_playing = false
		visible = false
		set_process(false)
		if free_when_finished:
			queue_free()
