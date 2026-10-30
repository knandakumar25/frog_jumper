extends Node2D
## Deterministic cinematic staging: all motion samples the narrator's single clock.
## No timers, physics, unbounded particle emitters or per-frame node allocations.

const CYAN := Color(0.23, 0.82, 0.91)
var seconds := 0.0
@onready var effects: Node2D = $Effects
@onready var camera: Camera2D = $CameraRig
@onready var hero: Sprite2D = $Shogun
@onready var ghost: Sprite2D = $Afterimage
@onready var portrait: Sprite2D = $Transmission/AXIOM7
@onready var transmission: Node2D = $Transmission


func _ready() -> void:
	## Draw effects above scenery while keeping communication artwork in front.
	effects.draw.connect(_draw_effects)


func sample(time: float) -> void:
	## Seek every visual state directly; safe when instantiated outside the tree.
	seconds = clampf(time, 0.0, 40.0)
	if not is_inside_tree() or not is_instance_valid(hero):
		return
	var t := seconds
	var reveal := clampf((t - 10.0) / 10.0, 0.0, 1.0)
	var action := clampf(t - 20.0, 0.0, 12.0)
	hero.visible = t >= 10.0 and t < 32.0
	$Roof.visible = t >= 10.0
	transmission.visible = t >= 32.0
	ghost.visible = t >= 28.1 and t < 29.15
	camera.offset = Vector2.ZERO
	camera.position = Vector2(960 + sin(t * 0.09) * 65, 540)
	camera.zoom = Vector2.ONE
	hero.position = Vector2(530, 765)
	hero.scale = Vector2.ONE
	var row := 0
	var frame := int(t * 5.0) % 8
	if t >= 10.0 and t < 20.0:
		camera.zoom = Vector2.ONE * lerpf(1.05, 1.48, smoothstep(0.0, 1.0, reveal))
		camera.position = Vector2(lerpf(870, 710, reveal), lerpf(520, 500, reveal))
		# The final idle pose settles the sword hand before deployment.
		if t > 17.0:
			row = 4
			frame = mini(int((t - 17.0) * 2.6), 7)
	elif t >= 20.0 and t < 32.0:
		camera.zoom = Vector2.ONE * 1.08
		camera.position = Vector2(960 + clampf(action - 4, 0, 6) * 13, 515)
		row = 1
		frame = int(action * 12) % 8
		if action < 1.0:
			# Compression and forward lean give the run an explicit anticipation.
			hero.scale = Vector2(1.0 + sin(action * PI) * 0.05, 1.0 - sin(action * PI) * 0.09)
			hero.position.x = lerpf(530, 570, action)
		elif action < 4.0:
			hero.position.x = lerpf(570, 690, (action - 1.0) / 3.0)
		elif action < 6.8:
			row = 2
			var jump := (action - 4.0) / 2.8
			hero.position = Vector2(lerpf(690, 1120, jump), 765 - sin(jump * PI) * 225)
		elif action < 7.6:
			row = 0
			hero.position.x = 1120
			var weight := sin((action - 6.8) / 0.8 * PI)
			hero.scale = Vector2(1.0 + weight * 0.12, 1.0 - weight * 0.16)
			camera.offset.y = weight * 5
		elif action < 9.2:
			row = 3
			hero.position.x = lerpf(1120, 1520, smoothstep(0.0, 1.0, (action - 7.6) / 1.6))
		else:
			row = 0
			hero.position.x = 1520
		ghost.position = hero.position - Vector2(100, 0)
		ghost.frame = row * 8 + frame
		ghost.modulate = Color(0.25, 0.85, 1.0, 0.22)
	else:
		camera.offset = Vector2.ZERO
	camera.zoom *= get_viewport_rect().size / Vector2(1920, 1080)
	hero.frame = row * 8 + frame
	# Three independent scene layers keep camera travel readable at different depths.
	$FarCity.position.x = -300 - fmod(t * 3.0, 80.0)
	$MidCity.position.x = -300 - fmod(t * 7.0, 100.0)
	$CityLights.position.x = $MidCity.position.x
	transmission.modulate.a = smoothstep(32.0, 33.0, t)
	portrait.scale = Vector2.ONE * (0.62 + sin(t * 1.8) * 0.006)
	effects.queue_redraw()


