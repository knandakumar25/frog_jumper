extends GdUnitTestSuite
## Operative and enemy sprite resources in assets/characters/rendered.
##
## The Blender-made sprites were deleted on 2026-11-11 and await SpriteCook regeneration
## (docs/ART_REGENERATION.md). Each test checks the full art contract when a file exists, and the
## invisible-placeholder fallback when it does not, so the suite passes before and after
## regeneration.

const OPERATIVES := {
	"Cyber Shogun": "cyber_shogun",
	"Surya Prime": "surya_prime",
	"Oceanus": "oceanus",
	"Mirage Blade": "mirage_blade",
	"Apex Titan": "apex_titan",
	"Aura Phoenicia": "phoenicia",
}
const ANIMATIONS := ["idle", "running", "jumping", "blade_dash", "sword_attack", "karate_attack"]
const ROOT := "res://assets/characters/rendered/"


## Checks a regenerated operative against the old layout: 768x1024 portrait, six 8-frame
## animations cut from <id>_atlas.png, one-shot attacks and the original attack timings.
func test_present_operative_art_matches_the_old_contract() -> void:
	for id in OPERATIVES.values():
		var prefix := ROOT + str(id)
		if ResourceLoader.exists(prefix + "_portrait.png"):
			var portrait = load(prefix + "_portrait.png") as Texture2D
			assert_vector(portrait.get_size()).is_equal(Vector2(768, 1024))
		if not ResourceLoader.exists(prefix + ".tres"):
			continue
		var frames = load(prefix + ".tres") as SpriteFrames
		assert_that(frames).is_not_null()
		for animation in ANIMATIONS:
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


## Every operative gets its own frames when they exist, and the shared invisible placeholder
## (which defines every animation actors play) when they do not.
func test_deploying_an_operative_uses_its_frames_or_the_placeholder() -> void:
	var player = load("res://scenes/gameplay/cyber_shogun.tscn").instantiate()
	player.call("_Ready")
	var sprite: AnimatedSprite2D = player.get_node("Sprite2D")
	assert_int(sprite.material.light_mode).is_equal(CanvasItemMaterial.LIGHT_MODE_UNSHADED)
	for operative in OPERATIVES:
		player.call("ApplyOperativeProfile", operative)
		var path: String = ROOT + OPERATIVES[operative] + ".tres"
		if ResourceLoader.exists(path):
			assert_str(sprite.sprite_frames.resource_path).is_equal(path)
		else:
			_assert_placeholder(sprite.sprite_frames)
		assert_that(sprite.modulate).is_equal(Color.WHITE)
	player.free()


## The selection card shows the operative's portrait, or stays empty without one.
func test_selection_uses_the_matching_portrait_when_present() -> void:
	var runner = scene_runner("res://scenes/main_menu/character_selection.tscn")
	var menu = runner.scene()
	var card: TextureRect = menu.get_node("MainLayout/RightShowcasePanel/VBox/CardImage")
	for operative in OPERATIVES:
		menu.call("SelectOperative", operative)
		var path: String = ROOT + OPERATIVES[operative] + "_portrait.png"
		if ResourceLoader.exists(path):
			assert_str(card.texture.resource_path).is_equal(path)
		else:
			assert_that(card.texture).is_null()


## Enemies load their own frames in code (not from the scene), with the placeholder as fallback.
func test_enemies_use_their_own_frames_or_the_placeholder() -> void:
	var scenes := {"cyber_drone_2d": "vanguard", "cyber_boss_mech_2d": "heavy"}
	for scene_name in scenes:
		var enemy = load("res://scenes/gameplay/" + scene_name + ".tscn").instantiate()
		add_child(enemy)
		var sprite: AnimatedSprite2D = enemy.get_node("Sprite2D")
		var path: String = ROOT + scenes[scene_name] + ".tres"
		if ResourceLoader.exists(path):
			assert_str(sprite.sprite_frames.resource_path).is_equal(path)
			assert_int(sprite.sprite_frames.get_frame_count("idle")).is_equal(8)
		else:
			_assert_placeholder(sprite.sprite_frames)
		enemy.free()


func test_rollback_portraits_and_reused_sprite_sheets_are_removed() -> void:
	for id in OPERATIVES.values():
		assert_bool(FileAccess.file_exists("res://assets/images/operatives/" + str(id) + ".jpg")).is_false()
	for path in [
		"res://assets/images/A-16-bit-pixel-art-2D-side-scroller-sprite-of-a-fu-64px-9.png",
		"res://assets/images/Gemini_Generated_Image_iv7rnaiv7rnaiv7r.png",
	]:
		assert_bool(FileAccess.file_exists(path)).is_false()


## Supplied boss art replaces the old chassis, follows facing transforms and keeps combat effects.
func test_campaign_bosses_use_spritecook_bodies_without_legacy_chassis() -> void:
	var families := {
		"BastionPraetorBoss2D": "apex_bastion",
		"IgnisColossusBoss2D": "foundry_forge",
		"AeroCenturionBoss2D": "stratos_docks",
		"SpireSentinelBoss2D": "spire_outer_hull",
		"NexusOverseerBoss2D": "cyber_nexus",
		"VoidCommanderBoss2D": "judgment_day",
	}
	for actor in families:
		var boss: Node2D = load("res://scripts/actors/" + actor + ".cs").new()
		add_child(boss)
		boss.set_physics_process(false)
		var visual: Node2D = boss.get_node_or_null("VisualRoot")
		if visual == null:
			visual = boss
		var armor: Sprite2D = visual.get_node("Armor_structure")
		assert_str(armor.texture.resource_path).is_equal(
			"res://assets/environments/later_campaign/" + families[actor] + "/boss_structure.png")
		assert_int(armor.texture_filter).is_equal(CanvasItem.TEXTURE_FILTER_NEAREST)
		assert_int(armor.material.light_mode).is_equal(CanvasItemMaterial.LIGHT_MODE_UNSHADED)
		var legacy_body_nodes: Array = visual.get_children().filter(func(child: Node) -> bool: return child is Polygon2D)
		assert_int(legacy_body_nodes.size()).is_equal(0)
		# The visible body follows the same facing/rotation root the controller animates.
		visual.scale.x = -1
		assert_float(armor.global_transform.x.x).is_less(0.0)
		assert_that(boss.get_node("CollisionShape2D").shape).is_not_null()
		if actor == "BastionPraetorBoss2D":
			boss.call("TakeDamage", 10000.0)
			assert_bool(visual.get_node("EnergyShield").visible).is_true()
		elif actor == "NexusOverseerBoss2D":
			assert_bool(visual.get_node("OrbitalRing").visible).is_true()
		elif actor == "VoidCommanderBoss2D":
			assert_bool(visual.get_node("DarkAura").visible).is_true()
		boss.free()


## The placeholder must define every animation actors play, each with one empty frame, so
## Play() never errors and nothing is drawn.
func _assert_placeholder(frames: SpriteFrames) -> void:
	assert_that(frames).is_not_null()
	for animation in ANIMATIONS + ["default", "finished"]:
		assert_bool(frames.has_animation(animation)).is_true()
		assert_int(frames.get_frame_count(animation)).is_equal(1)
		assert_that(frames.get_frame_texture(animation, 0)).is_null()
