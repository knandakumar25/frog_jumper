# Shattered Aegis — production and acceptance

## Implemented direction
Act I: testimony → civilian protection → occupation logistics → an opened border.
This guide records the shipped implementation and its acceptance checks;
docs/ACT_I_REVIEW.md records the subsequent Levels 1–3 readability pass.
The user has retired the story bible and fixed development plan in favor of
agent-led creative direction. The details below are implementation history,
not a mandatory script for future creative work. Existing Level 3 aftermath
(90 seconds), Level 4 arrival (120 seconds), recordings, mixed-state snapshot,
Skip and missing-resource behavior are retained by this documentation cleanup.

## Route and interaction contract
The 28,000px route has service road (0–4000), Surya's position (4000–8000),
thermal works (8000–12000), vertical gun deck (12000–17000), inner curtain
(17000–21000), Voss (21000–25000) and the safe crossing (25000–28000).
The **20–25 minute first successful run target is unvalidated**. Exclude all
cinematics and repeated deaths when measuring; diagnostics never establish it.

There are fourteen finite three-member formations, with a maximum of four live
ordinary enemies. Groups outside 1500px reset if incomplete, preserving awarded
slots; this allows bypassed groups to relinquish the concurrency budget. Required
defense: s1-3 at the approach interlock, both s2 groups before Surya, s3-3 before
the coolant intake, s4-3 before either gun controller route, s5-3 before Aura's
inner controller. Other groups may be bypassed. Original shared enemy health,
player stats and bindings remain unchanged.

Attack amber approach/crossing devices with ordinary melee. Surya must cross the
whole practice and live vent footprint under actual Bulwark; simply pressing Q
elsewhere fails. Width 140px plus body overlap leaves ample margin in the existing
1.4-second window. Oceanus permanently cools the defended intake. The upper
Oceanus coolant route and lower Titan maintenance breach commit the same western
gun event and reconnect locally. Mirage must actually Blink across the harmless
practice curtain before the separated lethal curtains; Aura disables their far
local controller. Every required rise is 100px, with stationary landing ground.
Cover must remain separate from stair edges to avoid combined 180px rises.

Exact objective order in AegisProgress:
interlock:approach → recruit:surya → lesson:bulwark → crossing:bulwark →
coolant:intake → guns:western → lesson:blink → curtain:inner → boss:voss →
crossing:open. Unknown, duplicate and premature IDs do nothing. Extraction needs
this chain, never every ordinary encounter. Rewards accept only authored slots.

Seven checkpoints follow completed sections and precede Voss. Recovery retains
the ledger, recruits, lessons, fragments and awarded slots; resets incomplete
formations, stops stale speech and clears projectiles. Voss recovery starts an
entire new 300HP attempt, including both pylons, outside the arena door. Defeat
is durable during that run. Reload/Replay/saved-session entry starts fresh under
the unchanged save model. Arrival never replays during checkpoint recovery.

## Fortress boss
AegisBossController is opt-in on the original armored missile mech. Existing
standalone behavior and health-signal/HUD bindings remain compatible. Health
clamps at 200, then 100, then zero; large hits cannot skip a phase. A five-second
cyan recovery lowers the core into melee range. Phase two creates two 60HP
ordinary-attack pylons; both must go offline. Phase three retains their destruction.

Aim warning is 1.1 seconds. The missile volley lasts at most 1.7 seconds, is
completely cancelled, then a floor strike warns for 1.05 seconds before a 0.3s
active footprint. One 420px lane is marked in a roughly 3000px arena; the whole
floor is never unsafe. Lockdown alternates two marked lanes. Phase three adds a
spread volley. Parry physically reflects the owned missile; Bulwark absorbs it;
Blink protects against environment, not ranged fire. Ordinary movement dodges.
Reflected counterfire may damage the core outside recovery except during pylon
shielding. All missiles have explicit ownership, swept collision and bounded
lifetime; transition/recovery/defeat/teardown cancels them immediately.

Voss defeat disables arena hazards, then AXIOM-7 offers extraction. Kaito asks to
open the crossing first. Attack the crossing release; invulnerable nonblocking
presentation civilians then follow a fixed road. Extraction has no timer and
snapshots/freezes once, displaying ACT I COMPLETE, Replay, Level Select and Main
Menu. There is no Continue target beyond the four-mission catalog.

