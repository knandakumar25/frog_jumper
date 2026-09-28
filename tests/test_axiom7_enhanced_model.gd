class_name TestAxiom7EnhancedModel
extends GdUnitTestSuite

## GdUnit4 Test Suite for Enhanced AXIOM-7 3D Model and Node Hierarchy

func test_enhanced_model_resource_exists() -> void:
	var path := "res://assets/models/hq/axiom_7_enhanced.glb"
	assert_bool(FileAccess.file_exists(path)).is_true()

func test_enhanced_model_instantiation_and_nodes() -> void:
	var path := "res://assets/models/hq/axiom_7_enhanced.glb"
	var scene := load(path) as PackedScene
	assert_that(scene).is_not_null()

	var instance: Node = scene.instantiate()
	assert_that(instance).is_not_null()

	# Recursive search helper
	var find_child_matching := func(root: Node, prefix: String) -> Node:
		var stack: Array[Node] = [root]
		while stack.size() > 0:
			var curr: Node = stack.pop_back()
			if curr.name.begins_with(prefix):
				return curr
			for child in curr.get_children():
				stack.push_back(child)
		return null

	var hand_l: Node = find_child_matching.call(instance, "Axiom_Hand_L")
	var hand_r: Node = find_child_matching.call(instance, "Axiom_Hand_R")
	var eyes: Node = find_child_matching.call(instance, "Axiom_Eyes")
	var visor: Node = find_child_matching.call(instance, "Axiom_Visor")
	var chest_core: Node = find_child_matching.call(instance, "Axiom_ChestCore")
	var antenna: Node = find_child_matching.call(instance, "Axiom_Antenna")
	var thruster: Node = find_child_matching.call(instance, "Axiom_ThrusterNozzle")

	assert_that(hand_l).is_not_null()
	assert_that(hand_r).is_not_null()
	assert_that(eyes).is_not_null()
	assert_that(visor).is_not_null()
	assert_that(chest_core).is_not_null()
	assert_that(antenna).is_not_null()
	assert_that(thruster).is_not_null()

	instance.free()
