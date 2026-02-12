extends CharacterBody2D
class_name BaseCharacter

# Common character properties
const SPEED = 400.0
const JUMP_VELOCITY = -1000.0
var is_moving = true

@onready var sprite_2d: AnimatedSprite2D = $Sprite2D
@onready var jump_sound: AudioStreamPlayer = $JumpSound


func _physics_process(delta: float) -> void:
	if Global.reached_finish:
		sprite_2d.animation = "idle"
	
	if not is_moving:
		sprite_2d.animation = "finished"
		return
	
	# Call character-specific actions (overridden in child classes)
	if handle_special_actions():
		return  # If special action is active, skip normal movement
	
	# Animations
	if (velocity.x > 1 or velocity.x < -1):
		sprite_2d.animation = "running"
	else:
		sprite_2d.animation = "default"

	# Add the gravity
	if not is_on_floor():
		velocity += get_gravity() * delta
		sprite_2d.animation = "jumping"

	# Handle jump
	if Input.is_action_just_pressed("jump") and is_on_floor():
		velocity.y = JUMP_VELOCITY
		jump_sound.play()

	# Get the input direction and handle the movement/deceleration
	var direction := Input.get_axis("left", "right")
	if direction:
		velocity.x = direction * SPEED
	else:
		velocity.x = move_toward(velocity.x, 0, 12)

	move_and_slide()

	var isLeft = velocity.x < 0
	sprite_2d.flip_h = isLeft


# Virtual method for character-specific actions
# Return true if special action is active (to skip normal movement)
func handle_special_actions() -> bool:
	return false


func stop_movement():
	is_moving = false