func _draw_effects() -> void:
	## Bounded rain, drone sweeps, transit, sparks and communication graphics.
	var t := seconds
	# Rain uses analytic positions at three depths: seeking never resets an emitter.
	for depth in range(3):
		for index in range(70):
			var x := fposmod(index * 173.7 + depth * 63 - t * (65 + depth * 25), 2600.0) - 350
			var y := fposmod(index * 97.3 + t * (280 + depth * 170), 1300.0) - 100
			effects.draw_line(Vector2(x, y), Vector2(x - 5 - depth * 3, y + 14 + depth * 8), Color(0.36, 0.66, 0.78, 0.10 + depth * 0.07), 1 + depth * 0.4)
	for index in range(3):
		var drone := Vector2(300 + index * 570 + sin(t * 0.27 + index) * 140, 190 + index * 58 + sin(t * 0.9 + index) * 12)
		var aim := sin(t * 0.5 + index) * 180
		effects.draw_colored_polygon(PackedVector2Array([drone, drone + Vector2(aim - 90, 590), drone + Vector2(aim + 130, 590)]), Color(0.40, 0.75, 0.85, 0.055))
		if has_node('Drone'):
			var texture: Texture2D = $Drone.texture
			effects.draw_texture_rect_region(texture, Rect2(drone - Vector2(70, 20), Vector2(140, 40)), Rect2(840, 440, 240, 60))
			effects.draw_texture_rect_region($DroneLights.texture, Rect2(drone - Vector2(70, 20), Vector2(140, 40)), Rect2(840, 440, 240, 60))
		effects.draw_circle(drone + Vector2(0, 8), 3, Color(0.9, 0.2, 0.22))
	for index in range(8):
		var x := fposmod(index * 230.0 + t * 240, 2300.0) - 200
		effects.draw_line(Vector2(x, 875), Vector2(x + 45, 871), Color(0.16, 0.76, 0.85, 0.6), 3)
	if t >= 20.0 and t < 32.0:
		var scan_x := 1390 + sin(t * 0.7) * 90
		effects.draw_colored_polygon(PackedVector2Array([Vector2(scan_x, 250), Vector2(scan_x - 60, 760), Vector2(scan_x + 90, 760)]), Color(0.95, 0.16, 0.22, 0.13))
		# Landing sparks spread out and decay; dash streaks stay behind the operative.
		var burst := t - 26.8
		if burst >= 0 and burst < 0.7:
			for index in range(14):
				var p := Vector2(1120 + (index - 7) * burst * 65, 765 - sin(burst * PI) * (30 + index * 8))
				effects.draw_line(p, p + Vector2(8, 5), Color(1, 0.68, 0.3, 1.0 - burst / 0.7), 2)
		if t >= 27.8 and t < 29.3:
			for index in range(5):
				effects.draw_line(hero.position + Vector2(-250, -80 - index * 24), hero.position + Vector2(-70, -90 - index * 24), Color(0.3, 0.85, 1, 0.5), 3)
	if t > 17.2 and t < 17.7:
		var blade := hero.position + Vector2(155, -220)
		effects.draw_line(blade - Vector2(26, 0), blade + Vector2(26, 0), Color(0.9, 1, 1), 2)
		effects.draw_line(blade - Vector2(0, 18), blade + Vector2(0, 18), Color(0.9, 1, 1), 2)
	if t >= 32:
		var assembly := smoothstep(32.0, 34.0, t)
		effects.draw_rect(Rect2(240, 180, 1440 * assembly, 650), Color(0.015, 0.045, 0.075, 0.96))
		effects.draw_arc(Vector2(1270, 465), 255, -PI / 2, -PI / 2 + TAU * assembly, 100, CYAN, 2, true)
		for index in range(22):
			var y := 230 + index * 25
			effects.draw_line(Vector2(280, y), Vector2(280 + (140 + index % 5 * 45) * assembly, y), Color(0.1, 0.48, 0.58, 0.35), 2)
