@tool
extends EditorScenePostImport
## Keeps imported materials responsive to scene lights without replacing their PBR maps.


func _post_import(scene: Node) -> Object:
	_configure_node(scene)
	return scene


func _configure_node(node: Node) -> void:
	if node is MeshInstance3D:
		var mesh_instance := node as MeshInstance3D
		_configure_material(mesh_instance.material_override)
		if mesh_instance.mesh != null:
			for surface_index in mesh_instance.mesh.get_surface_count():
				_configure_material(mesh_instance.mesh.surface_get_material(surface_index))
				_configure_material(mesh_instance.get_surface_override_material(surface_index))

	for child in node.get_children():
		_configure_node(child)


func _configure_material(material: Material) -> void:
	var base_material := material as BaseMaterial3D
	if base_material == null:
		return

	base_material.shading_mode = BaseMaterial3D.SHADING_MODE_PER_PIXEL
	base_material.specular_mode = BaseMaterial3D.SPECULAR_SCHLICK_GGX
	base_material.metallic_specular = maxf(base_material.metallic_specular, 0.5)
