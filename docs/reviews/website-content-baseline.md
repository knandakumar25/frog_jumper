# Website content baseline

Reviewed 2026-11-15 against the approved website redesign plan. Graphify was queried for LevelCatalog, OperativeRoster and OperativeProfile; source files below were then read. This review makes no runtime or release-certification claim. No code, scenes or assets changed.

## Campaign

`scripts/managers/LevelCatalog.cs:27` is the ordered source for the twelve mission titles and recruitment grants. Act boundaries are confirmed by `scripts/gameplay/FoundryForgeMission.cs:7` (Mission 7 is the Act II finale), `scripts/gameplay/StratosDocksMission.cs:7` (Mission 8 opens Act III), `scripts/ui/ActIIIStartScreen.cs:6` (Missions 8–12), and `README.md:336` (Act I ends at Mission 4).

| Act | Mission | Exact title | Recruitment |
|---|---:|---|---|
| I | 1 | Ghost in the Gridlock | Start with Cyber Shogun |
| I | 2 | The Concrete Wildfire | Oceanus, Apex Titan |
| I | 3 | Neon Rust and Data Dust | Aura Phoenicia, Mirage Blade |
| I | 4 | Shattered Aegis | Surya Prime |
| II | 5 | Names Without Numbers | None |
| II | 6 | Cyber-Grave Labs | None |
| II | 7 | The Foundry Forge | None |
| III | 8 | Stratos Docks | None |
| III | 9 | Spire Outer Hull | None |
| III | 10 | The Cyber-Nexus | None |
| III | 11 | The Apex Bastion | None |
| III | 12 | Judgment Day | None |

Do not divide the acts into equal groups. Do not present the existing website's training plus four sectors as the campaign (`website/src/pages/Gameplay.jsx:15`). Avoid revealing mission outcomes in showcase copy.

## Squad

Roles and named moves below are registry labels, from `scripts/actors/OperativeProfile.cs:45`. They should not be expanded into promises about separate implemented combat systems. Recruitment and signature tags come from `scripts/managers/OperativeRoster.cs:18` and `scripts/managers/LevelCatalog.cs:27`.

| Operative / civilian name | Registry role | Primary / secondary / dash labels | Signature tag | Joins |
|---|---|---|---|---|
| Cyber Shogun / Kaito Kurogane | Vanguard / Striker | Shogun's Flash / Kinetic Parry / Blade Dash | parry | Campaign start |
| Oceanus / Kiana Vaea | Hydro Vanguard / Tactician | Tidal Torrent / Maelstrom Vortex / Aqua Jet | douse | Mission 2 |
| Apex Titan / King Tariq Nzinga | Electro-Magnetic / Juggernaut | Tectonic Slam / Magnetic Tether / Titan Surge | slam | Mission 2 |
| Aura Phoenicia / Maya Khoury | Prismatic Resonance / Sound Weaver | Harmonic Pulse / Prism Reverberation / Wave Glide | resonate | Mission 3 |
| Mirage Blade / Zayd Al-Mansoor | Phantom Infiltrator / Assassin | Phase Blink / Holographic Decoy / Phase Dash | phase | Mission 3 |
| Surya Prime / Arjun Varma | Solar Bulwark / Tank | Solar Shield Bash / Aegis of the Sun / Vanguard Dash | bulwark | Mission 4 |

`scripts/actors/CyberShogun.cs:611` implements timed parry, phase blink and bulwark windows and sends signature tags to ability gates. Douse, slam and resonate must not be described as verified standalone elaborate attacks based solely on profile names. The controller's signature method sends those tags to gates; it does not implement the registry's full fantasy descriptions. Parry reflects ranged attacks and bulwark absorbs hazards/ranged fire (`CyberShogun.cs:748`).

