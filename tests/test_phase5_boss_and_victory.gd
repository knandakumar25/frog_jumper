class_name TestPhase5BossAndVictory
extends GdUnitTestSuite

## GdUnit4 Test Suite for Phase 5: Boss Encounter & Victory Evaluation Flow.

const BOSS_MECH_PATH := "res://scenes/gameplay/cyber_boss_mech_2d.tscn"
const BOSS_MISSILE_PATH := "res://scenes/gameplay/cyber_boss_missile_2d.tscn"
const SCORECARD_PATH := "res://scenes/ui/cyber_victory_scorecard.tscn"
const LEVEL4_PATH := "res://scenes/gameplay/level4.tscn"
const SFX_CANNON_PATH := "res://assets/audio/sfx/boss_cannon.wav"
const SFX_ALARM_PATH := "res://assets/audio/sfx/boss_alarm.wav"
const SFX_JINGLE_PATH := "res://assets/audio/sfx/victory_jingle.wav"

func test_boss_audio_assets_exist() -> void:
	assert_bool(FileAccess.file_exists(SFX_CANNON_PATH)).is_true()
	assert_bool(FileAccess.file_exists(SFX_ALARM_PATH)).is_true()
	assert_bool(FileAccess.file_exists(SFX_JINGLE_PATH)).is_true()

func test_boss_mech_scene_structure_and_defaults() -> void:
	assert_bool(FileAccess.file_exists(BOSS_MECH_PATH)).is_true()
	var scene := load(BOSS_MECH_PATH) as PackedScene
	assert_that(scene).is_not_null()

	var boss: Node = scene.instantiate()
	assert_that(boss).is_not_null()

	# Verify essential nodes
	assert_that(boss.get_node_or_null("Sprite2D")).is_not_null()
	assert_that(boss.get_node_or_null("CollisionShape2D")).is_not_null()
	assert_that(boss.get_node_or_null("EyeLight")).is_not_null()
	assert_that(boss.get_node_or_null("ThrusterSparks")).is_not_null()

	# Verify default values
	assert_float(boss.get("MaxHealth")).is_equal(300.0)
	assert_int(boss.get("CurrentPhase")).is_equal(1)
	assert_float(boss.get("PatrolDistance")).is_equal(400.0)
	assert_float(boss.get("BaseSpeed")).is_equal(130.0)
	assert_int(boss.get("PointValue")).is_equal(50)

	boss.free()

func test_boss_mech_phase2_transition_and_overdrive() -> void:
	var scene := load(BOSS_MECH_PATH) as PackedScene
	var boss: Node = scene.instantiate()
	assert_that(boss).is_not_null()
	boss.call("_Ready")

	# Initial phase should be 1
	assert_int(boss.get("CurrentPhase")).is_equal(1)

	# Dealing 160 damage reduces health from 300 to 140 (<= 50% HP = 150)
	boss.call("TakeDamage", 160.0)

	# Phase should now be 2 (Overdrive Rage)
	assert_int(boss.get("CurrentPhase")).is_equal(2)

	boss.free()

func test_boss_missile_scene_and_properties() -> void:
	assert_bool(FileAccess.file_exists(BOSS_MISSILE_PATH)).is_true()
	var scene := load(BOSS_MISSILE_PATH) as PackedScene
	assert_that(scene).is_not_null()

	var missile: Node = scene.instantiate()
	assert_that(missile).is_not_null()

	assert_that(missile.get_node_or_null("CollisionShape2D")).is_not_null()
	assert_that(missile.get_node_or_null("CoreVisual")).is_not_null()
	assert_that(missile.get_node_or_null("Trail")).is_not_null()

	assert_float(missile.get("Damage")).is_equal(18.0)
	assert_float(missile.get("Speed")).is_equal(420.0)
	assert_float(missile.get("Lifetime")).is_equal(4.0)

	missile.free()

func test_victory_scorecard_structure_and_rank_evaluation() -> void:
	assert_bool(FileAccess.file_exists(SCORECARD_PATH)).is_true()
	var scene := load(SCORECARD_PATH) as PackedScene
	assert_that(scene).is_not_null()

	var scorecard: Node = scene.instantiate()
	assert_that(scorecard).is_not_null()
	scorecard.call("_Ready")

	var rank_label: Label = scorecard.get_node_or_null("RootPanel/MainVBox/RankPanel/RankLabel")
	var score_label: Label = scorecard.get_node_or_null("RootPanel/MainVBox/StatsGrid/ScoreLabel")
	var combo_label: Label = scorecard.get_node_or_null("RootPanel/MainVBox/StatsGrid/ComboLabel")

	assert_that(rank_label).is_not_null()
	assert_that(score_label).is_not_null()
	assert_that(combo_label).is_not_null()

	# S Rank check (score >= 120 and combo >= 3)
	scorecard.call("ShowScorecard", 150, 4, "KAITO KUROGANE // CYBER SHOGUN")
	assert_bool(rank_label.text.contains("S RANK")).is_true()
	assert_bool(score_label.text.contains("150 PTS")).is_true()
	assert_bool(combo_label.text.contains("4x STRIKE")).is_true()

	# A Rank check (score >= 50 or combo >= 2)
	scorecard.call("ShowScorecard", 80, 1, "KAITO KUROGANE // CYBER SHOGUN")
	assert_bool(rank_label.text.contains("A RANK")).is_true()

	# B Rank check (lower score and combo)
	scorecard.call("ShowScorecard", 30, 1, "KAITO KUROGANE // CYBER SHOGUN")
	assert_bool(rank_label.text.contains("B RANK")).is_true()

	scorecard.free()

func test_level4_contains_boss_and_scorecard() -> void:
	assert_bool(FileAccess.file_exists(LEVEL4_PATH)).is_true()
	var scene := load(LEVEL4_PATH) as PackedScene
	assert_that(scene).is_not_null()

	var level: Node = scene.instantiate()
	assert_that(level).is_not_null()

	# Verify Boss Mech
	var boss := level.get_node_or_null("SceneObjects/Enemies/CyberBossMech2D")
	assert_that(boss).is_not_null()

	# Verify Victory Scorecard
	var scorecard := level.get_node_or_null("CyberVictoryScorecard")
	assert_that(scorecard).is_not_null()

	# Verify HUD with Boss status container
	var hud := level.get_node_or_null("CyberCombatHud")
	assert_that(hud).is_not_null()
	assert_that(hud.get_node_or_null("RootControl/BossStatusContainer")).is_not_null()

	level.free()
