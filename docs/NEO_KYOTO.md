# Neo-Kyoto exploration

The initial surface is 128,000 × 128,000 metres (16,384 km²), north-up, divided
into sixteen 32 km districts. Existing missions retain their own controllers and
cinematics. Finish Mission 1 to open exploration; every district and all 256 free
stations are available immediately. Progress locks mission terminals, not districts.

Move with WASD/left stick or D-pad, run with Shift/L3, interact with E/A, switch
earned operatives with Tab/Y and open the map with M/Back. The map supports mouse
wheel zoom, right-drag pan, left-click waypoints, WASD/stick pan, shoulder zoom,
native keyboard/controller destination selection, three layers and one waypoint.
The destination list includes all twelve mission entrances, 48 landmarks and
256 stations. Selecting a mission in the old mission menu marks its entrance;
travel to its neighboring station, then walk to the terminal to launch it.

The two underground networks are 2 km squares below Downtown and Foundry. Their
elevators share the transit plaza's logical position and connect to the surface.
Mission 6 is underground Downtown, Mission 7 is below Foundry, and Missions 8–12
use separate Citadel/Spire terminals. Arterials connect all station intersections;
local streets/trails cross chunk seams, river crossings have bridge decks, and
Rustrail has a continuous rail corridor. Water, mountain/forest scatter and solid
building/landmark footprints constrain walkability; the total region area is not
a claim that every square metre is walkable.

`WorldCatalog.cs` owns stable geography and IDs. `WorldGeneration.cs` uses a pinned
integer hash and immutable data generated on a worker. Only nine 128 m chunks have
Godot scenes; the data/prefetch window holds at most 25 chunks and one worker batch.
Scene creation applies one chunk per frame. Movement waits for all nine resident
chunks, including after transit. Rendering rebases at chunk boundaries; physics,
sprites and terrain cells use bounded local coordinates. Expansion must preserve
existing region/station IDs, hash rules and `neo-kyoto-v1` identity. Changing the
generation contract requires an explicit save migration, not silently reseeding.
This follows Godot's documented [2D precision limitations](https://docs.godotengine.org/en/stable/tutorials/physics/large_world_coordinates.html);
the engine's double-precision build option does not increase 2D rendering precision.

World snapshots live only in PostgreSQL `game_state`, keyed by campaign ID. They
include version/identity, logical position/layer, waypoint, safe extraction,
operative, mission and world/mission resume mode. Settlement writes extraction in
the same transaction as the exactly-once reward receipt. Replaying a receipt cannot
overwrite subsequent transit. Transit, elevators, mission entry, replay and menu
return commit before scene changes. Failed writes retain the last committed state
and expose retry; no local save fallback exists. Legacy campaigns receive safe
arrivals after Mission 1, while new campaigns keep deployment and Mission 1.
Opening a world scene directly without a campaign is explicitly unsaved preview.

The original Blender kit contains eight environment modules, sixteen architectural
and sixteen scenic landmarks, and six four-frame/eight-direction operative atlases.
The Old Kyoto block establishes the shared angled camera and amber/cyan basalt
palette. Foot colliders and fading upper sprites keep walking paths readable.
Rebuild with `docker compose --profile cutscene run --rm --entrypoint blender
cutscene --background --python tools/world/build_kit.py`. This is an opt-in art
pipeline, not a game export. The manifest records original procedural authorship.

`tools/world/localize.py` materializes ninety interface/place labels across all
seventeen supported languages from checked-in authored glossaries. Geographic
proper-name roots retain their canonical Latin spelling; their descriptors and
all interface controls are localized. No network translation runs at game time or
during this build step. Native-language acceptance remains a separate review.

Verification evidence is recorded in root HANDOFF.md. `tools/world/qa.py` captures
the live scene and drives the real free-transit UI for a rendered streaming tour
through Godot AI MCP. It refuses to operate in a selected player campaign. Its
screenshots/measurements stay ignored under `.validation`; headless tests cannot
establish a rendered 60 FPS claim. No exports are needed for these checks.

## Measured runtime limits

The 32-stop rendered transit sweep on this Windows machine used Intel Iris Xe
graphics at a 1920 × 1094 debug-game viewport. Across 3,840 sampled frame intervals,
median was 16.677 ms, p95 17.728 ms and maximum 43.013 ms. Resident chunks remained
exactly nine and the data cache exactly 25 at every sampled stop. Godot static
memory ranged from 103,312,598 to 109,882,564 bytes. These are short debug-session
measurements, not release-player GPU timings or evidence of indefinite memory
stability. Automated verification was also active during sampling. The median meets
approximately 60 FPS; slower frames miss that budget.
The requested sustained 1080p/60 target therefore remains a performance acceptance
target, not a certified result. A future release benchmark must use the authorized
Docker export workflow separately; this task performed no export.

Live MCP inspection covered the Old Kyoto reference block, highlands, coastline,
both underground elevators, full-map framing, controller Back/D-pad navigation,
4 m/s walk and 8 m/s run over sixty physics frames, and roof fading to 0.302 alpha.
All 46 PNGs have the expected dimensions, transparent/drawn pixels and generated
Godot `.import` metadata. Full manual combat playthroughs and native art/language
acceptance are not implied by these checks. The generic scene-reference checker
also reports existing addon signal/inheritance warnings; none target the new world
scene. Unit tests and Godot imports provide the runtime resource checks.
