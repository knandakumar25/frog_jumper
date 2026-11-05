extends SceneTree
## Diagnostic autonomous playthrough using production movement, attacks, swaps and signature/context actions.
## Never injects ledger events, health, damage or objective success. Reports stalls separately from completed runs.

var records: Array[Dictionary] = []


func _initialize() -> void:
	## Defer scene construction until autoloads and the tree are ready.
	_run.call_deferred()


func _release() -> void:
	## Prevent held movement or jump inputs leaking into the next independent fresh mission.
	for action in InputMap.get_actions():
		Input.action_release(action)


func _run() -> void:
	## Accelerated wall-clock execution preserves the normal 60-Hz gameplay step and all cooldowns.
	Engine.time_scale = 4.0
	Engine.physics_ticks_per_second = 240
	var requested := OS.get_cmdline_user_args()
	var report_path := "res://.validation/later-campaign/playthrough-%d.json" % Time.get_unix_time_from_system()
	var missions: Array[int] = [6, 7, 8, 9, 10, 11, 12]
	if not requested.is_empty():
		missions.clear()
		for number in requested:
			missions.append(int(number))
	for number in missions:
		_release()
		var bridge = load("res://scripts/managers/LevelCatalogBridge.cs").new()
		bridge.PrepareMission(number)
		bridge = null
		var mission: Node = load("res://scenes/gameplay/level%d.tscn" % number).instantiate()
		root.add_child(mission)
		current_scene = mission
		var player: CharacterBody2D = mission.get("Player")
		var hits := [0]
		player.connect("AttackLanded", func(_action): hits[0] += 1)
		var deaths := [0]
		player.connect("PlayerDefeated", func(): deaths[0] += 1)
		var last_x := player.position.x
		var stuck := 0
		var samples: Array[Dictionary] = []
		var tick := 0
		var last_objective := ""
		var objective_age := 0
		var drop_target := NAN
		for frame in 60 * 300:
			tick = frame
			await physics_frame
			if mission.get("MissionComplete"):
				break
			if mission.get("GameplaySuspended"):
				_release()
				continue
			var objective: String = mission.get("CurrentObjective")
			objective_age = objective_age + 1 if objective == last_objective else 0
			last_objective = objective
			var desired := player.position.x + 500.0
			var dodge := false
			var enemy: Node2D
			var best := INF
			for actor in get_nodes_in_group("enemies"):
				if not is_instance_valid(actor) or not actor is Node2D or not mission.is_ancestor_of(actor) or actor.is_queued_for_deletion():
					continue
				if actor.has_signal("BossDefeated"):
					continue # Supporting defenders must be cleared before a held boss phase can advance.
				if actor.get("Health") != null and float(actor.get("Health")) <= 0:
					continue
				var distance: float = absf(actor.global_position.x - player.position.x)
				if distance < best:
					best = distance
					enemy = actor
			# Arena bosses do not belong to the generic enemy group; support always takes priority.
			if enemy == null:
				for actor in mission.get_node("Actors").get_children():
					if actor.has_signal("BossDefeated") and not actor.get("IsDying"):
						enemy = actor
						break
			if enemy != null:
				if player.position.x >= 20400:
					player.call("SwapOperative", "Cyber Shogun")
				desired = enemy.global_position.x - signf(enemy.global_position.x - player.position.x) * 70.0
				var facing_correct: bool = player.get("FacingSign") == signf(enemy.global_position.x - player.position.x)
				if not facing_correct:
					desired = enemy.global_position.x
				if facing_correct and absf(enemy.global_position.x - player.position.x) < 120 and absf(enemy.global_position.y - player.position.y) < 95:
					player.call("PerformPrimaryAttack")
					if frame % 70 == 0 and player.get("AbilityTag") != "parry":
						player.call("TriggerSignatureAbility")
			elif player.position.x > 20400:
				# A final terminal can be behind the last support defenders; use its actual prop position.
				var terminal: Node2D
				var exit_prop: Node2D
				for actor in mission.get_node("Actors").get_children():
					if number != 9 and actor.get("Kind") is String and actor.get("Kind") == "Memory" and not actor.get("Collected") and actor.visible:
						terminal = actor
					elif actor.get("Kind") is String and actor.get("Kind") == "Exit":
						exit_prop = actor
				if terminal != null:
					desired = terminal.position.x
					if absf(desired - player.position.x) < 160 and terminal.position.y < player.position.y - 70 and player.is_on_floor():
						dodge = true # Reach the actual elevated processing-node interaction point with a normal jump.
				elif exit_prop != null:
					desired = exit_prop.position.x
			if player.get("AbilityTag") == "parry":
				for shot in mission.get_node("Actors").get_children():
					if shot is Node2D and (shot.has_method("Reflect") or shot.is_in_group("wildfire_rounds")) and shot.global_position.distance_to(player.position) < 190:
						player.call("TriggerSignatureAbility")
						if player.get("AbilityCooldownTimer") > 0 and not player.get("IsParrying"):
							dodge = true
			# A required gate is approached before firing its intended ability. Heat is traversed on foot.
			var gate: Node2D
			for candidate in get_nodes_in_group("ability_gates"):
				if not mission.is_ancestor_of(candidate) or candidate.get("Cleared"):
					continue
				if candidate.get("ObjectiveId") != "" and mission.call("HasActionEvidence", candidate.get("ObjectiveId")):
					continue
				var relevant: bool = (candidate.get("RequiredTag") == "douse" and "douse" in objective.to_lower()) or (candidate.get("RequiredTag") == "slam" and "shatter" in objective.to_lower()) or (candidate.get("RequiredTag") == "resonate" and ("reson" in objective.to_lower() or "acoustic" in objective.to_lower())) or (candidate.get("RequiredTag") == "bulwark" and ("bulwark" in objective.to_lower() or "absorb" in objective.to_lower()))
				relevant = relevant or (candidate.get("RequiredTag") == "bulwark" and "radiation" in objective.to_lower())
				if relevant or (candidate.global_position.x >= player.position.x - 220 and candidate.global_position.x < player.position.x + 500):
					if gate == null or candidate.global_position.x < gate.global_position.x:
						gate = candidate
			if gate != null and (enemy == null or (gate.position.x > player.position.x and gate.position.x < enemy.global_position.x and gate.position.x - player.position.x < 350)):
				var tag: String = gate.get("RequiredTag")
				desired = gate.position.x + 300 if tag == "bulwark" else gate.position.x - 240
				if tag == "bulwark" and (player.position.x > gate.position.x + 184 or player.get("AbilityCooldownTimer") > 0 and not player.get("IsBulwarked")):
					# Return to the complete entry footprint after an unprotected or expired attempt.
					desired = gate.position.x - 220
				var names := {"douse":"Oceanus", "slam":"Apex Titan", "resonate":"Aura Phoenicia", "bulwark":"Surya Prime"}
				if names.has(tag):
					player.call("SwapOperative", names[tag])
				if absf(gate.position.x - player.position.x) < (215.0 if tag == "bulwark" else 330.0):
					player.call("TriggerSignatureAbility")
			if "Phase" in objective or "phase through" in objective.to_lower():
				player.call("SwapOperative", "Mirage Blade")
				for laser in mission.get_node("Actors").get_children():
					if laser.get("AlwaysActive") == true and laser.position.x - player.position.x > 30 and laser.position.x - player.position.x < 145:
						player.call("TriggerSignatureAbility")
			# Pods use their actual mapped attack handler and retain their normal proximity/prerequisite checks.
			if number == 6:
				for index in [1, 2]:
					if mission.call("HasCombatEvent", "encounter:incubate%d" % index) and not mission.call("HasCombatEvent", "pod:%d" % index):
						desired = mission.get_node("Actors/Pod%d" % index).position.x
						break
			for actor in mission.get_node("Actors").get_children():
				if actor.name.begins_with("Pod") and actor.position.distance_to(player.position) < 100:
					var action := InputEventAction.new()
					action.action = "attack"
					action.pressed = true
					actor.call("_Input", action)
			# Walk off a one-way catwalk before returning to a defender underneath it.
			if enemy != null and enemy.global_position.y - player.position.y > 95:
				for index in player.get_slide_collision_count():
					var floor_body = player.get_slide_collision(index).get_collider()
					if floor_body != null and floor_body.get("OneWay") == true:
						var left: float = floor_body.position.x - 65
						var right: float = floor_body.position.x + floor_body.get("Size").x + 65
						drop_target = left if absf(left - player.position.x) < absf(right - player.position.x) else right
				if not is_nan(drop_target):
					desired = drop_target
				dodge = false
			else:
				drop_target = NAN
			Input.action_release("left")
			Input.action_release("right")
			if absf(desired - player.position.x) > 12:
				Input.action_press("right" if desired > player.position.x else "left")
			stuck = stuck + 1 if absf(player.position.x - last_x) < 0.25 else 0
			last_x = player.position.x
			Input.action_release("jump")
			if (dodge and player.is_on_floor() and frame % 12 == 0) or (stuck > 15 and player.is_on_wall() and frame % 24 == 0) or (enemy != null and enemy.global_position.y < player.position.y - 90 and frame % 45 == 0):
				Input.action_press("jump")
			if frame % 600 == 0:
				var sample := {"seconds":frame / 60.0,"position":str(player.position),"objective":objective,"hits":hits[0],"deaths":deaths[0]}
				samples.append(sample)
				print("PLAYTHROUGH ",number," ",JSON.stringify(sample))
			if stuck > 1200 or objective_age > 60 * 90 or deaths[0] > 8:
				break
		_release()
		var results_visible := false
		if mission.get("MissionComplete"):
			# Follow the real aftermath Skip boundary into results; do not inject a results reveal callback.
			for frame in 5:
				await process_frame
			for overlay in mission.get_children():
				if "Aftermath" in overlay.name and overlay.has_method("_Input"):
					var skip := InputEventKey.new()
					skip.pressed = true
					skip.keycode = KEY_ESCAPE
					overlay.call("_Input", skip)
			for frame in 20:
				await process_frame
			results_visible = mission.get_node("CyberVictoryScorecard/RootPanel").visible
		var result := {"mission":number,"completed":mission.get("MissionComplete"),"results_visible":results_visible,"seconds":tick / 60.0,"hits":hits[0],"deaths":deaths[0],"position":str(player.position),"objective":mission.get("CurrentObjective"),"samples":samples}
		records.append(result)
		var file := FileAccess.open("res://.validation/later-campaign/playthrough.json",FileAccess.WRITE)
		file.store_string(JSON.stringify(records,"  "))
		file.close()
		var archived := FileAccess.open(report_path,FileAccess.WRITE)
		archived.store_string(JSON.stringify(records,"  "))
		archived.close()
		print("PLAYTHROUGH RESULT ",JSON.stringify(result))
		mission.free()
		await process_frame
	Engine.time_scale = 1.0
	quit(0 if records.all(func(record): return record.completed and record.results_visible) else 1)

