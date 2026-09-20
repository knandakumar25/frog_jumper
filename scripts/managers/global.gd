extends Node

# ─── Global Game State ──────────────────────────────────────────────────────────
# These variables track overarching game progress across different scenes.
# Unity Migration: Move these to a 'GameState' ScriptableObject or a singleton Service.

var reached_finish = false
var selected_character = "Cyber Shogun"  # Default character
var unlocked_levels = 1  # Track highest unlocked level (1-4)
var current_display_mode: String = "Windowed Fullscreen"

# Local paths for user-specific settings.
# Unity Migration: Use Application.persistentDataPath.
const SETTINGS_PATH: String = "user://user_settings.json"
const KEYBINDINGS_PATH: String = "user://user_keybindings.json"

# Global menu music player reference.
var menu_music_player: AudioStreamPlayer = null

# ─── Lifecycle ──────────────────────────────────────────────────────────────────
func _ready() -> void:
	load_saved_settings()

	if DisplayServer.get_name() != "headless":
		apply_display_mode(current_display_mode)
		# Prevent the OS from putting the device to sleep during gameplay.
		DisplayServer.screen_set_keep_on(true)
		print("Global: Screen keep-awake enabled (prevents device sleep)")

	_register_all_translations()

	# Set default locale to english (matching the CSV column name).
	TranslationServer.set_locale("english")
	print("Global: Locale set to 'english'")

	# Configure Cairo & NotoSans as universal fallbacks for sci-fi fonts (Orbitron / Rajdhani)
	# to ensure correct rendering of Arabic, Hebrew, and CJK characters.
	update_font_for_locale(TranslationServer.get_locale())

	# Connect Joypad detection signals for gamepad hardware (Logitech F310, Xbox, DualShock).
	Input.joy_connection_changed.connect(_on_joy_connection_changed)
	_check_initial_gamepads()
	_setup_prompt_banner()

# ─── Localization & Fonts ──────────────────────────────────────────────────────
# Mapping from Godot's internal locale names to ISO codes for translation matching.
const LOCALE_ISO_MAP: Dictionary = {
	"english": "en",
	"spanish": "es",
	"eurofrench": "fr",
	"brazilianportuguese": "pt_BR",
	"german": "de",
	"italian": "it",
	"polish": "pl",
	"arabic": "ar",
	"turkish": "tr",
	"japanese": "ja",
	"korean": "ko",
	"mandarin": "zh_CN",
	"hebrew": "he",
	"traditionalchinese": "zh_TW",
	"indonesian": "id",
	"vietnamese": "vi",
	"thai": "th"
}

# Registers all .translation files found in the translations directory.
func _register_all_translations() -> void:
	var translations_dir = "res://assets/translations/"
	var dir = DirAccess.open(translations_dir)
	if dir != null:
		dir.list_dir_begin()
		var file_name = dir.get_next()
		while file_name != "":
			if not dir.current_is_dir() and file_name.ends_with(".translation"):
				var full_path = translations_dir + file_name
				if ResourceLoader.exists(full_path):
					var trans = load(full_path)
					if trans is Translation:
						TranslationServer.add_translation(trans)
						var raw = trans.locale
						var iso = LOCALE_ISO_MAP.get(raw, raw)
						if iso != raw:
							# Duplicate translation to register it under the ISO code as well.
							var iso_trans = trans.duplicate()
							iso_trans.locale = iso
							TranslationServer.add_translation(iso_trans)
						print("Global: Registered translation for locale: '", raw, "' (ISO: '", iso, "')")
			file_name = dir.get_next()
		dir.list_dir_end()

# Checks for connected gamepads on startup.
func _check_initial_gamepads() -> void:
	var joypads = Input.get_connected_joypads()
	if not joypads.is_empty():
		var device = joypads[0]
		var joy_name = Input.get_joy_name(device)
		print("Global: Detected connected gamepad: Device ", device, " - ", joy_name)

# ─── Input Device Management ──────────────────────────────────────────────────
signal input_device_changed(is_gamepad: bool)

var is_gamepad_mode: bool = false
var _prompt_label: Label = null

