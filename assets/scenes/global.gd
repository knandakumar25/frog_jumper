extends Node

var reached_finish = false
var selected_character = "Ninja Frog"  # Default character
var unlocked_levels = 1  # Track highest unlocked level (1-4)

# Global menu music player
var menu_music_player: AudioStreamPlayer = null

func _ready() -> void:
	# Create persistent menu music player
	menu_music_player = AudioStreamPlayer.new()
	menu_music_player.name = "MenuMusicPlayer"
	menu_music_player.stream = load("res://assets/music/game-music-loop-1-143979.mp3")
	menu_music_player.volume_db = 24.0
	menu_music_player.bus = "Master"
	add_child(menu_music_player)
	
func play_menu_music() -> void:
	if menu_music_player and not menu_music_player.playing:
		menu_music_player.play()
		
func stop_menu_music() -> void:
	if menu_music_player and menu_music_player.playing:
		menu_music_player.stop()


func unlock_next_level(current_level: int) -> void:
	if current_level >= unlocked_levels and current_level < 4:
		unlocked_levels = current_level + 1
