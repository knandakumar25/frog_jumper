extends Control

# Language mapping: display name -> locale code
# The language list is loaded dynamically from the translations CSV header so new
# languages can be added by updating the CSV without editing this script.
var languages: Dictionary = {}
const TRANSLATION_CSV: String = "res://assets/translations/Translations - Sheet1.csv"

# Known display overrides: locale_code -> [English name, native name]
const LANGUAGE_OVERRIDES := {
	"is": ["Icelandic", "Íslenska"],
	"english": ["English", "English"],
	"spanish": ["Spanish", "Español"],
	"eurofrench": ["French - Europe", "Français"],
	"canadianfrench": ["French - Canada", "Français"],
	"europortuguese": ["Portuguese - Europe", "Português"],
	"brazilianportuguese": ["Portuguese - Brazil", "Português"],
	"german": ["German", "Deutsch"],
	"dutch": ["Dutch", "Nederlands"],
	"italian": ["Italian", "Italiano"],
	"danish": ["Danish", "Dansk"],
	"polish": ["Polish", "Polski"],
	"arabic": ["Arabic", "العربية"],
	"turkish": ["Turkish", "Türkçe"],
	"japanese": ["Japanese", "日本語"],
	"korean": ["Korean", "한국어"],
	"mandarin": ["Mandarin", "中文"],
	"hindi": ["Hindi", "हिन्दी"],
	"ukrainian": ["Ukrainian", "Українська"],
	"thai": ["Thai", "ไทย"],
	"vietnamese": ["Vietnamese", "Tiếng Việt"],
	"malay": ["Malay", "Bahasa Melayu"],
	"indonesian": ["Indonesian", "Bahasa Indonesia"],
	"filipino": ["Filipino", "Filipino"],
	"malayalam": ["Malayalam", "മലയാളം"],
	"tamil": ["Tamil", "தமிழ்"],
	"norwegian": ["Norwegian", "Norsk"],
	"swedish": ["Swedish", "Svenska"],
	"finnish": ["Finnish", "Suomi"],
	"armenian": ["Armenian", "Հայերեն"],
	"azerbaijani": ["Azerbaijani", "Azərbaycan"],
	"bengali": ["Bengali", "বাংলা"],
	"lithuanian": ["Lithuanian", "Lietuvių"],
	"latvian": ["Latvian", "Latviešu"],
	"estonian": ["Estonian", "Eesti"],
	"hebrew": ["Hebrew", "עברית"],
	"sinhala": ["Sinhala", "සිංහල"],
	"hungarian": ["Hungarian", "Magyar"],
	"czech": ["Czech", "Čeština"],
	"slovak": ["Slovak", "Slovenčina"],
	"slovenian": ["Slovenian", "Slovenščina"],
	"romanian": ["Romanian", "Română"],
	"georgian": ["Georgian", "ქართული"],
	"telugu": ["Telugu", "తెలుగు"],
	"kannada": ["Kannada", "ಕನ್ನಡ"],
	"gujarati": ["Gujarati", "ગુજરાતી"],
	"marathi": ["Marathi", "मराठी"],
	"punjabi": ["Punjabi", "ਪੰਜਾਬੀ"],
}

var selected_language = ""

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	$VBoxContainer/MasterSlider.value = db_to_linear(AudioServer.get_bus_volume_db(0))

	# Load languages dynamically from the translations CSV
	_load_languages_from_csv()

	# Set initial language display
	var current_locale = TranslationServer.get_locale()
	for lang_name in languages.keys():
		if languages[lang_name] == current_locale:
			selected_language = lang_name
			$VBoxContainer/LanguageButton.text = lang_name
			break


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
