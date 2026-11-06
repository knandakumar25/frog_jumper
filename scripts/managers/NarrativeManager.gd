## NarrativeManager.gd
## Autoloaded singleton (res://assets/narrative/NarrativeManager)
##
## This manager serves as the central data repository for the game's narrative.
## It dynamically loads structured story data from JSON files based on the
## active translation locale, ensuring that level objectives, dialogue,
## and story arcs are correctly localized.
##
## JSON file naming convention: res://assets/narrative/{locale}.json
## Falls back to "english" if the requested locale file is not found.
##
## Translation Target:
## Unity Translation: Use a 'NarrativeManager' singleton (ScriptableObject or MonoBehaviour).
## Instead of raw JSON, use Unity's 'Addressables' or 'Resources' to load
## serialized JSON assets. Consider using a strongly-typed C# class structure
## (using JsonUtility or Newtonsoft.Json) to map the JSON to NarrativeData objects.
##
## Usage:
##   NarrativeManager.get_level(5)          -> Dictionary with .title, .setting, .objective, etc.
##   NarrativeManager.get_dialogue(5, 3)    -> Dictionary with .speaker, .text, .type
##   NarrativeManager.get_dialogue_count(5) -> int
##   NarrativeManager.get_arc("act2_arc1")  -> Dictionary with .title, .description, etc.

extends Node

# ---------------------------------------------------------------------------
# Constants
# ---------------------------------------------------------------------------

const NARRATIVE_DIR := "res://assets/narrative/"
const FALLBACK_LOCALE := "english"

# ---------------------------------------------------------------------------
# Private state
# ---------------------------------------------------------------------------

# The master dictionary containing all loaded narrative data for the current locale.
var _data: Dictionary = {}
# The locale code currently in use (e.g., "en", "ar").
var _loaded_locale: String = ""

# ---------------------------------------------------------------------------
# Lifecycle
# ---------------------------------------------------------------------------

func _ready() -> void:
	# Initialize narrative data using the default locale from the TranslationServer.
	_load_for_locale(TranslationServer.get_locale())


# ---------------------------------------------------------------------------
# Public API
# ---------------------------------------------------------------------------

## Returns the full data dictionary for a level (0-indexed int or string key).
## Returns an empty Dictionary if the level does not exist.
func get_level(level_number) -> Dictionary:
	var key := str(level_number)
	var levels := _data.get("levels", {}) as Dictionary
	var result: Dictionary = (levels.get(key, {}) as Dictionary).duplicate(true)
	# Only card metadata is consumed by the current campaign. Preserve older prose
	# for archival APIs, but never mistake its dialogue arrays for runtime events.
	var card: Dictionary = (_data.get("mission_cards", {}) as Dictionary).get(key, {})
	if not card.is_empty():
		result["title"] = tr(card["title_key"])
		result["objective"] = tr(card["objective_key"])
	return result


## Returns one dialogue entry by zero-based index for a given level.
## Returns an empty Dictionary if out of range.
func get_dialogue(level_number, line_index: int) -> Dictionary:
	var lvl := get_level(level_number)
	if lvl.is_empty():
		return {}
	var lines := lvl.get("dialogue", []) as Array
	if line_index < 0 or line_index >= lines.size():
		return {}
	return lines[line_index] as Dictionary


## Returns the total number of dialogue entries available for a specific level.
func get_dialogue_count(level_number) -> int:
	var lvl := get_level(level_number)
	if lvl.is_empty():
		return 0
	return (lvl.get("dialogue", []) as Array).size()


## Returns the solo monologue entries (non-dialogue) for a level.
func get_monologues(level_number) -> Array:
	var lvl := get_level(level_number)
	if lvl.is_empty():
		return []
	return lvl.get("solo_monologues", []) as Array


## Returns the arc dictionary by key (e.g. "act2_arc1").
## Used for high-level story progression markers.
func get_arc(arc_key: String) -> Dictionary:
	var acts := _data.get("acts", {}) as Dictionary
	return acts.get(arc_key, {}) as Dictionary


## Convenience wrapper: returns the title string for a specific level.
func get_level_title(level_number) -> String:
	return get_level(level_number).get("title", "") as String


## Convenience wrapper: returns the setting/location string for a specific level.
func get_level_setting(level_number) -> String:
	return get_level(level_number).get("setting", "") as String


## Convenience wrapper: returns the primary objective string for a specific level.
func get_level_objective(level_number) -> String:
	return get_level(level_number).get("objective", "") as String


## Checks if the level's dialogue has been fully authored.
## Returns true if status is "scripted", false if "pending".
func is_dialogue_ready(level_number) -> bool:
	return get_level(level_number).get("dialogue_status", "pending") == "scripted"


## Returns the current loaded locale string (e.g. "english").
func get_loaded_locale() -> String:
	return _loaded_locale


## Forces the manager to reload narrative data for a specific locale.
## This should be called when the user changes the game language in the settings.
func reload_for_locale(locale: String) -> void:
	_load_for_locale(locale)


# ---------------------------------------------------------------------------
# Private helpers
# ---------------------------------------------------------------------------

# Internal method to load and parse the JSON narrative file for a given locale.
# Includes a fallback mechanism to the English version if the target file is missing.
func _load_for_locale(locale: String) -> void:
	locale = LocaleService.Canonical(locale)
	var path := NARRATIVE_DIR + locale + ".json"

	# Fallback Logic: If the locale-specific file is missing, try the fallback.
	if not FileAccess.file_exists(path):
		if locale != FALLBACK_LOCALE:
			push_warning(
				"NarrativeManager: No narrative file for locale '%s', falling back to '%s'."
				% [locale, FALLBACK_LOCALE]
			)
			path = NARRATIVE_DIR + FALLBACK_LOCALE + ".json"
			locale = FALLBACK_LOCALE
		else:
			# Critical Error: Even the fallback file is missing.
			push_error(
				"NarrativeManager: Fallback narrative file '%s' not found. Narrative data unavailable."
				% path
			)
			_data = {}
			_loaded_locale = ""
			return

	# Open and read the JSON file.
	var file := FileAccess.open(path, FileAccess.READ)
	if file == null:
		push_error("NarrativeManager: Could not open '%s' (error %d)." % [path, FileAccess.get_open_error()])
		_data = {}
		_loaded_locale = ""
		return

	var json := JSON.new()
	var err := json.parse(file.get_as_text())
	file.close()

	# Handle JSON parsing errors.
	if err != OK:
		push_error(
			"NarrativeManager: JSON parse error in '%s' at line %d: %s"
			% [path, json.get_error_line(), json.get_error_message()]
		)
		_data = {}
		_loaded_locale = ""
		return

	# Successfully loaded narrative data.
	_data = json.data as Dictionary
	_loaded_locale = locale
	print("NarrativeManager: Loaded narrative for locale '%s' (%d levels)."
		% [locale, (_data.get("levels", {}) as Dictionary).size()])
