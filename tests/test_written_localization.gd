class_name TestWrittenLocalization
extends GdUnitTestSuite
## Runtime checks of selector parity, bundled script glyphs and a live English speech owner's locale change.


func test_settings_exposes_all_seventeen_languages() -> void:
	## Settings remains the single game language picker after browser authentication replaces login.
	var service := get_node("/root/LocaleService")
	var settings: Node = load("res://scripts/ui/audio_options.gd").new()
	add_child(settings)
	assert_int(settings.get("languages").size()).is_equal(17)
	assert_int(service.call("GetLanguages").size()).is_equal(17)
	settings.free()


func test_browser_auth_controls_retranslate_without_credential_fields() -> void:
	## Exercise the real menu component without opening a browser or making an API request.
	var service := get_node("/root/LocaleService")
	var previous := TranslationServer.get_locale()
	var panel: Node = load("res://scripts/main_menu/BrowserSignInUI.cs").new()
	add_child(panel)
	service.call("ApplyLocale", "japanese")
	var button := panel.find_child("AUTH_LOGIN", true, false) as Button
	assert_str(button.text).is_equal(tr("AUTH_LOGIN"))
	assert_bool(button.visible).is_true()
	assert_int(panel.find_children("*", "LineEdit", true, false).size()).is_equal(0)
	panel.free()
	service.call("ApplyLocale", previous)


func test_live_speech_changes_subtitles_without_restarting_timeline_or_stream() -> void:
	## Uses the real Tokyo audio owner, holding timeline position while every target language is applied.
	var service := get_node("/root/LocaleService")
	var previous := TranslationServer.get_locale()
	var overlay: Node = load("res://scripts/ui/TokyoRemembers.cs").new()
	add_child(overlay)
	overlay.set_process(false)
	overlay.call("Seek", 16.0)
	var player: AudioStreamPlayer = overlay.get_node("Speech")
	var stream := player.stream
	var languages: Dictionary = service.call("GetLanguages")
	for language: String in languages.values():
		var position := player.get_playback_position()
		service.call("ApplyLocale", language)
		overlay.call("Seek", 16.0)
		assert_float(overlay.get("Elapsed")).is_equal(16.0)
		assert_bool(player.stream == stream).is_true()
		assert_bool(player.playing).is_true()
		assert_float(player.get_playback_position()).is_greater_equal(position)
		assert_str(overlay.get_node("UI/SubtitlePanel/Subtitle").text).is_equal(tr("TOKYO_CUE_3"))
		assert_bool(overlay.get_node("UI/SubtitlePanel/Subtitle").is_layout_rtl()).is_equal(language in ["arabic", "hebrew"])
		assert_str(get_node("/root/NarrativeManager").call("get_loaded_locale")).is_equal(language)
		assert_str(get_node("/root/NarrativeManager").call("get_level_title", 12)).is_equal(tr("MISSION_12_TITLE"))
	overlay.free()
	service.call("ApplyLocale", previous)


func test_bound_results_retranslate_without_replaying_fanfare_or_interpolating_data() -> void:
	## Real catalog translations refresh the score while arbitrary brace-containing data and audio remain intact.
	var service := get_node("/root/LocaleService")
	var previous := TranslationServer.get_locale()
	service.call("ApplyLocale", "english")
	var card: Node = load("res://scenes/ui/cyber_victory_scorecard.tscn").instantiate()
	add_child(card)
	card.call("ShowScorecard", 350, 4, "User {0}")
	var fanfare: AudioStreamPlayer = card.get_node("JinglePlayer")
	var stream := fanfare.stream
	service.call("ApplyLocale", "eurofrench")
	assert_str(card.get_node("RootPanel/MainVBox/StatsGrid/ScoreLabel").text).is_equal(tr("UI_CYBERVICTORYSCORECARD_RENDERSCORECARD_EF1F4E5A").replace("{0}", "350"))
	assert_str(card.get_node("RootPanel/MainVBox/StatsGrid/OperativeLabel").text).contains("User {0}")
	var position := fanfare.get_playback_position()
	service.call("ApplyLocale", "german")
	assert_str(card.get_node("RootPanel/MainVBox/StatsGrid/ScoreLabel").text).is_equal(tr("UI_CYBERVICTORYSCORECARD_RENDERSCORECARD_EF1F4E5A").replace("{0}", "350"))
	assert_bool(fanfare.stream == stream).is_true()
	assert_float(fanfare.get_playback_position()).is_greater_equal(position)
	card.free()
	service.call("ApplyLocale", previous)


func test_explicit_script_fonts_have_representative_glyphs() -> void:
	## These glyph checks detect missing bundled scripts; screenshots/native review still own layout and linguistic approval.
	var service := get_node("/root/LocaleService")
	var samples := {"arabic": "مرحبا", "hebrew": "שלום", "thai": "ภาษาไทย", "japanese": "東京記憶",
		"korean": "기억", "mandarin": "记忆", "traditionalchinese": "記憶"}
	for language: String in samples:
		var font: Font = load(service.call("FontPath", language))
		assert_object(font).is_not_null()
		for character: String in samples[language]:
			assert_bool(font.has_char(character.unicode_at(0))).is_true()
	# A language picker shows all these scripts at once, even while English is active.
	var selector_font: Font = load("res://assets/fonts/LocaleSelectorFont.tres")
	for text: String in samples.values():
		for character: String in text:
			assert_bool(selector_font.has_char(character.unicode_at(0))).is_true()


