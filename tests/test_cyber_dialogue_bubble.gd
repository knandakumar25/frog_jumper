class_name TestCyberDialogueBubble
extends GdUnitTestSuite

## GdUnit4 Test Suite for AXIOM-7 Cyber Dialogue Bubble and 3D Speech Terminal

func test_dialogue_assets_exist() -> void:
	assert_bool(FileAccess.file_exists("res://assets/models/menu/axiom7_speech_terminal.glb")).is_true()
	assert_bool(FileAccess.file_exists("res://assets/textures/cyber_speech_frame.png")).is_true()
	assert_bool(FileAccess.file_exists("res://assets/textures/axiom7_avatar_icon.png")).is_true()
	assert_bool(FileAccess.file_exists("res://assets/textures/cyber_speech_pointer.png")).is_true()
	assert_bool(FileAccess.file_exists("res://assets/shaders/cyber_dialogue_bubble.gdshader")).is_true()

func test_cyber_dialogue_bubble_instantiation() -> void:
	var path := "res://scenes/ui/CyberDialogueBubble.tscn"
	var scene := load(path) as PackedScene
	assert_that(scene).is_not_null()

	var bubble := scene.instantiate() as Control
	assert_that(bubble).is_not_null()

	var pointer := bubble.get_node_or_null("HoloPointer")
	var frame := bubble.get_node_or_null("FrameBackground")
	var dialogue_label := bubble.get_node_or_null("ContentMargin/VBoxContainer/DialogueLabel")
	var avatar := bubble.get_node_or_null("ContentMargin/VBoxContainer/HeaderBar/AvatarIcon")
	var visualizer := bubble.get_node_or_null("ContentMargin/VBoxContainer/HeaderBar/AudioVisualizer")
	var prompt := bubble.get_node_or_null("ContentMargin/VBoxContainer/FooterBar/PromptIndicator")

	assert_that(pointer).is_not_null()
	assert_that(frame).is_not_null()
	assert_that(dialogue_label).is_not_null()
	assert_that(avatar).is_not_null()
	assert_that(visualizer).is_not_null()
	assert_that(prompt).is_not_null()

	bubble.free()

func test_3d_speech_terminal_model_hierarchy() -> void:
	var path := "res://assets/models/menu/axiom7_speech_terminal.glb"
	var scene := load(path) as PackedScene
	assert_that(scene).is_not_null()

	var terminal := scene.instantiate() as Node3D
	assert_that(terminal).is_not_null()
	assert_that(terminal.get_child_count()).is_greater(0)

	terminal.free()

func test_command_deck_background_assets_exist() -> void:
	assert_bool(FileAccess.file_exists("res://assets/models/menu/command_deck_interior.glb")).is_true()
	assert_bool(FileAccess.file_exists("res://assets/models/menu/cyber_city_skyline.glb")).is_true()

func test_command_deck_models_instantiation() -> void:
	var deck_scene := load("res://assets/models/menu/command_deck_interior.glb") as PackedScene
	assert_that(deck_scene).is_not_null()
	var deck_instance := deck_scene.instantiate() as Node3D
	assert_that(deck_instance).is_not_null()
	assert_that(deck_instance.get_child_count()).is_greater(0)
	deck_instance.free()

	var skyline_scene := load("res://assets/models/menu/cyber_city_skyline.glb") as PackedScene
	assert_that(skyline_scene).is_not_null()
	var skyline_instance := skyline_scene.instantiate() as Node3D
	assert_that(skyline_instance).is_not_null()
	assert_that(skyline_instance.get_child_count()).is_greater(0)
	skyline_instance.free()

func test_3d_story_intro_scene_with_3d_deck_background() -> void:
	var intro_scene := load("res://scenes/main_menu/3d_story_intro.tscn") as PackedScene
	assert_that(intro_scene).is_not_null()
	var intro_instance := intro_scene.instantiate() as Node3D
	assert_that(intro_instance).is_not_null()

	var camera := intro_instance.get_node_or_null("Camera3D")
	var deck := intro_instance.get_node_or_null("CommandDeckInterior")
	var skyline := intro_instance.get_node_or_null("CyberCitySkyline")
	var deck_light := intro_instance.get_node_or_null("InteriorAccentLight")
	var skyline_glow := intro_instance.get_node_or_null("SkylineNeonGlow")

	assert_that(camera).is_not_null()
	assert_that(deck).is_not_null()
	assert_that(skyline).is_not_null()
	assert_that(deck_light).is_not_null()
	assert_that(skyline_glow).is_not_null()

	intro_instance.free()
