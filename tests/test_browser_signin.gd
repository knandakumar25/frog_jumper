class_name TestBrowserSignIn
extends GdUnitTestSuite
## Exercise cancellation and teardown using the real autoload without touching a user account or opening a browser.


func test_cancelled_attempt_keeps_guest_controls_and_no_session() -> void:
	## Cancel before the unreachable API can complete; repeated button signals cannot create a second attempt.
	var auth := get_node("/root/AuthManager")
	var had_override := OS.has_environment("AXIOM_API_BASE_URL")
	var previous := OS.get_environment("AXIOM_API_BASE_URL")
	OS.set_environment("AXIOM_API_BASE_URL", "http://127.0.0.1:9/api")
	var panel: Node = load("res://scripts/main_menu/BrowserSignInUI.cs").new()
	add_child(panel)
	var login := panel.find_child("AUTH_LOGIN", true, false) as Button
	var signup := panel.find_child("AUTH_SIGNUP", true, false) as Button
	login.pressed.emit()
	assert_bool(auth.get("IsWaiting")).is_true()
	assert_bool(login.disabled).is_true()
	signup.pressed.emit()
	panel.find_child("AUTH_CANCEL", true, false).emit_signal("pressed")
	for attempt in range(30):
		if not auth.get("IsWaiting"):
			break
		await get_tree().create_timer(0.02).timeout
	assert_bool(auth.get("IsWaiting")).is_false()
	assert_bool(login.visible).is_true()
	assert_bool(signup.visible).is_true()
	# C# null strings cross Godot's Variant bridge as empty strings.
	assert_bool(not auth.get("AccessToken")).is_true()
	assert_bool(not auth.get("RefreshToken")).is_true()
	panel.free()
	# The autoload must no longer call a freed menu when its profile changes.
	auth.call("Logout")
	if had_override:
		OS.set_environment("AXIOM_API_BASE_URL", previous)
	else:
		OS.unset_environment("AXIOM_API_BASE_URL")


func test_leaving_menu_cancels_pending_work() -> void:
	## Freeing the menu while HTTP is pending must suppress late callbacks and leave no orphan nodes.
	var auth := get_node("/root/AuthManager")
	var had_override := OS.has_environment("AXIOM_API_BASE_URL")
	var previous := OS.get_environment("AXIOM_API_BASE_URL")
	OS.set_environment("AXIOM_API_BASE_URL", "http://127.0.0.1:9/api")
	var panel: Node = load("res://scripts/main_menu/BrowserSignInUI.cs").new()
	add_child(panel)
	panel.find_child("AUTH_LOGIN", true, false).emit_signal("pressed")
	panel.free()
	for attempt in range(30):
		if not auth.get("IsWaiting"):
			break
		await get_tree().create_timer(0.02).timeout
	assert_bool(auth.get("IsWaiting")).is_false()
	assert_bool(not auth.get("AccessToken")).is_true()
	auth.call("Logout")
	if had_override:
		OS.set_environment("AXIOM_API_BASE_URL", previous)
	else:
		OS.unset_environment("AXIOM_API_BASE_URL")
