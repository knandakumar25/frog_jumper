extends Node

# ─── Global Game State ──────────────────────────────────────────────────────────
# These variables track overarching game progress across different scenes.
# Unity Migration: Move these to a 'GameState' ScriptableObject or a singleton Service.

var reached_finish = false
var selected_character = "Cyber Shogun"  # Default character
var unlocked_levels = 1  # Highest mission reached (set from the save on Continue, raised by unlock_next_level)
var current_display_mode: String = "Windowed Fullscreen"

# Settings and keybindings are stored on the signed-in account through SessionManager (2026-11-18).

# Global menu music player reference.
var menu_music_player: AudioStreamPlayer = null

# ─── Lifecycle ──────────────────────────────────────────────────────────────────
func _ready() -> void:
	# Global is registered before SessionManager; defer until all autoloads exist, then reload
	# whenever an account signs in (its settings live on the account) or out (back to defaults).
	call_deferred("_connect_account_settings")
	Input.mouse_mode = Input.MOUSE_MODE_VISIBLE

	if DisplayServer.get_name() != "headless":
		apply_display_mode(current_display_mode)
		# Prevent the OS from putting the device to sleep during gameplay.
		DisplayServer.screen_set_keep_on(true)
		print("Global: Screen keep-awake enabled (prevents device sleep)")

	_register_all_translations()

	# Use ISO locale for Godot text direction/shaping; LocaleCatalog maps legacy column names.
	TranslationServer.set_locale("en")
	print("Global: Locale set to 'en'")

	# Configure Cairo & NotoSans as universal fallbacks for sci-fi fonts (Orbitron / Rajdhani)
	# to ensure correct rendering of Arabic, Hebrew, and CJK characters.
	update_font_for_locale(TranslationServer.get_locale())

	# Connect Joypad detection signals for gamepad hardware (Logitech F310, Xbox, DualShock).
	Input.joy_connection_changed.connect(_on_joy_connection_changed)
	_check_initial_gamepads()
	InputPromptService.PromptsChanged.connect(_on_prompts_changed)
	_setup_prompt_banner()

# ─── Localization & Fonts ──────────────────────────────────────────────────────
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
						LocaleService.RegisterSourceAliases(trans)
						TranslationServer.add_translation(trans)
						var raw = trans.locale
						var iso = LocaleService.Iso(raw)
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
# A RichTextLabel (button images for controller prompts); tests may substitute a plain Label.
var _prompt_label: Control = null

const GoStyleBoxCutScript = preload("res://addons/gohud/widgets/go_stylebox_cut.gd")

