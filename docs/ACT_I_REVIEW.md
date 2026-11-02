# Act I mission review — updated 2026-11-02

Levels 1–3 now implement the focused readability/objective-defense pass below.
The original review follows as historical context. Routes, recruits, abilities,
story events, recorded cinematics, launch/Skip rules and results transitions are
preserved; Level 4 is unchanged. Automated traversal verifies function, never
human pacing targets.

| Mission | Implemented change | Human observation still needed |
|---|---|---|
| Palace | Contextual parry binding, deflection confirmation, archive signs, live beacon charges and sealed/accessible Blade | First-time timing comprehension and archive discovery |
| Concrete Wildfire | Defender counts, fire/operative/barrier/battery prerequisites, movement/arrival states, named spire defenses | Rescue comprehension and optional patrol readability |
| Neon Rust | Nine required defenses of 30, local shipment dependencies, objective checkpoints, optional patrol signs, curtain escape guidance | Intentional bypass and understandable objective chain |

Levels 2–3 retire remote unfinished groups at 1,500 horizontal pixels without
granting completion, kills or repeat rewards. Recovery retains completed objectives
and rewards; Replay/reload starts fresh. Existing stats, caps and score incentives
are unchanged. No save migration, new recording, export or publication is included.

Human playtest checklist:

- In Lantern Court, identify the current parry control, attempt timing, and explain
  the deflection confirmation. At the beacon, explain remaining charges and when
  attacks become useful; repeat after recovery.
- Find the rooftop-to-archive descent from signs without verbal coaching, then
  distinguish the sealed Blade from its accessible state.
- At each rescue, name the blocking defenders/fire/barrier/battery and required
  operative. Watch civilians enter shelter before the rescue counter advances.
- In Level 3, bypass an optional patrol, complete a defended objective and activate
  its checkpoint. After Mirage's lesson, follow the curtain escape instruction.
- Record traversal, required combat, optional combat, recovery and cinematic time
  separately. Note stalls/backtracking; assisted timings do not establish pacing.
  Check physical controller/touch controls separately.

Final verification for this pass is recorded in HANDOFF.md and MEMORY.MD.

Final results (2026-11-02): build succeeds; NUnit 88/88, Godot 197/197 with zero
errors/failures/orphans, and copied-container Django 104/104 pass. All 51 captures
match requested pixel dimensions and were reviewed across the three ratios.
Results Replay/Continue/Level Select/Main Menu pass for each mission. The final
required-only Neon Rust pass completes with real movement/abilities, diagnostic
defense damage, zero recoveries and at most six live enemies; 157.436 seconds is
assisted route/objective wall time only. Human traversal/combat/recovery timing
and comprehension remain unmeasured. Earlier helper stalls are excluded from
that final result. Existing import and Docker mount diagnostics are separate
from the passing gameplay tests.

Reproduce the MCP review with the configured local Godot AI server and a stopped
game: `tools/gameplay/review_mission_readability.py` captures 51 guidance states
at the three requested sizes. `--level 1` repeats only Palace after a focused
change; `--navigation` verifies aftermath/results and all four destinations for
Levels 1–2. `tools/gameplay/review_neon_rust.py --navigation` covers Level 3, and
`--required-only` traverses its route with real movement/abilities and diagnostic
damage confined to objective defenders. These helpers preserve the open editor
scene, reject an active campaign session and write ignored `.validation/` evidence.
Still captures use diagnostic placement; they do not demonstrate reachable routes
or human play time. Full-resolution PNGs are saved in the game process through
MCP evaluation to avoid the server's 4 MiB screenshot transport limit.

## Historical review — 2026-11-01

Read-only design review of the current Palace, Concrete Wildfire and Neon Rust
mission code, objective ledgers and cinematic integration. This implementation
does not rebuild Levels 1–3. Prior assisted runs do not establish human pacing.

| Mission | Continuity | Teaching | Repetition and pacing | Separate proposed work |
|---|---|---|---|---|
| Palace | Recovery of testimony grounds Tokyo Remembers; Shogun starts alone. | Parry overload ties defense to the beacon objective. | Garden traversal and repeated sentries need human observation before cuts. | First: observe first-time parry recognition and archive discovery; improve cues only where players stall. |
| Concrete Wildfire | Refugee protection leads to Titan's breach; the occupation has material consequences. | Recruitment teaches douse and slam through local rescue tasks. | Six sectors and finite defense groups alternate travel with shelters; 75-second aftermath and next arrival extend downtime. | Second: measure rescue comprehension, defense repetition and control-free time; tune individual repeated groups separately. |
| Neon Rust | Freight/comm sabotage opens the fortress approach; independently powered fortress guns remain live. | Aura's real sonic-lock task and Mirage's harmless Blink beam precede pressure. | Eight sectors, 30 groups and mandatory clearance risk attrition; 90-second aftermath plus 120-second next arrival is 210 seconds without normal play. | Third: record route/combat/cinematic time independently; propose skippability/readability or formation reductions from observations, preserving shown events. |

The revised finale uses objective defense rather than mandatory elimination of
all ordinary formations. Do not apply that change to earlier ledgers without a
separate implementation and regression review. Preserve all existing recorded
cinematic durations. Schedule human keyboard/controller/touch runs and listening
review before declaring Act I's teaching, pacing or mix accepted.
