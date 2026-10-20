extends GdUnitTestSuite

func test_study_is_shogun_only_and_original_portrait_is_preserved() -> void:
	var runner = scene_runner("res://scenes/main_menu/character_selection.tscn")
	var menu = runner.scene()
	var study = menu.get_node("MainLayout/CenterShowcasePanel/VBox/ShogunArtStudy")
	var card = menu.get_node("MainLayout/CenterShowcasePanel/VBox/CardImage")
	assert_bool(study.visible).is_true()
	assert_bool(card.visible).is_false()
	assert_bool(ResourceLoader.exists("res://assets/characters/rendered/cyber_shogun_portrait.png")).is_true()
	menu.call("SelectOperative", "Oceanus")
	assert_bool(study.visible).is_false()
	assert_bool(card.visible).is_true()
	menu.call("SelectOperative", "Cyber Shogun")
	assert_bool(study.visible).is_true()

func test_actions_and_existing_loadout_buttons_drive_preview() -> void:
	var runner = scene_runner("res://scenes/main_menu/character_selection.tscn")
	var menu = runner.scene()
	var preview = menu.get_node("MainLayout/CenterShowcasePanel/VBox/ShogunArtStudy/ArtPreview")
	for action in ["Walk", "Run", "Jump", "Double jump", "Katana", "Martial strike"]:
		menu.call("PreviewStudyAction", action)
		assert_str(preview.get("CurrentAction")).is_equal(action)
	var abilities = ["Shogun's Flash", "Kinetic Parry", "Cyber Dash", "Ground Slam"]
	for index in range(4):
		menu.call("PreviewAbility", index)
		assert_str(preview.get("CurrentAction")).is_equal(abilities[index])
	menu.call("SelectOperative", "Oceanus")
	menu.call("PreviewStudyAction", "Run")
	assert_str(preview.get("CurrentAction")).is_equal("Idle")
