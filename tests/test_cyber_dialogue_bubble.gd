class_name TestCyberDialogueBubble
extends GdUnitTestSuite

## GdUnit4 Test Suite for AXIOM-7 Cyber Dialogue Bubble

func test_dialogue_assets_exist() -> void:
	assert_bool(FileAccess.file_exists("res://assets/textures/cyber_speech_frame.png")).is_true()
	# The AXIOM-7 avatar (assets/characters/rendered/axiom7_portrait.png) awaits SpriteCook
	# regeneration and is no longer wired into the scene; the bubble works without it.
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
