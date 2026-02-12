extends BaseCharacter

# Ninja Frog specific properties
var is_attacking = false
var mouse_clicked = false
var q_pressed = false


func _input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_LEFT and event.pressed:
			mouse_clicked = true
	elif event is InputEventKey:
		if event.keycode == KEY_Q and event.pressed and not event.echo:
			q_pressed = true


# Override the base character's special actions
func handle_special_actions() -> bool:
	# Handle attack inputs (only when on floor and not already attacking)
	if is_on_floor() and not is_attacking:
		if mouse_clicked and sprite_2d.sprite_frames.has_animation("sword_attack"):
			mouse_clicked = false
			is_attacking = true
			velocity.x = 0  # Stop horizontal movement during attack
			sprite_2d.animation = "sword_attack"
			sprite_2d.play("sword_attack")
			await sprite_2d.animation_finished
			is_attacking = false
			return true
		elif q_pressed and sprite_2d.sprite_frames.has_animation("karate_attack"):
			q_pressed = false
			is_attacking = true
			velocity.x = 0  # Stop horizontal movement during attack
			sprite_2d.animation = "karate_attack"
			sprite_2d.play("karate_attack")
			await sprite_2d.animation_finished
			is_attacking = false
			return true
	
	# Reset input flags if attacks didn't trigger
	if mouse_clicked:
		mouse_clicked = false
	if q_pressed:
		q_pressed = false
	
	# Return true if currently attacking (skip normal movement)
	return is_attacking
