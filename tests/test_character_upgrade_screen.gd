class_name TestCharacterUpgradeScreen
extends GdUnitTestSuite

## GdUnit4 Test Suite for Operative Upgrade Screen & Character Progression UI

func test_upgrade_screen_scene_exists() -> void:
	var path := "res://scenes/main_menu/operative_upgrade_screen.tscn"
	assert_bool(FileAccess.file_exists(path)).is_true()
	var scene := load(path) as PackedScene
	assert_that(scene).is_not_null()

func test_upgrade_screen_ui_node_hierarchy() -> void:
	var path := "res://scenes/main_menu/operative_upgrade_screen.tscn"
	var scene := load(path) as PackedScene
	var instance: Node = scene.instantiate()
	assert_that(instance).is_not_null()

	# Verify Header nodes
	var title_lbl = instance.get_node_or_null("Header/Title")
	var back_btn = instance.get_node_or_null("Header/BackBtn")
	var bank_sparks_lbl = instance.get_node_or_null("Header/BankSparksLabel")
	assert_that(title_lbl).is_not_null()
	assert_that(back_btn).is_not_null()
	assert_that(bank_sparks_lbl).is_not_null()

	# Verify Sparks and Level meter nodes
	var op_name = instance.get_node_or_null("MainVBox/TopCard/VBox/OperativeName")
	var level_badge = instance.get_node_or_null("MainVBox/TopCard/VBox/LevelRow/LevelBadge")
	var sparks_progress_lbl = instance.get_node_or_null("MainVBox/TopCard/VBox/SparksBox/SparksProgressLabel")
	var sparks_bar = instance.get_node_or_null("MainVBox/TopCard/VBox/SparksBox/SparksProgressBar")
	var level_up_btn = instance.get_node_or_null("MainVBox/TopCard/VBox/ActionRow/LevelUpBtn")
	var harvest_btn = instance.get_node_or_null("MainVBox/TopCard/VBox/ActionRow/HarvestSparksBtn")
	var skill_pts_lbl = instance.get_node_or_null("MainVBox/TopCard/VBox/ActionRow/SkillPointsLabel")
	var respec_btn = instance.get_node_or_null("MainVBox/TopCard/VBox/ActionRow/RespecBtn")

	assert_that(op_name).is_not_null()
	assert_that(level_badge).is_not_null()
	assert_that(sparks_progress_lbl).is_not_null()
	assert_that(sparks_bar).is_not_null()
	assert_that(level_up_btn).is_not_null()
	assert_that(harvest_btn).is_not_null()
	assert_that(skill_pts_lbl).is_not_null()
	assert_that(respec_btn).is_not_null()

	# Verify 3 upgrade track cards
	var track0 = instance.get_node_or_null("MainVBox/TracksContainer/TrackCard_0")
	var track1 = instance.get_node_or_null("MainVBox/TracksContainer/TrackCard_1")
	var track2 = instance.get_node_or_null("MainVBox/TracksContainer/TrackCard_2")

	assert_that(track0).is_not_null()
	assert_that(track1).is_not_null()
	assert_that(track2).is_not_null()

	# Verify Stat matrix
	var health_stat = instance.get_node_or_null("MainVBox/StatsPanel/HBox/HealthBox/Value")
	var speed_stat = instance.get_node_or_null("MainVBox/StatsPanel/HBox/SpeedBox/Value")
	var damage_stat = instance.get_node_or_null("MainVBox/StatsPanel/HBox/DamageBox/Value")
	var cdr_stat = instance.get_node_or_null("MainVBox/StatsPanel/HBox/CdrBox/Value")

	assert_that(health_stat).is_not_null()
	assert_that(speed_stat).is_not_null()
	assert_that(damage_stat).is_not_null()
	assert_that(cdr_stat).is_not_null()

	instance.free()

func test_character_selection_has_upgrade_button() -> void:
	var path := "res://scenes/main_menu/character_selection.tscn"
	var scene := load(path) as PackedScene
	var instance: Node = scene.instantiate()
	assert_that(instance).is_not_null()

	var upgrade_btn = instance.get_node_or_null("MainLayout/RightShowcasePanel/VBox/Footer/UpgradeOperativeBtn")
	assert_that(upgrade_btn).is_not_null()

	var upgrade_screen = instance.get_node_or_null("OperativeUpgradeScreen")
	assert_that(upgrade_screen).is_not_null()
	assert_bool(upgrade_screen.visible).is_false()

	instance.free()

func test_upgrade_screen_bind_all_operatives() -> void:
	var path := "res://scenes/main_menu/operative_upgrade_screen.tscn"
	var scene := load(path) as PackedScene
	var instance: Node = scene.instantiate()
	assert_that(instance).is_not_null()

	var operatives := [
		"Cyber Shogun",
		"Oceanus",
		"Surya Prime",
		"Mirage Blade",
		"Apex Titan",
		"Aura Phoenicia"
	]

	for op in operatives:
		instance.call("OpenForOperative", op)
		var op_lbl = instance.get_node_or_null("MainVBox/TopCard/VBox/OperativeName") as Label
		assert_that(op_lbl.text).contains(op.to_upper())

	instance.free()
