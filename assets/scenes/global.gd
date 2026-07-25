extends Node

var reached_finish = false
var selected_character = "Ninja Frog"  # Default character
var unlocked_levels = 1  # Track highest unlocked level (1-4)

# Global menu music player
var menu_music_player: AudioStreamPlayer = null

func _ready() -> void:
	# Set default locale to english (matching the CSV column name)
	# This ensures translations work from the start
	TranslationServer.set_locale("english")
	print("Global: Locale set to 'english'")
	print("Global: Available translations:", TranslationServer.get_loaded_locales())

	# Create persistent menu music player only if not in headless mode
	if DisplayServer.get_name() != "headless":
		print("Global: Creating menu music player...")
		menu_music_player = AudioStreamPlayer.new()
		menu_music_player.name = "MenuMusicPlayer"
		var music_path = "res://assets/music/Dystopian_Dawn_FULL_SONG_MusicGPT (2).mp3"
		if FileAccess.open(music_path, FileAccess.READ) != null:
			menu_music_player.stream = load(music_path)
			print("Global: Music loaded successfully")
		else:
			push_error("Global: Music file not found at " + music_path)
		menu_music_player.volume_db = 24.0
		menu_music_player.bus = "Master"
		add_child(menu_music_player)
	else:
		print("Global: Headless mode detected, skipping music player")

func play_menu_music() -> void:
	# Menu background music disabled per user request
	if menu_music_player and menu_music_player.playing:
		menu_music_player.stop()

func stop_menu_music() -> void:
	if menu_music_player and menu_music_player.playing:
		menu_music_player.stop()


func unlock_next_level(current_level: int) -> void:
	if current_level >= unlocked_levels and current_level < 4:
		unlocked_levels = current_level + 1