Operatives are earned during missions, not selected freely at new-game creation. Prior mission grants rebuild the roster on subsequent level starts (`OperativeRoster.cs:69`). Unlocked operatives can be swapped. Surya is unavailable during Mission 5 while protecting the civilian queue (`OperativeRoster.cs:54`); avoid saying all six can be fielded in every mission after Mission 4.

Remove made-up percentage ratings, deployment synchronization and combat claims based on the current Squad copy's graviton blasts, crystal refractions and similar embellishments (`website/src/pages/Squad.jsx:13`). Keep direct character IDs shogun, oceanus, apex, phoenicia, mirage and surya.

## World scope

World navigation becomes available after Mission 1: `scripts/world/WorldNavigation.cs:24` handles the initial Mission 1 separately, then routes mission selection through the world. The main setting is Los Angeles, 2157; the class name NeoKyotoWorld is retained for compatibility.

Within Los Angeles, eight quests in Little Tokyo and Concrete Wards are playable (`scripts/world/WorldQuestRules.cs:15`). The other LA district quests are catalog data. LA walking, running, map/waypoints, mission entrances, squad swap, safehouse rest, and quest-objective combat waves are listed as playable in `docs/NEO_KYOTO_EXPANSION_PLAN.md:18`. Roaming patrols, operative world combat powers, enterable interiors, shops and usable world upgrades must not be advertised as complete (`docs/NEO_KYOTO_EXPANSION_PLAN.md:24`). Branch choices save a story outcome without gameplay effects.

Important scope correction: the current source also registers Kyoto, Mumbai, Honolulu, Dubai, Luanda and Beirut (`scripts/world/WorldDefinition.cs:167`) and admits their quests through `WorldQuestRules.cs:29`. Skyport travel is wired in `scripts/world/NeoKyotoWorld.cs:690`. The status table says those cities each have eight quests and one operation, with English-only text (`docs/NEO_KYOTO_EXPANSION_PLAN.md:21`). Thus “only eight playable quests in the entire world” would contradict current source. Keep homepage copy scoped to LA's playable district slice, or describe the additional cities with their English-only limitation. Do not imply all sixteen LA regions have quests and combat.

## Availability, platforms and requirements

Verified repository facts: active development; Godot 4.7.2 .NET/C# game; React/Vite website; Express.js accounts API. Local build/export tooling is evidence of development support, not of released platform availability. `export/export.sh:32` accepts Windows, Linux, web, Android, macOS and iOS targets. Apple workflows require native completion (`README.md:944`). No source reviewed establishes a public release date, released storefront build, price, minimum hardware specification or certified platform support.

Safe FAQ copy: “Axiom Force is in development. A release date has not been announced here.” “Release platforms and system requirements will be confirmed before launch.” If listing development targets, label them as targets and retain Apple qualification. Do not promise game browser play merely because there is a web target or website.

Remove the claim of a basic GPU being enough (`website/src/pages/FAQ.jsx:47`), assured visionOS compatibility (`FAQ.jsx:23`), cooperative missions (`FAQ.jsx:29`), and early access/update benefits for creating an account (`FAQ.jsx:35`). Remove Xbox, PlayStation and Switch availability implications (`website/src/pages/Home.jsx:183`) unless the owner supplies a current supported release plan. Account registration supports account management and desktop browser sign-in; it does not establish access to a release, newsletter or early-access program.

## Development page

Remove simulated telemetry, zero-allocation architecture and verified reaction/performance claims (`website/src/pages/TechHub.jsx:70`). Physics frequency alone does not prove deterministic simulation. The configured renderer is Mobile (`project.godot:114`), so the current “VULKAN FORWARD+ / MOBILE” panel must not be treated as a verified configuration description. Use plain factual technology and implementation-status descriptions. Django is historical; Express.js is current.

## Acceptance notes

The existing 17 language values must be replaced where they contain these claims; English JSX fallbacks alone will retain old content for translated locales. Keep gallery art labelled as artwork unless it is a genuine game capture. This report does not certify translation quality or platform readiness.
