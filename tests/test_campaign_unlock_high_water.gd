class_name TestCampaignUnlockHighWater
extends GdUnitTestSuite
## Runtime Level Select uses the same twelve-mission boundary as persisted progress and Continue.


func test_later_missions_unlock_without_replay_regression_or_a_thirteenth_mission() -> void:
	## Restore the shared autoload after exercising its real public completion callback.
	var global: Node = get_node("/root/Global")
	var previous: int = global.get("unlocked_levels")
	global.set("unlocked_levels", 9)
	global.call("unlock_next_level", 9)
	assert_int(global.get("unlocked_levels")).is_equal(10)
	global.call("unlock_next_level", 10)
	assert_int(global.get("unlocked_levels")).is_equal(11)
	global.call("unlock_next_level", 11)
	assert_int(global.get("unlocked_levels")).is_equal(12)
	global.call("unlock_next_level", 5)
	assert_int(global.get("unlocked_levels")).is_equal(12)
	global.call("unlock_next_level", 12)
	assert_int(global.get("unlocked_levels")).is_equal(12)
	global.set("unlocked_levels", previous)
