extends GdUnitTestSuite
## Regression coverage for shared operative previews, loadout routing, and art preservation.

const ROSTER = {
	"Cyber Shogun": ["cyber_shogun", "Shogun's Flash", "Kinetic Parry", "Cyber Dash", "Ground Slam"],
	"Surya Prime": ["surya_prime", "Solar Shield Bash", "Aegis of the Sun", "Vanguard Dash", "Cosmic Slam"],
	"Oceanus": ["oceanus", "Tidal Torrent", "Maelstrom Vortex", "Aqua Jet", "Tidal Crash"],
	"Mirage Blade": ["mirage_blade", "Phase Blink", "Holographic Decoy", "Phase Dash", "Mirage Drop"],
	"Apex Titan": ["apex_titan", "Tectonic Slam", "Magnetic Tether", "Titan Surge", "Seismic Drop"],
	"Aura Phoenicia": ["phoenicia", "Harmonic Pulse", "Prism Reverberation", "Wave Glide", "Cedars' Echo"],
}
const STUDY = "MainLayout/RightShowcasePanel/VBox/ShogunArtStudy"

## Each selection must change the actual art body while retaining its original flat assets.
func test_all_operatives_show_their_own_hand_drawn_art_and_preserve_originals() -> void:
	var runner = scene_runner("res://scenes/main_menu/character_selection.tscn")
	var menu = runner.scene()
	var preview = menu.get_node(STUDY + "/ArtPreview")
	assert_bool(preview.get("UseOriginalDesign")).is_false()
	for operative in ROSTER:
		menu.call("SelectOperative", operative)
		var id = ROSTER[operative][0]
		assert_str(preview.get("ActiveOperativeId")).is_equal(id)
		assert_bool(menu.get_node(STUDY).visible).is_true()
		assert_bool(menu.get_node("MainLayout/RightShowcasePanel/VBox/CardImage").visible).is_false()
		var expected = "res://assets/characters/shogun_study/poses.png" if id == "cyber_shogun" else "res://assets/characters/hand_drawn/" + id + "_poses.png"
		assert_str(preview.get("StudyTexturePath")).is_equal(expected)
		# The rendered sprites (assets/characters/rendered) were deleted pending SpriteCook
		# regeneration, so they are no longer required here; the hand-drawn study art is.
		assert_bool(ResourceLoader.exists(expected)).is_true()
		assert_str(preview.get("CurrentAction")).is_equal("Idle")

## Each ability has one button in the unified dossier and drives the matching preview slot.
func test_every_loadout_button_drives_matching_ability_slot() -> void:
	var runner = scene_runner("res://scenes/main_menu/character_selection.tscn")
	var menu = runner.scene()
	var preview = menu.get_node(STUDY + "/ArtPreview")
	var buttons = menu.get_node("MainLayout/RightShowcasePanel/VBox/AbilitiesBox/AbilityButtonsGrid")
	assert_int(menu.get_node(STUDY + "/Actions").get_child_count()).is_equal(7)
	for operative in ROSTER:
		menu.call("SelectOperative", operative)
		for index in range(4):
			var expected = ROSTER[operative][index + 1]
			assert_str(buttons.get_child(index).text).contains(expected.to_upper())
			buttons.get_child(index).emit_signal("pressed")
			assert_str(preview.get("CurrentAction")).is_equal(expected)
			assert_int(preview.get("CurrentAbilitySlot")).is_equal(index)
			menu.call("PreviewStudyAction", "Idle")
			menu.call("PreviewAbility", index)
			assert_str(preview.get("CurrentAction")).is_equal(expected)
			assert_int(preview.get("CurrentAbilitySlot")).is_equal(index)

## Movement cancels ability state; changing operatives cannot retain the previous action.
func test_movement_and_selection_reset_preview_state() -> void:
	var runner = scene_runner("res://scenes/main_menu/character_selection.tscn")
	var menu = runner.scene()
	var preview = menu.get_node(STUDY + "/ArtPreview")
	for operative in ROSTER:
		menu.call("SelectOperative", operative)
		for action in ["Idle", "Walk", "Run", "Jump", "Double jump"]:
			menu.call("PreviewAbility", 0)
			menu.call("PreviewStudyAction", action)
			assert_str(preview.get("CurrentAction")).is_equal(action)
			assert_int(preview.get("CurrentAbilitySlot")).is_equal(-1)
		menu.call("SelectOperative", "Cyber Shogun")
		assert_str(preview.get("CurrentAction")).is_equal("Idle")

## Both comparison branches still accept Cyber Shogun's additional melee actions.
func test_original_art_comparison_and_shogun_melee_remain_available() -> void:
	var runner = scene_runner("res://scenes/main_menu/character_selection.tscn")
	var menu = runner.scene()
	var preview = menu.get_node(STUDY + "/ArtPreview")
	for original in [true, false]:
		preview.set("UseOriginalDesign", original)
		for action in ["Katana", "Martial strike"]:
			menu.call("PreviewStudyAction", action)
			assert_str(preview.get("CurrentAction")).is_equal(action)

## The merged dossier replaces the old third column and keeps its primary actions inside the panel.
func test_merged_dossier_has_two_panels_and_contains_preview_and_deploy() -> void:
	var runner = scene_runner("res://scenes/main_menu/character_selection.tscn")
	var menu = runner.scene()
	await get_tree().process_frame
	await get_tree().process_frame
	var layout = menu.get_node("MainLayout")
	assert_int(layout.get_child_count()).is_equal(2)
	assert_bool(menu.has_node("MainLayout/CenterShowcasePanel")).is_false()
	var dossier = menu.get_node("MainLayout/RightShowcasePanel")
	var preview = menu.get_node(STUDY + "/ArtPreview")
	var deploy = menu.get_node("MainLayout/RightShowcasePanel/VBox/Footer/DeployButton")
	assert_bool(dossier.get_global_rect().encloses(preview.get_global_rect())).is_true()
	assert_bool(dossier.get_global_rect().encloses(deploy.get_global_rect())).is_true()

## Focus links remain inside visible controls for every roster entry, including hidden Shogun-only moves.
func test_merged_navigation_returns_to_selected_operative_and_skips_hidden_actions() -> void:
	var runner = scene_runner("res://scenes/main_menu/character_selection.tscn")
	var menu = runner.scene()
	var dossier = menu.get_node("MainLayout/RightShowcasePanel")
	var primary = menu.get_node("MainLayout/RightShowcasePanel/VBox/AbilitiesBox/AbilityButtonsGrid/PrimaryAbilityBtn")
	for operative in ROSTER:
		menu.call("SelectOperative", operative)
		var roster_button = primary.get_node(primary.focus_neighbor_left)
		assert_str(roster_button.text).contains(operative)
		var idle = roster_button.get_node(roster_button.focus_neighbor_right)
		assert_str(idle.text).is_equal("Idle")
		for button in dossier.find_children("*", "Button", true, false):
			if not button.is_visible_in_tree():
				continue
			for property in ["focus_neighbor_left", "focus_neighbor_right", "focus_neighbor_top", "focus_neighbor_bottom", "focus_next", "focus_previous"]:
				var destination = button.get_node(button.get(property))
				assert_bool(destination.is_visible_in_tree()).is_true()