# Creates a persistent UI banner at the bottom of the screen that informs the player
# of the current control scheme (Keyboard vs Gamepad).
func _setup_prompt_banner() -> void:
	if DisplayServer.get_name() == "headless":
		return
	var canvas = CanvasLayer.new()
	canvas.layer = 90

	var margin = MarginContainer.new()
	margin.anchors_preset = Control.PRESET_BOTTOM_WIDE
	margin.add_theme_constant_override("margin_bottom", 12)
	margin.add_theme_constant_override("margin_left", 40)
	margin.add_theme_constant_override("margin_right", 40)

	var panel = PanelContainer.new()
	var style = StyleBoxFlat.new()
	style.bg_color = Color(0.02, 0.05, 0.12, 0.90) # Neon dark cyber panel
	style.border_color = Color(0.0, 0.94, 1.0, 0.8) # Neon cyan border
	style.set_border_width_all(2)
	style.set_corner_radius_all(6)
	style.content_margin_top = 8
	style.content_margin_bottom = 8
	style.content_margin_left = 16
	style.content_margin_right = 16
	panel.add_theme_stylebox_override("panel", style)

	_prompt_label = Label.new()
	_prompt_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_prompt_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER

	var orbitron_font = load("res://assets/fonts/Orbitron-VariableFont.ttf")
	if orbitron_font != null:
		_prompt_label.add_theme_font_override("font", orbitron_font)
	_prompt_label.add_theme_font_size_override("font_size", 16)
	_prompt_label.add_theme_color_override("font_color", Color(0.0, 0.94, 1.0, 0.95))

	panel.add_child(_prompt_label)
	margin.add_child(panel)
	canvas.add_child(margin)
	add_child(canvas)
	_update_prompt_text()

# Updates the control prompt text based on the active input device.
func _update_prompt_text() -> void:
	if _prompt_label == null: return
	if is_gamepad_mode:
		_prompt_label.text = tr("🎮 GAMEPAD MODE") + ": [ D-PAD/L-STICK: " + tr("Move") + "/" + tr("Navigate") + "  |  (A): " + tr("Select") + "/" + tr("Jump") + "  |  (X): " + tr("Sword") + "  |  (Y): " + tr("Karate") + "  |  (LB)/(RB): " + tr("Rotate") + "  |  (START): " + tr("Pause") + "/" + tr("Skip") + " ]"
	else:
		_prompt_label.text = tr("⌨️ KEYBOARD/MOUSE MODE") + ": [ WASD/ARROWS: " + tr("Move") + "  |  SPACE: " + tr("Jump") + "  |  CLICK: " + tr("Sword") + "  |  Q: " + tr("Karate") + "/" + tr("Rotate Left") + "  |  E: " + tr("Rotate Right") + "  |  ESC: " + tr("Pause") + "/" + tr("Skip") + " ]"

func _on_joy_connection_changed(device: int, connected: bool) -> void:
	if connected:
		var joy_name = Input.get_joy_name(device)
		print("Global: Gamepad connected: Device ", device, " - ", joy_name)
		show_gamepad_toast("🎮 GAMEPAD CONNECTED: " + joy_name)
	else:
		print("Global: Gamepad disconnected: Device ", device)
		show_gamepad_toast("⚠️ GAMEPAD DISCONNECTED")

# Monitors input events to automatically toggle between Keyboard and Gamepad modes.
func _input(event: InputEvent) -> void:
	if event is InputEventJoypadButton or event is InputEventJoypadMotion:
		var is_pressed_or_moved = false
		if event is InputEventJoypadButton and event.pressed:
			is_pressed_or_moved = true
		elif event is InputEventJoypadMotion and abs(event.axis_value) > 0.4:
			is_pressed_or_moved = true

		if is_pressed_or_moved and not is_gamepad_mode:
			is_gamepad_mode = true
			_update_prompt_text()
			emit_signal("input_device_changed", true)
			print("Global: Input device switched to -> GAMEPAD MODE 🎮")

	elif event is InputEventKey or event is InputEventMouseButton:
		if event.pressed and is_gamepad_mode:
			is_gamepad_mode = false
			_update_prompt_text()
			emit_signal("input_device_changed", false)
			print("Global: Input device switched to -> KEYBOARD/MOUSE MODE ⌨️")

# ─── UI Prompt Helpers ──────────────────────────────────────────────────────────
func get_nav_prompt() -> String:
	if is_gamepad_mode:
		return tr("[ 🎮 D-PAD / L-STICK: Navigate  |  (A): Select  |  (B): Back ]")
	else:
		return tr("[ ⌨️ ARROWS / WASD: Navigate  |  ENTER / SPACE: Select  |  ESC: Back ]")

func get_home_prompt() -> String:
	if is_gamepad_mode:
		return tr("// PRESS (A) OR (START) ON GAMEPAD TO INITIALIZE //")
	else:
		return tr("// PRESS ANY KEY OR CLICK TO INITIALIZE SYSTEM //")

func get_story_prompt() -> String:
	if is_gamepad_mode:
		return tr("Press (A) / (X) to Advance  |  Press (START) / (Y) to Skip")
	else:
		return tr("Press SPACE / ENTER to Advance  |  Press ESC to Skip")

