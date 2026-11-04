class_name TestActIIIStartScreen
extends GdUnitTestSuite
## Verifies Act III Start Screen instantiation, title nodes, operative badges, and button signals.

const SCENE := "res://scenes/ui/act_iii_start_screen.tscn"


func test_act_iii_start_screen_instantiation_and_nodes() -> void:
	## Confirms Act III title, narrative briefing, squad badges, and action buttons.
	var scene: Node = load(SCENE).instantiate()
	scene.set("AutoNavigate", false)
	add_child(scene)
	await get_tree().process_frame

	assert_object(scene.get_node_or_null("RootControl")).is_not_null()
	assert_object(scene.get_node_or_null("RootControl/ContentMargin/MainVBox/ActTitle")).is_not_null()
	assert_object(scene.get_node_or_null("RootControl/ContentMargin/MainVBox/ActSubtitle")).is_not_null()
	assert_object(scene.get_node_or_null("RootControl/ContentMargin/MainVBox/BriefingPanel/BriefingText")).is_not_null()
	assert_object(scene.get_node_or_null("RootControl/ContentMargin/MainVBox/SquadRow")).is_not_null()
	assert_object(scene.get_node_or_null("RootControl/ContentMargin/MainVBox/ButtonRow/StartButton")).is_not_null()
	assert_object(scene.get_node_or_null("RootControl/ContentMargin/MainVBox/ButtonRow/SelectButton")).is_not_null()

	var squad_row: HBoxContainer = scene.get_node("RootControl/ContentMargin/MainVBox/SquadRow")
	assert_int(squad_row.get_child_count()).is_equal(6)

	scene.free()


func test_act_iii_start_screen_signals() -> void:
	## Confirms button presses emit expected signals without orphans.
	var scene: Node = load(SCENE).instantiate()
	scene.set("AutoNavigate", false)
	add_child(scene)
	await get_tree().process_frame

	var started_emitted := [false]
	var select_emitted := [false]

	scene.connect("OperationStarted", func() -> void: started_emitted[0] = true)
	scene.connect("MissionSelectRequested", func() -> void: select_emitted[0] = true)

	var start_btn: Button = scene.get_node("RootControl/ContentMargin/MainVBox/ButtonRow/StartButton")
	start_btn.emit_signal("pressed")
	assert_bool(started_emitted[0]).is_true()

	var select_btn: Button = scene.get_node("RootControl/ContentMargin/MainVBox/ButtonRow/SelectButton")
	select_btn.emit_signal("pressed")
	assert_bool(select_emitted[0]).is_true()

	scene.free()
