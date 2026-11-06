extends Control

## Audio settings presentation; language identities come from the shared LocaleCatalog.
## Display labels map to stable catalog names rather than persisted game identities.
var languages: Dictionary = {}
var selected_language = ""

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	var slider = get_node_or_null("VBoxContainer/MasterSlider")
	if slider != null:
		slider.value = db_to_linear(AudioServer.get_bus_volume_db(0))
		_update_volume_label(slider.value)
		if not slider.is_connected("value_changed", _on_volume_changed):
			slider.connect("value_changed", _on_volume_changed)

	# Use the canonical game language identities and display names.
	_load_languages()

	# Set initial language display
	var current_locale: String = LocaleService.Canonical(TranslationServer.get_locale())
	var lang_btn = get_node_or_null("VBoxContainer/LanguageButton")
	for lang_name in languages.keys():
		if languages[lang_name] == current_locale:
			selected_language = lang_name
			if lang_btn != null:
				lang_btn.text = lang_name
			break


## Refreshes the displayed percentage after a slider edit.
func _on_volume_changed(val: float) -> void:
	_update_volume_label(val)


## Displays localized volume while retaining the numeric percentage.
func _update_volume_label(val: float) -> void:
	var lbl = get_node_or_null("VBoxContainer/VolumeLabel")
	if lbl != null:
		var pct = int(round(val * 100.0))
		lbl.text = tr("Volume") + "   [ " + str(pct) + "% ]"


## Populates Settings from the shared game language catalog.
func _load_languages() -> void:
	languages = LocaleService.GetLanguages()


## Applies presentation in place; never reloads missions or restarts audio.
func apply_language_change() -> void:
	if selected_language != "":
		LocaleService.ApplyLocale(languages[selected_language])
		var slider = get_node_or_null("VBoxContainer/MasterSlider")
		if slider != null:
			_update_volume_label(slider.value)