# Creates a persistent GoHUD sci-fi tactical telemetry bar at the top of the screen
# informing the player of the active control scheme (Keyboard vs Gamepad).
func _setup_prompt_banner() -> void:
	if DisplayServer.get_name() == "headless":
		return
	var canvas = CanvasLayer.new()
	canvas.layer = 90

	var root_ctrl = Control.new()
	root_ctrl.mouse_filter = Control.MOUSE_FILTER_IGNORE
	root_ctrl.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	canvas.add_child(root_ctrl)

	var margin = MarginContainer.new()
	margin.mouse_filter = Control.MOUSE_FILTER_IGNORE
	margin.set_anchors_and_offsets_preset(Control.PRESET_TOP_WIDE)
	margin.add_theme_constant_override("margin_top", 10)
	margin.add_theme_constant_override("margin_left", 28)
	margin.add_theme_constant_override("margin_right", 28)
	root_ctrl.add_child(margin)

	var center_row = HBoxContainer.new()
	center_row.mouse_filter = Control.MOUSE_FILTER_IGNORE
	center_row.alignment = BoxContainer.ALIGNMENT_CENTER
	margin.add_child(center_row)

	var panel = PanelContainer.new()
	panel.mouse_filter = Control.MOUSE_FILTER_IGNORE
	panel.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	var style = GoStyleBoxCutScript.new()
	style.bg_color = Color(0.02, 0.055, 0.12, 0.88)
	style.border_color = Color(0.0, 0.78, 0.96, 0.72)
	style.border_width = 1.5
	style.cut = 10.0
	style.cut_corners = GoStyleBoxCutScript.DIAGONAL
	style.edge_color = Color(0.0, 0.95, 1.0, 0.95)
	style.edge_width = 2.5
	style.edge_side = SIDE_TOP
	style.glow_color = Color(0.0, 0.88, 1.0, 0.22)
	style.glow_size = 5.0
	style.content_margin_top = 7.0
	style.content_margin_bottom = 7.0
	style.content_margin_left = 18.0
	style.content_margin_right = 18.0
	panel.add_theme_stylebox_override("panel", style)

	var inner_hbox = HBoxContainer.new()
	inner_hbox.mouse_filter = Control.MOUSE_FILTER_IGNORE
	inner_hbox.add_theme_constant_override("separation", 14)
	panel.add_child(inner_hbox)

	var orbitron_font = load("res://assets/fonts/Orbitron-VariableFont.ttf")

	var sys_tag = Label.new()
	sys_tag.text = "◆ AXIOM-OS v4.7"
	if orbitron_font != null:
		sys_tag.add_theme_font_override("font", orbitron_font)
	sys_tag.add_theme_font_size_override("font_size", 12)
	sys_tag.add_theme_color_override("font_color", Color(1.0, 0.15, 0.58, 0.92))
	inner_hbox.add_child(sys_tag)

	var rich := RichTextLabel.new()
	rich.bbcode_enabled = true
	rich.fit_content = true
	rich.scroll_active = false
	rich.mouse_filter = Control.MOUSE_FILTER_IGNORE
	# Button images are 128 px shrunk to text height; mipmaps keep them smooth.
	rich.texture_filter = CanvasItem.TEXTURE_FILTER_LINEAR_WITH_MIPMAPS
	rich.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	rich.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	rich.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	rich.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	if orbitron_font != null:
		rich.add_theme_font_override("normal_font", orbitron_font)
	rich.add_theme_font_size_override("normal_font_size", 13)
	rich.add_theme_color_override("default_color", Color(0.0, 0.95, 1.0, 0.95))
	_prompt_label = rich
	inner_hbox.add_child(_prompt_label)

	center_row.add_child(panel)
	add_child(canvas)
	_update_prompt_text()

# Updates the control prompt text based on the active input device.
func update_prompt_text() -> void:
	if not is_instance_valid(_prompt_label):
		return
	if _prompt_label is RichTextLabel:
		# BBCode with controller button images, 18 px tall to sit with the 13 px text.
		_prompt_label.text = InputPromptService.GetRichContextText(18)
	elif _prompt_label is Label:
		_prompt_label.text = InputPromptService.GetContextText()

func _on_prompts_changed() -> void:
	var current: bool = InputPromptService.IsGamepad
	is_gamepad_mode = current
	update_prompt_text()
	# Legacy consumers also need a refresh for bindings, family, context and language.
	input_device_changed.emit(is_gamepad_mode)

func _update_prompt_text() -> void:
	update_prompt_text()

func _on_joy_connection_changed(device: int, connected: bool) -> void:
	if connected:
		var joy_name = Input.get_joy_name(device)
		print("Global: Gamepad connected: Device ", device, " - ", joy_name)
		show_gamepad_toast("🎮 GAMEPAD CONNECTED: " + joy_name)
	else:
		print("Global: Gamepad disconnected: Device ", device)
		show_gamepad_toast("⚠️ GAMEPAD DISCONNECTED")

# Compatibility prompt methods use the shared binding formatter.
func get_nav_prompt() -> String:
	return tr("Navigate") + ": [ " + InputPromptService.GetBinding("ui_up") + " / " + InputPromptService.GetBinding("ui_down") + " ]   |   " + tr("Select") + ": [ " + InputPromptService.GetBinding("ui_accept") + " ]   |   " + tr("Back") + ": [ " + InputPromptService.GetBinding("ui_cancel") + " ]"

func get_home_prompt() -> String:
	return tr("Select") + ": [ " + InputPromptService.GetBinding("home_start") + " ]"

## BBCode versions for rich labels: controller bindings become button images `height` px tall.
func get_nav_prompt_rich(height: int) -> String:
	return _rich_text(tr("Navigate")) + ": [lb] " + InputPromptService.GetRichBinding("ui_up", height) + " / " + InputPromptService.GetRichBinding("ui_down", height) + " [rb]   |   " + _rich_text(tr("Select")) + ": [lb] " + InputPromptService.GetRichBinding("ui_accept", height) + " [rb]   |   " + _rich_text(tr("Back")) + ": [lb] " + InputPromptService.GetRichBinding("ui_cancel", height) + " [rb]"

