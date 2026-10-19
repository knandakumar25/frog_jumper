@tool
extends Control
## Orthographic Blender illustrations composited and animated entirely in 2D.
## Three shared textures per screen; no video, model, viewport, or per-frame loads.

@export_enum("City", "Command Bay") var setting: int = 0
@export var foundation: Texture2D
@export var architecture: Texture2D
@export var illumination: Texture2D
@export var motion_enabled: bool = true:
	set(value):
		motion_enabled = value
		set_process(value)
		queue_redraw()

var elapsed: float = 0.0
const ART_SIZE := Vector2(1920, 1080)
const CYAN := Color(0.22, 0.72, 0.79)


func _ready() -> void:
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	focus_mode = Control.FOCUS_NONE
	resized.connect(queue_redraw)
	set_process(motion_enabled and not Engine.is_editor_hint())
	queue_redraw()


func _process(delta: float) -> void:
	if not is_visible_in_tree() or not motion_enabled:
		return
	elapsed += delta
	queue_redraw()


func parallax_at(seconds: float) -> Vector2:
	return Vector2(sin(seconds * TAU / 24.0) * 7.0, sin(seconds * TAU / 16.0) * 3.0)


func _draw() -> void:
	if size.x <= 0.0 or size.y <= 0.0:
		return
	draw_rect(Rect2(Vector2.ZERO, size), Color("070d17"))
	# Overscan prevents exposed edges while the architecture drifts.
	var cover := maxf(size.x / ART_SIZE.x, size.y / ART_SIZE.y) * 1.025
	var origin := (size - ART_SIZE * cover) * 0.5
	draw_set_transform(origin, 0.0, Vector2.ONE * cover)
	var t := elapsed if motion_enabled else 0.0
	var drift := parallax_at(t)
	var bounds := Rect2(Vector2.ZERO, ART_SIZE)
	if foundation:
		draw_texture_rect(foundation, bounds, false)
	bounds.position = drift
	if architecture:
		draw_texture_rect(architecture, bounds, false)
	if illumination:
		var pulse := 0.82 + 0.12 * sin(t * TAU / 6.0)
		draw_texture_rect(illumination, bounds, false, Color(1, 1, 1, pulse))
	if setting == 0:
		_draw_city_motion(t, drift)
	else:
		_draw_bay_motion(t)
	draw_set_transform(Vector2.ZERO)


func _draw_city_motion(t: float, drift: Vector2) -> void:
	# Rain is behind UI, restrained to the visible skyline.
	for i in range(72):
		var x := 630.0 + fposmod(i * 173.0 - t * 28.0, 1340.0)
		var y := fposmod(i * 113.0 + t * (110.0 + (i % 4) * 25.0), 1130.0) - 25.0
		var length := 12.0 + (i % 4) * 5.0
		draw_line(Vector2(x, y), Vector2(x - 4, y + length), Color(0.37, 0.63, 0.70, 0.12), 1.0, true)
	# Two tiny transit lights follow the elevated rail, with fading trails.
	for i in range(2):
		var x := 650.0 + fposmod(t * 48.0 + i * 635.0, 1270.0)
		var y := 905.0 - (x - 650.0) * 93.0 / 1270.0
		for trail in range(10):
			var at := Vector2(x - trail * 6.0, y + trail * 0.44) + drift
			draw_line(at, at + Vector2(5, -0.37), Color(0.50, 0.91, 0.91, (1.0 - trail / 10.0) * 0.7), 2.0, true)
	# A graded scrim keeps the menu copy readable without hiding the skyline.
	for i in range(40):
		var alpha := 0.65 * pow(1.0 - float(i) / 40.0, 1.4)
		draw_rect(Rect2(i * 25.0, 0, 26, 1080), Color(0.015, 0.025, 0.045, alpha))


func _draw_bay_motion(t: float) -> void:
	var center := Vector2(960, 580)
	var angle := t * TAU / 32.0
	for i in range(3):
		var start := angle + i * TAU / 3.0
		draw_arc(center, 391.0, start, start + 0.36, 28, Color(0.18, 0.65, 0.72, 0.38), 2.0, true)
	# Slow scan rather than flashing; small data motes follow the peripheral rails.
	var scan_y := fposmod(t * 22.0, 1080.0)
	for i in range(10):
		draw_line(Vector2(230, scan_y - i * 2), Vector2(1690, scan_y - i * 2), Color(0.20, 0.69, 0.74, 0.025 * (1.0 - i / 10.0)), 1.0)
	for side in [58.0, 1862.0]:
		for i in range(7):
			var y := 180.0 + fposmod(i * 113.0 + t * 24.0, 720.0)
			draw_circle(Vector2(side, y), 1.6, Color(0.45, 0.84, 0.83, 0.5))
