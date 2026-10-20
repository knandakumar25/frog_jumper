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
const STUDY = "MainLayout/CenterShowcasePanel/VBox/ShogunArtStudy"

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
		assert_bool(menu.get_node("MainLayout/CenterShowcasePanel/VBox/CardImage").visible).is_false()
		var expected = "res://assets/characters/shogun_study/poses.png" if id == "cyber_shogun" else "res://assets/characters/hand_drawn/" + id + "_poses.png"
		assert_str(preview.get("StudyTexturePath")).is_equal(expected)
		for suffix in [".tres", "_portrait.png", "_atlas.png"]:
			assert_bool(ResourceLoader.exists("res://assets/characters/rendered/" + id + suffix)).is_true()
		assert_str(preview.get("CurrentAction")).is_equal("Idle")

## Both center buttons and the existing right-hand loadout must address the same operative slot.
func test_every_loadout_button_drives_matching_ability_slot() -> void:
	var runner = scene_runner("res://scenes/main_menu/character_selection.tscn")
	var menu = runner.scene()
	var preview = menu.get_node(STUDY + "/ArtPreview")
	var buttons = menu.get_node(STUDY + "/Actions")
	for operative in ROSTER:
		menu.call("SelectOperative", operative)
		for index in range(4):
			var expected = ROSTER[operative][index + 1]
			assert_str(buttons.get_child(index + 7).text).is_equal(expected)
			buttons.get_child(index + 7).emit_signal("pressed")
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
