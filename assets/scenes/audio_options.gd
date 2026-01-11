extends Control

# Language mapping: display name -> locale code
var languages = {
	"English (English)": "english",
	"Spanish (Español)": "spanish",
	"French - Europe (Français)": "eurofrench",
	"French - Canada (Français)": "canadianfrench",
	"Portuguese - Europe (Português)": "europortuguese",
	"Portuguese - Brazil (Português)": "brazilianportuguese",
	"German (Deutsch)": "german",
	"Dutch (Nederlands)": "dutch",
	"Italian (Italiano)": "italian",
	"Danish (Dansk)": "danish",
	"Polish (Polski)": "polish",
	"Arabic (العربية)": "arabic",
	"Turkish (Türkçe)": "turkish",
	"Japanese (日本語)": "japanese",
	"Korean (한국어)": "korean",
	"Mandarin (中文)": "mandarin",
	"Hindi (हिन्दी)": "hindi",
	"Ukrainian (Українська)": "ukrainian",
	"Thai (ไทย)": "thai",
	"Vietnamese (Tiếng Việt)": "vietnamese",
	"Malay (Bahasa Melayu)": "malay",
	"Indonesian (Bahasa Indonesia)": "indonesian",
	"Filipino (Filipino)": "filipino",
	"Malayalam (മലയാളം)": "malayalam",
	"Tamil (தமிழ்)": "tamil",
	"Norwegian (Norsk)": "norwegian",
	"Swedish (Svenska)": "swedish",
	"Finnish (Suomi)": "finnish"
}

var selected_language = ""

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	$VBoxContainer/MasterSlider.value = db_to_linear(AudioServer.get_bus_volume_db(0))
	
	# Set initial language display
	var current_locale = TranslationServer.get_locale()
	for lang_name in languages.keys():
		if languages[lang_name] == current_locale:
			selected_language = lang_name
			$VBoxContainer/LanguageButton.text = lang_name
			break


func apply_language_change() -> void:
	if selected_language != "":
		var locale = languages[selected_language]
		TranslationServer.set_locale(locale)
