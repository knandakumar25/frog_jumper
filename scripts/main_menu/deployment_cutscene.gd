extends Control
## Forty-second live deployment cinematic; audio is the master timeline.
## Missing/ended/stalled narration falls back to elapsed time, never to video decoding.

signal completed
const BRIEFING := "res://scenes/main_menu/story_intro.tscn"
const VOICE := "res://assets/cutscenes/deployment/narration.ogg"
const DURATION := 40.0
const CUES := [
	[1.0, 9.5, "Beyond the neon, the city waits. Every rooftop is watched. Every signal is traced."],
	[11.0, 19.5, "But one operative moves beyond their reach. Cyber Shogun. Your deployment begins tonight."],
	[21.0, 31.0, "Follow the rooftops. Keep your blade ready. Somewhere beneath these lights, the resistance is still alive."],
	[33.0, 39.0, "This is AXIOM-7. Secure channel established. Stand by for your mission briefing."]
]
## QA may disable replacement/audio without substituting production resources.
@export var transition_to_briefing := true
@export var narration_path := VOICE
var transitioning := false
var elapsed := 0.0
var _last_audio := -1.0
var _stalled := 0.0
@onready var narrator: AudioStreamPlayer = $Narrator
@onready var subtitle: Label = $UI/SubtitlePanel/Subtitle
@onready var stage: Node2D = $Frame/Screen/World/Stage
@onready var fade: ColorRect = $UI/Fade
@onready var shot_label: Label = $UI/ShotLabel


func _ready() -> void:
	## Stop menu music and start the separate, unchanged narration if available.
	var global := get_node_or_null('/root/Global')
	if global and global.has_method('stop_menu_music'):
		global.call('stop_menu_music')
	if global and global.has_method('set_control_prompt_visible'):
		global.call('set_control_prompt_visible', false)
	$UI/Skip.pressed.connect(finish)
	$UI/Skip.grab_focus()
	# Skip straight to the briefing while the cutscene art awaits regeneration
	# (docs/ART_REGENERATION.md), as the C# cutscenes do when ResourcesAvailable is false.
	if not stage.art_available():
		finish.call_deferred()
		return
	if not narration_path.is_empty() and ResourceLoader.exists(narration_path):
		narrator.stream = load(narration_path)
		_apply_voice_gain(narration_path)
		narrator.play()
	seek_visuals(0.0)


func _apply_voice_gain(path: String) -> void:
	## Level-match the narration with the AXIOM-7 briefing that follows it, using the same table
	## and Voice bus as scripts/managers/VoiceGain.cs. Volume does not affect the timeline,
	## which is driven by playback position. Boosts are applied only where the Voice bus
	## limiter runs: the bus must exist, and Web builds (whose default sample playback skips
	## bus effects) get cuts only. A missing table or entry leaves the player unchanged.
	const TABLE := "res://assets/audio/voice_gain.json"
	if not FileAccess.file_exists(TABLE):
		return
	var bus := AudioServer.get_bus_index("Voice")
	var table: Variant = JSON.parse_string(FileAccess.get_file_as_string(TABLE))
	if not table is Dictionary or not (table as Dictionary).get("files") is Dictionary:
		return
	var entry: Variant = (table as Dictionary)["files"].get(path)
	if not entry is Dictionary or not (entry as Dictionary).get("gain_db") is float:
		return
	# Same clamp as VoiceGain.MaxGainDb and MAX_GAIN_DB in tools/assets/voice_gain.py.
	var gain := clampf((entry as Dictionary)["gain_db"], -12.0, 12.0)
	if bus >= 0:
		narrator.bus = "Voice"
	if gain > 0.0 and (bus < 0 or OS.has_feature("web")):
		gain = 0.0
	narrator.volume_db += gain


func _process(delta: float) -> void:
	## Compensate the audio mixer/output latency and recover from absent/stalled audio.
	if transitioning:
		return
	var clock := elapsed + delta
	if is_instance_valid(narrator) and narrator.playing:
		var playback_position := narrator.get_playback_position()
		var audio_time := maxf(0.0, playback_position + AudioServer.get_time_since_last_mix() - AudioServer.get_output_latency())
		_stalled = _stalled + delta if playback_position <= _last_audio else 0.0
		_last_audio = playback_position
		if _stalled < 0.5:
			clock = maxf(elapsed, audio_time)
	seek_visuals(clock)
	if elapsed >= DURATION:
		finish()


func seek_visuals(time: float) -> void:
	## Deterministically seek shots/subtitles for runtime QA; never seek narration.
	elapsed = clampf(time, 0.0, DURATION)
	if not is_inside_tree() or not is_instance_valid(stage):
		return
	stage.sample(elapsed)
	subtitle.text = subtitle_at(elapsed)
	$UI/SubtitlePanel.visible = not subtitle.text.is_empty()
	var shots := ['01  /  OCCUPIED CITY', '02  /  CYBER SHOGUN', '03  /  DEPLOYMENT', '04  /  SECURE TRANSMISSION']
	shot_label.text = shots[0 if elapsed < 10 else 1 if elapsed < 20 else 2 if elapsed < 32 else 3]
	# Only the opening and final handoff fade; shot cuts remain continuous.
	fade.color.a = maxf(1.0 - smoothstep(0.0, 0.8, elapsed), smoothstep(39.0, 40.0, elapsed))
	var scan_material: ShaderMaterial = $Frame/Screen/World/Stage/Transmission/AXIOM7.material
	scan_material.set_shader_parameter('seconds', elapsed)


func subtitle_at(time: float) -> String:
	## Return the unchanged localized dialogue for a half-open cue interval.
	for cue in CUES:
		if time >= cue[0] and time < cue[1]:
			return tr(cue[2])
	return ''


func _input(event: InputEvent) -> void:
	## Catch the Skip region before Global consumes mouse events project-wide.
	## Native touch is handled directly, independent of mouse-emulation settings.
	if transitioning or not is_inside_tree():
		return
	var pointer_pressed := false
	var point := Vector2.ZERO
	if event is InputEventScreenTouch:
		pointer_pressed = event.pressed
		point = event.position
	elif event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT:
		pointer_pressed = event.pressed
		point = event.position
	if pointer_pressed and $UI/Skip.get_global_rect().has_point(point):
		get_viewport().set_input_as_handled()
		finish()


func _unhandled_input(event: InputEvent) -> void:
	## Keyboard/controller skip uses the same guarded path as the touch button.
	if not is_inside_tree():
		return
	if event.is_action_pressed('ui_cancel') or (event is InputEventJoypadButton and event.pressed and event.button_index == JOY_BUTTON_START):
		get_viewport().set_input_as_handled()
		finish()


func finish() -> void:
	## Stop once and defer scene replacement; repeated input/end events are harmless.
	if transitioning or not is_inside_tree():
		return
	transitioning = true
	set_process(false)
	_stop_audio()
	completed.emit()
	if transition_to_briefing:
		get_tree().call_deferred('change_scene_to_file', BRIEFING)


func _stop_audio() -> void:
	## Cleanup also works for a standalone node whose onready fields never resolved.
	if is_instance_valid(narrator):
		narrator.stop()


func _exit_tree() -> void:
	## Removing the scene terminates audio; all visuals are owned child nodes.
	_stop_audio()
	set_process(false)
	var global := get_node_or_null('/root/Global')
	if global and global.has_method('set_control_prompt_visible'):
		global.call('set_control_prompt_visible', true)
