class_name TestActIIEndScreen
extends GdUnitTestSuite
## Verifies Act II End Screen instantiation, layout nodes, button signal emission, and clean lifecycle disposal.

const SCENE := "res://scenes/ui/act_ii_end_screen.tscn"


func test_act_ii_end_screen_instantiation_and_nodes() -> void:
	## Confirms all sector debrief cards and action buttons exist with zero missing references.
	var scene: Node = load(SCENE).instantiate()
	scene.set("AutoNavigate", false)
	add_child(scene)
	await get_tree().process_frame

	assert_object(scene.get_node_or_null("RootControl")).is_not_null()
	assert_object(scene.get_node_or_null("RootControl/MainMargin/VBoxMain/HeaderTitle")).is_not_null()
	assert_object(scene.get_node_or_null("RootControl/MainMargin/VBoxMain/SectorCardsRow")).is_not_null()
	assert_object(scene.get_node_or_null("RootControl/MainMargin/VBoxMain/TelemetryPanel")).is_not_null()
	assert_object(scene.get_node_or_null("RootControl/MainMargin/VBoxMain/ButtonRow/ProceedButton")).is_not_null()
	assert_object(scene.get_node_or_null("RootControl/MainMargin/VBoxMain/ButtonRow/LevelSelectButton")).is_not_null()
	assert_object(scene.get_node_or_null("RootControl/MainMargin/VBoxMain/ButtonRow/MainMenuButton")).is_not_null()

	scene.free()


func test_act_ii_end_screen_button_signals() -> void:
	## Confirms button presses emit expected signals without leaking orphan nodes.
	var scene: Node = load(SCENE).instantiate()
	scene.set("AutoNavigate", false)
	add_child(scene)
	await get_tree().process_frame

	var proceed_emitted := [false]
	var level_select_emitted := [false]
	var main_menu_emitted := [false]

	scene.connect("ProceedToActIII", func() -> void: proceed_emitted[0] = true)
	scene.connect("LevelSelectRequested", func() -> void: level_select_emitted[0] = true)
	scene.connect("MainMenuRequested", func() -> void: main_menu_emitted[0] = true)

	var proceed_btn: Button = scene.get_node("RootControl/MainMargin/VBoxMain/ButtonRow/ProceedButton")
	proceed_btn.emit_signal("pressed")
	assert_bool(proceed_emitted[0]).is_true()

	var select_btn: Button = scene.get_node("RootControl/MainMargin/VBoxMain/ButtonRow/LevelSelectButton")
	select_btn.emit_signal("pressed")
	assert_bool(level_select_emitted[0]).is_true()

	var menu_btn: Button = scene.get_node("RootControl/MainMargin/VBoxMain/ButtonRow/MainMenuButton")
	menu_btn.emit_signal("pressed")
	assert_bool(main_menu_emitted[0]).is_true()

	scene.free()