func test_roster_nested_ability_fields_refresh_without_replaying_preview() -> void:
	## Real roster captions, nested damage/category fields and animation commands survive a locale round trip.
	var service := get_node("/root/LocaleService")
	var previous := TranslationServer.get_locale()
	var mouse_mode := Input.mouse_mode
	service.call("ApplyLocale", "english")
	var roster: Node = load("res://scenes/main_menu/character_selection.tscn").instantiate()
	add_child(roster)
	roster.call("SelectOperative", "Oceanus")
	var badge: Label = roster.get_node("MainLayout/RightShowcasePanel/VBox/AbilitiesBox/AbilityDescPanel/DescVBox/AbilityBadgeLabel")
	var button: Button = roster.get_node("MainLayout/RightShowcasePanel/VBox/AbilitiesBox/AbilityButtonsGrid/PrimaryAbilityBtn")
	var status: Label = roster.get_node("MainLayout/RightShowcasePanel/VBox/ShogunArtStudy").get_child(1)
	assert_str(badge.text).contains("[PRIMARY]")
	assert_str(badge.text).contains("DMG: 28")
	service.call("ApplyLocale", "eurofrench")
	assert_str(badge.text).contains(tr("ABILITY_CATEGORY_PRIMARY"))
	assert_str(badge.text).contains(tr("Tidal Torrent"))
	assert_str(badge.text).not_contains("DMG:")
	assert_str(button.text).contains(tr("Tidal Torrent"))
	# A translated movement button dispatches the retained English animation ID.
	var walk: Button = roster.get_node("MainLayout/RightShowcasePanel/VBox/ShogunArtStudy/Actions").get_child(1)
	walk.emit_signal("pressed")
	assert_str(status.text).contains(tr("PREVIEW_WALK"))
	service.call("ApplyLocale", "german")
	assert_str(status.text).contains(tr("PREVIEW_WALK"))
	assert_str(badge.text).contains(tr("ABILITY_CATEGORY_PRIMARY"))
	service.call("ApplyLocale", "english")
	assert_str(status.text).is_equal("Walk • character preview")
	assert_str(badge.text).contains("DMG: 28")
	roster.free()
	Input.mouse_mode = mouse_mode
	service.call("ApplyLocale", previous)


func test_loading_locale_change_refreshes_logo_and_status_without_changing_target() -> void:
	## Pause loader polling while allowing its presentation tween and threaded request to finish cleanly.
	var service := get_node("/root/LocaleService")
	var previous := TranslationServer.get_locale()
	service.call("ApplyLocale", "japanese")
	var loading: Node = load("res://scenes/main_menu/loading_screen.tscn").instantiate()
	loading.set("TargetScenePath", "res://scenes/main_menu/main_menu.tscn")
	add_child(loading)
	loading.set_process(false)
	await get_tree().create_timer(0.35).timeout
	var label: Label = loading.get_node("CenterContainer/MainPanel/VBoxContainer/LoadingLabel")
	assert_str(label.text).is_equal("INITIALIZING SYSTEM...")
	service.call("ApplyLocale", "thai")
	var banner: TextureRect = loading.get_node("CenterContainer/MainPanel/VBoxContainer/TitleLogoBanner")
	var expected := "res://assets/images/axiom_force_title_2d_thai.png"
	if not ResourceLoader.exists(expected):
		expected = "res://assets/images/axiom_force_title_2d.png"
	# Title art awaits SpriteCook regeneration; with no file at all the banner stays empty.
	if ResourceLoader.exists(expected):
		assert_str(banner.texture.resource_path).is_equal(expected)
	else:
		assert_that(banner.texture).is_null()
	assert_str(label.tr(label.text)).is_equal(tr("INITIALIZING SYSTEM..."))
	assert_str(loading.get("TargetScenePath")).is_equal("res://scenes/main_menu/main_menu.tscn")
	# The test deliberately never polls to transition, so consume its completed request explicitly.
	assert_object(ResourceLoader.load_threaded_get("res://scenes/main_menu/main_menu.tscn")).is_not_null()
	loading.free()
	service.call("ApplyLocale", previous)


func test_tactical_english_case_does_not_uppercase_authored_turkish() -> void:
	## A live locale round trip preserves the English presentation and Turkish dotted/dotless-I wording.
	var service := get_node("/root/LocaleService")
	var previous := TranslationServer.get_locale()
	var turkish := Translation.new()
	turkish.locale = "tr"
	# Unique synthetic identities avoid competing with the installed roster translations.
	turkish.add_message("Case Probe", "ince")
	turkish.add_message("Identity Probe", "Şogun")
	TranslationServer.add_translation(turkish)
	service.call("ApplyLocale", "english")
	var card: Node = load("res://scenes/ui/cyber_victory_scorecard.tscn").instantiate()
	add_child(card)
	card.call("ShowScorecard", 350, 4, "Case Probe // Identity Probe")
	var label: Label = card.get_node("RootPanel/MainVBox/StatsGrid/OperativeLabel")
	assert_str(label.text).contains("CASE PROBE // IDENTITY PROBE")
	service.call("ApplyLocale", "turkish")
	assert_str(label.text).contains("ince // Şogun")
	service.call("ApplyLocale", "english")
	assert_str(label.text).contains("CASE PROBE // IDENTITY PROBE")
	card.free()
	TranslationServer.remove_translation(turkish)
	service.call("ApplyLocale", previous)
