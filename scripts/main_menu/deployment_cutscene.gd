extends Control
## Plays the deployment film before the briefing; missing media falls through.
## Narration remains a separate resource so voice work does not require rendering.

const BRIEFING := "res://scenes/main_menu/story_intro.tscn"
const FILM := "res://assets/cutscenes/deployment/deployment.ogv"
const VOICE := "res://assets/cutscenes/deployment/narration.ogg"
var transitioning := false
var elapsed := 0.0
var player: VideoStreamPlayer
var narrator: AudioStreamPlayer
var subtitle: Label
const CUES := [
	[1.0, 9.5, "Beyond the neon, the city waits. Every rooftop is watched. Every signal is traced."],
	[11.0, 19.5, "But one operative moves beyond their reach. Cyber Shogun. Your deployment begins tonight."],
	[21.0, 31.0, "Follow the rooftops. Keep your blade ready. Somewhere beneath these lights, the resistance is still alive."],
	[33.0, 39.0, "This is AXIOM-7. Secure channel established. Stand by for your mission briefing."]
]


func _ready() -> void:
	## Build aspect-preserving playback and accessible skip controls in the tree.
	var global := get_node_or_null("/root/Global")
	if global and global.has_method("stop_menu_music"):
		global.call("stop_menu_music")
	if not ResourceLoader.exists(FILM):
		call_deferred("finish")
		return
	var frame := AspectRatioContainer.new()
	frame.ratio = 16.0 / 9.0
	frame.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	frame.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(frame)
	player = VideoStreamPlayer.new()
	player.expand = true
	player.mouse_filter = Control.MOUSE_FILTER_IGNORE
	frame.add_child(player)
	player.stream = load(FILM)
	player.finished.connect(finish)
	narrator = AudioStreamPlayer.new()
	add_child(narrator)
	if ResourceLoader.exists(VOICE):
		narrator.stream = load(VOICE)
		narrator.play()
	subtitle = Label.new()
	subtitle.set_anchors_and_offsets_preset(Control.PRESET_BOTTOM_WIDE)
	subtitle.offset_top = -150
	subtitle.offset_bottom = -65
	subtitle.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	subtitle.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	subtitle.add_theme_font_size_override("font_size", 26)
	subtitle.add_theme_color_override("font_outline_color", Color.BLACK)
	subtitle.add_theme_constant_override("outline_size", 8)
	add_child(subtitle)
	var skip := Button.new()
	skip.text = tr("Skip") + "  [Esc / Start]"
	skip.position = Vector2(30, 30)
	skip.pressed.connect(finish)
	add_child(skip)
	skip.grab_focus()
	player.play()


func _process(delta: float) -> void:
	## Use video position as the subtitle clock, avoiding accumulated audio drift.
	if transitioning or not is_instance_valid(player):
		return
	elapsed += delta
	subtitle.text = ""
	for cue in CUES:
		if player.stream_position >= cue[0] and player.stream_position < cue[1]:
			subtitle.text = tr(cue[2])
			break
	if elapsed > 43.0:
		finish() # Guard a decoder that never emits its finished signal.


func _unhandled_input(event: InputEvent) -> void:
	## Keyboard/controller skipping shares the same idempotent transition path.
	if event.is_action_pressed("ui_cancel") or (event is InputEventJoypadButton and event.pressed and event.button_index == JOY_BUTTON_START):
		get_viewport().set_input_as_handled()
		finish()


func finish() -> void:
	## Stop media and defer replacement so simultaneous skip/end signals are safe.
	if transitioning or not is_inside_tree():
		return
	transitioning = true
	if is_instance_valid(player):
		player.stop()
	if is_instance_valid(narrator):
		narrator.stop()
	get_tree().call_deferred("change_scene_to_file", BRIEFING)
