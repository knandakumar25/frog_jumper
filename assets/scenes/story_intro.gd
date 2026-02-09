extends Control

const LoadingScreen = preload("res://assets/scenes/loading_screen.gd")

@onready var robot_sprite: AnimatedSprite2D = $HBoxContainer/RobotContainer/RobotSprite
@onready var dialogue_box: Panel = $HBoxContainer/DialogueBox
@onready var dialogue_label: RichTextLabel = $HBoxContainer/DialogueBox/MarginContainer/DialogueLabel
@onready var continue_button: Button = $ContinueButton
@onready var skip_button: Button = $SkipButton

var dialogue_lines: Array = []
var current_line_index: int = 0
var typewriter_speed: float = 0.03  # Seconds per character
var is_typing: bool = false
var current_text: String = ""
var current_char_index: int = 0
var typewriter_timer: float = 0.0

func _ready() -> void:
	Global.play_menu_music()
	
	# Setup dialogue lines with translation support
	dialogue_lines = [
		"[center][color=#00FF00]" + tr("STORY_LINE_1") + "[/color][/center]\n\n" + tr("STORY_LINE_1_INTRO"),
		tr("STORY_LINE_2"),
		tr("STORY_LINE_3"),
		tr("STORY_LINE_4"),
		tr("STORY_LINE_5"),
		tr("STORY_LINE_6"),
		tr("STORY_LINE_7"),
		tr("STORY_LINE_8"),
		tr("STORY_LINE_9")
	]
	
	# Hide continue button initially
	continue_button.visible = false
	dialogue_label.visible_characters = 0
	
	# Start robot animation if sprite frames are configured
	if robot_sprite.sprite_frames != null:
		robot_sprite.play()
	
	# Start first dialogue line
	_start_next_line()


func _process(delta: float) -> void:
	if is_typing:
		typewriter_timer += delta
		
		if typewriter_timer >= typewriter_speed:
			typewriter_timer = 0.0
			current_char_index += 1
			
			if current_char_index <= current_text.length():
				dialogue_label.visible_characters = current_char_index
			else:
				# Finished typing current line
				is_typing = false
				continue_button.visible = true


func _start_next_line() -> void:
	if current_line_index < dialogue_lines.size():
		current_text = dialogue_lines[current_line_index]
		dialogue_label.text = current_text
		current_char_index = 0
		dialogue_label.visible_characters = 0
		is_typing = true
		continue_button.visible = false
		typewriter_timer = 0.0
	else:
		# All dialogue finished, proceed to level selection
		_proceed_to_level_selection()


func _on_continue_button_pressed() -> void:
	if is_typing:
		# Skip typewriter effect for current line
		dialogue_label.visible_characters = -1
		is_typing = false
		continue_button.visible = true
	else:
		# Move to next line
		current_line_index += 1
		_start_next_line()


func _on_skip_button_pressed() -> void:
	_proceed_to_level_selection()


func _proceed_to_level_selection() -> void:
	LoadingScreen.load_scene(get_tree(), "res://assets/scenes/level_selection.tscn")


func _input(event: InputEvent) -> void:
	# Allow spacebar or Enter to continue
	if event is InputEventKey and event.pressed and not event.echo:
		if event.keycode == KEY_SPACE or event.keycode == KEY_ENTER:
			if continue_button.visible:
				_on_continue_button_pressed()
			elif is_typing:
				# Skip typewriter for current line
				dialogue_label.visible_characters = -1
				is_typing = false
				continue_button.visible = true
