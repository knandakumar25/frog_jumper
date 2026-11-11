extends RefCounted
## Test helper: reports whether a cutscene stage has the art it needs to play.
##
## The Blender-made cutscene art was deleted on 2026-11-11 and awaits SpriteCook regeneration
## (docs/ART_REGENERATION.md). Without it each cutscene skips itself, which the suites'
## "missing art" tests cover. Tests that need real playback (timelines, skip buttons, freeze and
## restore) call available() first and return early while the art is absent, so they run again
## automatically once the art is back. Not a test suite: it does not extend GdUnitTestSuite.


## Creates the stage briefly under `parent` so its _Ready loads textures, reads its
## ResourcesAvailable property, then frees it. Returns false if the stage cannot be created.
static func available(stage_script: String, parent: Node) -> bool:
	var script: Script = load(stage_script)
	if script == null:
		return false
	var stage: Node = script.new()
	parent.add_child(stage)
	var ready: bool = stage.get("ResourcesAvailable")
	stage.free()
	return ready
