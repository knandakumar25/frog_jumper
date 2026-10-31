# Neon Rust gameplay production and acceptance

Level 3 expands to 48,000px and eight linear sectors. The existing 75-second
Silent Freight arrival is unchanged and excluded from all pacing targets.

| Sector | World X | Target | Formations | Required events |
| --- | --- | --- | --- | --- |
| Scrapyard Insertion | 0–6000 | 4 min | s1-1..3 | Titan breaches insertion rubble |
| Freight Marshalling | 6000–12000 | 4 min | s2-1..4 | Clear defenders; Slam freight locking mechanism |
| Runoff Maintenance | 12000–18000 | 4 min | s3-1..3 | Douse defended shipment vent; permanent safety |
| Harmonic Access | 18000–22000 | 3 min | s4-1..2 | Aura joins at 18700; open practice lock at 19500 |
| West Uplink | 22000–28000 | 4 min | s5-1..4 | Harmonic cargo restraint, then west uplink |
| East Uplink/Tower | 28000–35000 | 5 min | s6-1..5 | Stair climb, three timed lasers, east uplink |
| Central Relay | 35000–40000 | 4 min | s7-1..5 | Approach, two couplers, two sequential reinforcements, relay |
| Blind-Grid Escape | 40000–48000 | 5 min | s8-1..4 | Mirage, practice crossing, separated lethal beams, extraction |

**33-minute budget / 30–35 minute human first-run target: UNVALIDATED.**
The movement-assisted blockout took about 145 seconds of physics simulation with
diagnostic enemy kills and zero recoveries. That is a geometry/functional check,
not a normal combat playthrough. Automated review stores positions, health,
objective transitions and batch times; per-sector human traversal, combat and
recovery times remain unmeasured. No minimum timers, health inflation or repeated
waves are used to enforce a duration.

`NeonRustProgress` accepts exact event IDs, rejects premature/unknown objectives,
and deduplicates kill slots. All 30 encounter IDs plus sabotage, recruits and
lessons are mandatory for extraction. Central relay acceptance requires three
shipments, both uplinks, both couplers and both reinforcement groups. Ability
targets additionally check their defenders and suspension state. HUD counters
and instructions follow unmet prerequisites, never player X position.

Shrines stand at (5850,600), (11850,600), (17850,600), (21900,600),
(27900,600), (34900,-500), (40900,-500). They activate only after prior combat
is secure. Archive loops reconnect at (8350,400), (25050,400), (45350,400).
Main tower rises are 100px, escape drops 110px. This route uses stationary decks;
no moving-platform docking gap is introduced. Practice beam x40500 deals zero
damage; Mirage must actually blink across it. Lethal curtains at 43800, 45000,
46600 are isolated on stationary ground and fit the existing 180px Blink.

Recovery clears transient rounds, restarts only incomplete encounters and restores
actor processing modes. Completed objectives, recruitment, lessons and reward
slots persist in this scene. Replay/reload creates a clean three-operative run.
Results freeze gameplay and expose Continue to Level 4 (five operatives), Replay,
Level Select and Main Menu. There is no new resume schema or ending cinematic.

## Art production

Seven original 512x512 orthographic RGBA emission-mesh renders use navy steel,
cyan surveillance and amber cargo. No external source art or new paid audio.
`assets/environment/neon_rust/manifest.json` records provenance, dimensions,
filtering and collision separation. Godot uses lossless textures, linear filtering,
no mipmaps, alpha-border correction; `NeonRustVisual` sets explicit world sizes.
Background depots: 1900x950; freight: 900x390; machinery: 330x350; supports:
410x650; sabotage props: 150x180; laser fixtures: 110x240. Props have no collision;
authored platforms and hazards retain authority.

```powershell
docker compose --profile cutscene run --rm --entrypoint blender cutscene --background --python tools/gameplay/render_neon_rust.py -- --preview
docker compose --profile cutscene run --rm --entrypoint blender cutscene --background --python tools/gameplay/render_neon_rust.py
python tools/gameplay/validate_neon_rust.py
```