## Art and voice reproduction
Use the existing axiom-force-cutscene Docker image (Blender/FFmpeg). Do not run
exports. For the existing D: bind-mount fault, create an owned unmounted helper,
copy tools/art and tools/gameplay into /workspace/tools and the cue assets/cache
into their equivalent /workspace paths. Keep the normal four services running.
Run Blender --background --python tools/gameplay/render_aegis.py -- --preview;
copy and inspect .validation/aegis/art-target.png before running the family
without --preview. Copy assets/environment/shattered_aegis back to the host.
The manifest records original emission-mesh authorship, palette, dimensions and
collision separation. Existing operative palettes and Voss's sprite are retained.

python tools/gameplay/narrate_aegis.py uses the local key without printing it,
checks established cast IDs and caches matching request hashes. Model is
multilingual-v2, stability .70, similarity .75, style .10, speed 1. Requests are
not automatically retried on HTTP failure. Run package_aegis.py inside the owned
media helper, then copy assets/audio/aegis back. Complete words are encoded
without trimming or tempo changes. cues.json and speech_provenance.json record
text, voice IDs/settings, request/audio hashes and measured durations.

AegisDialogue owns one speech player. Critical recruitment/boss beats take
priority over twelve-second expiring optional route hints; no voices overlap.
Every beat and objective has an English fallback and localization key. Missing
voice never gates an objective. Pause pauses the speech and its subtitle timer;
recovery/teardown/results cancel owned audio. Human speaker listening is required
before claiming mix or pronunciation acceptance.

## Verification and manual acceptance
Use python tools/gameplay/review_aegis.py for diagnostic route evidence.
Use traverse_aegis.py for automated real movement/attacks/abilities without
injected damage; --resume continues the retained review scene after a tool-side
intervention. Such automation is still not an unassisted human playthrough.
Use inspect_aegis.py for isolated section/phase/aspect captures and
navigate_aegis.py for real results, Continue/selector and owned temporary-save
load routes. The owned save is deleted and verified absent before the game stops. It uses real
movement, collision, attacks and abilities but diagnostic enemy/boss damage.
Capture every section and every boss phase at 1920×1080, 1920×1200, 2560×1080
through Godot AI MCP, preserving the user's editor scene. Keep launch/navigation,
diagnostic traversal and normal-play/human pacing evidence separate.

Automated checks: AegisProgressTests plus test_aegis_fortress.gd cover exact
dependencies/rewards, both gun routes, actual defense windows, Blink success and
failure, phase clamps/pylons/projectiles, checkpoint recovery and final results.
The existing cinematic/campaign/results/save/HUD suites must remain green.
After final edits: background import, dotnet build AxiomForce.csproj,
./run_tests.ps1, and explicit container Django if host venv is absent. Require
zero test failures/errors/orphans. Check generated import sidecars and runtime
logs. Report existing import/addon diagnostics separately from gameplay errors.

Unassisted full combat/traversal, first-play pacing, physical controller/touch and
speaker listening are distinct human acceptance checks, not inferred from unit
tests, screenshots or assisted traversal. Final measured evidence and remaining
limitations are recorded in root HANDOFF.md and MEMORY.MD.


## Executed acceptance — 2026-11-01

Final import exited 0 with all twenty import sidecars; existing addon TLS and
editor shutdown/resource diagnostics remain. Incremental build: zero warnings/
errors; full compile: 153 existing warnings. NUnit 97/97, Godot 190/190 (report_388,
zero failures/errors/orphans and no engine errors in its final suite log), explicit
copy-based Django 104/104, Python compilation and media QA pass. The ordinary
backend bind mount still fails with ENODEV; the normal stack was not stopped.

All 30 dressed section/phase/crossing/results aspect captures were inspected.
Automated continuous real-combat traversal reached extraction; one thermal
checkpoint return was observed between batches, with two tool-side automation
resumptions and no scene reload. Peak live ordinary enemies was three; all mission
objectives and the crossing were completed using actual abilities/attacks. Timing
includes automation stalls and cannot establish the 20–25 minute human target.
Separate movement fixtures verified Oceanus's upper route/reconnection/backtracking
and Titan's 184.71px measured jump plus the 100px gun-deck ascents. Separate diagnostic
kill traversal is retained as such, never counted as normal combat evidence.

Replay/results/Continue/selector/saved-load checks pass; the owned test save was
deleted. Both owned media/Django helpers are stopped; four normal services remain
running. Editor remains stopped on Level 1. Human pacing, physical controller/touch
and speaker-listening acceptance remain outstanding. No exports or Git publication.
