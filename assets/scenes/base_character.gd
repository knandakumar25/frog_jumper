extends CharacterBody2D
class_name BaseCharacter

# Common character properties
const SPEED = 400.0
const JUMP_VELOCITY = -1000.0
var is_moving = true

@onready var sprite_2d: AnimatedSprite2D = $Sprite2D
@onready var jump_sound: AudioStreamPlayer = $JumpSound


func _ready() -> void:
	print("=== BaseCharacter Debug ===")
	print("Character instance created: ", name)
	print("Sprite2D node: ", sprite_2d)
	if sprite_2d:
		print("Sprite frames: ", sprite_2d.sprite_frames)
		print("Current animation: ", sprite_2d.animation)
		print("Sprite visible: ", sprite_2d.visible)
		print("Sprite modulate: ", sprite_2d.modulate)
		if sprite_2d.sprite_frames:
			print("Available animations: ", sprite_2d.sprite_frames.get_animation_names())
			# Force the sprite to be visible
			sprite_2d.visible = true
			sprite_2d.modulate = Color(1, 1, 1, 1)
			# Make sure animation is playing
			sprite_2d.play("idle")
			print("Started playing 'idle' animation")
	print("Character position: ", global_position)
	print("Character scale: ", scale)
	print("===========================")
	
	# Debug timer to periodically check sprite state
	var timer = Timer.new()
	add_child(timer)
	timer.timeout.connect(func():
		if sprite_2d and sprite_2d.sprite_frames:
			print("[", Time.get_ticks_msec(), "] Sprite playing: ", sprite_2d.is_playing(), " | Animation: ", sprite_2d.animation, " | Frame: ", sprite_2d.frame)
	)
	timer.wait_time = 2.0
	timer.start()


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
	var prev_animation = sprite_2d.animation
	if (velocity.x > 1 or velocity.x < -1):
		sprite_2d.animation = "running"
	else:
		sprite_2d.animation = "default"

	# Add the gravity
	if not is_on_floor():
		velocity += get_gravity() * delta
		sprite_2d.animation = "jumping"
	
	# Debug: Print animation changes
	if prev_animation != sprite_2d.animation:
		print("Animation changed: ", prev_animation, " -> ", sprite_2d.animation)

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
