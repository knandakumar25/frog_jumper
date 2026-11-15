extends GdUnitTestSuite
## Actual campaign/open-world actor construction must select the approved SpriteCook body,
## including when constructed outside the tree by standalone tests.

const VANGUARD := "res://assets/characters/rendered/vanguard.tres"
const ATLAS := "res://assets/characters/rendered/vanguard_atlas.png"


func test_campaign_sentry_uses_spritecook_without_changing_hurtbox() -> void:
	var sentry = auto_free(load("res://scripts/actors/PalaceSentry.cs").new())
	sentry.call("_Ready")
	_assert_spritecook_body(sentry)
	assert_int(sentry.collision_layer).is_equal(2)
	assert_int(sentry.collision_mask).is_equal(0)
	var collisions: Array = sentry.get_children().filter(func(node: Node) -> bool: return node is CollisionShape2D)
	assert_int(collisions.size()).is_equal(1)
	assert_float(collisions[0].shape.radius).is_equal(34.0)
	assert_float(sentry.get("CurrentHealth")).is_equal(70.0)


func test_open_world_hostile_uses_spritecook_with_feet_on_ground_ring() -> void:
	var hostile = auto_free(load("res://scripts/world/WorldHostile.cs").new())
	hostile.call("_Ready")
	_assert_spritecook_body(hostile)
	var body: AnimatedSprite2D = hostile.get_node("SpriteCookBody")
	# The existing cell baseline is y=224, relative to a centred 256-pixel cell.
	var feet_y := (224.0 - 128.0 + body.offset.y) * body.scale.y + body.position.y
	assert_float(feet_y).is_equal_approx(0.0, 0.001)
	assert_float(hostile.get("Health")).is_equal(60.0)
	assert_int(hostile.call("RobotDirection", 0.0, 1.0)).is_equal(8)
	assert_int(hostile.call("RobotDirection", 1.0, 0.0)).is_equal(6)


func _assert_spritecook_body(actor: Node) -> void:
	var body: AnimatedSprite2D = actor.get_node("SpriteCookBody")
	assert_object(body).is_not_null()
	assert_object(actor.get_node_or_null("StandInBody")).is_null()
	assert_bool(body.show_behind_parent).is_true()
	assert_int(body.texture_filter).is_equal(CanvasItem.TEXTURE_FILTER_NEAREST)
	assert_int(body.material.light_mode).is_equal(CanvasItemMaterial.LIGHT_MODE_UNSHADED)
	assert_str(body.sprite_frames.resource_path).is_equal(VANGUARD)
	assert_str(body.animation).is_equal("idle")
	assert_int(body.sprite_frames.get_frame_count("idle")).is_equal(8)
	var first: AtlasTexture = body.sprite_frames.get_frame_texture("idle", 0)
	var last: AtlasTexture = body.sprite_frames.get_frame_texture("idle", 7)
	assert_str(first.atlas.resource_path).is_equal(ATLAS)
	assert_bool(first.region != last.region).is_true()
