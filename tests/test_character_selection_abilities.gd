class_name TestCharacterSelectionAbilities
extends GdUnitTestSuite

## GdUnit4 Test Suite for Character Selection Tactical Ability Buttons and Previews

func test_character_selection_scene_loads() -> void:
	var path := "res://scenes/main_menu/character_selection.tscn"
	assert_bool(FileAccess.file_exists(path)).is_true()
	var scene := load(path) as PackedScene
	assert_that(scene).is_not_null()

func test_character_selection_ability_ui_nodes() -> void:
	var path := "res://scenes/main_menu/character_selection.tscn"
	var scene := load(path) as PackedScene
	var instance: Node = scene.instantiate()
	assert_that(instance).is_not_null()

	var primary_btn = instance.get_node_or_null("MainLayout/RightShowcasePanel/VBox/AbilitiesBox/AbilityButtonsGrid/PrimaryAbilityBtn")
	var secondary_btn = instance.get_node_or_null("MainLayout/RightShowcasePanel/VBox/AbilitiesBox/AbilityButtonsGrid/SecondaryAbilityBtn")
	var dash_btn = instance.get_node_or_null("MainLayout/RightShowcasePanel/VBox/AbilitiesBox/AbilityButtonsGrid/DashAbilityBtn")
	var slam_btn = instance.get_node_or_null("MainLayout/RightShowcasePanel/VBox/AbilitiesBox/AbilityButtonsGrid/SlamAbilityBtn")
	var badge_label = instance.get_node_or_null("MainLayout/RightShowcasePanel/VBox/AbilitiesBox/AbilityDescPanel/DescVBox/AbilityBadgeLabel")
	var desc_label = instance.get_node_or_null("MainLayout/RightShowcasePanel/VBox/AbilitiesBox/AbilityDescPanel/DescVBox/AbilityDescLabel")
	var status_label = instance.get_node_or_null("MainLayout/RightShowcasePanel/VBox/AbilitiesBox/AbilityDescPanel/DescVBox/AbilityStatusLabel")

	assert_that(primary_btn).is_not_null()
	assert_that(secondary_btn).is_not_null()
	assert_that(dash_btn).is_not_null()
	assert_that(slam_btn).is_not_null()
	assert_that(badge_label).is_not_null()
	assert_that(desc_label).is_not_null()
	assert_that(status_label).is_not_null()

	instance.free()

func test_operative_ability_previews() -> void:
	var path := "res://scenes/main_menu/character_selection.tscn"
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
		instance.call("SelectOperative", op)
		for ability_idx in range(4):
			instance.call("PreviewAbility", ability_idx)

	instance.free()

## The merged dossier retains the keyboard/controller-only interaction policy.
func test_buttons_mouse_filter_ignored() -> void:
	var path := "res://scenes/main_menu/character_selection.tscn"
	var scene := load(path) as PackedScene
	var instance: Node = scene.instantiate()
	assert_that(instance).is_not_null()

	var button_paths := [
		"MainLayout/LeftListPanel/VBox/CharacterGrid/CyberShogun",
		"MainLayout/LeftListPanel/VBox/CharacterGrid/Oceanus",
		"MainLayout/LeftListPanel/VBox/CharacterGrid/SuryaPrime",
		"MainLayout/LeftListPanel/VBox/CharacterGrid/MirageBlade",
		"MainLayout/LeftListPanel/VBox/CharacterGrid/ApexTitan",
		"MainLayout/LeftListPanel/VBox/CharacterGrid/Phoenicia",
		"MainLayout/RightShowcasePanel/VBox/AbilitiesBox/AbilityButtonsGrid/PrimaryAbilityBtn",
		"MainLayout/RightShowcasePanel/VBox/AbilitiesBox/AbilityButtonsGrid/SecondaryAbilityBtn",
		"MainLayout/RightShowcasePanel/VBox/AbilitiesBox/AbilityButtonsGrid/DashAbilityBtn",
		"MainLayout/RightShowcasePanel/VBox/AbilitiesBox/AbilityButtonsGrid/SlamAbilityBtn",
		"MainLayout/RightShowcasePanel/VBox/Footer/DeployButton",
	]

	for b_path in button_paths:
		var btn := instance.get_node_or_null(b_path) as Button
		assert_that(btn).is_not_null()
		assert_that(btn.mouse_filter).is_equal(Control.MOUSE_FILTER_IGNORE)

	instance.free()

## Each operative's card shows its portrait when one exists (portraits await SpriteCook
## regeneration, docs/ART_REGENERATION.md), and the dossier never uses 3D nodes.
func test_all_operatives_use_2d_cards_without_3d_models() -> void:
	var instance = load("res://scenes/main_menu/character_selection.tscn").instantiate()
	var card: TextureRect = instance.get_node("MainLayout/RightShowcasePanel/VBox/CardImage")
	var ids := {"Cyber Shogun": "cyber_shogun", "Oceanus": "oceanus", "Surya Prime": "surya_prime",
		"Mirage Blade": "mirage_blade", "Apex Titan": "apex_titan", "Aura Phoenicia": "phoenicia"}
	for operative in ids:
		instance.call("SelectOperative", operative)
		var portrait: String = "res://assets/characters/rendered/%s_portrait.png" % ids[operative]
		if ResourceLoader.exists(portrait):
			assert_that(card.texture).is_not_null()
		else:
			assert_that(card.texture).is_null()
	assert_int(instance.find_children("*", "Node3D", true, false).size()).is_equal(0)
	instance.free()
