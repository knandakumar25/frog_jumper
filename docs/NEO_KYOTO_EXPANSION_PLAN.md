> Historical note: an earlier agent wrote this plan for a fresh session to implement. That instruction no longer applies.

# Status as of 2026-11-08

`README.md`, `HANDOFF.md` and `MEMORY.MD` once called this roadmap complete. It is not. **Everything from "Original plan" down is the original plan, not current state. Where they differ, this table wins.** The plan is kept as a list of possible future work and is not an instruction to implement it.

Status was checked by reading the code and by automated tests that drive the real scene in preview mode with no saved campaign. No person has played these features, and saving through a scene was not tested end to end; the save service has its own database tests.

| Word | Meaning |
|---|---|
| Playable | A player can reach and use it in a normal game. |
| Partial | Part of it works; the row says which part. |
| Data only | Defined in files and tests; players can't reach it. |
| Disabled | Code exists but is switched off. |
| Not built | No code. |

| System | Status |
|---|---|
| Walking, running, map, waypoints, minimap, free transit, elevators, mission entrances, squad swap, autosave with Retry | Playable |
| 8 quests in Little Tokyo and Concrete Wards, with conversations | Playable |
| Branch choices in those quests | Partial: the choice is saved and shown as a story outcome, with no gameplay effect. The five choices in data-only quests still describe effects and must be reworded or built if those quests become playable |
| Melee attack, dodge, guard, objective drone waves, alert states, defeat and return | Playable; waves appear only for quest objectives |
| Operative combat abilities, parry, roaming patrols | Not built |
| Electric bike | Playable, drawn from placeholder shapes |
| Safehouse rest (sets the defeat return point), resident greetings, clock, resident day and evening positions | Playable |
| Enterable building interiors | Not built; entrances heal or show a message. The Downtown and Foundry safehouses are placed where they can't be reached |
| The other 64 quests, written conversations for residents | Data only |
| 48 challenges, 16 side operations (their scenes don't exist) | Data only |
| Boats | Disabled; water blocks the player |
| Shops, modules, gadgets, spending credits | Data only; quest rewards are saved but can't be used |
| Operative upgrades in the open world | Data only; world combat uses each operative's base stats |
| Campaign-owned progression | Partial: quest rewards are written to it, but nothing in play reads it, including the upgrade screen |
| Accessibility settings, weather, day/night lighting, liberated-world states | Data only |
| Operative traversal abilities, leisure activities, ambient audio | Not built |
| Performance limits | Partial: chunk counts and world hostiles are capped; the 60-minute stability test has not been run |

# Original plan (not current state)

## Neo-Kyoto: A Living Resistance World

## 1. Vision and design rules

Expand Neo-Kyoto into a single-player hybrid action world: populated neighborhoods, personal stories, optional combat zones, vehicles, operative traversal and separate side-scrolling operations.

The core loop becomes:

**Explore → meet people or discover trouble → choose an approach → complete an activity → earn tactical rewards → see the district change.**

Preserve these foundations:

- All sixteen regions remain accessible after Mission 1. Transit stays free.
- The twelve campaign missions, operative recruitment order, cinematics and ending remain intact.
- Side activities never become prerequisites for campaign missions.
- Choices affect district services, allies and reconstruction.
- Progression works consistently in world combat, side operations and campaign missions.
- Every campaign owns its progression, economy, relationships and world changes.
- Build concentrated, authored neighborhoods around the existing geography. Keep wilderness and long-distance routes spacious.

Target approximately **20–30 hours of optional authored content** across the completed roadmap, without requiring repeated grinding.

## 2. What the world will contain

### Living neighborhoods

Develop **64 activity pockets**: the existing arrival neighborhood plus three satellite locations in each region. Place satellites near existing transit stations; preserve existing landmark and station identities.

Each pocket contains a deliberate combination of residents, environmental storytelling, services, interiors, exploration rewards and nearby activities. Main objectives within a pocket should generally be reachable within 30–60 seconds on foot.

Add:

- **48 named residents**, four faction representatives and the six operatives.
- Markets, food stalls, workshops, clinics, garages, gardens, memorials and community gathering places.
- Selected enterable buildings, including safehouses, shops, shrines, depots and investigation sites.
- Pedestrians, workers, maintenance drones, wildlife and modest road traffic.
- Authored schedules for named characters and simplified routines for crowds.
- A 48-minute day/night cycle, with clear weather, rain, fog and regional storms.
- Regional ambience, surface-dependent footsteps, interior acoustics and music that responds to danger.

Time pauses in menus and cinematics. Important services remain accessible, and quests never require waiting through a particular weather condition.

### District identities and activities

| Region | Signature additions |
|---|---|
| **Old Kyoto** | Lantern markets, palace investigations, rooftop routes, shrines, community gardens and the primary resistance safehouse. |
| **Concrete Wards** | Elevated infrastructure, emergency shelters, infrastructure repairs, contested courtyards and viaduct challenges. |
| **Downtown Core** | Registry investigations, surveillance evasion, signal hacking, rooftop communications and underground evidence trails. |
| **Eastlight Harbor** | Dockworker stories, cargo inspections, courier routes, boat access and waterfront operations. |
| **Citadel Ring** | Restricted machine facilities, security puzzles, advanced training and public spaces that change after liberation. |
| **Rustrail Belt** | Rail depots, salvage recovery, moving industrial scenery, courier time trials and abandoned maintenance sites. |
| **Foundry Basin** | Worker resistance, furnace shutdowns, industrial hazards, heavy machinery and underworks expeditions. |
| **Cedarcrest Highlands** | Mountain settlements, monastery stories, lookout restoration and climbing routes. |
| **Cloudstep Reservoir** | Water-management disputes, floodgate puzzles, boat exploration and submerged-history discoveries. |
| **Stormbreak Peaks** | Weather stations, lightning infrastructure, difficult trails and rescue operations. |
| **Northlight Coast** | Lighthouse restoration, coastal settlements, storm shelters and scenic boat routes. |
| **Cedarwild Preserve** | Conservation stories, wildlife photography, hidden research sites and forest navigation. |
| **Solar Belt** | Solar-array repairs, agricultural communities, power allocation and open-road riding. |
| **Tideglass Coast** | Fishing settlements, tide-pool discoveries, coastal races and damaged maritime infrastructure. |
| **Kurogane Escarpment** | Historic shrines, operative memories, cliff paths and precision traversal. |
| **Reclamation Wetlands** | Ecological restoration, water purification, boardwalk exploration and recovered industrial evidence. |

### Stories, factions and consequences

Deliver **72 authored quests**:

- **48 district quests:** one three-part story per region.
- **12 operative episodes:** two personal quests per operative.
- **12 faction quests:** three quests for each of four organizations.

The four organizations are:

| Organization | Interests and rewards |
|---|---|
| **Civic Recovery Network** | Housing, medical care and public infrastructure; community services and safehouse improvements. |
| **Independent Couriers** | Reliable transport and delivery; vehicle customization and route challenges. |
| **Reclamation Guild** | Industrial recovery and environmental repair; workshop services and equipment sidegrades. |
| **Free Signal Collective** | Communications, privacy and recovered identities; reconnaissance and hacking tools. |

Use three relationship tiers: **Contact, Trusted, Partner**. These unlock optional services, dialogue and cosmetics. They never lock entire districts or campaign entrances.

Choices produce visible, lasting differences: which community project receives priority, who runs a local service, which allies assist an operation and how residents describe the player. Explain the immediate tradeoff before committing a consequential choice.

Support **pre-mission, aftermath and liberated** district states. After Mission 12, occupation activities become reconstruction tasks, remnant incidents or explicitly labeled archive simulations. The world must respect the existing finale.

### Combat and side operations

Introduce top-down combat in clearly marked hostile pockets. Markets, transit plazas and safehouses remain dependable safe spaces.

- Give all six operatives top-down attack, defense, dodge and signature-ability adaptations.
- Keep their existing identities, strengths and squad swapping.
- Share combat rules and damage calculations with missions, while retaining separate movement controllers.
- Add patrols, sentries, drones, investigation, pursuit and reinforcement behavior.
- Use three local alert states: **Suspicion → Search → Pursuit**.
- Keep alerts local to an encounter area rather than introducing a citywide wanted system.
- Protect civilian hubs from roaming combat and uncontrolled reinforcement spawning.
- Disable transit during active pursuit; escaping restores access.

Create **16 replayable side-scrolling operations**, one associated with each region. Operation families include rescue, sabotage, infiltration, defense and recovery. Their initial completions connect to authored quests; replays offer challenges and bounded rewards.

Defeat returns the player to a committed safe arrival without losing equipment or completed quests. Reset only unfinished encounter progress.

### Vehicles and operative traversal

Retain walking at **4 m/s** and running at **8 m/s**.

Add:

- **Electric bike:** first vehicle, approximately 20 m/s maximum on suitable roads.
- **Utility boat:** approximately 14 m/s on authored waterways.
- **Passenger transport:** ambient buses, trains and ferries that can provide scenic rides alongside instant transit.
- Garage delivery, vehicle recovery and cosmetic customization.
- Road and water races, checkpoint trials and courier challenges.

The starter bike is available through a short orientation with a free-loan option. Vehicles have no mandatory fuel, maintenance or insurance grind. Prevent vehicle entry into interiors, narrow pedestrian spaces and unsuitable underground routes.

Provide optional traversal uses for each operative:

| Operative | Exploration role |
|---|---|
| **Cyber Shogun** | Precision dashes, deflection puzzles and disciplined movement trials. |
| **Oceanus** | Dousing hazards, manipulating water equipment and opening canal shortcuts. |
| **Mirage Blade** | Phasing through marked barriers and accessing ventilation routes. |
| **Apex Titan** | Moving heavy obstructions and operating industrial mechanisms. |
| **Aura Phoenicia** | Revealing resonance clues and activating sonic mechanisms. |
| **Surya Prime** | Shielding environmental hazards and energizing solar equipment. |

Ability routes provide shortcuts, secrets and optional challenges. Ordinary travel routes remain available throughout the world.

### Discovery, leisure and player expression

Add **48 regional challenges**: one race, one environmental/operative puzzle and one discovery trail per region.

Also provide:

- Photography mode, a discovery album and a regional codex.
- Collectible testimony, historical records and environmental clues.
- Fishing, arcade and music activities in suitable settlements.
- Operative appearances, vehicle liveries and safehouse decorations.
- A primary Old Kyoto headquarters with visible improvements.
- District safehouses offering preparation, conversations and access to services.

These activities reward exploration and expression without becoming mandatory stat farming.

## 3. Progression, interfaces and persistence

### Functional tactical progression

Retain the existing level-10 cap, three augmentation tracks and five ranks per track.

Replace modifier logic based on display-text matching with typed gameplay effects. Only describe effects that are actually implemented. Initially support passive effects; introduce complex triggered effects through separately tested additions.

Apply a combined **20% passive bonus ceiling** to each of health, shield capacity, damage and cooldown reduction. Preserve operative base-stat differences. Keep ordinary exploration speeds fixed; mobility upgrades affect combat actions and optional traversal.

Add two equipped module slots and two gadget slots. Modules offer tradeoffs rather than stacking unrestricted bonuses. Gadget families are reconnaissance, decoy, smoke and localized disruption.

Use:

- **Hero Sparks and skill points** for operative growth.
- **City Credits** for gadgets, modules, appearances and vehicle customization.
- Salvage pickups that convert into credits, avoiding a separate crafting inventory.

Authored quests award Sparks once. Repeatable activities primarily award credits, challenge medals and cosmetics. Transit remains free.

Balance all twelve campaign missions at baseline and upgraded configurations. Preserve required mechanic lessons and boss phases; upgrades must not bypass story gates or invalidate encounter rules.

### Campaign isolation and migration

Move operative progression from its current shared document into campaign-owned storage.

- Preserve the legacy document as a migration source.
- Capture one immutable legacy baseline when the migration first runs.
- Give existing campaigns a one-time copy of that baseline.
- Start newly created campaigns from normal defaults.
- Never reset existing levels, purchased ranks or balances silently.

Extend the versioned world save to include quest progress, relationships, district decisions, discoveries, vehicle ownership, loadouts and world time.

Commit rewards, purchases, quest transitions and their resulting world changes atomically. Use stable action receipts and revision checks so retries cannot duplicate rewards or spending. Preserve PostgreSQL-only saving and visible retry handling.

### Content and runtime architecture

Introduce engine-independent definitions for:

- `WorldContentCatalog`: stable activity, resident, interior and vehicle identities.
- `QuestDefinition` and `QuestState`: prerequisites, objectives, branches and rewards.
- `DistrictState`: local consequences and campaign-era presentation.
- `CampaignProgressionState`: operative progression, balances and equipment.
- A transaction service for committing a player action and its associated state changes.

Author content in validated JSON with localization and voice cue IDs. Reference Godot scenes for presentation. Use a bounded dialogue graph with supported commands rather than arbitrary executable expressions.

Preserve the existing world seed, IDs and logical coordinates. Add authored content through overlays and reserved clearings. Validate saved positions against new geometry and relocate obstructed arrivals safely.

Simulate nearby actors fully and distant named characters through lightweight schedules. Keep the current nine active chunks and 25-chunk data window until profiling supports a change. Vehicle streaming must load ahead of motion and safely limit speed when terrain is not ready.

Expand the map with quest tracking, service filters, routes, activity states, vehicle locations and district consequences. Preserve the player’s independent waypoint and controller navigation.

## 4. Delivery roadmap

| Phase | Delivery and completion gate |
|---|---|
| **0 — Foundation and representative neighborhood** | Resolve streaming spikes, navigation, authored clearings, interiors, actor budgets, transactional progression migration and typed combat modifiers. Finish one Old Kyoto neighborhood with final lighting, collision, NPC interaction and audio before producing the wider kit. |
| **1 — Old Kyoto and Concrete Wards** | Deliver **8 quests**, **6 regional challenges**, **2 side operations**, two safehouses, six named residents, the starter bike, functional progression and bounded top-down combat supporting all six operatives. Target 2–4 hours of optional content. |
| **2 — Urban expansion** | Add Downtown, Eastlight Harbor, Citadel Ring, Rustrail and Foundry content, including both underground networks. Deliver **27 additional quests**, **15 challenges** and **5 operations**. Introduce boats, faction services, broader interiors and headquarters customization. |
| **3 — Natural and coastal expansion** | Complete the remaining nine regions with **37 additional quests**, **27 challenges** and **9 operations**. Finish operative episodes, faction stories, regional weather, wildlife, fishing and leisure activities. Totals become **72 quests, 48 challenges and 16 operations**. |
| **4 — Endgame and final acceptance** | Complete liberated-world variants, remnant events, operation modifiers, mastery rewards, accessibility, localization review and long-session performance verification. Expand beyond the existing boundaries only after these gates pass. |

For the first release, author these district chains:

- **Old Kyoto:** *The Lantern Circuit* → *Names Behind the Walls* → *The Moonbridge Promise*. Restore local infrastructure, investigate erased community history and choose a neighborhood reconstruction priority.
- **Concrete Wards:** *The Broken Line* → *Signal Under Rain* → *Shelter and Steel*. Repair civilian infrastructure, expose surveillance and choose how local recovery resources are deployed.

Old Kyoto quests begin after Mission 1. Concrete Wards’ recovery chain begins after Mission 2, respecting its events. Include the first Shogun and Oceanus personal episodes when their story prerequisites are satisfied.

Voice named quest dialogue, operative scenes and major events in English. Localize all written content across the existing seventeen languages. Use text and limited barks for ambient interactions; retain provenance, cue validation and listening review.

## 5. Acceptance criteria and explicit boundaries

Before each release:

- Validate quest reachability, branching consequences, recruitment requirements and post-finale variants.
- Test every transaction under interruption, retry, competing writes and unsupported save versions.
- Verify legacy migration and independence between campaigns.
- Test all six operatives in both combat perspectives, including upgraded campaign encounters.
- Verify vehicle handling, bridges, docking, collision, origin shifts and high-speed streaming.
- Exercise keyboard, mouse and controller navigation, remapping, readable telegraphs and puzzle assistance.
- Inspect representative city, industrial, underground, mountain, forest, wetland and coastal content through Godot AI MCP.
- Run repeatable 1080p performance captures and a 60-minute streaming soak. Target p95 frame intervals within 16.67 ms, no recurring streaming stalls above 33.3 ms and bounded resident entities, caches and memory.
- Profile before increasing crowd density or adding simulation complexity.
- Complete the project’s background import, build, mandatory suites and documentation updates.

Use an initial nearby simulation budget of **16 civilians, eight enemies and four moving traffic vehicles**, with pooled presentation and reduced-frequency AI decisions. Increase these limits only after measured acceptance.

This roadmap excludes multiplayer, a shared online world, alternate campaign endings, survival meters, unrestricted destruction and an endless rarity-based loot economy. It includes no automatic exports, deployment, commits or pushes. Release-export benchmarking follows the project’s separately authorized Docker workflow.
