extends Node2D
class_name CharacterSpawner

# The spawned character instance
var character: CharacterBody2D = null


func _ready() -> void:
	spawn_character()


func spawn_character() -> void:
	var character_scene: PackedScene
	
	# Load the appropriate character scene based on selection
	match Global.selected_character:
		"Cyber Shogun":
			character_scene = load("res://assets/scenes/cyber_shogun.tscn")
		_:
			# All other characters (Ninja Frog, Mask Dude, Pink Man, Virtual Guy)
			# use the main_character scene which loads sprites from Global.selected_character
			character_scene = load("res://assets/scenes/main_character.tscn")
	
	# Instantiate and add the character
	character = character_scene.instantiate()
	character.global_position = global_position
	
	# Add to parent (usually the SceneObjects node or level root)
	get_parent().add_child(character)
	
	# Add camera to character if needed
	setup_camera()
	
	# Remove this spawner node as it's no longer needed
	queue_free()


func setup_camera() -> void:
	# Create and attach camera to the character
	var camera = Camera2D.new()
	camera.enabled = true
	camera.zoom = Vector2(0.5, 0.5)
	camera.position_smoothing_enabled = true
	camera.position_smoothing_speed = 10.0
	character.add_child(camera)