Preview was inspected after the movement-tested blockout and before family
production. Asset validation checks exact dimensions, real alpha, occupied
margins and `.import` sidecars. Existing cinematic artwork and audio are untouched.

## Reproducible review

```powershell
dotnet build AxiomForce.csproj
python tools/gameplay/review_neon_rust.py
python tools/gameplay/review_neon_rust.py --captures
python tools/gameplay/review_neon_rust.py --navigation
.\run_tests.ps1
docker compose exec -T backend python manage.py test accounts
```

Review uses the local Godot AI MCP through `tools/cutscene/mcp_probe.py`. It checks
project identity, refuses to replace an already running user game, launches Level 3
with autosave disabled, and stops only its own test game. The user's Level 1 editor
scene is retained. Eval batches stay under eight seconds. Route review uses real
movement, collision, swaps and abilities but explicitly diagnostic enemy damage.
Captures cover all eight sectors at 1920x1080, 1920x1200 and 2560x1080. Evidence:
`.validation/neon-rust-gameplay/` (ignored local review output).

NUnit covers each missing extraction event, prerequisites, idempotent events,
rewards and replay ledger reset. Godot tests cover wrong/premature abilities,
concurrency, local recovery, interrupted reinforcements, recruit conditions,
actual lesson success, zero-damage practice, death suspension and direct results.
Existing Level 1/2 and arrival tests must remain green after shared changes.
Hardware controls, speaker listening and unassisted first-play pacing are separate
acceptance checks. Final import/build/test and visual results are recorded in the
root HANDOFF and MEMORY after execution.

Final review update: all 24 dressed aspect/sector captures were collected. MCP
verified Replay to a fresh arrival/three operatives, Continue to Level 4/five
operatives, Level Select and Main Menu. Initial full-suite failure was an outdated
immediate-spawn assertion; it now activates a local finite group before checking
real enemy bodies. The final suite rerun is recorded in root HANDOFF/MEMORY.

## Final verification — 2026-10-31

- Incremental C# build: zero errors/warnings. Earlier full compile: 154 existing warnings.
- run_tests.ps1: NUnit 93/93; Godot 161/161, zero errors/failures/orphans (report_367).
  Django host venv absent; explicit container suite passed 104/104 after final edits.
- Background refresh exited 0 and generated seven PNG import sidecars. Asset
  dimensions/real alpha/margins/import checks passed. Editor TLS and shutdown/
  resource diagnostics remain in the import log; it is not an error-free import.
- Dressed MCP route completed using actual movement, collision, swaps and abilities,
  with diagnostic enemy kills. No recoveries; observed peak live enemies: four.
- Captured all eight sectors at three aspect ratios. Replay, Continue (five
  operatives entering Level 4), Level Select and Main Menu verified live. Editor
  remains stopped on the user's Level 1 scene. Runtime logs had no warnings/errors.

| Sector | Assisted traversal/objective wall time | Combat | Recoveries |
| --- | ---: | --- | ---: |
| Scrapyard | 18.859 s | Diagnostic; unmeasured | 0 |
| Freight | 23.470 s | Diagnostic; unmeasured | 0 |
| Runoff | 18.882 s | Diagnostic; unmeasured | 0 |
| Harmonic | 10.794 s | Diagnostic; unmeasured | 0 |
| West | 19.431 s | Diagnostic; unmeasured | 0 |
| Tower | 27.374 s | Diagnostic; unmeasured | 0 |
| Relay | 14.745 s | Diagnostic; unmeasured | 0 |
| Escape | 17.778 s | Diagnostic; unmeasured | 0 |
| Total (arrival excluded) | 151.333 s | Not normal combat/pacing | 0 |

The totals include bounded diagnostic tool handoffs during gameplay. They cannot
establish a first-time player's experience. **Human 30–35 minute pacing remains
unvalidated**, as do physical controller/touch hardware and speaker listening.
Full-project static reference checking reports ten existing signal-path errors in
unchanged Proton Scatter/main-menu scenes and 27 warnings. Gameplay suites and
MCP runtime checks above pass; the static checker is not a clean result.
