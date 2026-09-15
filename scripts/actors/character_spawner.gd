extends Node2D
class_name CharacterSpawner

# The spawned character instance
var character: CharacterBody2D = null


func _ready() -> void:
	# Automatically spawn the character upon the spawner entering the scene tree.
	spawn_character()


# Handles the instantiation of the player character based on the global selection.
# It maps the selected character name to the corresponding .tscn file.
func spawn_character() -> void:
	var character_scene: PackedScene

	# Load the appropriate character scene based on selection.
	# Unity Migration: Use a CharacterDatabase (ScriptableObject) to map names to Prefabs.
	match Global.selected_character:
		"Cyber Shogun":
			character_scene = load("res://scenes/gameplay/cyber_shogun.tscn")
		_:
			# All other characters (Ninja Frog, Mask Dude, Pink Man, Virtual Guy)
			# use the main_character scene which loads sprites from Global.selected_character.
			character_scene = load("res://scenes/gameplay/main_character.tscn")

	# Instantiate the character scene and set its position.
	character = character_scene.instantiate()
	character.global_position = global_position

	# Add the instantiated character to the parent node (usually the level root).
	get_parent().add_child(character)

	# Initialize the follow-camera.
	setup_camera()

	# Remove this spawner node from the scene as its job is complete.
	queue_free()


# Dynamically creates and attaches a Camera2D to the spawned character.
# Configures zoom and smoothing for a polished feel.
func setup_camera() -> void:
	# Create and attach camera to the character.
	# Unity Migration: Use Cinemachine for professional camera follow behavior.
	var camera = Camera2D.new()
	camera.enabled = true
	camera.zoom = Vector2(0.5, 0.5)
	camera.position_smoothing_enabled = true
	camera.position_smoothing_speed = 10.0
	character.add_child(camera)