func get_home_prompt_rich(height: int) -> String:
	return _rich_text(tr("Select")) + ": [lb] " + InputPromptService.GetRichBinding("home_start", height) + " [rb]"

## Escapes translated text for BBCode so brackets are shown, not read as tags.
func _rich_text(text: String) -> String:
	var out := ""
	for c in text:
		out += "[lb]" if c == "[" else ("[rb]" if c == "]" else c)
	return out

func get_story_prompt() -> String:
	return tr("Advance") + ": [ " + InputPromptService.GetBinding("story_advance") + " ]   |   " + tr("Skip") + ": [ " + InputPromptService.GetBinding("story_skip") + " ]"

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
## Configures explicit script fonts and shared sci-fi font fallbacks after a locale change.
func update_font_for_locale(locale_code: String) -> void:
	var loc: String = LocaleService.Canonical(locale_code)
	var is_arabic: bool = loc == "arabic"
	var cairo_font = load("res://assets/fonts/Cairo.ttf")
	var arabic_font = load("res://assets/fonts/NotoSansArabic.ttf")
	var noto_font = load(LocaleService.FontPath(loc))

	var fallbacks_list: Array[Font] = []
	if noto_font != null: fallbacks_list.append(noto_font)
	if arabic_font != null and arabic_font != noto_font: fallbacks_list.append(arabic_font)

	if is_arabic and cairo_font != null:
		ThemeDB.fallback_font = cairo_font
		print("Global: Set ThemeDB.fallback_font to Cairo.ttf for Arabic")
	elif noto_font != null:
		ThemeDB.fallback_font = noto_font
		print("Global: Locale font: ", LocaleService.FontPath(loc))

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
		print("Global: Configured explicit script fallbacks for sci-fi fonts")


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
## Follows account sign-in so the account's settings apply as soon as they load.
func _connect_account_settings() -> void:
	var sessions := get_node_or_null("/root/SessionManager")
	if sessions != null and not sessions.is_connected("AccountLoaded", _on_account_loaded):
		sessions.connect("AccountLoaded", _on_account_loaded)
	load_saved_settings()

## Applies the loaded account's settings, or the defaults after sign-out.
func _on_account_loaded(_signed_in: bool) -> void:
	load_saved_settings()

## Restores display/input preferences from the signed-in account (defaults before sign-in).
func load_saved_settings() -> void:
	load_custom_keybindings()
	var data: Dictionary = get_node("/root/SessionManager").ReadState("user_settings")
	if data.get("display_mode") is String:
		current_display_mode = data["display_mode"]
		if DisplayServer.get_name() != "headless":
			apply_display_mode(current_display_mode)

## Saves the selected display mode to the signed-in account (not saved before sign-in).
func save_user_settings() -> void:
	if not get_node("/root/SessionManager").WriteState("user_settings", {"display_mode": current_display_mode}):
		push_warning("Display settings were not saved: sign in to keep settings on your account.")

## Restores supported keyboard mappings from the account document.
func load_custom_keybindings() -> void:
	InputPromptService.ApplyKeyboardBindings(get_node("/root/SessionManager").ReadState("user_keybindings"))

## Commits keyboard bindings without altering controller bindings or creating a local save.
func save_custom_keybindings() -> void:
	if not get_node("/root/SessionManager").WriteState("user_keybindings", InputPromptService.CaptureKeyboardBindings()):
		push_warning("Keybindings were not saved: sign in to keep them on your account.")
	InputPromptService.NotifyBindingsChanged()

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
## Advances runtime level selection through the complete catalog without lowering replay progress or exposing Mission 13.
func unlock_next_level(current_level: int) -> void:
	var catalog = load("res://scripts/managers/LevelCatalogBridge.cs").new()
	var final_mission: int = catalog.Count()
	if current_level >= unlocked_levels and current_level < final_mission:
		unlocked_levels = current_level + 1
	elif current_level == final_mission:
		print("Global: Campaign Complete! All sectors liberated.")

## Lets a scene with contextual gameplay instructions temporarily hide the generic menu prompt.
## The scene restores visibility on exit so other menus retain their existing guidance.
func set_control_prompt_visible(visible: bool) -> void:
	if is_instance_valid(_prompt_label):
		var canvas := _prompt_label.get_parent()
		while canvas != null and not canvas is CanvasLayer:
			canvas = canvas.get_parent()
		if canvas is CanvasLayer:
			canvas.visible = visible
