# Axiom Force

## SpriteCook art reset (2026-11-13)

All art made with SpriteCook on 2026-11-12 was deleted at the owner's request: the Phase 1
character atlases, portraits and `.tres`, the button states, dialogue frame/pointer, app
icon and favicon, the city menu base and the 17 localized titles with their website copies.
`spritecook-assets.json` is empty again and the icon references in `project.godot`,
`export_presets.cfg` and both `index.html` files were removed. Operatives, the drone and
the boss are invisible placeholders until the art is regenerated on the Adventurer plan.
The deleted files are in git at `3d05726`. The redo is planned in `docs/ART_REGENERATION.md` ("Methods" and "Adventurer budget"): about 60 paid generations at 2K/4K (2,104 credits, with about 676 in reserve after the owner's top-up) make all 325 images, using grid sheets, layers derived locally and cutscene poses built from the Phase 1 animations (`tools/art/slice_sheet.py`, `derive_layers.py`, `compose_poses.py`, `pack_topdown.py`). The helpers in `tools/art/` are kept; the
title helper needs Pillow with libraqm for Arabic and Hebrew (use Linux Docker on Windows).

## Open world status (2026-11-08)

The Phase 0–4 roadmap below was logged as complete, but much of it is catalog data with no way to reach it in play. What is playable now: exploration, transit, missions, safehouses, the electric bike, and eight quests in Little Tokyo and Concrete Wards with conversations and drone combat. Branch choices are saved and shown as story outcomes, with no gameplay effect yet. Combat controls are (F/B attack, Space/RB dodge, Q/LB guard). The other 14 regions can be explored but have no working quests. Conversations are English only. See the status table in `docs/NEO_KYOTO_EXPANSION_PLAN.md`.

Back up saves with `tools/db/backup.ps1`; restore with `tools/db/restore.ps1 -File <dump> -ConfirmOverwrite <database>`. Saves live only in PostgreSQL. After listening to a flagged voice line, record it in `docs/voice-coverage/listening_acceptance.json`.

## Neo-Los Angeles 2157: Living Resistance World (roadmap; see status above) — 2026-11-07

The open world exploration map is grounded in the game's canonical master setting: **Greater Los Angeles Resistance Basin, 2157 (Neo-Los Angeles)**.
Following the [World Expansion Plan](docs/NEO_KYOTO_EXPANSION_PLAN.md), the open world spans a massive 128 km × 128 km streamed terrain
across all sixteen districts, completing all phases of the master roadmap: Phase 0 (Architecture & Persistence),
Phase 1 (Core Neighborhoods), Phase 2 (Urban Core Expansion), Phase 3 (Natural & Coastal Expansion), and Phase 4 (Endgame & Final Acceptance):

- **All 16 Districts Fully Authored & Liberated-World State (Los Angeles 2157)**:
  - Urban & Metropolitan: Historic Core & Little Tokyo (5), Concrete Wards & LA River (9), Downtown LA Core (6), San Pedro Harbor (7), Century Citadel Ring (10), Alameda Rustrail Belt (13), Vernon Foundry Basin (14).
  - Natural, Coastal & Highlands: Angeles Crest Highlands (0), Castaic Aqueduct Reservoir (1), San Gabriel Peaks (2), Malibu Coast (3), Topanga Canyon Preserve (4), Mojave Solar Belt (8), Santa Monica Coast (11), Kurogane Ridge Escarpment (12), Ballona Reclamation Wetlands (15).
  - Post-Mission 12 all 16 districts transition into `DistrictEra.Liberated` supporting reconstruction tasks and historical combat archives.
  - Backward compatibility: Exploration saves and PostgreSQL persistence accept both `"neo-los-angeles-v1"` and legacy `"neo-kyoto-v1"` identities.
- **Endgame Systems (Phase 4 Deliverables)**:
  - **16 Regional Reconstruction Tasks**: 1 per region focused on community infrastructure, civilian clinics, clean energy, and transit restoration.
  - **8 Corpo Remnant Tactical Incidents**: High-threat localized patrol and sabotage encounters for liberated sectors.
  - **6 Archive Historical Combat Simulations**: Replayable tactical battle milestones for each operative with custom mutator rules.
  - **5 Operation Challenge Modifiers**: `Ironman Protocol` (1.50x), `Overclocked Mechanoids` (1.35x), `Glass Vanguard` (1.30x), `EMP Atmosphere` (1.20x), and `Axiom Protocol` (2.00x).
  - **Operation Mastery Scoring**: 4 distinct achievement tiers (Bronze, Silver, Gold, Platinum) awarding Credits, Faction Reputation, and exclusive operative vehicle liveries/skins (`livery_gold_`, `skin_platinum_`).
  - **Comprehensive Accessibility Profile**: High-contrast attack telegraphs, parry/dodge window scaling (1.0x–2.5x), puzzle assistance modes (`Standard`, `ExtendedClues`, `AutoSolveAllowed`), screen shake scaling (0.0x–1.0x), and controller glyph customisation.
  - **Simulation & Streaming Performance Budget**: Strict runtime entity caps (16 civilians, 8 hostiles, 4 moving vehicles) across 9 resident and 25 cached chunks, guaranteeing p95 frame times within 18.0 ms and max streaming stalls <= 33.3 ms.
- **Master Target Quotas Fulfilled**:
  - **72 Authored Quests**: 48 District storyline quests (3-part narrative chains across all 16 regions), 12 Operative Personal Episodes (2 per operative for Kaito, Arjun, Kiana, Zayd, Tariq, Maya), and 12 Faction Quests across all 4 canonical organizations (Civic Recovery Network, Independent Couriers, Reclamation Guild, Free Signal Collective).
  - **48 Regional Challenges**: Exactly 3 per region across all 16 regions (Race sprint, Traversal/Operative Puzzle, Discovery Trail).
  - **16 Replayable Side Operations**: Exactly 1 per region across all 16 regions with specific terminal coordinates and side-scrolling mission scenes.
  - **48 Named Residents**: Exactly 3 per region across all 16 regions with authored Day and Evening routines.
  - **16 District Safehouses**: Exactly 1 per region with complete resting and vitals restoration, plus 2 secondary facilities (Palace Archives, Viaduct Clinic) and Old Kyoto HQ customization.
  - **64 Activity Pockets**: Exactly 4 per region across all 16 regions with environmental interactions, lore caches, and vendor hubs.
- **Dynamic 4-State Weather Cycle**:
  - Predictable 48-minute regional weather cycle (`Clear` → `Rain` → `Fog` → `RegionalStorm`) computed deterministically in `NeoKyotoWorld.cs` and exposed in runtime inspection.
- **Vehicles & Traversal**:
  - Starter Electric Bike: ~20 m/s road traversal, auto-dismounts in interiors, water, and rough terrain.
  - Utility Boat: ~14 m/s waterway traversal across reservoirs, coastal inlets, and canals, restricted to water surfaces.
  - Bounded top-down combat mechanics: Attack, Defend, and Dodge actions for all six operatives.
  - 3-state encounter alert system (`Suspicion` → `Search` → `Pursuit`). Fast transit is locked out during active pursuit.
- **Campaign-Isolated Progression & Atomic Persistence**:
  - Operative progression is fully isolated per campaign (`progression:{session}`) with typed modifiers (`StatModifierType`), 2 module slots, 2 gadget slots, and a strict 20% passive bonus ceiling.
  - `WorldTransactionService` provides atomic PostgreSQL commits with idempotent action receipts (`receipt:{session}:{token}`) ensuring zero double-spending or duplicate quest reward settlements.
- **Verification**: All test suites passing (Django 111/111, NUnit 361/361, GdUnit4 333/333 across 71 suites with 0 errors/failures/orphans).

## Cities beyond Los Angeles

Six cities join Los Angeles, one for each operative: Kyoto (Kaito), Mumbai (Arjun), Honolulu (Kiana), Dubai (Zayd),
Luanda (King Tariq) and Beirut (Maya). Walk to the **Skyport** just south of the Little Tokyo station (Interact) to fly
for free. Flights are blocked during pursuit. A city opens when its operative is on the squad.

Each city is a 32 km square with four districts, eight quests with written conversations, eight residents, eight
landmarks, a safehouse, and one optional side-scrolling operation entered from its terminal. An operation pays Sparks on
the first clear and credits on up to five repeats. Cities never affect the 12-mission campaign; finishing a campaign
mission always returns to Los Angeles.

To add a city, write a `CitySpec` like `scripts/world/cities/MumbaiCity.cs`, add it to `CityRegistry.All`
(`scripts/world/WorldDefinition.cs`), and add a `BeaconOperationMission`
subclass in its own file with a thin scene. Each city saves to its own row, `world:{session}:{city}`; Los Angeles keeps
`world:{session}`. City art (`assets/world/<city>/module_N.png` and `landmark_N.png`, 384x384) is made with SpriteCook;
see [Art pipeline](#art-pipeline-spritecook). A city with no art yet still plays, with blank props.

Quest text, conversations and place names in these cities show in English in every language, as the Los Angeles quest
conversations do. Only the five new interface labels are translated, and no native speaker has reviewed them.

## Neo-Los Angeles 2157 exploration

After Mission 1, results return to a streamed 128 km × 128 km region with sixteen
districts, 48 named destinations, 256 free transit stations and Downtown/Foundry
underground layers. Walk/run with WASD or a controller; M/Back opens the pan/zoom
map and transit list. Missions launch through their story-gated world entrances;
aftermaths, results, replay and the final return to exploration remain available.
World progress uses the existing PostgreSQL save store with retry on failure.
See [controls, architecture and art workflow](docs/NEO_KYOTO.md).

The rendered debug sweep held nine resident chunks and 25 cached chunks. Median
frame interval was 16.68 ms; p95 was 17.73 ms, so sustained 1080p/60 remains an
acceptance target. Native art/language review also remains pending.

Final verification: Django 111, NUnit 314, Godot 321 (zero errors/failures/orphans),
20 offline Python checks, build/import and all twelve live entrance routes passed.

## Written localization — catalog complete (2026-11-06)

Login and settings share seventeen languages, ISO aliases, script fonts and RTL
direction. All 1,565 currently required entries per language are populated, with
zero missing cells, malformed format contracts or unexplained English matches.
English speech, cue timing and the original English catalog remain unchanged.
Dynamic HUD, result and roster text refresh without restarting gameplay or audio.

Spanish campaign text is authored directly; the remaining campaign additions use
machine-assisted drafts with agent checks and contextual corrections. This is
structural completion and representative presentation QA, not native-speaker
certification. External linguistic review remains recommended before release.
See [coverage](docs/localization/coverage.md) and the
[authoring and review guide](docs/localization/README.md) for scope and provenance.

Both language pickers include fonts for every displayed script. Roster ability
categories/damage and preview captions retain canonical gameplay IDs across locale
changes. The loading tagline is a separate translated label rather than embedded
English artwork. Forty directly referenced raster assets were inspected at the time;
brand-only logos remain unchanged.

Localization checkpoint verification passed: Django 104, NUnit 290, Godot 317 (zero suite errors,
failures or orphans), plus 19 offline Python checks. Godot import exits 0;
existing editor-addon/shutdown diagnostics remain documented in HANDOFF.md.
The backend, database and website remain running. No game exports or Git
publication were performed.


A high-fidelity 2D cyber-action game developed in **Godot 4.7.2 .NET (C#)** with a **Django REST Framework** backend and **React** web client. The twelve-mission campaign spans three acts. The game features dynamic operative combat, special abilities, multi-operative passive synergy, multi-phase boss mechs, and a reactive 2D combat HUD.

## Code knowledge graph (Graphify, local)

Agents query a local code graph built by the `graphifyy` Python package. Nothing is uploaded.

```powershell
uv tool install --system-certs --python 3.14 "graphifyy[mcp]"
graphify update .                 # (re)build graphify-out/graph.json; no LLM; about 2 min
graphify explain "MusicGain"      # a node and its connections, with file:line
graphify query "how is mission music levelled"
```

- **Claude Code:** reads the graph through a local-scope MCP server, `graphify` (`graphify-mcp graphify-out/graph.json`).
- **Output:** `graphify-out/` is git-ignored.
- **Coverage:** the graph covers C#, Python, JS/JSX, Markdown, JSON and YAML. It does not include GDScript, scenes or assets.

## Art pipeline (SpriteCook)

Blender was retired on 2026-11-11. All game and website art is now made with the SpriteCook plugin (the `spritecook@spritecook` plugin in Claude Code, the official `spritecook@openai-curated-remote` plugin in Codex, and an API-key MCP entry in Antigravity). Generation spends paid credits, so the owner approves each batch. The account was on the SpriteCook Starter plan (2026-11-12); the owner is moving to the Adventurer plan for the regeneration (2026-11-13). Each agent signs in to SpriteCook once before its tools work.

- **Style:** pixel art (owner, 2026-11-12).
- **Deleted:** every image Blender made, which is 344 game images plus 78 website copies of the titles. Also deleted: the generator scripts, the `blendot`, `blender_godot_pipeline` and `gltf_fix_blender_light_intensity` editor add-ons, and Blender in the cutscene Docker image. The originals are in git at commit `ca92679`. `addons/animation_combiner` (an FBX/GLB tool with Blender helper scripts) is still installed, pending an owner decision.
- **Also deleted (2026-11-12):** every image that was left (115 files at commit `67eab0a`):
  - the hand-drawn operative studies (`assets/characters/hand_drawn`, `shogun_study`) and the code that drew them over the real sprites (`TrainingArtwork`, `OperativeStudyAtlas`);
  - the older title logos and their website copies;
  - the UI art, backgrounds and template images;
  - the app icon and favicon.
  Of these, 9 are listed to remake; the rest are unused or superseded.
- **Plan:** one month of the Adventurer plan, 2K/4K only; see `docs/ART_REGENERATION.md` ("Adventurer budget").
- **Done so far:** Phase 3, the other eight cutscenes (2026-11-14, 282 credits: Wildfire arrival and aftermath, Tokyo, Neon Rust intro and aftermath, Shattered Aegis, Names without Numbers, Act II), so every cutscene plays again; Tokyo's escape shot still draws the blade above Kaito's head until `TokyoStage.cs` gets a new offset. Phase 2 deployment cutscene (2026-11-13, 72 credits: city, roof, drone and Shogun layers, plus both Phase 3 blades), so the opening cutscene plays again. Phase 1 characters, redone on 2026-11-13 on the Adventurer plan (634 credits): pixel-art atlases, portraits and restored `.tres` for all six operatives, the `vanguard` and `heavy` idles, and the AXIOM-7 portrait. Asset IDs are in `spritecook-assets.json`.
- **To regenerate:** `docs/ART_REGENERATION.md` lists each image's path, pixel size, layer role, frame layout and the code that loads it. Put each new file at its listed path and size; the game picks it up with no code change. `ArtRegenerationSpecTests` checks the sizes against `docs/art_regeneration.json`.
- **Until then the game has gaps on purpose:**
  - Every mission, Level 1, the squad cutscenes and the selection preview draw operatives from their SpriteCook atlas through `OperativeVisuals`.
    - All six operatives, the drone and the boss have pixel-art atlases again (2026-11-13).
    - An actor without an atlas would be invisible but still play, using an empty placeholder animation set.
    - Sentries, open-world hostiles and Aegis civilians are drawn in code.
  - Cities, menus and the Level 1 map have no art. Level 1 uses its procedural skyline.
  - Buttons show text only, the AXIOM-7 speech bubble has no frame, and the game window and browser tab use default icons.
  - Cutscenes play again (Phases 2 and 3 done).
  - The title slot is empty in the game and shows text on the website (`website/src/components/TitleLogo.jsx`).
- The downloaded CC0 stand-ins (`assets/stand_in/`) and the Pixel Adventure leftovers were deleted as well. The bike, sentries and open-world drones are drawn in code, and their sounds are silent.
- Mentions of "Blender" or "Docker/Blender" artwork in the dated sections below describe art that has since been deleted.

## Asset quality tools

`tools/assets/` measures and compares assets:
- `audit_images.py` and `compare_images.py` run on Pillow and numpy, for example `uv run --system-certs --no-project --with pillow --with numpy python tools/assets/audit_images.py --out .validation/asset_quality/images.json`. Use them to check SpriteCook art.
- `audit_audio.py` uses ffmpeg in the cutscene container.

The synthesized sound effects come from `scripts/tools/generate_cyber_sfx.py`. The Blender re-render tools were removed with Blender on 2026-11-11.

Voice files are never re-encoded, because tests, transcripts and the owner's listening sign-off record their hashes. Voice lines are level-matched at playback instead:
- `tools/assets/voice_gain.py` (cutscene container) writes `assets/audio/voice_gain.json`.
- `scripts/managers/VoiceGain.cs` adds each line's gain to the speech players, and `deployment_cutscene.gd` does the same for the narration.
- Those players use the limited `Voice` bus, which sends into SFX.
- Re-run the tool after changing any recording; `VoiceGainTests` fails until you do.

Mission music is levelled the same way: `tools/assets/music_gain.py` writes `assets/audio/music_gain.json` (target -29 LUFS, about 9 LU under speech), and `CodeLevel` plays each track on the Music bus at its gain via `scripts/managers/MusicGain.cs`. Re-run it after adding or changing a mission track; `MusicGainTests` fails until you do. Findings and before/after numbers are in [docs/ASSET_QUALITY_AUDIT.md](docs/ASSET_QUALITY_AUDIT.md).

## Stand-in assets

Candidate free packs for temporary art and audio, with license limits, are listed in [docs/STAND_IN_ASSETS.md](docs/STAND_IN_ASSETS.md). The packs that were in use (the electric bike, combat sounds, the `PalaceSentry` and `WorldHostile` bodies) were deleted on 2026-11-11. `scripts/gameplay/StandInAssets.cs` returns null for the missing files, so those are drawn in code and silent. `tests/test_stand_in_assets.gd` checks those fallbacks.

## Godot AI startup

The pinned Godot AI 4.4.1 launcher uses an isolated uv-managed Python 3.15 tooling
interpreter and the operating system certificate store (`--system-certs`). PyYAML
has no Python 3.15 wheel on PyPI, so the launcher also reads
`%APPDATA%\uv\wheelhouse`, which holds one PyYAML 6.0.3 wheel built locally without
its optional C extension (`PYYAML_FORCE_LIBYAML=0`). If that folder is lost, Godot AI
fails with a missing-wheel error; rebuild the wheel with `uv build --wheel`. Launches
keep `--no-build`, and certificate validation, canonical package sources and
the release dependency cutoff remain enabled. After a launcher-policy update,
click **Reload Plugin** in the Godot AI dock. A plugin reinstall/update can replace
this local policy patch; recheck these flags if the same startup failure returns.

## Python dependencies

Application Docker images and the root `.venv` target Python **3.15.2**. Backend
and cutscene orchestration use the pinned official slim image; the opt-in export
Dockerfile copies the same Python version with a build-time native-library check.
The cutscene image holds only Python and ffmpeg (Blender was removed on 2026-11-11). The exporter's
setup stage verifies Python 3.15.2, .NET 10.0.403 and native Python imports without exporting a game.

Root `requirements.txt` is the single Python dependency manifest. The duplicate
`backend/requirements.txt` remains removed. A Git-ignored root `.venv` is available
for local Python work; packages are installed only when explicitly requested.

To populate it with all seven direct requirements and their dependencies:

```powershell
python -m venv .venv
.\.venv\Scripts\python.exe -m pip install --upgrade pip
.\.venv\Scripts\python.exe -m pip install -r requirements.txt
.\.venv\Scripts\python.exe -m pip check
```

The backend image builds from the repository root with `backend/Dockerfile`:
`docker compose build backend`. Its Dockerfile-specific ignore file limits the
build context to backend source and root requirements. Dependencies are installed
inside the image. Django tests use the running backend container through
`./run_tests.ps1 -Suite django`; the runner performs no package installation.
Keep the normal Docker stack running with `docker compose up -d`.

Validated on 2026-11-05: Compose configuration, backend image build and dependency
imports passed; all suites passed (Django 104, NUnit 276, GdUnit4 303). No local
virtualenv was recreated or populated during that earlier cleanup validation.

Local setup verified on 2026-11-06: Python 3.15.2, pip 26.3, all six direct
requirements plus dependencies installed in `.venv`, and `pip check` passed.
The three default Docker services are running; PostgreSQL accepts connections.
HTTP checks passed with status 200 at `http://127.0.0.1:5173/` and
`http://127.0.0.1:8000/api/auth/countries/basic/`, including the website API proxy.

## Later campaign repair (2026-11-05)

Missions 6–12 now require actual accepted ability actions and full protected
crossings, independently of defender clearance. Pods use the attack binding or
click/tap. Sequential waves, reachable boss hurtboxes, parry return shots,
defender-held phases and arena retries preserve accepted progress and one-time
rewards. Terminal attempts remain usable until accepted.

Results restore Continue 5→6 and 6→7 and end with **Return to Main Menu**. Runtime
unlocks and PostgreSQL saves use the twelve-mission campaign boundary. A stable
per-run receipt settles score/progress atomically, with visible save status and
Retry Save; replay cannot lower progress or duplicate an uncertain settlement.

Seven original layered environment families and matching boss plates replace the
later generic skyline. Recognizable approved operative art is shared with fourteen
cinematics. Thirteen missing dialogue tracks use 117 complete natural-speed takes
from the established cast; fourteen distinct atmosphere beds include the missing
Foundry aftermath. Provenance, dimensions, cue timing and hashes ship beside assets.
Cinematics consume Skip, release actions, remove finished overlays and restore
owned cursor/help/audio state. Small windows retain readable subtitle text.

See [verification and human acceptance](docs/LATER_CAMPAIGN_ACCEPTANCE.md) for
reproduction tools, current evidence and the physical-device/listening checklist.
Automated verification passes: **104 Django, 276 NUnit and 303 Godot tests**, with
zero Godot errors, failures or orphans. Missions 6–12 each reached actual results
from fresh entry using movement/combat/abilities; all 126 later cinematic shots
were reviewed at three aspect ratios, plus a small-window check (504 captures).
Human pacing, physical-device and listening acceptance remains on the checklist.
Existing addon import/TLS/shutdown diagnostics are documented separately. The user authorized Git publication of this verified batch. No game exports were
produced. The core Docker services remain running.

---

## Mission 5 & Act II Revamp — Names Without Numbers (2026-11-02)

The transition into Act II and Mission 5 features comprehensive cyber-action polish:
- **Act II Transition & Operations Directory**: The tactical operations directory features canonical mission names across Act I (*Ghost in the Gridlock*, *The Concrete Wildfire*, *Neon Rust and Data Dust*, *Shattered Aegis*) and Act II (*Names Without Numbers*), with distinctive amber/gold Act II premiere styling. Level 4's scorecard explicitly provides the **CONTINUE TO ACT II** progression milestone.
- **Act II Opening Cutscene (`ActIIOpening` & `ActIIStage`)**: Features high-fidelity Shot 0 title reveal with anamorphic flares, digital scanline effects, smooth camera focal tracking, volumetric searchlight cones, biometric scanner frequency grids, tactical squad staging (Surya holding the refugee crossing, Kaito vanguard, Oceanus with civilians, Titan anchor, Aura perimeter, Mirage flank), and character-colored subtitle speaker badges.
- **Multi-Tier Level 5 Architecture (`NamesWithoutNumbersMission`)**: Replaces flat monotonous ground with vertical platforming, including wall-jump ventilation shafts, elevated service conduits, multi-tier inspection catwalks, archive mezzanine balconies, and fortified core vault platforms. All primary stairs retain conservative 100px steps, while one-way upper platforms provide full acrobatic combat freedom.
- **Encounter & System Polish**: Sector-tailored combat encounters (fast service sentry ambushers, elevated hall gunner nests, and phased core defense waves), interactive floating holographic slates on `RecoveryTerminal`, and telegraphed cyber laser scanners with warning chevrons and floor ionization rings on `RegistryScanner`.

Six connected sectors cover scanner timing, processing control, archive cooling and
maintenance, three authenticated original records, exclusion isolation, three ordered
finite recovery waves and restored civilian access. Seventeen three-member formations
use the existing combat/score/checkpoint systems with a six-live-enemy cap. Required
archive stairs rise 100px; records are indestructible context terminals using the normal
attack action, or click/tap the nearby console. Exact prerequisites reject premature/duplicate interactions. Completed
objectives and formations survive checkpoints, incomplete groups reset and each member
rewards once across retries. Three optional civilian testimony fragments never gate exit.

Mission 5 combat tuning separates most adjacent formation activation ranges and gives its
gunners 1.45-second aim warnings with 2.6 seconds of post-burst recovery; sentry beams
warn for 1 second. Enemy health, formation count, six-enemy cap and other missions'
default combat timings are unchanged. The full unassisted route and 25–30-minute target
still need a sustained playtest. The tuning build, import and full test runner passed;
see [Mission 5 acceptance](tools/gameplay/NAMES_WITHOUT_NUMBERS.md) for details.

Surya remains unlocked and speaks over comms from the queue. His slot shows
“Protecting the queue”; roster selection, direct player swaps and cycling all exclude
him only in Mission 5. Invalid preferred selections fall back to Kaito. Mirage phases
into processing, Aura isolates scanner denial, Oceanus stabilizes coolant and Titan
opens the damaged maintenance route away from protected records. Committing restored
identities visibly opens amber denial gates with accepted-access markers.

Original Docker/Blender props, nineteen new cached established-cast cues and synthesized
atmosphere/scanner cues include reproducible production scripts and provenance. A single
mission-owned speech lane serializes critical dialogue above expiring hints; missing
speech retains localized English fallback subtitles. Owned audio pauses/stops on mission
lifecycle transitions. Docker's D: bind mount failed, so production used copied inputs
in the same existing media image; the running stack was preserved.

Assisted blockout traversal completed all records, sequential recovery, commit and exit
using real movement, operative abilities and diagnostic enemy clearance. It exposed an
upper gallery approach problem, fixed with solid conservative stairs before dressing.
This is functional evidence, not unassisted combat balance or the requested 25–30-minute
first-play duration. Physical controller/touch and subjective mix listening remain
separate acceptance checks. Production and final verification details are in
[Mission 5 acceptance](tools/gameplay/NAMES_WITHOUT_NUMBERS.md).

### Pre-Level 6 Opening Cutscene — Into the Cyber-Grave (2026-11-02)

A skippable 120-second live 2D cinematic prelude (`CyberGraveIntro` & `CyberGraveStage`, scene `scenes/cutscenes/cyber_grave_intro.tscn`) establishing the descent into Mission 6 (*Cyber-Grave Labs - Underground Facility*):
- **Narrative & Authored Script**: Following the liberation of the biometric registry in Mission 5, AXIOM-7 traces corrupted citizen transit manifests 400 meters beneath Downtown Core into the clandestine bio-processing foundry designated "Cyber-Grave".
- **Cast Performance**: 9 fully voiced dialogue cues featuring Kaito (Cyber Shogun), Oceanus, Aura Phoenicia, Apex Titan, Mirage Blade, Surya Prime, and AXIOM-7, synthesized with ElevenLabs multilingual TTS using designated voice profiles and normalized to an exact 120.0s OGG stream with matching procedural atmospheric audio (42Hz drone, steam hiss, rushing elevator wind, and metallic deceleration).
- **Procedural Staging**: Heavy industrial freight lift descending through deep bedrock, rushing parallax girders, bioluminescent chemical nutrient conduits (`#00ff99`), rotating volumetric amber hazard strobes (`#ffaa00`), wireframe holographic facility schematics with live depth readout (`DEPTH: -412M`), operative squad silhouettes, and speaker-color-coded subtitle modulation.
- **Lifecycle & Navigation**: Owned by `CanvasLayer` with deterministic seekable clock; natural finish, Skip (ESC / Space / Enter / Gamepad Start / Click), or missing audio fallback safely transitions to mission preparation without orphan nodes.

### Mission 6 Gameplay — Cyber-Grave Labs (2026-11-03)

Full Act II Mission 6 2D platformer gameplay (`CyberGraveMission.cs`, `scenes/gameplay/level6.tscn`) extending `CodeLevel` and implementing `IFiniteCombatHost`:
- **6-Operative Squad Fielded**: For the first time in the campaign, all six operatives (Cyber Shogun, Oceanus, Apex Titan, Aura Phoenicia, Mirage Blade, and Surya Prime) are available and fieldable together, supporting instant tactical swapping with shared health/shield ratios.
- **6 Deep Underground Sectors**:
  1. *Freight Sump & Breach*: Infiltration elevator shaft with vertical wall-jump shafts and steel catwalks.
  2. *Incubation Foundry*: Bio-synthetic cloning facility with interactive `GenePod.cs` stasis cylinders, rising nutrient bubble shaders, and captive operatives.
  3. *Cryo-Coolant Conduit*: Sub-zero piping networks guarded by timed `CoolantVent.cs` cryogenic steam hazards.
  4. *Resonance Induction Chamber*: High-frequency sonic lock requiring Aura Phoenicia's harmonic resonance bypass.
  5. *Geothermal Siphon*: Magma/steam conduits with pulsing lethal laser curtains traversable via Mirage Blade phase dash.
  6. *Central Vault & Extraction Lift*: 3-stage core defense waves, biometric data core override terminal, and high-speed extraction freight elevator.
- **Interactive Hazards & Props**:
  - `GenePod.cs`: Interactive stasis pod prop with animated procedural nutrient bubbles, captive humanoid silhouette, and liberation logic.
  - `CoolantVent.cs`: Area2D cryogenic/superheated steam hazard with cycle timers and neutralization hooks.
  - Ability Gates: Titan Rubble Wall, Oceanus Fire Vent, Phoenicia Sonic Lock, Oceanus Heat Vent, and Mirage Laser Grid.
- **Ledger & Campaign Integration**: Pure C# `CyberGraveProgress.cs` mission ledger tracking 13 milestones and 15 authored combat encounters; registered in `LevelCatalog.cs`, accessible from campaign chain and Level Select Sector 6 card. Passed all unit test suites (Django 104/104, NUnit 120/120, GdUnit4 220/220 across 38 suites with 0 errors, 0 failures, 0 orphans).

### Mission 7 Gameplay — The Foundry Forge (Act II Finale) (2026-11-04)

Full Act II finale 2D platformer gameplay (`FoundryForgeMission.cs`, `scenes/gameplay/level7.tscn`) extending `CodeLevel` and implementing `IFiniteCombatHost`:
- **6-Sector Geothermal & Volcanic Layout**:
  1. *SEC.01 // BASALT RIFT INGRESS*: Volcanic chasm entry catwalks, basalt chimney wall-jump shafts, and initial thermal patrol drones.
  2. *SEC.02 // CRYO-COOLANT TURBINE LOOPS*: Superheated steam hazards (`CoolantVent.cs`) and steam vent gate requiring Oceanus Douse.
  3. *SEC.03 // MAGNETIC INDUCTION CONDUITS*: High-voltage laser grid (`CyberLaserGrid2D.cs`) bypassed via Mirage Blade Phase Shift and heavy magnetic pressure valve requiring Apex Titan Slam.
  4. *SEC.04 // PRIMARY CONTAINMENT OVERLOOK*: High-frequency ultrasonic barrier requiring Aura Phoenicia Resonance, extreme radiation vents requiring Surya Bulwark, and laser curtains.
  5. *SEC.05 // CORE GUARDIAN // IGNIS-COLOSSUS*: Climactic multi-phase 2D boss mech encounter (`IgnisColossusBoss2D.cs`, 450 HP) with sweeping swept molten mortar projectiles (`FoundrySlagBall2D.cs`) parryable by Kaito's blade for 40 reflected damage, sweeping flamethrowers, and Phase 3 core overdrive meltdown.
  6. *SEC.06 // CRITICAL OVERLOAD EXTRACTION*: Core Terminal override, planetary sky-shield collapse, and emergency extraction gantry.
- **Act II Completion & Victory Scorecard**:
  - Overload override terminal drops the Citadel Spire's sky-shields across all municipal sectors.
  - Extraction triggers `CyberVictoryScorecard.ShowActIIScorecard` presenting `ACT II COMPLETE`, overall mission grade, and campaign completion status.
- **Pure C# Ledger & Verification**:
  - `FoundryProgress.cs` mission state ledger tracking 13 milestones and 15 authored combat encounters.
  - Registered in `LevelCatalog.cs` (`level7.tscn`) and Sector 7 card in Level Selection menu (`level_selection.tscn`).
  - Verified across test suites with 0 errors, 0 failures, 0 orphans (Django 104/104, NUnit 143/143, GdUnit4 231/231 across 41 test suites).

### Mission 8 — Stratos Docks (Aviation Hub) (2026-11-04)

Full Act III opening 2D platformer gameplay (`StratosDocksMission.cs`, `scenes/gameplay/level8.tscn`) extending `CodeLevel` and implementing `IFiniteCombatHost`:
- **6-Sector High-Altitude Aerial Layout**:
  1. *SEC.01 // PERIMETER GANTRY CATWALKS*: High-wind maintenance gantries at 1,200m elevation, mooring pylon wall-jump columns, and aerial patrol drones (`CyberDrone2D.cs`).
  2. *SEC.02 // CRYO-FUEL DEPOT & CONDUITS*: Pressurized cryogenic hydrogen vents (`CoolantVent.cs`) requiring Oceanus Douse, and heavy magnetic titan-clamp mooring locks requiring Apex Titan Slam.
  3. *SEC.03 // FLIGHT DECK SCANNER ARRAYS*: High-voltage laser grid (`CyberLaserGrid2D.cs`) bypassed via Mirage Blade Phase Shift, and acoustic frequency radar lock requiring Aura Phoenicia Resonance.
  4. *SEC.04 // AIR TRAFFIC CONTROL GANTRY*: Flak battery fire corridors requiring Surya Prime Bulwark, elevated crane gantry traversal, and garrison waves.
  5. *SEC.05 // FLIGHT DECK GUARDIAN // AERO-CENTURION*: Climactic multi-phase 2D aerial mechanoid boss encounter (`AeroCenturionBoss2D.cs`, 480 HP) launching high-velocity swept plasma missiles (`AeroMissile2D.cs`) parryable by Kaito's blade for 33 reflected damage back to the boss, jet charges, and Phase 3 overdrive storm thruster dive-bombs.
  6. *SEC.06 // BAY 4 VTOL COMMAND INTERLOCK*: Heavy military gunship override terminal, flight telemetry sync with AXIOM-7, and boarding ramp extraction trigger initiating Spire ascent.
- **Squad Synergy & Level Selection Card**:
  - Full 6-operative squad synergy across all sectors with dedicated signature ability gates.
  - Registered in `LevelCatalog.cs` and Sector 8 card in Level Selection menu (`level_selection.tscn` / `level_selection.gd`).
- **Pure C# Ledger & Verification**:
  - `StratosProgress.cs` mission state ledger tracking 13 milestones and 14 authored combat formations.
  - Covered by automated unit test suites (`StratosProgressTests.cs` and `test_stratos_docks_mission.gd`) with 0 errors, 0 failures, 0 orphans.

### Pre-Level 12 Cutscene — The Final Reckoning (Judgment Day Intro) (2026-11-04)

A skippable 90-second live 2D cinematic prologue (`JudgmentDayIntro` & `JudgmentDayStage`, scene `scenes/cutscenes/judgment_day_intro.tscn`) initiating the campaign finale Mission 12 (*Judgment Day*) as Axiom Force breaches the Citadel summit:
- **Narrative & Staging**: Axiom Force arrives at Spire Level 120 (5,000 meters elevation) inside the colossal Void Singularity Chamber & Throne Room. Above them looms the apocalyptic Void Singularity Core—a tear in cosmic reality draining the planetary geothermal energy grid. Suspended on a floating obsidian dais before the event horizon sits the Void Commander in cybernetic armor, wielding twin dual-phase energy katanas. All six operatives assemble in battle formation for the ultimate confrontation to liberate the world.
- **Squad Performance & Authored Dialogue**: 9 timed dialogue cues across 90 seconds featuring AXIOM-7 alerting arrival at 5,000m and registering extreme singularity reactor output, Surya raising solar bulwark shields against gravitational warping, Oceanus detecting the planetary energy grid diversion, Mirage observing the Void Commander waiting alone on the floating throne, Titan slamming electromagnetic fists together, Aura sensing the heartbeat of millions across the eleven liberated sectors below, AXIOM-7 intercepting the Void Commander's chilling broadcast from the throne, Mirage synchronizing phase blades and squad shields, and Kaito drawing his blade to lead Axiom Force into the final strike (*Judgment Day*).
- **Procedural Graphics Architecture**: Deep cosmic void background gradient (`#020008` to `#160028` to `#32004d`), massive pulsating cosmic singularity core with pitch-black event horizon, cyan/magenta/gold gravitational lensing halo rings, 50-particle orbiting accretion disk, swirling coronal flares, cathedral vaulted ceilings, colossal obsidian columns with pulsing glyph conduits, reflective polished floor reflecting the singularity and conduits, floating obsidian throne dais with anti-gravity rings and imperial crest, imposing Void Commander silhouette with glowing crimson visor and ignited twin katanas, all 6 operative silhouettes with signature weapon and shield glows, CRT scanlines, 2.39:1 anamorphic cinema bars, and guaranteed zero external texture dependencies (`ResourcesAvailable => true`).
- **Engine & Progression Wiring**: Formally registered Mission 12 (*Judgment Day*, `res://scenes/gameplay/level12.tscn`, all 6 abilities) in `LevelCatalog.cs`, routed via `CyberVictoryScorecard.cs` when continuing from Mission 11, and integrated Sector 12 into `scenes/main_menu/level_selection.tscn` and `scripts/main_menu/level_selection.gd`. Fully verified across NUnit (`JudgmentDayIntroTests.cs`, 243 tests) and GdUnit4 (`test_judgment_day_intro.gd`, 282 tests across 57 suites) with 0 errors, 0 failures, and 0 orphan nodes.

### Post-Level 11 Cutscene — The Gates of Judgment (Apex Bastion Aftermath) (2026-11-04)

A skippable 90-second live 2D cinematic epilogue (`ApexBastionAftermath` & `ApexBastionAftermathStage`, scene `scenes/cutscenes/apex_bastion_aftermath.tscn`) concluding Mission 11 (*The Apex Bastion*) upon extraction:
- **Narrative & Staging**: Axiom Force stands atop the high obsidian battlements at 4,000 meters altitude before the monumental blast doors of the Grand Citadel. Below and around them lies the smoldering, spark-arcing chassis of the defeated Bastion Praetor war mechanoid and silenced perimeter flak batteries. As atmospheric void storms rage above in the stratospheric sky, the Citadel blast doors part outward, revealing a blinding radiant void portal shimmering with golden and crimson energy leading into the Grand Citadel Throne Room for the ultimate confrontation against the Void Commander (Mission 12).
- **Squad Performance & Authored Dialogue**: 9 timed dialogue cues across 90 seconds featuring AXIOM-7 confirming Bastion Praetor destroyed and outer fortress defenses silenced, Mirage observing the collapse of the Praetorian defensive cordon, Titan kicking smoking wreckage off the ramparts, Oceanus reading violent energy spikes in the stratospheric vortex above, Surya noting the liberated city districts below watching the summit illuminate, Aura feeling the ultrasonic pulse of the Void Core resonating through the portal, Mirage confirming Throne Room security overrides complete, AXIOM-7 warning of maximum singularity drive output, and Kaito drawing his blade to lead Axiom Force through the portal threshold into the final battle (*Judgment Day*).
- **Procedural Graphics Architecture**: High-altitude void storm sky (`#03020a` to `#180b26`) with dynamic arcing void lightning strikes (magenta, crimson, gold), heavy obsidian battlements deck with illuminated cyan/amber hazard channels, smoldering wreck of the defeated Bastion Praetor with sparking cannon mounts and smoke plumes, monumental Grand Citadel Throne Room blast doors parting to reveal radiant golden/crimson portal, 6 operative silhouettes advancing toward portal, CRT scanlines, 2.39:1 cinema bars, and guaranteed zero external texture dependencies (`ResourcesAvailable => true`).
- **Engine & Scorecard Integration**: Seamlessly attached in `CodeLevel.CompleteMission` when `MissionNumber == 11`, freeze-isolating the extracted level, playing the cinematic epilogue, and deferring to `CyberVictoryScorecard.ShowMissionScorecard` targeting Mission 12 upon natural finish or skip (ESC / Space / Enter / gamepad / click). Verified across NUnit (`ApexBastionAftermathTests.cs`) and GdUnit4 (`test_apex_bastion_aftermath.gd`) with 0 errors, 0 failures, and 0 orphan nodes.

### Mission 11 Gameplay — The Apex Bastion (Stratospheric Fortress) (2026-11-04)

Full Act III penultimate 2D platformer gameplay (`ApexBastionMission.cs`, `scenes/gameplay/level11.tscn`) extending `CodeLevel` and implementing `IFiniteCombatHost`:
- **6-Sector Stratospheric Fortress Layout (28,500 Unit Horizontal Scope)**:
  1. *SEC.01 // STRATOSPHERIC SUMMIT GANTRY*: Open-air freight lift arrival platform at 4,000m altitude, howling crosswinds, high-altitude gantry catwalks, and automated perimeter patrols (`summit1`, `summit2`). Checkpoint at x=1,200.
  2. *SEC.02 // FLAK BATTERY TRENCHES & COOLANT CONDUITS*: Pressurized plasma flak coolant lines (`CoolantVent.cs`) requiring Oceanus Douse (`GateKind.FireVent`), heavy magnetic titan-clamp battery mooring barricades requiring Apex Titan Slam (`GateKind.RubbleWall`), and flak battery neutralization (`flak1`, `flak2`, `flak3`). Checkpoint at x=5,500.
  3. *SEC.03 // LOWER REDOUBT & LASER DEFENSE CORRIDOR*: Multi-tier obsidian battlements, high-voltage exterior laser grid curtains (`CyberLaserGrid2D.cs`) bypassed via Mirage Blade Phase Shift, concentrated plasma radiation thermal vents defended under Surya Prime Bulwark (`GateKind.HeatVent`), and lower redoubt clearance (`redoubt1`, `redoubt2`, `redoubt3`). Checkpoint at x=10,500.
  4. *SEC.04 // PRAETORIAN HYDRAULIC ELEVATOR SHAFT*: Vertical wall-jump lift shaft, harmonic dissonance acoustic security locks requiring Aura Phoenicia Resonance (`GateKind.SonicLock`), and praetorian vanguard waves (`praetorian1`, `praetorian2`). Checkpoint at x=16,000.
  5. *SEC.05 // GRAND BASTION REDOUBT // BASTION PRAETOR*: Multi-platform fortress rampart arena facing the Citadel Spire summit, featuring the penultimate multi-phase boss mechanoid `BastionPraetorBoss2D` (620 HP) armed with swept deflectable `PraetorPlasmaBolt2D` projectiles (reflectable by Kaito's Kinetic Parry for 45 reflected damage), deployable energy shields, sweeping flak salvos, and Phase 3 Praetor Overdrive Fury (`arena1`, `arena2`, `arena3`). Checkpoint at x=21,000.
  6. *SEC.06 // THRONE ROOM EMBANKMENT & CITADEL BREACH*: Citadel blast doors override terminal hack, final praetorian garrison encounter (`breach1`), and extraction threshold leading into the Grand Citadel Throne Room (Mission 12). Checkpoint at x=26,000.
- **Deflectable Boss Projectile (`PraetorPlasmaBolt2D.cs`)**:
  - Swept high-velocity plasma bolt fired by the Bastion Praetor boss.
  - Implements `public void TakeDamage(float amount) { Reflect(); }` which seamlessly triggers `CyberShogun.cs` parry logic, turning bolt cyan and reflecting it back to the boss for 45 reflected damage.
- **Multi-Phase Boss Mechanoid (`BastionPraetorBoss2D.cs`)**:
  - 620 HP boss mechanoid defending the high fortress ramparts.
  - Procedural rendering: heavy obsidian gunmetal chassis, crimson command visor, twin shoulder-mounted plasma flak cannons, deployable hexagonal energy shield barrier, and rocket thruster flames.
  - Phase 1 (100% - 70% HP): Rhythmic plasma cannon salvos, ground hover repositioning, and jump stomps.
  - Phase 2 (70% - 35% HP): Rapid swept plasma flak bursts, deployable Praetorian energy shield, sweeping beam cannons, and tactical hover dashes.
  - Phase 3 (35% - 0% HP): Praetor Overdrive Fury — dual overcharged plasma barrages, seismic shockwave ground slams, and rapid jet thruster charges.
- **Squad Synergy & Campaign Wiring**:
  - Full 6-operative roster active with operative-specific ability gates and dynamic in-level squad swapping.
  - Sector 11 card in Level Selection (`scenes/main_menu/level_selection.tscn`, `scripts/main_menu/level_selection.gd`).
  - Scorecard routing in `scripts/gameplay/CodeLevel.cs` (`MissionNumber == 11` targeting Mission 12).
- **Pure C# Ledger & Verification**:
  - `ApexBastionProgress.cs` pure C# engine-free mission ledger tracking 14 ordered milestones, 14 authored combat formations, and bounded retry slot rewards.
  - Automated unit test suites: NUnit (`ApexBastionProgressTests.cs`, 237 tests total) and GdUnit4 (`test_apex_bastion_mission.gd`), with all 11 missions validated in `test_campaign_levels.gd` and `test_level_selection_2d.gd` (0 errors, 0 failures, 0 orphans).

### Pre-Level 11 Cutscene — The Praetorian Vanguard (Apex Bastion Intro) (2026-11-04)

A skippable 90-second live 2D cinematic prologue (`ApexBastionIntro` & `ApexBastionStage`, scene `scenes/cutscenes/apex_bastion_intro.tscn`) initiating Mission 11 (*The Apex Bastion*) as the high-speed Apex Ascent Lift reaches the stratospheric summit:
- **Narrative & Staging**: Axiom Force's freight lift decelerates onto the open-air summit staging platform at 4,000 meters elevation. Looming above in the cloud sea stand the brutalist obsidian battlements of the Apex Bastion, defended by automated twin-linked plasma turrets, Praetorian cyber-samurai silhouettes, and the colossal silhouette of the Bastion Praetor war mechanoid guarding the entrance to the Citadel Throne Room.
- **Squad Performance & Authored Dialogue**: 9 timed dialogue cues across 90 seconds featuring AXIOM-7 alerting arrival at 4,000m and registering extreme void-resonance readings, Titan cracking knuckles over the open-air battlements, Surya anchoring the squad with bulwark shields against high-altitude crosswinds, Mirage spotting heavy Praetorian heavy mechs locking magnetic clamps, Oceanus analyzing automated flak batteries firing blind through the cloud sea, Aura feeling the ultrasonic psychic resonance of the Void Commander, AXIOM-7 confirming this is the penultimate defense line before the Citadel Throne Room, Mirage highlighting the elevator access hatch, and Kaito drawing his blade to lead the breach into the Apex Bastion.
- **Procedural Graphics Architecture**: High-altitude cloud sea gradient (`#060814` to `#16223e`) with drifting atmospheric vapor layers, sweeping red alert sky searchlights, towering brutalist fortress battlements, illuminated twin-linked plasma turret arrays, Praetorian defender silhouettes, Bastion Praetor war mechanoid silhouette with pulsing crimson visor, open freight lift platform with glowing magnetic safety rails, operative squad silhouettes, CRT scanlines, 2.39:1 anamorphic letterbox bars, and zero external texture dependencies (`ResourcesAvailable => true`).
- **Engine & Progression Wiring**: Registered in `LevelCatalog.cs` for Mission 11 (*The Apex Bastion*), routed via `CyberVictoryScorecard.cs` when continuing to Mission 11, integrated into `scenes/main_menu/level_selection.tscn` and `scripts/main_menu/level_selection.gd`. Fully verified across NUnit (`ApexBastionIntroTests.cs`) and GdUnit4 (`test_apex_bastion_intro.gd`) with 0 errors, 0 failures, and 0 orphan nodes.

### Post-Level 10 Cutscene — The Logic Collapse (Mission 10 Aftermath) (2026-11-04)

A skippable 90-second live 2D cinematic epilogue (`CyberNexusAftermath` & `CyberNexusAftermathStage`, scene `scenes/cutscenes/cyber_nexus_aftermath.tscn`) concluding Mission 10 (*The Cyber-Nexus*) upon extraction:
- **Narrative & Staging**: Axiom Force stands amidst the smoldering wreckage of the defeated Nexus Overseer and cascading failure of the three quantum processing nodes. With server monoliths flashing fatal error codes, liquid helium coolant venting into the crumbling core, and the automated robot armies flatlined, the squad activates the high-speed **Apex Ascent Lift** to ascend directly into **The Apex Bastion** (Mission 11) as Praetorian defenders lock down the upper citadel.
- **Squad Performance & Authored Dialogue**: 9 timed dialogue cues across 90 seconds featuring AXIOM-7 confirming Overseer destruction and defense grid collapse, Mirage observing automated reinforcement queues zeroing out, Titan celebrating the shattered containment prism ring and venting coolant gaskets, Oceanus warning of reactor thermal runaway, Surya spotting Praetorian auxiliary generators powering up above, Aura sensing heavy hydraulic footsteps of elite mechs rallying on the ramparts, Mirage overriding the freight lift security lockouts, AXIOM-7 warning of extreme summit energy signatures from the Void Commander, and Kaito ordering the squad onto the lift to breach the Apex Bastion.
- **Procedural Graphics Architecture**: Abyssal midnight cybernetic chasm (`#02040c` to `#0a0f24`), perspective digital circuit floor grid with emergency flashing alarm washes, towering server monoliths with pulsating red alert runes (`#ff2255`) and amber status indicators (`#ffaa00`), falling corrupted data rain streams, shattered hexagonal Overseer boss chassis with electric spark discharge and rising smoke, fractured cyan orbital containment prism shards strewn across the gantry deck, billowing white/cyan cryogenic helium steam plumes, massive vertical magnetic Apex Ascent Lift gantry with animated opening boarding gates and illuminated interior, ominous fortress ramparts and sweeping red searchlights high in the shaft, operative silhouettes with distinctive glowing energy accents, CRT scanlines, 2.39:1 anamorphic cinema bars, and guaranteed zero external texture dependencies (`ResourcesAvailable => true`).
- **Engine & Scorecard Integration**: Seamlessly attached in `CodeLevel.CompleteMission` when `MissionNumber == 10`, freeze-isolating the extracted level, playing the cinematic epilogue, and deferring to `CyberVictoryScorecard.ShowMissionScorecard` targeting Mission 11 upon natural finish or skip (ESC / Space / Enter / gamepad / click). Verified across NUnit (`CyberNexusAftermathTests.cs`) and GdUnit4 (`test_cyber_nexus_aftermath.gd`) with 0 errors, 0 failures, and 0 orphan nodes.

### Mission 10 Gameplay — The Cyber-Nexus (Mainframe Core) (2026-11-04)

Full Act III interior mainframe 2D platformer gameplay (`CyberNexusMission.cs`, `scenes/gameplay/level10.tscn`) extending `CodeLevel` and implementing `IFiniteCombatHost`:
- **6-Sector Mainframe Core Layout (27,500 Unit Horizontal Scope)**:
  1. *SEC.01 // QUANTUM INGRESS CATWALK*: High-voltage server catwalks, wall-jump vertical data bus shafts, and automated defense patrols (`nexus1`, `nexus2`).
  2. *SEC.02 // LIQUID HELIUM CONDUITS*: Pressurized sub-zero cryogenic coolant lines (`CoolantVent.cs`) requiring Oceanus Douse (`GateKind.FireVent`), heavy magnetic titan-clamp data bus housings requiring Apex Titan Slam (`GateKind.RubbleWall`), and quantum node Alpha override terminal.
  3. *SEC.03 // OPTICAL DATA FIREWALL ARRAYS*: High-voltage optical laser firewalls (`CyberLaserGrid2D.cs`) bypassed via Mirage Blade Phase Shift, thermal radiation exhaust corridors defended under Surya Prime Bulwark (`GateKind.HeatVent`), and quantum node Beta override terminal.
  4. *SEC.04 // HIGH-FREQUENCY RESONANCE VAULT*: Vertical server shafts, acoustic encryption locks requiring Aura Phoenicia Resonance (`GateKind.SonicLock`), and quantum node Gamma override terminal.
  5. *SEC.05 // MAINFRAME SANCTUM // NEXUS OVERSEER*: Multi-phase 2D boss mechanoid encounter (`NexusOverseerBoss2D.cs`, 560 HP) armed with deflecting `NexusPulseBolt2D` swept projectiles (reflectable by Kaito's Kinetic Parry for 40 reflected damage), orbital containment prisms, ground shockwaves, and Phase 3 core purge overdrive.
  6. *SEC.06 // APEX ASCENT GANTRY*: Mainframe Core terminal hack, data purge initiation, and extraction gantry threshold unlocking access to the Apex Bastion (Mission 11).
- **Squad Synergy & Campaign Wiring**:
  - Full 6-operative roster active with operative-specific ability gates and dynamic in-level squad swapping.
  - Sector 10 card integrated in Level Selection (`scenes/main_menu/level_selection.tscn`, `scripts/main_menu/level_selection.gd`).
  - Scorecard routing in `scripts/gameplay/CodeLevel.cs` (`MissionNumber == 10`).
- **Pure C# Ledger & Verification**:
  - `NexusProgress.cs` pure C# engine-free mission ledger tracking 14 ordered milestones, 14 authored combat formations, and bounded retry slot rewards.
  - Comprehensive automated tests: NUnit (`NexusProgressTests.cs`, 213 tests total) and GdUnit4 (`test_cyber_nexus_mission.gd`), with all 10 missions validated in `test_campaign_levels.gd` and `test_level_selection_2d.gd` (0 errors, 0 failures, 0 orphans).

### Pre-Level 10 Cutscene — The Heart of the Machine (The Cyber-Nexus Intro) (2026-11-04)

A skippable 90-second live 2D cinematic prologue (`CyberNexusIntro` & `CyberNexusStage`, scene `scenes/cutscenes/cyber_nexus_intro.tscn`) establishing the ingress into Mission 10 (*The Cyber-Nexus*):
- **Narrative & Tactical Staging**: Axiom Force steps through the unsealed blast doors into the heart of the Citadel Spire—the Cyber-Nexus mainframe. Entering a vast, abyssal cavern of server monoliths, optical data streams, and liquid helium conduits, the squad prepares to disable three quantum processing nodes regulating the Spire's automated defense grid and silence the Nexus Overseer before assaulting the Apex Bastion.
- **Squad Performance & Authored Dialogue**: 9 timed dialogue cues across 90 seconds featuring AXIOM-7 confirming ingress and hardline telemetry link, Mirage marveling at the colossal scale of the humming machine belly, Titan noting server racks generating enough power for city blocks, Oceanus identifying liquid helium coolant conduits vulnerable to sabotage, Aura detecting the active neural matrix rerouting automated security drones, Surya priming his solar bulwark against defense lasers and biometric firewalls, AXIOM-7 identifying three quantum processing nodes whose neutralization will trigger a fatal logic collapse, Mirage declaring this is where they break the Citadel's spine, and Kaito ordering the advance.
- **Procedural Graphics Architecture**: Multi-layered abyssal chasm background (`#030510` to `#0d122b`), perspective grid floor with circuit conduits, towering vertical server monoliths with pulsating status LEDs (`#00f0ff`, `#00ff88`, `#ffaa00`), cascading optical data rain columns (`#00f0ff` / `#ffd700`), central glowing quantum core sphere (`#00e5ff`) with orbiting containment rings and radial energy pulse waves, floating surveillance prism drones with scanning search cones, suspended ingress gantry bridge with warning chevron plating and safety stanchions, holographic AXIOM-7 tactical interface, operative silhouettes with glowing energy accents, CRT scanlines, 2.39:1 anamorphic cinema bars, and zero external texture dependencies (`ResourcesAvailable => true`).
- **Engine & Campaign Integration**: Registered Mission 10 in `LevelCatalog.cs` (`The Cyber-Nexus`, `res://scenes/gameplay/level10.tscn`, all 6 abilities required) and hooked in `CyberVictoryScorecard.cs` to launch upon continuing from Mission 9. Standalone tests verify skip/finish handling, subtitle timeline seeking, and 0 orphan nodes across NUnit (`CyberNexusIntroTests.cs`) and GdUnit4 (`test_cyber_nexus_intro.gd`).

### Post-Level 9 Cutscene — The Core Breach (Mission 9 Aftermath) (2026-11-04)

A skippable 90-second live 2D cinematic epilogue (`SpireOuterHullAftermath` & `SpireOuterHullAftermathStage`, scene `scenes/cutscenes/spire_outer_hull_aftermath.tscn`) concluding Mission 9 (*Spire Outer Hull*) upon airlock extraction:
- **Narrative & Staging**: Axiom Force gathers on the windswept summit catwalk at 3,000m elevation beside the smoldering, sparking chassis of the defeated Spire Sentinel mechanoid. As Mirage overrides the security protocols, the massive circular biometric blast door unseals with hydraulic hiss and billowing steam, sliding apart to reveal the awe-inspiring interior of the Citadel Spire: a towering, cavernous quantum core filled with cascading blue/gold data rivers and floating surveillance prisms—**The Cyber-Nexus** (Mission 10). Looking back over the railing, the squad sees the clear, unshielded skyline of liberated Neo-Kyoto kilometers below before Kaito gives the order to cross the threshold into the heart of the machine.
- **Squad Performance & Authored Dialogue**: 9 timed dialogue cues across 90 seconds featuring AXIOM-7 reporting the Spire Sentinel chassis neutralized, Titan admiring the nuclear-rated blast doors, Oceanus observing Neo-Kyoto's clear skyline glowing below, Surya warning that the Cyber-Nexus mainframe can synthesize replacement armies if not destroyed, Aura sensing the acoustic hive mind vibration through the steel floor, Mirage cracking the terminal security protocols, AXIOM-7 confirming atmospheric purge and portal release, Mirage marveling at the digital labyrinth inside, and Kaito ordering the squad forward into the Cyber-Nexus.
- **Procedural Graphics Architecture**: High-altitude stratospheric twilight sky gradient with stars, rolling cloud bank at 3,000m, distant glowing skyline of liberated Neo-Kyoto, smoldering Spire Sentinel mechanoid husk with spark arcs and rising smoke, summit catwalk with yellow hazard stripes and railings, monolithic Spire titanium wall with power conduits, animated circular Cyber-Nexus blast doors sliding open into the wall to reveal the glowing quantum core interior (cascading blue/gold data waterfalls, quantum fiber lines, floating surveillance prisms), operative silhouettes with distinctive glowing energy accents, floating steam clouds, CRT scanlines, 2.39:1 anamorphic cinema bars, and guaranteed zero external texture dependencies (`ResourcesAvailable => true`).
- **Engine & Scorecard Integration**: Seamlessly attached in `CodeLevel.CompleteMission` when `MissionNumber == 9`, freeze-isolating the extracted level, playing the cinematic epilogue, and deferring to `CyberVictoryScorecard.ShowMissionScorecard` targeting Mission 10 upon natural finish or skip (ESC / Space / Enter / gamepad / click). Verified across NUnit (`SpireOuterHullAftermathTests.cs`) and GdUnit4 (`test_spire_outer_hull_aftermath.gd`) with 0 errors, 0 failures, and 0 orphan nodes.

### Mission 9 — Spire Outer Hull (High-Mobility Vertical Ascent) (2026-11-04)

Full Act III high-altitude exterior 2D platformer gameplay (`SpireOuterHullMission.cs`, `scenes/gameplay/level9.tscn`) extending `CodeLevel` and implementing `IFiniteCombatHost`:
- **6-Sector High-Altitude Exterior Layout (2,500m Elevation)**:
  1. *SEC.01 // GANTRY 42 EXTERIOR INSERTION*: High-wind maintenance gantries, structural pylon wall-jump shafts, and automated defense patrols across the Spire foundation.
  2. *SEC.02 // CRYOGENIC HEAT EXHAUST CONDUITS*: Pressurized cryogenic coolant louvers (`CoolantVent.cs`) requiring Oceanus Douse, and heavy magnetic hull clamp braces requiring Apex Titan Slam.
  3. *SEC.03 // FLUID-COUPLING FLAK BATTERY DECK*: Perimeter flak laser grid (`CyberLaserGrid2D.cs`) bypassed via Mirage Blade Phase Shift, and flak artillery corridors defended under Surya Prime Bulwark.
  4. *SEC.04 // CARGO ELEVATOR & STRUCTURAL PYLONS*: Vertical elevator shafts, drone acoustic relay lock requiring Aura Phoenicia Resonance, and summit overlook garrison.
  5. *SEC.05 // SUMMIT CATWALK // SPIRE SENTINEL*: Multi-phase 2D aerial mechanoid boss encounter (`SpireSentinelBoss2D.cs`, 520 HP) firing swept high-velocity plasma bolts (`SpirePlasmaBolt2D.cs`, 35 damage) deflectable by Kaito's Kinetic Parry back at the boss, overdrive thruster charges, and Phase 3 emergency dive-bombs with seismic shockwaves.
  6. *SEC.06 // CYBER-NEXUS INTAKE AIRLOCK*: Cyber-Nexus breach terminal hack, central intake airlock release, and extraction threshold opening into the Citadel interior.
- **Squad Synergy & Campaign Wiring**:
  - Full 6-operative roster active with operative-specific ability gates and dynamic in-level squad swapping.
  - Sector 9 card integrated in Level Selection (`scenes/main_menu/level_selection.tscn`, `scripts/main_menu/level_selection.gd`).
  - Scorecard routing in `scripts/gameplay/CodeLevel.cs` (`MissionNumber == 9`).
- **Pure C# Ledger & Verification**:
  - `SpireProgress.cs` pure C# engine-free mission ledger tracking 13 milestones and 14 authored combat formations.
  - Comprehensive automated tests: NUnit (`SpireProgressTests.cs`) and GdUnit4 (`test_spire_outer_hull_mission.gd`), with all 9 missions validated in `test_campaign_levels.gd` and `test_level_selection_2d.gd` (0 errors, 0 failures, 0 orphans).

### Pre-Level 9 Cutscene — Exterior Breach (Spire Outer Hull) (2026-11-04)

A skippable 90-second live 2D cinematic prologue (`SpireOuterHullIntro` & `SpireOuterHullStage`, scene `scenes/cutscenes/spire_outer_hull_intro.tscn`) establishing the exterior assault on Mission 9 (*Spire Outer Hull*):
- **Narrative & Tactical Briefing**: Axiom Force arrives at 2,500m elevation outside the monolithic Citadel Spire aboard the commandeered heavy military VTOL gunship. With automated exterior flak cannons tracking the gunship, the squad opens the side cargo ramps amidst stratospheric storm gales and deploys magnetic tether winches to disembark onto Gantry Level 42 for a foot ascent up the Spire's armored outer skin.
- **Squad Performance & Dialogue**: 9 authored narrative cues across 90 seconds featuring AXIOM-7 reporting altitude and 65-knot wind gusts, Surya raising a forward solar barrier to deflect flak air-bursts, Mirage spotting Gantry 42 maintenance rails along the hull, Titan advising magnetic boot solenoids, Oceanus warning of freezing cryogenic exhaust louvers, Aura tracking automated interceptor drone silos deploying swarms, AXIOM-7 deploying cargo ramps before pulling back to high orbit, Mirage noting the one-way vertical climb, and Kaito ordering the breach onto the Spire outer skin.
- **Procedural Graphics Architecture**: High-altitude stratospheric twilight sky gradient with twinkling stars and lower storm ceiling at 2,500m, colossal Citadel Spire vertical monolith with titanium armor seams, glowing cyan power conduits (`#00f0ff`), flashing red hazard indicator beacons (`#ff2222`), freezing cryogenic mist venting from exhaust louvers, arcing orange anti-air flak tracers and air-burst explosions, hovering heavy VTOL gunship with glowing cyan tilt-rotor thrust plumes, open cargo bay with hazard warning stripes and dangling magnetic grapple cables, operative silhouettes with distinctive glowing energy accents, CRT scanlines, and 2.39:1 anamorphic cinema bars (`ResourcesAvailable => true`).
- **Engine & Campaign Integration**: Registered Mission 9 in `LevelCatalog.cs` (`Spire Outer Hull`, `res://scenes/gameplay/level9.tscn`, all 6 abilities required) and hooked in `CyberVictoryScorecard.cs` to launch upon continuing from Mission 8. Multi-input skip (ESC / Space / Enter / gamepad Start / A / click / touch) and safe mission transition logic with fallback to level selection when Mission 9 gameplay is being authored. Verified across NUnit (`SpireOuterHullIntroTests.cs`) and GdUnit4 (`test_spire_outer_hull_intro.gd`) with 0 errors, 0 failures, and 0 orphan nodes.

### Post-Level 8 Cutscene — Airborne Ascension (Mission 8 Aftermath) (2026-11-04)

A skippable 90-second live 2D cinematic epilogue (`StratosDocksAftermath` & `StratosDocksAftermathStage`, scene `scenes/cutscenes/stratos_docks_aftermath.tscn`) concluding Mission 8 (*Stratos Docks - Aviation Hub*) upon extraction:
- **Narrative & Staging**: Axiom Force takes flight in the commandeered heavy military VTOL gunship, tearing free of Bay 4 docking clamps. Below, the floating docks recede into storm squalls alongside the illuminated liberated municipal sectors. Breaking through the dense cloud ceiling into high-altitude twilight, the squad beholds the colossal Citadel Spire Outer Hull—towering kilometers into the stratosphere with automated anti-air flak batteries, setting the stage for their exterior foot ascent in Mission 9.
- **Squad Performance & Authored Dialogue**: 9 timed dialogue cues across 90 seconds featuring AXIOM-7 flight telemetry, Mirage observing grounded enemy interceptors, Titan admiring reinforced heavy gunship armor, Oceanus celebrating three unshielded municipal sectors, Aura monitoring chaotic Citadel distress broadcasts, Surya locking onto the Spire's exterior armor plating, AXIOM-7 warning of summit anti-air grids, Mirage charting lower exterior catwalk drops, and Kaito ordering the squad to harness in and breach the Spire's outer hull.
- **Procedural Graphics Architecture**: High-altitude twilight sky gradient, billowing volumetric cloud banks, receding floating gantries with blinking red hazard beacons (`#ff2222`), heavy military VTOL gunship with dual glowing cyan tilt-rotor thrust plumes (`#00f0ff`) and rear exhaust flare, monolithic Citadel Spire Outer Hull with armor seams and arcing flak tracers, operative silhouettes with distinctive glowing energy accents, tactical holographic flight HUD, CRT scanline overlay, 2.39:1 anamorphic cinema bars, and guaranteed zero external texture dependencies (`ResourcesAvailable => true`).
- **Engine & Scorecard Integration**: Seamlessly attached in `CodeLevel.CompleteMission` when `MissionNumber == 8`, freeze-isolating the extracted level, playing the cinematic epilogue, and deferring to `CyberVictoryScorecard.ShowMissionScorecard` targeting Mission 9 upon natural finish or skip (ESC / Space / Enter / gamepad / click). Verified across NUnit (`StratosDocksAftermathTests.cs`) and GdUnit4 (`test_stratos_docks_aftermath.gd`) with 0 errors, 0 failures, and 0 orphan nodes.

### Act II End Screen & Act III Start Screen (2026-11-04)

- **Act II End Screen** (`ActIIEndScreen.cs`, `scenes/ui/act_ii_end_screen.tscn`): A dedicated tactical campaign debrief screen triggered upon continuing from Mission 7's scorecard. Celebrates the liberation of all three Act II infrastructure sectors (Sector 5 Downtown Civic District, Sector 6 Cyber-Grave Labs, Sector 7 The Foundry Forge), confirms planetary sky-shield integrity at 0.0%, and provides responsive navigation (`PROCEED TO ACT III`, `MISSION SELECT`, `MAIN MENU`).
- **Act III Start Screen** (`ActIIIStartScreen.cs`, `scenes/ui/act_iii_start_screen.tscn`): A stylized cyberpunk act title card ushering in **ACT III: THE CITADEL SPIRE & FINAL LIBERATION (Missions 8–12)**. Displays the operational briefing for infiltrating Stratos Docks to commandeer a heavy VTOL gunship, presents the full 6-operative squad readiness strip, and launches the Pre-Level 8 prologue.

### Pre-Level 8 Cutscene — Infiltration at Stratos Docks (2026-11-04)

A skippable 90-second live 2D cinematic prologue (`StratosDocksIntro` & `StratosDocksStage`, scene `scenes/cutscenes/stratos_docks_intro.tscn`) bridging into Mission 8 (*Stratos Docks - Aviation Hub*):
- **Narrative & Tactical Briefing**: Axiom Force gathers on high-altitude steel gantries 1,200 meters above Neo-Kyoto amid driving rain and high wind shear. With ground and subterranean transit sealed following the sky-shield collapse, the squad coordinates breaching Bay 4 to commandeer a heavy military VTOL gunship for their assault on the Spire summit.
- **Squad Performance & Dialogue**: 9 authored narrative cues across 90 seconds featuring AXIOM-7 thermal telemetry, Mirage locating the idling Bay 4 gunship, Titan planning to slam the magnetic mooring titan-locks, Oceanus cautioning against hydrogen fuel ignitions, Aura tracking air traffic control communications, Surya offering solar shield flak protection, Mirage mapping phase vectors through laser tripwires, AXIOM-7 charting the Spire intake flight path, and Kaito ordering the breach.
- **Procedural Graphics Architecture**: High-altitude stormy sky with animated lightning flashes, towering control towers and mooring pylons with flashing red aviation beacons (`#ff2222`), massive silhouette of the heavy VTOL gunship with glowing tilt-rotor turbine ionization, gantry walkway, operative silhouettes with distinctive glowing energy accents, sweeping searchlights, angled driving rain lines, CRT scanlines, and 2.39:1 anamorphic cinema bars (`ResourcesAvailable => true`).
- **Lifecycle & Navigation**: Multi-input skip (ESC, Space, Enter, Gamepad Start/A, mouse/touch click), monotonic audio-compensated timeline, speaker-coded subtitle panel, and clean transition to `scenes/gameplay/level8.tscn`. Verified across NUnit (`ActTransitionTests.cs`) and GdUnit4 (`test_stratos_docks_intro.gd`) with 0 errors, 0 failures, and 0 orphan nodes.

### Post-Level 7 Epilogue Cutscene — The Sky-Shield Falls (Act II Finale) (2026-11-04)

A skippable 90-second live 2D cinematic epilogue (`FoundryForgeAftermath` & `FoundryForgeAftermathStage`, scene `scenes/cutscenes/foundry_forge_aftermath.tscn`) concluding Act II upon extraction from Mission 7 (*The Foundry Forge*):
- **Narrative & Staging**: Axiom Force's extraction transport crests the rim of the volcanic caldera as the overloaded geothermal core triggers a catastrophic failure cascade up the primary conduit. High above Neo-Kyoto, the Citadel Spire's planetary hexagonal sky-shield shatters and collapses, exposing the Spire to direct assault and marking the end of Act II.
- **Squad Performance & Authored Dialogue**: 9 timed dialogue cues across 90 seconds featuring AXIOM-7 telemetry, Titan observing reactor stabilization and slag flood diversion, Oceanus reporting coolant conduit pressure normalization, Aura detecting seismic harmonic balance, Mirage displaying satellite telemetry of city-wide shield matrix collapse, Surya verifying Spire sky-shield deactivation, and Kaito declaring the dawn of Act III as the path to the Spire opens.
- **Procedural Graphics Architecture**: Multi-layer caldera rim silhouette with glowing magma gradient, distant Neo-Kyoto skyline and towering Citadel Spire, animated hexagonal sky-shield lattice with arcing energy fractures and dissolving particle bursts, extraction transport with thruster plumes, operative silhouettes with distinctive glowing energy accents, CRT scanlines, 2.39:1 anamorphic cinema bars, and guaranteed zero external texture dependencies (`ResourcesAvailable => true`).
- **Engine & Scorecard Integration**: Seamlessly attached in `CodeLevel.CompleteMission` when `MissionNumber == 7`, freezing the extracted level, playing the cinematic epilogue, and deferring to `CyberVictoryScorecard.ShowActIIScorecard` upon natural finish or skip (ESC / Space / Enter / gamepad / click). Verified across NUnit (`FoundryForgeAftermathTests.cs`) and GdUnit4 (`test_foundry_forge_aftermath.gd`) with 0 errors, 0 failures, and 0 orphan nodes.

### Pre-Level 7 Cutscene — Descent to The Foundry Forge (2026-11-03)

A skippable 90-second live 2D cinematic prologue (`FoundryForgeIntro` & `FoundryForgeStage`, scene `scenes/cutscenes/foundry_forge_intro.tscn`) bridging Mission 6 and Mission 7 (*The Foundry Forge*):
- **Narrative & Staging**: Axiom Force descends deep into the sub-surface industrial rift where the Citadel's geothermal plasma reactors churn at 4,000 Kelvin. Primary conduits channel raw planetary power upward to maintain the Citadel Spire's impenetrable sky-shields.
- **Squad Briefing & Dialogue**: 9 authored narrative cues across 90 seconds featuring AXIOM-7 thermal telemetry, Kaito analyzing conduit feeds, Titan identifying magnetic containment coils and slave labor infrastructure, Oceanus mourning drained subterranean aquifers, Aura listening to harmonic core fault lines, Mirage charting high-heat catwalk infiltration vectors, Surya cautioning against irreversible reactor overload cascades, and Kaito ordering the breach to shatter the Citadel's planetary shield.
- **Procedural Graphics Architecture**: Volcanic basalt rift silhouettes, bubbling convective molten slag rivers with thermodynamic wave physics, cooling towers venting billowing steam clouds, high-voltage lightning arcs across magnetic coils, metal catwalk gantry, operative silhouettes with distinctive glowing energy accents, holographic wireframe reactor schematics with flashing breach zones, CRT scanline overlay, and 2.39:1 anamorphic cinema bars.
- **Lifecycle & Handoff Safety**: Multi-input skip (ESC / Space / Enter / gamepad Start / A / click / touch), monotonic audio clock with latency compensation, character-color-coded subtitle panel borders, and safe mission transition logic with fallback to level selection when Mission 7 gameplay is being authored. Verified across NUnit (`FoundryForgeIntroTests.cs`) and GdUnit4 (`test_foundry_forge_intro.gd`) with 0 orphan nodes.

### Post-Level 6 Epilogue Cutscene — Ascent from the Cyber-Grave (2026-11-03)

A skippable 90-second live 2D cinematic epilogue (`CyberGraveAftermath` & `CyberGraveAftermathStage`, scene `scenes/cutscenes/cyber_grave_aftermath.tscn`) triggered seamlessly upon extraction from Mission 6:
- **Narrative & Staging**: Axiom Force and seventeen liberated civilians ascend 400 meters up the industrial shaft on the heavy freight platform. Behind them, emergency containment doors seal the subterranean bio-processing foundry and geothermal reactors safely vent steam.
- **Squad Performance & Dialogue**: 9 authored narrative beats featuring AXIOM-7, Oceanus administering hydro-stabilization, Aura sensing network agony dissipating, Titan confirming reactor containment, Mirage projecting decrypted central vault ledgers connecting the foundry to the Citadel Spire, Surya reporting surface medical transports and orbital lockdown warnings, and Kaito rallying the squad to storm the Citadel Spire.
- **Procedural Graphics Architecture**: Downward-rushing shaft rail parallax, diamond-grated freight lift with flashing amber beacons, thermal blanket wraps with foil shimmer, character silhouettes with active ability accents, holographic wireframe Citadel Spire blueprint and orbital radar sweep, 16:9 cinema bars, and CRT scanlines with zero missing-resource crashes (`ResourcesAvailable => true`).
- **Engine & Campaign Integration**: Attached in `CodeLevel.CompleteMission` on Mission 6 extraction; freeze-isolated with safe audio termination; seamlessly transitions to `CyberVictoryScorecard`. Verified across NUnit (`CyberGraveAftermathTests.cs`) and GdUnit4 (`test_cyber_grave_aftermath.gd`) with 0 orphan nodes.

### Mission 5 epilogue — Names Without Numbers

After extraction, a 90-second skippable live 2D cinematic returns to the restored civic processing district.
Civilian Amara reclaims and speaks her name as the registry terminal turns green; fellow residents celebrate
as their records are restored. Mirage, Aura, Oceanus, and Titan react from their district perches while
Surya reports over comms that the outer barriers collapsed and the queue is crossing safely. Kaito rallies the
squad for the impending push into the Citadel heart.
- **Cinematic Presentation**: Smooth Hermite camera transitions across 8 shots, procedural volumetric sweeping floodlights, holographic data stream rising from the terminal, golden identity particles, operative ground speaker beacons, and 2.39:1 widescreen letterboxing with anamorphic lens flares.
- **Synthesized Voiceover**: 8 fully voiced lines generated via ElevenLabs multilingual TTS with calibrated voice profiles, normalized into an exact 90-second Ogg Vorbis stream and matching ambience.
- **Dynamic Subtitles**: Cyber-styled container with `#00f0ff` accent line and speaker-color-coded typography matching each operative's identity.
- **Lifecycle & Safety**: Freezes the extracted mission and preserves score snapshots; natural finish, Skip (ESC/Space/Enter/gamepad/click), or independent overlay removal cleanly yields to `CyberVictoryScorecard`.
the existing **MISSION COMPLETE** card once. Missing speech keeps the timed English
subtitles. Replay restarts Mission 5 with a fresh run; the epilogue adds no unlock or
progression write and does not introduce Mission 6. Art/audio production and review
steps are in [cutscene production notes](tools/cutscene/README.md#mission-5-epilogue-names-without-numbers).


Verified 2026-11-02 after final engine edits: background import exit 0 with all
30 new media sidecars; final incremental C# build zero warnings/errors; NUnit 100/100;
GdUnit4 210/210 (`reports/report_406`, zero errors, failures and orphans); isolated
copied-source Django 104/104. Historically, the test runner was then updated to use only the
root Linux `.venv`; its full run passes NUnit 100/100, GdUnit4 214/214 and Django
104/104 (`reports/report_408`; zero GdUnit errors/failures/orphans). Django runs from
a disposable copy of the root venv with the then-separate backend requirements installed, using an
unmounted helper container on the existing PostgreSQL network. The backend `/app` and
media D: mounts were unreadable in that historical run; normal services stayed running.
The current Docker-backed runner above supersedes that venv workaround.
The temporary production/test helpers were stopped; the core services remain
running. No exports, staging, commits or pushes; editor remains stopped on Level 1.

Runtime evidence under `.validation/names/`: assisted mandatory-route completion,
21 sector/restored/results captures across 1920×1080, 1920×1200 and 2560×1080,
actual keyboard/controller-event/mouse/touch terminal dispatch, repeated opening Skip,
natural completion and missing-art handoff, Mission 5 Replay and direct Level Select.
A temporary campaign stayed at Mission 4 until completion, then saved/resumed directly
into 5; its cleanup preserved the original campaign row hash. Seven transparent art
assets and nineteen complete voice cues pass alpha/margin, cast/hash, decoded duration
and clipping checks. The gallery spawn regression passes for all six archive groups.
Python tool compilation and diff hygiene pass. These checks do not establish an
unassisted uninterrupted 25–30-minute run, physical hardware or subjective listening.

Known diagnostics: a full rebuild retains 153 pre-existing nullable/addon warnings;
one concurrent build had a transient PDB-sharing retry, resolved by the final clean
incremental build. Headless editor imports still log existing addon TLS and shutdown
EditorSettings/ObjectDB/resource/allocator messages. The reference checker retains
10 pre-existing addon/menu connection errors and 27 warnings, none in Mission 5.

## Act I opening label

Level 1 shows a centered cyan **ACT I** above its mission title as gameplay begins.
It uses `CAMPAIGN_ACT_I_TITLE` with English fallback, shares the existing 3.2-second
hold and 1.2-second fade, and adds no pause or input. Fresh launches (briefing
completion/Skip, level selection and Replay) show it; checkpoint recovery does
not replay it. Levels 2–4 retain their existing title cards.

Verified 2026-11-02: all launch paths and recovery checked through runtime
callbacks; layouts inspected at 1920×1080, 1920×1200 and 2560×1080. Build/import
complete; NUnit 88/88, Godot 197/197 (zero failures/errors/orphans) and isolated
container Django 104/104 pass. Existing compile/import diagnostics remain; Django
used a temporary source copy because the running container's host bind mount is
unreadable. The wrapper skips Django when the host virtual environment is absent.

## PostgreSQL-only persistence and browser checks

If Docker has no existing project containers or cached images, restoring the
stack with `docker compose up -d` also pulls images and builds the application
services. Wait for completion and endpoint readiness; initial image downloads
can delay the first startup.

Campaign saves, operative progression, display preferences, and keybindings persist
only in PostgreSQL. There is no local JSON, ConfigFile, browser storage, or SQLite
test fallback. New Game requires a successful database save. Start the stack with
`docker compose up -d`; the game reads PostgreSQL credentials from environment
(`PGHOST`, `PGPORT`, `PGDATABASE`, `PGUSER`, `PGPASSWORD`) or the development
root `.env`. Compose shares the database's `POSTGRES_*` credentials with Django.
Existing local saves are left untouched and are no longer loaded; they have not
been migrated automatically. State remains scoped to this development database,
as with the existing shared campaign table.

Game and portal JWTs live only in memory; restarting the game or reloading the
portal requires sign-in again. Portal language is carried in the URL. Upstream
Clerk identity remains an external authentication dependency; its own session
cookies are separate from application saves. Assets, build caches, logs, Docker
volumes remain normal runtime/tooling files.

The opt-in `playwright` Compose service uses Microsoft's official
[`mcr.microsoft.com/playwright:v1.64.1-noble`](https://playwright.dev/docs/docker)
with the matching npm runner. Run `docker compose build playwright`, then
`docker compose run --rm playwright` for the portal/API smoke check with browser
storage access disabled. It exits after testing and is not an always-on server.
Django tests now use an isolated PostgreSQL test database:
`docker compose exec -T backend python manage.py test accounts --noinput`.

Verified 2026-10-31: 47 NUnit, 135 Godot (zero errors/failures/orphans),
104 PostgreSQL-backed Django tests, and the container Chromium smoke check pass.
The headless editor import exits successfully with existing addon TLS/shutdown
diagnostics; the C# build has zero errors and existing project warnings.

## Story

Earth has fallen under the tyrannical rule of an autonomous cybernetic syndicate. A clandestine strike team known as **Axiom Force** deploys into the neon-drenched sectors of the metropolis. Begin as Cyber Shogun, gain new operatives as the story unfolds, infiltrate deep behind enemy lines, neutralize syndicate drone armadas, and eliminate the frontline warmachine—**Vanguard-X**—to reclaim humanity's future.

Creative direction is agent-led under the user's current requests. The former
story bible and full development plan were removed at the user's request;
historical plans and production notes describe prior work rather than prescribe
future story, dialogue or design. Existing gameplay and cinematics remain playable.

---

Scene background photos and project-owned 3D assets, scenes, scripts, and web viewers have been removed. Operative portraits, sprites, tiles, UI artwork, and 2D glow remain.

## Features

- **Levels 1–3 objective guidance** — Lantern Court keeps the current parry control
  visible until a successful deflection; archive signs, beacon shield charges and
  sealed Data-Blade access are explicit. Civilian markers show local defenders,
  route obstacles, moving evacuees and actual shelter arrival. Neon Rust retains
  all 30 formations/rewards but requires nine objective defenses; 21 patrols are
  optional score opportunities. Checkpoints follow objective milestones. Levels
  2–3 retire unfinished groups beyond 1,500 horizontal pixels without completion
  or repeat rewards. Recovery preserves progress; Replay starts fresh. See the
  [mission review and human checklist](docs/ACT_I_REVIEW.md).

  Verified 2026-11-02: C# build, 88 NUnit, 197 Godot (zero errors/failures/orphans)
  and 104 isolated-container Django tests pass. MCP reviewed three viewport sizes
  and all results destinations; assisted traversal confirms optional-patrol bypass.
  Human pacing and physical controller/touch acceptance remain pending. Existing
  import shutdown/TLS diagnostics and Docker bind-mount fault are recorded in
  [HANDOFF.md](HANDOFF.md).
- **Shattered Aegis / Act I finale** — A 28,000px seven-section fortress assault,
  fourteen finite formations (four live ordinary enemies maximum), earned Surya
  recruitment, real Bulwark/Blink lessons, permanent coolant/gun/curtain shutdown,
  and two locally reconnecting gun-deck approaches. Ordinary combat is bypassable
  outside designated objective defenders. Seven checkpoints retain local progress.
  Voss keeps his armor/missiles in a three-phase encounter with 300 total HP, destructible
  phase-two pylons, reflected missiles and five-second melee openings. Defeat
  unlocks a separate crossing release; civilians cross a safe nonblocking road
  before extraction freezes ACT I COMPLETE results (Replay, Level Select, Main Menu).
  The Act I mission behavior, saves and earlier recorded cinematics are preserved.
  Fourteen cached event voices and six original Docker/Blender fortress assets
  have reproducible provenance. The 20–25 minute first-run target is **unvalidated**.
  See [production/acceptance](tools/gameplay/SHATTERED_AEGIS.md),
  and [Levels 1–3 review](docs/ACT_I_REVIEW.md).
  Verified 2026-11-01: build, 97 NUnit, 190 Godot (zero failures/errors/orphans),
  104 copied-container Django tests, all three display ratios, launch/results
  routes and automated traversal using actual combat. Physical input/listening
  and human pacing remain outstanding; existing import/bind-mount issues persist.
  For the next session, use the latest [handoff](HANDOFF.md) and acceptance guide.

- **Evacuation Under Siege (Level 2 gameplay)** — Six linear sectors now span a
  35,800 px route with 20 finite encounter groups, stationary burst gunners with
  parryable cyan bullets, four recovery shrines, three mandatory civilian shelter
  routes, three defended biometric spires, and a two-relay siege battery. Oceanus
  joins before the first fire lesson; Titan joins beyond the membrane before his
  safe rubble lesson. Orange artillery warns for 2.4 seconds and locks its target.
  Civilians are invulnerable and use short local paths; their route fires become
  permanently safe before they move. Completed objectives persist across deaths in
  the current scene; replay/reload resets this local progress. Extraction requires
  all rescues, all spires, the battery core and final barrier. Arrival dialogue and
  the existing transition to Level 3 are preserved. New prompts support localization
  keys with English fallback and keyboard/controller/touch device wording.
  The **20–25 minute first-playthrough goal remains unvalidated**; automated
  geometry and objective checks are not a human pacing measurement. See
  [gameplay tooling](tools/gameplay/README.md) for the route and reproducible review.
  Final verification: 134 Godot tests (zero failures/errors/orphans), 45 NUnit
  tests, 104 container Django tests, and the live objective chain through Level 3.

- **The Concrete Wildfire arrival** — Every fresh Level 2 launch plays a skippable
  60-second live 2D introduction with Daniel as AXIOM-7 and Adam as Kaito. Escape,
  Space, Enter, controller Start/A, or the pointer/touch Skip button restores gameplay.
  Mission physics, pause input, HUDs and audio stay frozen until handoff. Missing
  speech retains timed subtitles; missing essential artwork safely starts gameplay.
  Checkpoint recovery does not replay it. Oceanus/Apex Titan introductions and all
  objectives remain in gameplay. Open-world exploration remains future work.
  Verified across campaign Continue, Level Select, saved-session and direct launches,
  full playback, six shots and three aspect ratios. Final suites: 120 Godot tests
  (zero failures/errors/orphans), 34 NUnit tests and 104 container Django tests.

- **Tokyo Remembers** — Every Level 1 extraction plays a skippable 40-second live 2D
  epilogue, with Daniel as AXIOM-7 and Adam as Kaito, then the combat-grade scorecard.
  Escape/Space/Enter, controller Start/A, and the pointer/touch Skip button advance
  to results. Continue prepares mission 2; Replay restarts mission 1. Extraction saves
  progress once and freezes mission processing/input while the overlays run.
  Missing speech uses timed subtitles; missing essential art goes directly to results.
  See `tools/cutscene/README.md` for reproducible Docker assets and Godot MCP review.
  Verified with 114 Godot tests (zero errors/failures/orphans), 34 NUnit tests,
  104 container Django tests, and six-shot MCP captures at three aspect ratios.
- **6 Unique Cyber Operatives** — Each equipped with distinct tactical profiles, attributes, and passive synergies:
  1. **Cyber Shogun (Kaito Kurogane)** — Balanced Vanguard Striker (Katana combos, blade dash, parry, rapid shield recharge).
  2. **Surya Prime (Arjun Varma)** — Solar Bulwark Tank (140 HP, 80 Shield, 15% armor damage mitigation, solar flare).
  3. **Oceanus (Kiana Vaea)** — Hydro Vanguard Tactician (90 HP, 70 Shield, 25 HP/s passive hydro shield regen, hydro torrents).
  4. **Mirage Blade (Zayd Al-Mansoor)** — Phantom Infiltrator Assassin (460 Speed, 1000 Dash velocity, 0.6s dash cooldown, phase daggers).
  5. **Apex Titan (King Tariq Nzinga)** — Electro-Magnetic Juggernaut (160 HP, 60 Shield, 20% kinetic armor mitigation, knockback resistance).
  6. **Aura Phoenicia (Maya Khoury)** — Ultrasonic Prism Weaver (95 HP, 55 Shield, 8% mitigation, 0.85s dash cooldown, harmonic resonance).
- **Earned operatives** — A new campaign starts with **Cyber Shogun** alone (no selection screen). Each other operative **joins during a mission**, where a recruit beat introduces them and teaches their signature ability on the spot (there is no separate training level). Swap between unlocked operatives any time with `Tab` / `Shift+Tab`, the number keys `1-6`, or the gamepad (D-pad Down / Back); the squad shares one health/shield ratio and swapping has a short cooldown. The **Squad Roster** (main menu) shows locked operatives and lets you upgrade them and choose who starts the next mission.
- **Signature abilities (`Q`)** — Shogun: **Kinetic Parry** (throws ranged attacks back); Oceanus: **Tidal Torrent** (douses fire); Apex Titan: **Tectonic Slam** (shatters heavy rubble); Aura Phoenicia: **Harmonic Pulse** (opens sonic locks); Mirage Blade: **Phase Blink** (slips through lasers and hazards); Surya Prime: **Solar Bulwark** (absorbs heat and ranged fire). Levels contain obstacles only the matching operative can clear.
- **The campaign (4 side-scrolling missions, code-built on `CodeLevel`)** —
  1. **Ghost in the Gridlock** (Shogun): a ~9,500 px infiltration of the Eternal Zen Dojo over a painted night garden — jumping, a timed laser bridge, a wall-jump gorge, rooftops, an archive; parry sentry fire to overload the beacon's shield, destroy it, take the Data-Blade, extract.
  2. **The Concrete Wildfire** (Oceanus, then Apex Titan join): evacuation road, burning residential shelter routes, four overpass ferries, three defended spires, Titan's breach, a relay-shielded siege battery and final evacuation.
  3. **Neon Rust and Data Dust** (Aura Phoenicia, then Mirage Blade join): eight sectors of freight sabotage, three shipments, two uplinks, a relay blackout and a blind-grid escape across 48,000px.
  4. **Shattered Aegis** (Surya Prime joins): the Border Fortress, superheated corridors, a laser curtain, and Border Commander Voss; defeating him unlocks the crossing control, then opening the border and reaching extraction ends Act I with its scorecard.
  Each mission has three optional memory fragments, a story title card, and chasm recovery. Level 2 has four shrine checkpoints; the other missions have three. Its revised gameplay briefing replaces outdated title-card objective metadata.
- **New-campaign flow** — New Game → AXIOM-7's story briefing → Mission 1. Operatives use their SpriteCook pixel-art sprites (redone on the Adventurer plan, 2026-11-13). Levels use a procedural skyline backdrop; Level 1's painted garden was deleted on 2026-11-12 and awaits regeneration.
- **Dynamic Combat HUD** — Real-time telemetry monitoring health, shields, combo streaks (`DOUBLE STRIKE!`, `TRIPLE COMBO!`, `TACTICAL OVERDRIVE!`), operative ability status, and boss telemetry.
- **Border Commander Voss (boss mech)** — Level 4 uses three phases at 300/200/100 HP, phase-two power pylons, aimed/spread missiles, marked defense lanes and stationary melee recovery. Parry returns the actual missile to its owner. Standalone mech scenes retain the legacy overdrive behavior.
- **Tactical Victory Scorecard** — End-of-level combat assessment evaluating completion time, score, and damage taken to award combat grades (**S Rank / Flawless**, **A Rank / Vanguard**, **B Rank / Operative**) with victory fanfare.
- **Dynamic 2D Environmental Hazards** — Pulsing lethal laser grids (`CyberLaserGrid2D`) and rhythmic electrified floor panels (`CyberHazardFloor2D`).
- **4-Level Campaign Chain** — Linear mission progression through neon rooftops and industrial sectors (`level1.tscn` $\rightarrow$ `level4.tscn`).
- **Synthesized 16-Bit SFX Engine** — Dedicated procedural sound design for blade slashes, dashes, clashes, laser hums, drone explosions, and boss alarms.
- **Squad Roster** — Reached from the main menu. A two-panel layout pairs the roster (locked operatives marked) with one unified dossier containing the animated sprite preview (empty for operatives without SpriteCook art yet), identity, stats, movement controls, a single set of ability buttons, and Upgrade / Set Active actions. Keyboard and controller navigation follow the visible controls for each operative.
- **Cross-Platform Backend & Web Portal** — Django REST Framework backend with PostgreSQL session leaderboards and a modern React + Vite frontend.

---

## Controls

| Action | Keyboard | Gamepad |
|---|---|---|
| **Move Left / Right** | `A` / `D` or `←` / `→` | D-Pad Left / Right or Left Stick |
| **Jump / Wall Jump** | `Space` / `W` / `↑` | A / Cross Button |
| **Wall Slide** | Hold movement against wall | Hold stick against wall |
| **Katana Slash** | `F` / Left Mouse Button | X / Square Button |
| **Signature Ability / Martial Strike** | `Q` | Y / Triangle Button |
| **Swap Operative** | `Tab` / `Shift+Tab` (or `1`–`6`) | D-Pad Down (next) / Back (previous) |
| **Blade Dash** | `Shift` / `C` | Right Shoulder / Trigger |
| **Pause Game** | `Escape` | Start / Options |

> [!NOTE]
> All combat actions (Katana Slash, Signature Ability, and Blade Dash) can be executed both grounded and airborne. Aerial attacks preserve jump momentum to strike hovering enemies like patrol sentries, while aerial dashes provide responsive horizontal air mobility.

The on-screen hint switches between keyboard and gamepad, and lists the swap keys once a second operative has joined.

---

## Tech Stack & Architecture

| Component | Technology | Description |
|---|---|---|
| **Game Engine** | Godot 4.7.2 .NET | C# scripting with .NET 10 runtime (`net10.0`). |
| **Core Architecture** | Component & Manager Pattern | `OperativeRegistry`, `SessionManager`, `UISoundManager`, `CyberCombatHud`. |
| **Backend API** | Django 5.x / DRF | Python 3.15.2 REST API managing authentication and high scores. |
| **Web Frontend** | React + Vite + Tailwind | Web dashboard and player hub. |
| **Database** | PostgreSQL 18 | Sole persistent application store; no local save fallback. |

---

## Getting Started

### Prerequisites

- [Godot 4.7.2 .NET (Mono)](https://godotengine.org/)
- [.NET SDK 11.0.100](https://dotnet.microsoft.com/) (latest; builds the `net10.0` projects) — **required on Windows, not from Docker**
- Python 3.15.2 (for Django backend tests and sound synthesis tools)

> **"Failed to load .NET runtime, specifically hostfxr" on launching Godot** means Windows has no usable .NET SDK. The Godot editor is a native Windows binary that loads Windows `hostfxr.dll`, so the Linux .NET inside the export image cannot satisfy it. Confirm with `dotnet --list-sdks` and by checking that `C:\Program Files\dotnet\host\fxr\` exists. If the installer leaves `C:\Program Files\dotnet` without `dotnet.exe` and `host\`, install per-user and point Godot at it:
>
> ```powershell
> curl.exe -sSL -o $env:TEMP\dotnet-install.ps1 https://dot.net/v1/dotnet-install.ps1
> powershell -ExecutionPolicy Bypass -File $env:TEMP\dotnet-install.ps1 -Channel 10.0 -InstallDir "$env:LOCALAPPDATA\Microsoft\dotnet" -NoPath
> [Environment]::SetEnvironmentVariable('DOTNET_ROOT', "$env:LOCALAPPDATA\Microsoft\dotnet", 'User')
> ```
>
> Then sign out and back in (or restart Explorer) so newly launched apps inherit `DOTNET_ROOT`, and relaunch Godot.

> **Godot AI dock says "No server command found" or shows `uv: not found`** means the plugin found no usable launcher. It resolves a server in three tiers — project `.venv` → `uvx` → a system `godot-ai` — and on Windows a `.venv` only counts if it is `.venv\Scripts\python.exe` next to a `src/godot_ai/` checkout. The former repo `./.venv` was a Linux environment and has now been deleted; it was never a valid Windows editor interpreter. Install `uv` for Windows into one of the directories the plugin searches (`%LOCALAPPDATA%\Programs\uv`, `%USERPROFILE%\.local\bin`, `%USERPROFILE%\.cargo\bin`, `%ProgramFiles%\uv`), then click **Reload Plugin** in the dock (its "not found" result is cached for the session). `uvx` then fetches the pinned server itself — for plugin v4.3.1 that is `godot-ai==4.3.1`:
>
> ```powershell
> curl.exe -sSL -o $env:TEMP\uv.zip https://github.com/astral-sh/uv/releases/latest/download/uv-x86_64-pc-windows-msvc.zip
> Expand-Archive $env:TEMP\uv.zip -DestinationPath "$env:LOCALAPPDATA\Programs\uv" -Force
> ```

### Running the Stack with Docker

The Django backend, PostgreSQL database and React web portal run together with Docker Compose:

These three services are the complete default stack; the local inference service
has been removed. The exporter and browser test runner remain opt-in.
Verified 2026-11-05: Compose validation and website/API readiness passed with the
three services running; all three project test suites passed.

```powershell
docker compose up -d --build     # build/start db, backend, website
docker compose ps                # verify all three run; inspect configured health checks
docker compose logs -f backend   # follow the Django log
docker compose down              # stop everything (add -v to drop the DB volume)
```

Allow Django's startup checks to finish before testing API readiness; a running
container can briefly precede the HTTP server accepting connections.

- **Django API**: <http://localhost:8000/api/> (admin site at `/admin/`)
- **Web portal**: <http://localhost:5173/> — Nginx serves the built bundle and proxies `/api/` and `/admin/` to Django
- **PostgreSQL 18**: published on `5432`, data kept in the `pgdata` volume
- Container startup and application readiness are separate checks: verify the
  portal on `http://127.0.0.1:5173/`, the backend on
  `http://127.0.0.1:8000/api/auth/countries/basic/`.
  Initial image downloads/builds can delay readiness.
- Configuration comes from the gitignored `.env` (see `.env.example`); every value has a default, so the stack also boots without one
- `backend/Dockerfile` builds on Python 3.15.2; `website/Dockerfile` builds with Node 26 and serves the bundle through Nginx

### Exporting the Game (Docker)

Game export preparation is requested locally through Docker with an explicit target.
Apple signing/distribution and iOS native project generation are the documented
manual Mac/Xcode exception; see [Apple release steps](export/APPLE_RELEASE.md).
GitHub Actions must not export the game, including from manual or container jobs.
The repository and GitHub workflow list were checked on 2026-11-01: neither has
any Actions workflows to remove. Existing historical runs/artifacts were not deleted.

Game exports run headless from a dedicated image (`export/Dockerfile`: Godot 4.7.2 .NET, .NET 10 SDK, Android SDK with build-tools 34, **JDK 17 only**, and the matching export templates). The image owns a container-native `.godot` import cache — importing through the Windows bind mount is ~1 KB/s, so imports stay on the Linux filesystem and only the finished binaries are written back to `./build`.

```powershell
docker compose --profile export build export   # first run, or after editing the Dockerfile
docker compose run --rm export linux            # also: windows | web | android | macos | ios | all
```

Outputs land in `./build/`. Exports only ever run when a target is given explicitly: the service sits behind the `export` profile, so `docker compose up` never starts it, and even starting the service directly is inert — it prints usage and exits without building anything.

The root launchers `export.bat linux` (Windows) and `./export.sh linux` (Bash)
build and invoke that same `export` service from the repository directory.
Supported targets are `linux`, `windows`, `web`, `android`, `macos`, `ios`, and `all`. A bare
invocation or `--help` only prints usage; invalid targets fail before any build.
Both launchers refuse `GITHUB_ACTIONS=true`, and Compose forwards that marker
to the container driver, which also refuses Actions and host execution.
The driver is baked into the image: rebuild it before your next authorized
export after changing `export/export.sh`. No exporter image was built or game
export performed during this configuration-only change.

Configuration verification (2026-11-01): launcher safety paths, Bash syntax,
Compose isolation, 93 NUnit tests, 161 Godot tests (zero errors/failures/orphans),
and 104 container Django tests pass. No packaged game build was produced.

Apple setup (2026-11-01): `export.bat macos` prepares an unsigned universal ZIP;
`export.bat ios` prepares a credential-filtered source ZIP for a Mac with Xcode.
The latter is not an Xcode project or IPA. Set a real macOS bundle ID before
requesting `macos` or `all`; the checked-in ID is an intentional example.
The iOS preset has a blank Team ID and project-only export for later completion.
The native Mac helper `bash export/finish_ios.sh --check` checks prerequisites;
its explicit `project` command generates the Xcode project on the Mac.
Apple signing, provisioning, icons/privacy review, physical device validation,
and distribution remain manual. Setup alone does not certify an Apple release.

Setup verification: four synthetic helper tests plus 93 NUnit, 161 Godot
(zero failures/errors/orphans) and 104 container Django tests pass. Syntax,
configuration and safety checks pass; background import exits 0 with existing
addon/shutdown diagnostics. No real Apple export or exporter image build ran.
Mac/device/signing validation is still required.

Game and website edits may run alongside the stack, but an export is exclusive — never run one while another export, a Godot import, or a website build is still running.

### Building the C# Project

To restore packages and build the Godot C# project:

```powershell
dotnet restore AxiomForce.csproj
dotnet build AxiomForce.csproj
```

### Running Unit Tests

A unified test runner is provided to execute all automated test suites (NUnit for .NET 10 C#, GdUnit4 for Godot GDScript/engine tests, and Django backend tests):

```powershell
# Run the complete test suite
.\run_tests.ps1

# Or run individual suites
.\run_tests.ps1 -Suite nunit
.\run_tests.ps1 -Suite gdunit
.\run_tests.ps1 -Suite django
```

The Django suite runs `python manage.py test accounts` inside the existing Compose
backend. Its installed dependencies come from root `requirements.txt` when the
backend image is built. The runner does not require host Python, package a venv,
install packages or skip Django merely because `.venv` is absent. Start the normal
stack with `docker compose up -d` before running tests.

> **Note for Windows Headless Testing**: GdUnit4 executes using `Godot_v4.7.2-stable_mono_win64_console.exe` to avoid headless console deadlocks.

---

## Project Structure

```text
AxiomForce/
├── project.godot                     # Godot project configuration
├── AxiomForce.csproj                 # .NET 10 C# project specification
├── HANDOFF.md                        # Strategic multi-agent handoff briefing
├── run_tests.ps1                     # Unified multi-suite test runner
├── assets/
│   ├── images/operatives/            # 2D character portrait cards (6 operatives)
│   ├── music/                        # Synthesized SFX and soundtrack WAVs
│   └── fonts/                        # Cyber typography resources
├── scenes/
│   ├── main_menu/                    # Squad roster, menus, and story
│   ├── gameplay/                     # Level 1-4 scenes, boss encounter, hazards
│   └── ui/                           # Combat HUD, scorecard, and dialog bubbles
├── scripts/
│   ├── actors/                       # CyberShogun, OperativeProfile, CyberBossMech2D, CyberDrone2D
│   ├── gameplay/                     # CodeLevel + the four mission classes, PalacePlatform, AbilityGate, RecruitBeat, hazards
│   ├── ui/                           # CyberCombatHud, CyberVictoryScorecard
│   ├── managers/                     # SessionManager, UISoundManager, OperativeRoster, LevelCatalog, CampaignProgress
│   └── tools/                        # generate_cyber_sfx.py sound synthesizer
├── backend/                          # Django REST Framework backend service
├── frontend/                         # React + Vite web application
└── tests/                            # GdUnit4 and NUnit test suites
```

---

## Contributing & Agent Guidelines

The current development milestone is the Evacuation Under Siege gameplay expansion,
following the Level 2 arrival cinematic (`4260282`). See HANDOFF and MEMORY for
verification and outstanding pacing/hardware checks.

AI agents collaborating on this repository must consult the following guides:

[`MEMORY.MD`](./MEMORY.MD) preserves durable user preferences, project decisions,
integration context, and the current cinematic design. Use it for orientation;
current agent instructions and live environment checks remain authoritative.
Every agent must update `MEMORY.MD` before finishing every task, including
planning, read-only investigations, reviews, documentation-only work, and Git
operations. Keep outcomes and durable context concise, record only actual
verification, and exclude secrets. This supplements the README/HANDOFF rules;
memory-only edits do not require container preflight, imports, or tests.

For all game-related tasks, agents must read the `router` and apply the relevant
skills from [awesome-gamedev-agent-skills](https://github.com/gamedev-skills/awesome-gamedev-agent-skills)
before acting. The router plus 77 specialized skills are installed globally in
`~/.agents/skills/` for Codex, `~/.claude/skills/` for Claude Code,
`~/.gemini/config/skills/` for Antigravity app/IDE, and
`~/.gemini/antigravity-cli/skills/` for Antigravity CLI. Use Godot/C# and the
relevant task skills for this project. If discovery is unavailable, read the
installed files directly; resolve missing access before continuing.

For every Docker-related task, agents must first read and apply the relevant
skills from Docker's official `docker/skills` package (`docker-skills@docker`).
This includes Docker commands and preflight checks, troubleshooting, builds,
Compose, exports, reviews, advice, and documentation. If skill discovery does
not expose the installed package, read its skill files directly; resolve missing
skill access before continuing Docker work. Use `docker` directly from PATH.
- [`HANDOFF.md`](./HANDOFF.md) — Comprehensive technical briefing on recent milestones, current 2D architecture and roadmap.
- [`CODEX.md`](./CODEX.md) / [`CLAUDE.md`](./CLAUDE.md) / [`GEMINI.md`](./GEMINI.md) / [`AGENTS.md`](./AGENTS.md) — Universal agent operational rules, mandatory testing protocol, and permissions.

Before starting any task that edits code files, agents must confirm the Docker stack is running (`docker ps`; if the project containers are absent, start them with `docker compose up -d`). Read-only, review, documentation-only, and Git-only tasks are exempt.

## Agent Task Completion

After any task that edits the game or its website (including backend, assets, configuration, tests, tooling, or related documentation), agents must update both this root `README.md` and `HANDOFF.md` before finishing. Keep README behavior and contributor instructions current; record implementation decisions, verification status, limitations, and follow-up work in the handoff. Complete documentation edits alongside the task. Unit tests and Godot refreshes are required only when code, scene, or asset files are created, modified, or deleted, and are skipped for documentation-only tasks. Read-only and Git-only tasks are exempt. Commits and pushes still require explicit permission for the current changes. This requirement is shared by `CLAUDE.md`, `CODEX.md`, and `GEMINI.md`.
# Deployment cinematic

New Game opens a live, 40-second Godot 2D sequence before the existing AXIOM-7
briefing. The occupied-city flyover, rooftop reveal, run/jump/landing/blade dash,
and secure transmission animate independently: layered scenery, bounded rain,
drone searchlights, moving transit, cloth/breathing poses, sparks and a scan shader.
Escape, controller Start, or the touch-friendly Skip button advances once to the
briefing. Continue still loads the saved mission.

The unchanged `assets/cutscenes/deployment/narration.ogg` is the master clock,
compensated for audio latency. Missing, ended or stalled narration uses elapsed
time; dialogue and subtitle windows remain unchanged. A responsive UI sits outside
the action, and the cinematic preserves its 16:9 composition on wider/taller screens.

The layered art in `assets/cutscenes/deployment/live` was deleted with Blender on
2026-11-11 and is listed for SpriteCook regeneration in `docs/ART_REGENERATION.md`.
`scripts/main_menu/deployment_stage.gd` loads each layer by path, and until all of
them (and `assets/characters/rendered/axiom7_portrait.png`) exist the cutscene skips straight to the briefing. The legacy `deployment.ogv`
video and its build script were removed too.
Narration regeneration remains optional via `tools/cutscene/narrate.py` and the
existing private API key/cue cache; live-art regeneration never requests paid speech.


Runtime review captures cover 16:9, 16:10 and ultrawide; the generic keyboard-help
banner is suppressed during the cinematic and restored for the following scene.
The final handoff records test results and existing project-wide diagnostics.

Skip accepts direct touch and pointer presses in its button region, including when
the global keyboard/controller-only input policy consumes other mouse events.

Verification: live captures cover all four shots at 1080p and both alternate aspect
ratios; narration-driven natural completion and Escape/Start/pointer/touch skipping
reach the briefing. GdUnit4 109/109 (zero errors/failures/orphans), NUnit 34/34 and
container Django 104/104 pass. Existing project-wide import/reference diagnostics
and hardware-testing limits are recorded in `HANDOFF.md`.

## Through the Breach — Level 2 extraction

Successful extraction plays a 75-second live evacuation aftermath before Level 2
results. Continue enters Level 3; Replay restarts Level 2 with its arrival cinematic.
Escape, Space, Enter, controller Start/A, or the pointer/touch Skip button skip to
results. Missing voices retain timed subtitles; missing essential art safely opens
results. No watched flag or local save is added.

Production and review commands are in `tools/cutscene/README.md`. The eight new
English cues use Daniel (AXIOM-7), Adam (Kaito), Sarah (Oceanus), George (Titan),
and original Docker/Blender artwork plus synthesized ambience.

Verified on 2026-10-31: build/import, NUnit 47/47, Godot 142/142 (zero
failures/errors/orphans), container Django 104/104, 27 shot/aspect captures,
full playback and result navigation. Existing editor-import addon diagnostics
remain documented in HANDOFF; hardware input and speaker listening are untested.

## Silent Freight — Level 3 arrival

Level 2 results → Continue loads Level 3 into a 75-second skippable live 2D
arrival. Shogun, Oceanus and Titan follow freight beneath depot surveillance.
Every fresh launch (including mission selection, session loading and Replay)
plays it; checkpoint recovery does not. Escape/Space/Enter, controller Start/A
and the pointer/touch Skip button resume at the existing scrapyard spawn.
Gameplay, HUD and music resume from a complete captured state; the opening
mission briefing receives its full display window. Missing speech keeps timed
subtitles; missing essential art starts gameplay safely. Aura Phoenicia and
Mirage Blade join in Harmonic Access and after blackout respectively. No viewing flag or
persistence migration is introduced. See `tools/cutscene/README.md` for production.

Verified through all fresh-entry routes with Godot AI MCP, including natural
playback and three aspect ratios. Automated suites pass: 151 Godot, 47 NUnit
and 104 container Django tests. Physical controller/touch hardware and
speaker-listening review remain separate from synthetic input and audio QA.

The Level 2 aftermath and Level 3 arrival share the campaign's existing results
and roster flow. Their asset manifests and reproduction tools are versioned
with the PostgreSQL-only storage and browser verification tooling described above.

Git-only publication does not require another import or test run when project
code and assets remain unchanged; retain the verification results above.

## Neon Rust sabotage expansion — 2026-10-31

Level 3 now spans eight sectors and 48,000px with 30 finite formations (three or
all four normal-health members, at most six alive), three defended shipments, two
uplinks, two power couplers, finite relay reinforcements and the blind-grid escape.
Aura joins before the sonic-lock lesson; Mirage joins after blackout and must
actually blink across a harmless practice beam. Seven stationary recovery shrines
retain completed objectives, recruits, lessons and kill-reward slots in this scene.
Replay/reload starts a fresh three-operative run with the existing arrival.

Extraction now plays **The City Finds Its Voice**, a 90-second skippable live 2D
aftermath voiced by the five earned operatives and AXIOM-7, before the existing
results. Continue enters Level 4 with five operatives; Replay restores Level 3’s
arrival and three-operative starting roster. Level Select and Main Menu retain
their destinations. The mission stays frozen and results retain extraction-time
score, peak combo and operative. Missing speech preserves subtitles; missing
essential art safely opens results. Keyboard, controller, pointer and touch Skip
share one deferred handoff. Gameplay dialogue, objectives and save schema remain
unchanged. Original Docker/Blender poses and fortress scenery, ten cached
ElevenLabs cues and separate 90-second dialogue/atmosphere tracks are documented
in [cinematic reproduction](tools/cutscene/README.md#the-city-finds-its-voice).

The **30–35 minute first-playthrough goal remains UNVALIDATED**. Assisted blockout
traversal used real movement/abilities with diagnostic enemy kills, zero recoveries,
and about 145 seconds of simulation. It cannot measure normal combat or human
pacing. See [Neon Rust reproduction and acceptance](tools/gameplay/NEON_RUST.md).

Verified on 2026-10-31: NUnit 93/93, Godot 161/161 (zero errors/failures/orphans),
container Django 104/104, seven-asset validation, complete assisted route, all
four results actions and 24 aspect/sector captures. User editor preserved on
Level 1. Background refresh exited 0 with seven sidecars but retains editor/TLS/
shutdown diagnostics; final game logs were clear. Human pacing remains unvalidated.

Server state update (2026-10-31): the user requested shutdown before publication;
the core Compose services are stopped. Restart with docker compose up -d
before code editing or running services. Existing validation results remain valid.

Publication verified: Neon Rust implementation commit 3c92724 is on origin/main.
The services remain stopped at the user's request; prior test results still apply.

The City Finds Its Voice verification (2026-11-01): C# build passes; NUnit 93/93,
Godot 170/170 with zero errors/failures/orphans, isolated container Django 104/104.
All 33 shot/aspect views, full 90-second frozen playback, four results destinations,
raster/atlas QA and both complete 90-second audio tracks pass. Background import
exits 0 with five sidecars; existing addon/TLS/shutdown diagnostics remain. Physical
controller/touch and speaker-listening review remain unperformed. Docker Desktop’s
D: bind-mount fault remains unresolved (including backend /app); copied inputs in
owned unmounted containers enabled production and Django verification without
restarting the four running services. User editor stays stopped on Level 1.


## The Wall That Faces Inward — 2026-11-01

Level 4 now begins with a skippable 120-second live cinematic on every fresh
launch. Thirteen shots introduce the independently powered fortress, separated
Surya Prime (Brian) and armored Commander Voss (Bill). The five-operative squad,
spawn, gameplay briefing, recruitment and arena confrontation remain intact.
Surya still joins at the approach trigger; the gameplay boss appears only in the
arena. The complete mission/HUD/pause/audio snapshot restores once on natural
completion, Skip, missing art or independent overlay removal. Missing speech
keeps the full timed subtitles; checkpoint recovery does not replay the intro.

Original Docker/Blender fortress, shield-road and command artwork reuse existing
operative/boss designs. Twelve cached ElevenLabs multilingual-v2 cues use .70
stability, .75 similarity, .10 style and speed 1; Voss receives restrained speaker
EQ/echo. Separate dialogue/seeded atmosphere tracks measure 120 seconds each.
Production and review: tools/cutscene/README.md and *_inward_wall.py. No watched
flag, save migration, export or publication.

Final verification: C# build has zero errors (154 existing addon warnings);
background import exits 0 with all nine PNG/Ogg sidecars. NUnit 93/93, Godot
180/180 (report_375; zero failures/errors/orphans), and isolated container Django
104/104 pass. All thirteen shots were inspected at 1920×1080, 1920×1200 and
2560×1080. Full playback holds the mission frozen, hands control back naturally,
and stays within 32 ms sampled audio/visual separation. Continue, Level Select,
Replay and loading an owned temporary save activate arrival; the save was removed.

Existing import diagnostics and Docker bind-mount fault remain. Physical input
hardware and speaker-listening checks are outstanding. Changes are local and
uncommitted; production/reproduction details are in tools/cutscene/README.md.

## English campaign speech coverage

Gameplay story dialogue now uses one mission-owned English speech queue across all twelve missions, including recruitment, Mission 5 civilian testimony, and later collectible logs. Localized subtitles retain their existing keys; ordinary status messages remain silent and cannot interrupt speech. The queue follows clip completion, pauses with gameplay, and cancels on recovery, results and scene teardown. Existing cinematic Skip and briefing Continue controls remain available.

The reproducible inventory, per-mission/speaker totals, transcript discrepancies and historical provenance gaps are in [the voice coverage report](docs/voice-coverage/README.md). `python tools/gameplay/english_voice_coverage.py` rebuilds the gameplay inventory; `python tools/gameplay/audit_english_voice.py --check` is the strict evidence gate. Generation/transcription are opt-in cached ElevenLabs authoring tools; runtime makes no API calls. Automated transcript evidence does not imply subjective listening acceptance.

Coverage evidence currently includes 296 spoken cue occurrences and five explicit silent classifications. All required clips have imports, decoded audio, English transcripts and provenance. Seven pronunciation/word-recognition candidates remain flagged for listening acceptance; the strict coverage gate remains open until they are resolved. See the [content review](docs/voice-coverage/REVIEW.md).

Validation on 2026-11-05: C# build/import, NUnit 276, GdUnit4 309 (zero errors/failures/orphans), five inventory checks, and Django 104 passed. The normal aggregate runner encountered a Docker Desktop D: mount error; Django was verified using an unchanged temporary source copy in the existing backend container. This workaround did not repair the host mount or restart services.

## Speech recognition evaluation

Speech-to-text is proposed, not implemented. The current recommendation is to prototype [Godot Whisper](https://github.com/appsinacup/godot-whisper) behind a C# adapter for local player voice input. Its Asset Store listing is marked unstable, so Godot 4.7.2 compatibility, recognition latency during combat, and exported-build native libraries still need validation. Keep normal game audio quality and resample only microphone input for recognition.

OllamaSharp is the preferred C# client if an Ollama-backed conversational feature is added later. As checked on 2026-11-05, [Liquid AI documents LFM3-Omni audio input through Ollama on macOS](https://huggingface.co/LiquidAI/LFM3-Omni-2B-GGUF/blob/main/README.md); the Ollama [Phonon model](https://ollama.com/library/phonon) is text-to-speech. These are not currently selected as the Windows game's STT dependency.

## Browser sign-in for the desktop game

The main menu offers **Log In** and **Sign Up** in the system browser. Guest play remains available. Finish the existing Clerk sign-in and any emailed country/date-of-birth onboarding, then explicitly approve the account. The game updates its profile display, hides both auth buttons and requests window/menu focus. **Open Browser Again** reuses the request; **Cancel**, menu exit and game shutdown cancel polling. Closing the browser leaves the request pending until cancellation or its fifteen-minute deadline. Logout clears only the game session.

Set the game's `AXIOM_API_BASE_URL` process environment variable to override `http://localhost:8000/api`. Remote API/browser URLs require HTTPS; loopback HTTP is allowed for development. Configure the backend's exact `CLERK_ISSUER`, optional `CLERK_JWKS_URL` and `CLERK_AUTHORIZED_PARTIES` website origins, plus the existing backend Clerk and website publishable keys. See `.env.example`. Rebuild/recreate the backend after dependency/environment changes and rebuild the website after frontend changes.

The handoff stores only a private-secret hash and uses POST polling/cancellation with transactional single-use redemption. Public browser URLs contain a request UUID and display intent/locale only; old secretless requests must restart. Clerk factor/verification routes and emailed onboarding preserve that public association and locale. Game credentials and website Django tokens stay in process memory; the independent Clerk browser session follows Clerk's normal session behavior. New game/browser copy covers all seventeen languages. Existing local PostgreSQL storage is retained.

Windows checks verified the exact Clerk-approved profile reaching the running game, guest navigation, both buttons hiding, Play focus and restoration/foreground ownership after minimizing. Final checks passed: C# build, background import, Django 111/111, NUnit 303/303, Godot 319/319 (zero errors/failures/orphans), Python 20/20, localization coverage and Playwright 4/4. Existing editor-import diagnostics and the independent seven-item voice-review gate remain documented in `HANDOFF.md`.

The isolated PostgreSQL/mail browser workflow is in [tools/playwright/README.md](tools/playwright/README.md). Disposable Clerk accounts and the test database are removed afterward; normal backend/db/website stay running. macOS/Linux verification, mobile integration and production rollout remain deferred. No Neon/game-storage migration, exports or deployment are included. Git publication is handled separately from implementation verification.
