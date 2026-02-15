extends Node2D
class_name CharacterSpawner

# The spawned character instance
var character: BaseCharacter = null


func _ready() -> void:
	spawn_character()


func spawn_character() -> void:
	var character_scene: PackedScene
	
	# Load the appropriate character scene based on selection
	match Global.selected_character:
		"Cyber Shogun":
			character_scene = load("res://assets/scenes/cyber_shogun.tscn")
		"Mask Dude":
			character_scene = load("res://assets/scenes/mask_dude.tscn")
		"Pink Man":
			character_scene = load("res://assets/scenes/pink_man.tscn")
		"Virtual Guy":
			character_scene = load("res://assets/scenes/virtual_guy.tscn")
		_:
			# Default to Cyber Shogun if no match
			character_scene = load("res://assets/scenes/cyber_shogun.tscn")
	
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
