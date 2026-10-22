extends GdUnitTestSuite

const OPERATIVES := {
	"Cyber Shogun": "cyber_shogun",
	"Surya Prime": "surya_prime",
	"Oceanus": "oceanus",
	"Mirage Blade": "mirage_blade",
	"Apex Titan": "apex_titan",
	"Aura Phoenicia": "phoenicia",
}


func test_each_operative_has_original_model_portrait_and_combat_frames() -> void:
	for id in OPERATIVES.values():
		var prefix := "res://assets/characters/rendered/" + str(id)
		var portrait = load(prefix + "_portrait.png") as Texture2D
		assert_that(portrait).is_not_null()
		assert_vector(portrait.get_size()).is_equal(Vector2(768, 1024))
		var frames = load(prefix + ".tres") as SpriteFrames
		assert_that(frames).is_not_null()
		for animation in ["idle", "running", "jumping", "blade_dash", "sword_attack", "karate_attack"]:
			assert_int(frames.get_frame_count(animation)).is_equal(8)
			var first = frames.get_frame_texture(animation, 0) as AtlasTexture
			var last = frames.get_frame_texture(animation, 7) as AtlasTexture
			assert_str(first.atlas.resource_path).is_equal(prefix + "_atlas.png")
			assert_bool(first.region != last.region).is_true()
		assert_bool(frames.get_animation_loop("sword_attack")).is_false()
		assert_bool(frames.get_animation_loop("karate_attack")).is_false()
		assert_float(8.0 / frames.get_animation_speed("sword_attack")).is_equal_approx(0.25, 0.001)
		assert_float(8.0 / frames.get_animation_speed("karate_attack")).is_equal_approx(0.3, 0.001)
		assert_float(8.0 / frames.get_animation_speed("blade_dash")).is_equal_approx(1.0 / 7.0, 0.001)


func test_deploying_different_operative_changes_body_not_just_color() -> void:
	var player = load("res://scenes/gameplay/cyber_shogun.tscn").instantiate()
	player.call("_Ready")
	var sprite: AnimatedSprite2D = player.get_node("Sprite2D")
	assert_int(sprite.material.light_mode).is_equal(CanvasItemMaterial.LIGHT_MODE_UNSHADED)
	for operative in OPERATIVES:
		player.call("ApplyOperativeProfile", operative)
		assert_str(sprite.sprite_frames.resource_path).is_equal(
			"res://assets/characters/rendered/" + OPERATIVES[operative] + ".tres")
		assert_that(sprite.modulate).is_equal(Color.WHITE)
	player.free()


func test_selection_uses_the_matching_original_model_portrait() -> void:
	var runner = scene_runner("res://scenes/main_menu/character_selection.tscn")
	var menu = runner.scene()
	var card: TextureRect = menu.get_node("MainLayout/RightShowcasePanel/VBox/CardImage")
	for operative in OPERATIVES:
		menu.call("SelectOperative", operative)
		assert_str(card.texture.resource_path).is_equal(
			"res://assets/characters/rendered/" + OPERATIVES[operative] + "_portrait.png")


func test_enemies_use_their_own_animated_original_models() -> void:
	var scenes := {"cyber_drone_2d": "vanguard", "cyber_boss_mech_2d": "heavy"}
	for scene_name in scenes:
		var enemy = load("res://scenes/gameplay/" + scene_name + ".tscn").instantiate()
		var sprite: AnimatedSprite2D = enemy.get_node("Sprite2D")
		assert_str(sprite.sprite_frames.resource_path).is_equal(
			"res://assets/characters/rendered/" + scenes[scene_name] + ".tres")
		assert_int(sprite.sprite_frames.get_frame_count("idle")).is_equal(8)
		enemy.free()


func test_rollback_portraits_and_reused_sprite_sheets_are_removed() -> void:
	for id in OPERATIVES.values():
		assert_bool(FileAccess.file_exists("res://assets/images/operatives/" + str(id) + ".jpg")).is_false()
	for path in [
		"res://assets/images/A-16-bit-pixel-art-2D-side-scroller-sprite-of-a-fu-64px-9.png",
		"res://assets/images/Gemini_Generated_Image_iv7rnaiv7rnaiv7r.png",
	]:
		assert_bool(FileAccess.file_exists(path)).is_false()
