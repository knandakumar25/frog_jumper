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

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	$VBoxContainer/MasterSlider.value = db_to_linear(AudioServer.get_bus_volume_db(0))
	
	# Populate language dropdown
	var language_option = $VBoxContainer/LanguageOption
	var current_locale = TranslationServer.get_locale()
	var selected_index = 0
	var index = 0
	
	# Add English first
	language_option.add_item("English (English)")
	if languages["English (English)"] == current_locale:
		selected_index = 0
	index += 1
	
	# Sort remaining languages alphabetically
	var sorted_langs = []
	for lang_name in languages.keys():
		if lang_name != "English (English)":
			sorted_langs.append(lang_name)
	sorted_langs.sort()
	
	# Add sorted languages
	for lang_name in sorted_langs:
		language_option.add_item(lang_name)
		if languages[lang_name] == current_locale:
			selected_index = index
		index += 1
	
	language_option.selected = selected_index


func _on_master_slider_mouse_exited() -> void:
	release_focus()


func _on_language_option_item_selected(_index: int) -> void:
	# Don't apply immediately, wait for Apply button
	pass


func apply_language_change() -> void:
	var language_option = $VBoxContainer/LanguageOption
	var selected_language = language_option.get_item_text(language_option.selected)
	var locale = languages[selected_language]
	TranslationServer.set_locale(locale)