# Displays a temporary, animated notification toast when a gamepad is connected/disconnected.
func show_gamepad_toast(message: String) -> void:
	if DisplayServer.get_name() == "headless":
		return
	var root = get_tree().root
	if root == null: return

	var canvas = CanvasLayer.new()
	canvas.layer = 100

	var margin = MarginContainer.new()
	margin.anchors_preset = Control.PRESET_TOP_WIDE
	margin.add_theme_constant_override("margin_top", 20)
	margin.add_theme_constant_override("margin_left", 300)
	margin.add_theme_constant_override("margin_right", 300)

	var panel = PanelContainer.new()
	var style = StyleBoxFlat.new()
	style.bg_color = Color(0.05, 0.15, 0.25, 0.95)
	style.border_color = Color(0.0, 0.9, 1.0, 0.9) # Cyber cyan border
	style.set_border_width_all(2)
	style.set_corner_radius_all(6)
	style.content_margin_top = 8
	style.content_margin_bottom = 8
	style.content_margin_left = 16
	style.content_margin_right = 16
	panel.add_theme_stylebox_override("panel", style)

	var label = Label.new()
	label.text = message
	label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	label.add_theme_color_override("font_color", Color(0.0, 1.0, 0.8)) # Bright cyber green/cyan
	label.add_theme_font_size_override("font_size", 20)

	panel.add_child(label)
	margin.add_child(panel)
	canvas.add_child(margin)
	root.add_child(canvas)

	# Animate fade in and fade out using a Tween.
	var tween = canvas.create_tween()
	tween.tween_property(margin, "modulate:a", 1.0, 0.3).from(0.0)
	tween.tween_interval(2.5)
	tween.tween_property(margin, "modulate:a", 0.0, 0.5)
	tween.tween_callback(canvas.queue_free)

# ─── System Settings & Rendering ────────────────────────────────────────────────
# Dynamically adjusts the window mode (Fullscreen, Windowed, etc.).
func update_font_for_locale(locale_code: String) -> void:
	var loc = locale_code.to_lower()
	var is_arabic = "ar" in loc
	var cairo_font = load("res://assets/fonts/Cairo.ttf")
	var arabic_font = load("res://assets/fonts/NotoSansArabic.ttf")
	var noto_font = load("res://assets/fonts/NotoSans-Regular.ttf")

	var fallbacks_list = []
	if is_arabic and cairo_font != null:
		fallbacks_list.append(cairo_font)
	if arabic_font != null: fallbacks_list.append(arabic_font)
	if noto_font != null: fallbacks_list.append(noto_font)

	if is_arabic and cairo_font != null:
		ThemeDB.fallback_font = cairo_font
		print("Global: Set ThemeDB.fallback_font to Cairo.ttf for Arabic")
	elif noto_font != null:
		ThemeDB.fallback_font = noto_font
		print("Global: Set ThemeDB.fallback_font to NotoSans-Regular.ttf")

	# Patch custom theme resource cyber_shogun.tres so Buttons and Labels in CharacterSelection switch fonts for Arabic.
	if ResourceLoader.exists("res://assets/themes/cyber_shogun.tres"):
		var cyber_theme = load("res://assets/themes/cyber_shogun.tres")
		if cyber_theme != null and cyber_theme is Theme:
			if is_arabic and cairo_font != null:
				cyber_theme.default_font = cairo_font
				cyber_theme.set_font("font", "Label", cairo_font)
				cyber_theme.set_font("font", "Button", cairo_font)
				cyber_theme.set_font("font", "LineEdit", cairo_font)
				print("Global: Updated cyber_shogun.tres theme fonts to Cairo.ttf for Arabic")
			else:
				var raj_med = load("res://assets/fonts/Rajdhani-Medium.ttf")
				var raj_bold = load("res://assets/fonts/Rajdhani-Bold.ttf")
				if raj_med != null: cyber_theme.default_font = raj_med
				if raj_med != null: cyber_theme.set_font("font", "Label", raj_med)
				if raj_bold != null: cyber_theme.set_font("font", "Button", raj_bold)

	if fallbacks_list.size() > 0:
		var fonts_to_patch = [
			"res://assets/fonts/Orbitron-VariableFont.ttf",
			"res://assets/fonts/Rajdhani-Bold.ttf",
			"res://assets/fonts/Rajdhani-Medium.ttf",
			"res://assets/fonts/Rajdhani-Regular.ttf"
		]
		for font_path in fonts_to_patch:
			if ResourceLoader.exists(font_path):
				var f = load(font_path)
				if f != null and f is FontFile:
					f.fallbacks = fallbacks_list
		print("Global: Configured Cairo & Arabic font fallbacks for Orbitron & Rajdhani sci-fi fonts")

	# Initialize the custom cyber mouse cursor from a PNG image.
	var cursor_path = ProjectSettings.globalize_path("res://assets/images/cursor.png")
	if FileAccess.file_exists(cursor_path):
		var img = Image.load_from_file(cursor_path)
		if img != null:
			var cursor_tex = ImageTexture.create_from_image(img)
			Input.set_custom_mouse_cursor(cursor_tex, Input.CURSOR_ARROW, Vector2(4, 4))
			print("Global: Custom cyber cursor set successfully at runtime!")

