extends GdUnitTestSuite
## Covers the Voice bus that level-matched speech plays on (see scripts/managers/VoiceGain.cs).


func test_voice_bus_exists_and_sends_into_sfx() -> void:
	## Voices must stay under the SFX bus so existing volume settings still control them.
	var index := AudioServer.get_bus_index("Voice")
	assert_int(index).is_greater_equal(0)
	assert_str(String(AudioServer.get_bus_send(index))).is_equal("SFX")


func test_voice_bus_limits_boosted_peaks() -> void:
	## Per-line gains boost quiet lines; the hard limiter keeps their peaks at or below -1 dB.
	var index := AudioServer.get_bus_index("Voice")
	assert_int(AudioServer.get_bus_effect_count(index)).is_greater_equal(1)
	var limiter := AudioServer.get_bus_effect(index, 0) as AudioEffectHardLimiter
	assert_object(limiter).is_not_null()
	assert_bool(AudioServer.is_bus_effect_enabled(index, 0)).is_true()
	# Exactly -1 dB: voice_gain.py CEILING_DBTP assumes it. A lower ceiling would limit more of the
	# boosted lines; a higher one would let peaks clip.
	assert_float(limiter.ceiling_db).is_equal_approx(-1.0, 0.001)


func test_voice_gain_table_imports() -> void:
	## The game reads the table through Godot's file system; it must be present and parse.
	var text := FileAccess.get_file_as_string("res://assets/audio/voice_gain.json")
	var table: Variant = JSON.parse_string(text)
	assert_bool(table is Dictionary).is_true()
	## The table lists 98 recordings; this only guards against a truncated or empty file.
	## The exact set is checked in VoiceGainTests.
	assert_int((table as Dictionary)["files"].size()).is_greater(90)
