class_name TestCyberDialogueBubble
extends GdUnitTestSuite

## GdUnit4 Test Suite for AXIOM-7 Cyber Dialogue Bubble

## The shader must exist. The frame and pointer images were deleted on 2026-11-12 and await
## SpriteCook regeneration, so the scene must not hard-reference them: the script loads them by
## path when present. The AXIOM-7 avatar is likewise not wired into the scene.
func test_dialogue_assets_exist() -> void:
	assert_bool(FileAccess.file_exists("res://assets/shaders/cyber_dialogue_bubble.gdshader")).is_true()
	var scene_text := FileAccess.get_file_as_string("res://scenes/ui/CyberDialogueBubble.tscn")
	assert_str(scene_text).not_contains("cyber_speech_frame.png")
	assert_str(scene_text).not_contains("cyber_speech_pointer.png")

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
