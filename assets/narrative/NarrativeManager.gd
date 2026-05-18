## NarrativeManager.gd
## Autoloaded singleton (res://assets/narrative/NarrativeManager)
##
## Loads the per-language narrative JSON file and exposes helpers for
## retrieving structured level data and dialogue lines.
##
## JSON file naming convention: res://assets/narrative/{locale}.json
## Falls back to "en" if the requested locale is not found.
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
const FALLBACK_LOCALE := "en"

# ---------------------------------------------------------------------------
# Private state
# ---------------------------------------------------------------------------

var _data: Dictionary = {}
var _loaded_locale: String = ""

# ---------------------------------------------------------------------------
# Lifecycle
# ---------------------------------------------------------------------------

func _ready() -> void:
	_load_for_locale(TranslationServer.get_locale())


# ---------------------------------------------------------------------------
# Public API
# ---------------------------------------------------------------------------

## Returns the full data dictionary for a level (0-indexed int or string key).
## Returns an empty Dictionary if the level does not exist.
func get_level(level_number) -> Dictionary:
	var key := str(level_number)
	var levels := _data.get("levels", {}) as Dictionary
	return levels.get(key, {}) as Dictionary


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


## Returns the number of dialogue entries for a level.
func get_dialogue_count(level_number) -> int:
	var lvl := get_level(level_number)
	if lvl.is_empty():
		return 0
	return (lvl.get("dialogue", []) as Array).size()


## Returns the solo monologue entries for a level.
func get_monologues(level_number) -> Array:
	var lvl := get_level(level_number)
	if lvl.is_empty():
		return []
	return lvl.get("solo_monologues", []) as Array


## Returns the arc dictionary by key (e.g. "act2_arc1").
## Returns an empty Dictionary if not found.
func get_arc(arc_key: String) -> Dictionary:
	var acts := _data.get("acts", {}) as Dictionary
	return acts.get(arc_key, {}) as Dictionary


## Returns the title string for a level (convenience wrapper).
func get_level_title(level_number) -> String:
	return get_level(level_number).get("title", "") as String


## Returns the setting string for a level (convenience wrapper).
func get_level_setting(level_number) -> String:
	return get_level(level_number).get("setting", "") as String


## Returns the objective string for a level (convenience wrapper).
func get_level_objective(level_number) -> String:
	return get_level(level_number).get("objective", "") as String


## Returns true if the level's dialogue is fully scripted ("scripted"),
## false if still pending ("pending").
func is_dialogue_ready(level_number) -> bool:
	return get_level(level_number).get("dialogue_status", "pending") == "scripted"


## Returns the current loaded locale string.
func get_loaded_locale() -> String:
	return _loaded_locale


## Reload narrative data for a specific locale. Called automatically on _ready()
## with the active TranslationServer locale; call again when the user changes language.
func reload_for_locale(locale: String) -> void:
	_load_for_locale(locale)


# ---------------------------------------------------------------------------
# Private helpers
# ---------------------------------------------------------------------------

func _load_for_locale(locale: String) -> void:
	var path := NARRATIVE_DIR + locale + ".json"
	if not FileAccess.file_exists(path):
		if locale != FALLBACK_LOCALE:
			push_warning(
				"NarrativeManager: No narrative file for locale '%s', falling back to '%s'."
				% [locale, FALLBACK_LOCALE]
			)
			path = NARRATIVE_DIR + FALLBACK_LOCALE + ".json"
			locale = FALLBACK_LOCALE
		else:
			push_error(
				"NarrativeManager: Fallback narrative file '%s' not found. Narrative data unavailable."
				% path
			)
			_data = {}
			_loaded_locale = ""
			return

	var file := FileAccess.open(path, FileAccess.READ)
	if file == null:
		push_error("NarrativeManager: Could not open '%s' (error %d)." % [path, FileAccess.get_open_error()])
		_data = {}
		_loaded_locale = ""
		return

	var json := JSON.new()
	var err := json.parse(file.get_as_text())
	file.close()

	if err != OK:
		push_error(
			"NarrativeManager: JSON parse error in '%s' at line %d: %s"
			% [path, json.get_error_line(), json.get_error_message()]
		)
		_data = {}
		_loaded_locale = ""
		return

	_data = json.data as Dictionary
	_loaded_locale = locale
	print("NarrativeManager: Loaded narrative for locale '%s' (%d levels)."
		% [locale, (_data.get("levels", {}) as Dictionary).size()])
