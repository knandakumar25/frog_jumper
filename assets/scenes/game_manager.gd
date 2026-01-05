extends Node

@onready var points_label: Label = %PointsLabel

var points = 0
var is_reloading = false

func add_point():
	points += 1
	print(points)
	points_label.text = tr("Points") + ": " + str(points)


func _on_water_body_entered(body: Node2D) -> void:
	if is_reloading:
		return
	
	if body is CharacterBody2D:
		is_reloading = true
		get_tree().call_deferred("reload_current_scene")
