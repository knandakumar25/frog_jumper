extends Control

const LoadingScreen = preload("res://assets/scenes/loading_screen.gd")

func _ready() -> void:
	Global.play_menu_music()
	_setup_character_animations()


func _on_ninja_frog_pressed() -> void:
	Global.selected_character = "Ninja Frog"
	_create_session_and_start("Ninja Frog")


func _on_mask_dude_pressed() -> void:
	Global.selected_character = "Mask Dude"
	_create_session_and_start("Mask Dude")


func _on_pink_man_pressed() -> void:
	Global.selected_character = "Pink Man"
	_create_session_and_start("Pink Man")


func _on_virtual_guy_pressed() -> void:
	Global.selected_character = "Virtual Guy"
	_create_session_and_start("Virtual Guy")

func _create_session_and_start(character_name: String) -> void:
	# Create a new game session in the database
	var session_id = SessionManager.create_new_session(character_name)
	
	if session_id > 0:
		print("Created new session: ", session_id, " for character: ", character_name)
		# Go to story intro before level selection
		LoadingScreen.load_scene(get_tree(), "res://assets/scenes/story_intro.tscn")
	else:
		print("Error creating session!")
		# Fallback: still go to story intro
		LoadingScreen.load_scene(get_tree(), "res://assets/scenes/story_intro.tscn")


func _setup_character_animations() -> void:
	var characters = [
		{"name": "Ninja Frog", "node": $VBoxContainer/CharacterGrid/NinjaFrog/CharacterButton/AnimatedSprite},
		{"name": "Mask Dude", "node": $VBoxContainer/CharacterGrid/MaskDude/CharacterButton/AnimatedSprite},
		{"name": "Pink Man", "node": $VBoxContainer/CharacterGrid/PinkMan/CharacterButton/AnimatedSprite},
		{"name": "Virtual Guy", "node": $VBoxContainer/CharacterGrid/VirtualGuy/CharacterButton/AnimatedSprite}
	]
	
	for character in characters:
		var sprite: AnimatedSprite2D = character.node
		var base_path = "res://assets/images/" + character.name + "/"
		
		# Create sprite frames
		var sprite_frames = SpriteFrames.new()
		
		# Load idle animation
		var idle_texture = load(base_path + "Idle (32x32).png")
		_add_animation_frames(sprite_frames, "idle", idle_texture, 11, 10.0)
		
		# Apply and play
		sprite.sprite_frames = sprite_frames
		sprite.play("idle")


func _add_animation_frames(sprite_frames: SpriteFrames, anim_name: String, texture: Texture2D, frame_count: int, speed: float) -> void:
	sprite_frames.add_animation(anim_name)
	sprite_frames.set_animation_loop(anim_name, true)
	sprite_frames.set_animation_speed(anim_name, speed)
	
	for i in range(frame_count):
		var atlas = AtlasTexture.new()
		atlas.atlas = texture
		atlas.region = Rect2(i * 32, 0, 32, 32)
		sprite_frames.add_frame(anim_name, atlas)
