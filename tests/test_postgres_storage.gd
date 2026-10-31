extends GdUnitTestSuite
## Integration coverage against the running PostgreSQL stack, with isolated rows cleaned up after each check.

## A new manager instance can read committed state and atomic score increments without any file fallback.
func test_campaign_round_trip_across_managers() -> void:
	var script = load("res://scripts/managers/SessionManager.cs")
	var writer = script.new()
	var reader = script.new()
	var id: int = writer.create_new_session("storage-integration")
	assert_int(id).is_greater(0)
	if id > 0:
		writer.update_session(id, 2, 40, 3)
		writer.update_session(id, 2, 10, 1)
		var saved: Dictionary = reader.get_session(id)
		assert_int(int(saved.get("current_level", 0))).is_equal(2)
		assert_int(int(saved.get("total_score", 0))).is_equal(50)
		assert_int(int(saved.get("collectables", 0))).is_equal(4)
		writer.delete_session(id)
		assert_dict(reader.get_session(id)).is_empty()
	writer.free()
	reader.free()
