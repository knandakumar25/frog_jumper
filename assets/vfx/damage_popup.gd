extends Node3D
## Bounded, time-based RGB displacement; never random-walks away from the hit.
@export var free_when_finished := true
@export var rise_distance := 0.85
var _label: Label3D
var _cyan: Label3D
var _magenta: Label3D
var _elapsed := 0.0
var _duration := 1.0
var _color := Color.WHITE

func _ready() -> void:
	_label = _make_label(0)
	_cyan = _make_label(-1)
	_magenta = _make_label(-2)
	visible = false
	set_process(false)

func _make_label(priority: int) -> Label3D:
	var label := Label3D.new()
	label.font_size = 64
	label.pixel_size = 0.005
	label.outline_size = 9
	label.outline_modulate = Color(0.015, 0.02, 0.045, 0.8)
	label.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	label.no_depth_test = false
	label.render_priority = priority
	add_child(label)
	return label

func play(amount: float, color: Color = Color.WHITE, lifetime: float = 1.0) -> void:
	_elapsed = 0.0
	_duration = maxf(lifetime, 0.05)
	_color = color
	for label in [_label, _cyan, _magenta]:
		label.text = str(roundi(amount))
		label.position = Vector3.ZERO
	_label.modulate = color
	_cyan.visible = false
	_magenta.visible = false
	visible = true
	set_process(true)

func _process(delta: float) -> void:
	_elapsed += delta
	var progress := clampf(_elapsed / _duration, 0.0, 1.0)
	var tick := int(_elapsed * 30.0)
	var glitch := tick % 11 == 1 or tick % 11 == 2 or tick % 17 == 6
	var displacement := sin(float(tick) * 9.73) * 0.06 if glitch else 0.0
	var y := rise_distance * (1.0 - pow(1.0 - progress, 2.0))
	var fade := 1.0 - smoothstep(0.55, 1.0, progress)
	_label.position = Vector3(displacement, y, 0.0)
	var tint := Color.WHITE if glitch else _color
	tint.a = fade
	_label.modulate = tint
	_label.outline_modulate.a = fade * 0.8
	_cyan.position = Vector3(displacement - 0.075, y + 0.012, 0.0)
	_magenta.position = Vector3(displacement + 0.075, y - 0.012, 0.0)
	_cyan.modulate = Color(0.02, 1.0, 1.0, fade * 0.65)
	_magenta.modulate = Color(1.0, 0.05, 0.65, fade * 0.65)
	_cyan.visible = glitch
	_magenta.visible = glitch
	if progress >= 1.0:
		visible = false
		set_process(false)
		if free_when_finished:
			queue_free()