func apply_display_mode(mode_name: String) -> void:
	current_display_mode = mode_name
	save_user_settings()
	match mode_name:
		"Windowed Fullscreen":
			DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED)
			DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_MAXIMIZED)
		"Windowed":
			DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED)
			DisplayServer.window_set_size(Vector2i(1280, 720))
			var screen_size = DisplayServer.screen_get_size()
			var window_size = Vector2i(1280, 720)
			DisplayServer.window_set_position(Vector2i((screen_size.x - window_size.x) / 2, (screen_size.y - window_size.y) / 2))
		_: # "Fullscreen" (Default)
			DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_FULLSCREEN)

# ─── Persistence & Input Customization ──────────────────────────────────────────
func load_saved_settings() -> void:
	load_custom_keybindings()
	if FileAccess.file_exists(SETTINGS_PATH):
		var file = FileAccess.open(SETTINGS_PATH, FileAccess.READ)
		if file != null:
			var text = file.get_as_text()
			file.close()
			var json = JSON.new()
			if json.parse(text) == OK and json.data is Dictionary:
				if json.data.has("display_mode"):
					current_display_mode = json.data["display_mode"]
					print("Global: Loaded saved display mode: ", current_display_mode)

func save_user_settings() -> void:
	var data = {}
	if FileAccess.file_exists(SETTINGS_PATH):
		var file = FileAccess.open(SETTINGS_PATH, FileAccess.READ)
		if file != null:
			var text = file.get_as_text()
			file.close()
			var json = JSON.new()
			if json.parse(text) == OK and json.data is Dictionary:
				data = json.data
	data["display_mode"] = current_display_mode
	var file = FileAccess.open(SETTINGS_PATH, FileAccess.WRITE)
	if file != null:
		file.store_string(JSON.stringify(data))
		file.close()

# Loads custom user keybindings from a JSON file.
func load_custom_keybindings() -> void:
	if not FileAccess.file_exists(KEYBINDINGS_PATH):
		return
	var file = FileAccess.open(KEYBINDINGS_PATH, FileAccess.READ)
	if file == null:
		return
	var text = file.get_as_text()
	file.close()
	var json = JSON.new()
	if json.parse(text) == OK and json.data is Dictionary:
		var bindings = json.data
		for action_name in bindings.keys():
			if InputMap.has_action(action_name):
				var keycodes = bindings[action_name]
				if keycodes is Array and keycodes.size() > 0:
					InputMap.action_erase_events(action_name)
					for kc in keycodes:
						var code_val = int(kc)
						if code_val != 0:
							var ev = InputEventKey.new()
							ev.physical_keycode = code_val
							ev.keycode = code_val
							InputMap.action_add_event(action_name, ev)

# Saves current input action mappings to a JSON file.
func save_custom_keybindings() -> void:
	var bindings = {}
	var actions = ["left", "right", "jump", "escape"]
	for action in actions:
		if InputMap.has_action(action):
			var keycodes = []
			for ev in InputMap.action_get_events(action):
				if ev is InputEventKey:
					var code = ev.physical_keycode if ev.physical_keycode != 0 else ev.keycode
					if code != 0 and not keycodes.has(code):
						keycodes.append(code)
			bindings[action] = keycodes
	var file = FileAccess.open(KEYBINDINGS_PATH, FileAccess.WRITE)
	if file != null:
		file.store_string(JSON.stringify(bindings))
		file.close()

# ─── Audio Management ──────────────────────────────────────────────────────────
func load_mp3_stream(res_path: String) -> AudioStream:
	if ResourceLoader.exists(res_path):
		var res = load(res_path)
		if res != null:
			if res is AudioStreamMP3:
				res.loop = true
			return res
	var global_path = ProjectSettings.globalize_path(res_path)
	if FileAccess.file_exists(global_path):
		var file = FileAccess.open(global_path, FileAccess.READ)
		if file != null:
			var buffer = file.get_buffer(file.get_length())
			file.close()
			if not buffer.is_empty():
				var mp3 = AudioStreamMP3.new()
				mp3.data = buffer
				mp3.loop = true
				return mp3
	return null

func play_audio_track(_res_path: String) -> void:
	pass

func play_home_music() -> void:
	pass

func play_menu_music() -> void:
	pass

func stop_menu_music() -> void:
	pass

# ─── Level Progression ──────────────────────────────────────────────────────────
# Unlocks the next level in the sequence if the current one is completed.
func unlock_next_level(current_level: int) -> void:
	if current_level >= unlocked_levels and current_level < 9:
		unlocked_levels = current_level + 1
	elif current_level == 9:
		print("Global: Campaign Complete! All sectors liberated.")
