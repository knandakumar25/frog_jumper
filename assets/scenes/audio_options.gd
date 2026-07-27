extends Control

# Language mapping: display name -> locale code
# The language list is loaded dynamically from the translations CSV header so new
# languages can be added by updating the CSV without editing this script.
var languages: Dictionary = {}
const TRANSLATION_CSV: String = "res://assets/translations/Translations - Sheet1.csv"

# Known display overrides: locale_code -> [English name, native name]
const LANGUAGE_OVERRIDES := {
	"english": ["English", "English"],
	"spanish": ["Spanish", "Español"],
	"eurofrench": ["French", "Français"],
	"brazilianportuguese": ["Portuguese - Brazil", "Português"],
	"german": ["German", "Deutsch"],
	"italian": ["Italian", "Italiano"],
	"polish": ["Polish", "Polski"],
	"arabic": ["Arabic", "العربية"],
	"turkish": ["Turkish", "Türkçe"],
	"japanese": ["Japanese", "日本語"],
	"korean": ["Korean", "한국어"],
	"mandarin": ["Mandarin", "中文"],
	"hebrew": ["Hebrew", "עברית"]
}

var selected_language = ""

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	var slider = get_node_or_null("VBoxContainer/MasterSlider")
	if slider != null:
		slider.value = db_to_linear(AudioServer.get_bus_volume_db(0))
		_update_volume_label(slider.value)
		if not slider.is_connected("value_changed", _on_volume_changed):
			slider.connect("value_changed", _on_volume_changed)

	# Load languages dynamically from the translations CSV
	_load_languages_from_csv()

	# Set initial language display
	var current_locale = TranslationServer.get_locale()
	var lang_btn = get_node_or_null("VBoxContainer/LanguageButton")
	for lang_name in languages.keys():
		if languages[lang_name] == current_locale:
			selected_language = lang_name
			if lang_btn != null:
				lang_btn.text = lang_name
			break


func _on_volume_changed(val: float) -> void:
	_update_volume_label(val)


func _update_volume_label(val: float) -> void:
	var lbl = get_node_or_null("VBoxContainer/VolumeLabel")
	if lbl != null:
		var pct = int(round(val * 100.0))
		lbl.text = tr("Volume").to_upper() + "   [ " + str(pct) + "% ]"


func _load_languages_from_csv() -> void:
	# Attempt to read the CSV header and extract locale codes
	var f = FileAccess.open(TRANSLATION_CSV, FileAccess.READ)
	if f == null:
		push_error("audio_options: Failed to open translations CSV: " + TRANSLATION_CSV)
		return
	var header_line = f.get_line()
	f.close()

	if header_line == "":
		push_error("audio_options: translations CSV header is empty")
		return

	var cols = header_line.split(",")
	# First column is the key, remaining columns are locale codes
	for i in range(1, cols.size()):
		var code = cols[i].strip_edges().to_lower()
		if code == "":
			continue
		var display: String = ""
		if LANGUAGE_OVERRIDES.has(code):
			var pair = LANGUAGE_OVERRIDES[code]
			display = str(pair[0]) + " (" + str(pair[1]) + ")"
		else:
			# Fallback: show code in both English and native position
			display = code.capitalize() + " (" + code + ")"
		languages[display] = code


func apply_language_change() -> void:
	if selected_language != "":
		var locale = languages[selected_language]
		TranslationServer.set_locale(locale)
		var slider = get_node_or_null("VBoxContainer/MasterSlider")
		if slider != null:
			_update_volume_label(slider.value)
		print("audio_options: TranslationServer.set_locale set to '", locale, "'. Active locale is now: '", TranslationServer.get_locale(), "'")
