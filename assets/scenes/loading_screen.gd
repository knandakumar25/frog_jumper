extends Control

@onready var progress_bar: ProgressBar = $CenterContainer/VBoxContainer/ProgressBar
@onready var status_label: Label = $CenterContainer/VBoxContainer/StatusLabel
@onready var fade_overlay: ColorRect = $FadeOverlay

var target_scene_path: String = ""
var loading_status: int = 0
var fade_duration: float = 0.3
var loading_started: bool = false


func _ready() -> void:
	if target_scene_path.is_empty():
		push_error("No target scene path set for loading screen")
		return
	
	# Start with fade overlay visible, then fade in
	fade_overlay.modulate.a = 1.0
	_fade_in()
	
	# Start loading the target scene asynchronously
	var error = ResourceLoader.load_threaded_request(target_scene_path)
	if error == OK:
		loading_started = true
		status_label.text = tr("Loading scene...")
	else:
		push_error("Failed to start loading: " + target_scene_path)
		status_label.text = tr("Error: Failed to start loading")


func _fade_in() -> void:
	var tween = create_tween()
	tween.tween_property(fade_overlay, "modulate:a", 0.0, fade_duration)


func _fade_out() -> void:
	var tween = create_tween()
	tween.tween_property(fade_overlay, "modulate:a", 1.0, fade_duration)
	await tween.finished


func _process(_delta: float) -> void:
	if target_scene_path.is_empty() or not loading_started:
		return
	
	# Check loading progress
	var progress = []
	loading_status = ResourceLoader.load_threaded_get_status(target_scene_path, progress)
	
	match loading_status:
		ResourceLoader.THREAD_LOAD_INVALID_RESOURCE:
			status_label.text = tr("Error: Invalid resource")
			push_error("Failed to load scene: " + target_scene_path)
			loading_started = false
			
		ResourceLoader.THREAD_LOAD_IN_PROGRESS:
			# Update progress bar (progress[0] is between 0.0 and 1.0)
			if progress.size() > 0:
				progress_bar.value = progress[0] * 100.0
				status_label.text = tr("Loading... ") + str(int(progress[0] * 100)) + "%"
			
		ResourceLoader.THREAD_LOAD_FAILED:
			status_label.text = tr("Error: Loading failed")
			push_error("Failed to load scene: " + target_scene_path)
			loading_started = false
			
		ResourceLoader.THREAD_LOAD_LOADED:
			# Loading complete
			progress_bar.value = 100.0
			status_label.text = tr("Complete!")
			loading_started = false
			_on_loading_complete()


func _on_loading_complete() -> void:
	# Get the loaded resource
	var packed_scene = ResourceLoader.load_threaded_get(target_scene_path)
	
	if packed_scene:
		# Small delay to show 100% completion
		await get_tree().create_timer(0.3).timeout
		# Fade out before changing scene
		await _fade_out()
		get_tree().change_scene_to_packed(packed_scene)
	else:
		push_error("Failed to retrieve loaded scene: " + target_scene_path)


# Static method to transition to loading screen with target scene
static func load_scene(tree: SceneTree, scene_path: String) -> void:
	# Create loading screen instance
	var loading_screen = load("res://assets/scenes/loading_screen.tscn").instantiate()
	loading_screen.target_scene_path = scene_path
	
	# Change to loading screen
	tree.get_root().add_child(loading_screen)
	tree.current_scene.queue_free()
	tree.current_scene = loading_screen
