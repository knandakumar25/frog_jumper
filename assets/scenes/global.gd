extends Node

var reached_finish = false
var selected_character = "Ninja Frog"  # Default character
var unlocked_levels = 1  # Track highest unlocked level (1-4)
var current_display_mode: String = "Fullscreen"
const SETTINGS_PATH: String = "user://user_settings.json"
const KEYBINDINGS_PATH: String = "user://user_keybindings.json"

# Global menu music player
var menu_music_player: AudioStreamPlayer = null

func _ready() -> void:
	load_saved_settings()

	# The game MUST ALWAYS launch in Fullscreen mode by default on every rerun
	current_display_mode = "Fullscreen"
	if DisplayServer.get_name() != "headless":
		apply_display_mode("Fullscreen")
		DisplayServer.screen_set_keep_on(true)
		print("Global: Screen keep-awake enabled (prevents device sleep)")

	# Set default locale to english (matching the CSV column name)
	TranslationServer.set_locale("english")
	print("Global: Locale set to 'english'")

	# Configure Cairo & NotoSans as universal fallbacks for sci-fi fonts (Orbitron / Rajdhani) for Arabic, Hebrew, CJK support
	var cairo_font = load("res://assets/fonts/Cairo.ttf")
	var arabic_font = load("res://assets/fonts/NotoSansArabic.ttf")
	var noto_font = load("res://assets/fonts/NotoSans-Regular.ttf")
	var fallbacks_list = []
	if cairo_font != null: fallbacks_list.append(cairo_font)
	if arabic_font != null: fallbacks_list.append(arabic_font)
	if noto_font != null: fallbacks_list.append(noto_font)

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

	# Load and set custom cyber cursor at runtime
	var cursor_path = ProjectSettings.globalize_path("res://assets/images/cursor.png")
	if FileAccess.file_exists(cursor_path):
		var img = Image.load_from_file(cursor_path)
		if img != null:
			var cursor_tex = ImageTexture.create_from_image(img)
			Input.set_custom_mouse_cursor(cursor_tex, Input.CURSOR_ARROW, Vector2(4, 4))
			print("Global: Custom cyber cursor set successfully at runtime!")

	# Create persistent menu music player only if not in headless mode
	if DisplayServer.get_name() != "headless":
		menu_music_player = AudioStreamPlayer.new()
		menu_music_player.name = "MenuMusicPlayer"
		var music_path = "res://assets/music/Dystopian_Dawn_FULL_SONG_MusicGPT (2).mp3"
		if FileAccess.open(music_path, FileAccess.READ) != null:
			menu_music_player.stream = load(music_path)
		menu_music_player.volume_db = 24.0
		menu_music_player.bus = "Master"
		add_child(menu_music_player)

func apply_display_mode(mode_name: String) -> void:
	current_display_mode = mode_name
	match mode_name:
		"Windowed Fullscreen":
			DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED)
			DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_MAXIMIZED)
		"Windowed":
			DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED)
			DisplayServer.window_set_size(Vector2i(1280, 720))
			var screen_size = DisplayServer.screen_get_size()
			var window_size = Vector2i(1280, 720)
			DisplayServer.window_set_position((screen_size - window_size) / 2)
		_: # "Fullscreen" (Default)
			DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_FULLSCREEN)

func load_saved_settings() -> void:
	load_custom_keybindings()

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

func play_audio_track(res_path: String) -> void:
	if menu_music_player == null:
		return
	if menu_music_player.playing and menu_music_player.stream != null:
		if menu_music_player.stream.resource_path == res_path:
			return # Already playing this exact track
	var stream = load_mp3_stream(res_path)
	if stream != null:
		menu_music_player.stream = stream
		menu_music_player.volume_db = 0.0
		menu_music_player.play()

func play_home_music() -> void:
	play_audio_track("res://assets/audio/Sub_Level_Three.mp3")

func play_menu_music() -> void:
	play_audio_track("res://assets/audio/Street_Level_Siege.mp3")

func stop_menu_music() -> void:
	if menu_music_player and menu_music_player.playing:
		menu_music_player.stop()

func unlock_next_level(current_level: int) -> void:
	if current_level >= unlocked_levels and current_level < 4:
		unlocked_levels = current_level + 1
