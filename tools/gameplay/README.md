# Wildfire gameplay production and review

The route is linear and scene-local. Stable identifiers are owned by
`WildfireProgress`; mission completion requires three distinct shelter arrivals,
three spires, `battery:core` and `barrier:final`. Recovery cancels impacts and rounds
before teleporting, restarts incomplete groups, and retains completed objectives.
Replaying/reloading resets local state under the existing campaign save model.

| Sector | World X | Main beats | Design target |
| --- | --- | --- | --- |
| Evacuation Road | 0–5200 | Two short formations; cyan parry rounds; dash cover; safe shell-warning preview | 3 min |
| Burning Residential Blocks | 5200–11200 | Oceanus x5700; isolated practice vent; temporary fire traversal; residential shelter | 4 min |
| Broken Overpass | 11200–16200 | Four ferries and stable islands; local combat; east shelter; secured-route checkpoint | 4 min |
| Biometric Compound | 16200–24600 | Street barricade, tower and courtyard formations; three shielded scanners; membrane x24400 | 6 min |
| Titan's Breach | 24600–28600 | Titan x25000; safe practice rubble; defended approach; battery checkpoint x28300 | 3 min |
| Battery and Extraction | 28600–35800 | Relays x30340/x31260, core x30800; finite reinforcements; final barrier x33100; shelter/extraction | 4 min |

These sector targets total 24 minutes **and have not been established by a human
first-playthrough measurement**. There are no minimum mission timers or health
padding. The assisted blockout check took approximately 96 seconds of movement
with enemies cleared through diagnostic damage, so it cannot validate the pacing
goal. Collect unassisted per-sector times before treating that goal as accepted.

Required jumps use at most 110px stairs and 170px ferry docking gaps. Optional
fragments are at (8500,390), (13350,450), (21300,170). Artillery uses only authored
combat lanes, locks its location for a 2.4s warning, and cannot strike shelters,
recruit lessons or shrines. Civilians have no combat hurtbox, use a short 440px
same-floor route, and never board ferries. Their fires are permanently safe before
movement; other fires still expire after eight seconds.

Original art is made in the existing opt-in Docker image. Inspect the preview
before making the family; no narration or existing cinematic assets are replaced:

```powershell
docker compose --profile cutscene run --rm --entrypoint blender cutscene --background --python tools/gameplay/render_wildfire.py -- --preview
docker compose --profile cutscene run --rm --entrypoint blender cutscene --background --python tools/gameplay/render_wildfire.py
python tools/gameplay/validate_wildfire.py
```

Preview: `.validation/wildfire-gameplay/art-target.png`. Production files and
provenance: `assets/environment/wildfire/manifest.json`. Each is a 512x512 RGBA
orthographic emission render, scaled explicitly by `WildfireVisual`; collision is
authored separately. Asset validation checks dimensions, real alpha, occupied
margins and generated `.import` sidecars after the required background import.

Godot AI MCP review scripts verify project identity, refuse to replace an existing
running game, launch Level 2 with autosave disabled, and stop only their test game.
The user's edited scene is preserved. Eval batches stay below MCP's eight-second
limit. Diagnostic damage is clearly distinguished from real combat playtesting:

```powershell
python tools/gameplay/review_wildfire.py --blockout
python tools/gameplay/review_wildfire.py
python tools/gameplay/verify_wildfire_objectives.py
```

Evidence goes to `.validation/wildfire-gameplay/`: `blockout.json`, `captures.json`,
`objectives.json`, `assets.json`, and six-sector captures at three aspect ratios.
Blockout movement uses real inputs, gravity and collision with diagnostic combat
clearance; it does not measure first-playthrough pacing. Full-chain verification
uses actual objective callbacks, signature abilities, civilian movement and the
existing automatic Level 3 transition. Never use its timings as a speedrun result.

Regression suites cover temporary/permanent fire safety, local activation, exact
extraction conditions, repeated events, relay shielding, reinforcement limits,
recovery, bullet cover/parry, recruitment prompts and natural/skip arrival handoff.
Physical controller/touch hardware, listening tests and exports are separate checks.

Final check (2026-10-30): background import exit 0 with five PNG sidecars; incremental
C# build zero errors/warnings; NUnit 45/45; GdUnit4 134/134 with zero errors,
failures or orphan nodes (`reports/report_354`); container Django 104/104. The
Windows wrapper skips Django because its backend venv is absent. Runtime objective
verification observed Level 3, and the user's editor scene remains Level 1.
Known unrelated diagnostics remain: ten reference-check errors in untouched menu /
proton_scatter signal connections, headless addon TLS/shutdown warnings, and the
host PostgreSQL/local-JSON fallback. Touch/controller checks used synthetic input
and prompt paths; no full physical touch-only play session was performed.
