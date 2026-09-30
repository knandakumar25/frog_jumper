extends SceneTree

func _initialize() -> void:
	var models = [
		"cyber_shogun.glb",
		"solaria.glb",
		"oceanus.glb",
		"surya_prime.glb",
		"mirage_blade.glb",
		"apex_titan.glb"
	]
	for m in models:
		var p = "res://assets/models/hq/" + m
		var res = load(p) as PackedScene
		if res == null:
			print(m, ": FAILED TO LOAD")
			continue
		var inst = res.instantiate()
		var ap: AnimationPlayer = inst.find_child("AnimationPlayer", true, false)
		if ap:
			print("\n=== ", m, " (", ap.get_animation_list().size(), " anims) ===")
			for a in ap.get_animation_list():
				var anim = ap.get_animation(a)
				print("  - ", a, " (len: ", anim.length, "s, loop: ", anim.loop_mode, ")")
		else:
			print(m, ": NO ANIMATION PLAYER")
		inst.queue_free()
	quit()
