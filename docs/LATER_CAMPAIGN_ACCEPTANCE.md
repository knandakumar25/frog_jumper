# Later campaign acceptance — 2026-11-05

This checklist separates engine and media evidence from human acceptance. Changes
remain local and uncommitted; no exports were produced. The normal four-service
Docker stack stays running at the user's latest request.

## Implemented behavior

- Mandatory gates record accepted correct abilities independently of defender
  clearance. Mirage must cross the laser boundary while phased; Surya must cross
  the complete heat footprint while protected. Full-height interlocks prevent
  bypassing progression. Decorative supports leave reachable ground combat space.
- Pods accept keyboard/controller attack and click/tap with proximity, prerequisite
  and one-time checks. Rejected terminals remain usable and show the requirement.
- Sequential finite waves retain accepted completion and rewards through recovery.
  Unfinished boss attempts reset supporting waves, phase milestones and projectiles;
  accepted defeat remains durable. Bosses retain original health/phase thresholds.
- Real enemy hurtboxes, explicit targets, HUD telemetry and source-aware projectile
  reflection connect all six later bosses to normal melee and Kaito's parry.
- Continue 5→6 and 6→7, both acts, later prologues and final Return to Main Menu are
  connected. Level Select, Resume and Replay retain direct gameplay entry. Unlocks
  use the complete twelve-mission catalog, and the finale never exposes Mission 13.
- Extraction uses one stable run receipt and a PostgreSQL transaction for score and
  high-water progress. Results show saving, failure/retry or no active campaign.
- Fourteen later overlays own cursor/help/audio and deliver completion once after
  Skip, natural finish, missing essential artwork or independent removal. Missing
  speech uses the complete subtitle timeline; small windows retain readable text.

## Media and reproduction

Seven families contain 54 original PNG plates: 1920×1080 base, transparent structure
and emissive layers for gameplay/prologues and distinct aftermath settings, plus
six matching 512×512 boss pairs. `manifest.json` records provenance, transparency,
dimensions and hashes. Collision remains authored in Godot.

Thirteen missing dialogue tracks contain 117 full ElevenLabs takes using the
established cast's actual IDs. `speech_provenance.json` records settings, original
cast sources and request/take hashes; `dialogue_timing.json` records complete cue
delivery at speed 1. The existing Cyber-Grave introduction recording is preserved.
All fourteen atmosphere beds use distinct original synthesized layer combinations.
`assets/cutscenes/later_campaign_validation.json` records full-decode duration,
peak and audio hashes for all 28 tracks.

Reproduction tools under `tools/cutscene/`:

- `render_later_campaign.py`: Blender targets and production layers in the media container.
- `package_later_campaign.py --validate-only`: full audio decoding without regenerating speech.
- `review_later_campaign.py`: native Godot AI MCP captures of every cue at 1920×1080,
  1920×1200, 2560×1080 and 800×600; seeking is composition evidence only.
- `assemble_later_review.py`: aspect-preserving contact sheets in the media container.
- `review_campaign_navigation.py`: actual scene changes, direct Resume of an owned
  temporary PostgreSQL fixture, and cleanup of that exact fixture.
- `later_playthrough.gd`: actual movement, attacks, swaps, signatures and context
  actions without ledger injection or health overrides. Failed bot development
  runs remain diagnostic failures, not successful traversal evidence.

Local captures/logs are under ignored `.validation/later-campaign/`. They are not
included as shipped game assets or exports.

## Verification status

Final `run_tests.ps1`: **104 Django, 276 NUnit and 303 Godot tests pass**, with
zero errors, failures, skips or orphans. Build succeeds with zero errors (155
project/addon warnings). Background import completed and every PNG has its sidecar.
Real PostgreSQL fixtures verify high-water progress, concurrent settlement,
discarded committed responses/retry and explicit failure; exact owned fixtures are
removed. Results UI tests cover pending, failed/retry and no active campaign.

Fresh production-input runs reached actual results, then used the normal aftermath
Skip input. They never injected milestones, health or direct damage:

| Mission | Fresh entry → results | Checkpoint recoveries in accepted run |
| --- | --- | --- |
| 6 | Passed | 2 |
| 7 | Passed | 0 |
| 8 | Passed | 0 |
| 9 | Passed | 2 |
| 10 | Passed | 2 |
| 11 | Passed | 2 |
| 12 | Passed | 2 |

The optimized bot runs at accelerated wall-clock speed with the normal gameplay
physics step. Their simulation times do **not** certify normal human pacing.
Accepted raw runs are indexed by `.validation/later-campaign/accepted-playthroughs.json`;
failed diagnostic runner development attempts remain separate. Mission 5's restored
Continue was verified through actual scene navigation; its combat was covered by
the full existing regression suite, not claimed as a new fresh bot run.

Godot AI MCP navigation exercised both act transitions, every later prologue,
Continue, Replay, direct Resume/Level Select and final menu. Resume used a temporary
owned PostgreSQL fixture that was removed afterward. Every one of 126 later shots
was inspected at all three required aspect ratios, plus 800×600: **504 native
captures**, zero size or subtitle-bound failures. Final fixes restored redraws for
the fortress gates/dawn and separated visual sampling from audio scrubbing.
Mounted-audio tests verify speech continues naturally during visual sampling.
Full decoding validates all 28 complete tracks, unclipped peaks, complete cue takes
and distinct atmosphere hashes. Subjective listening remains a human check below.

Existing addon TLS/HTTP/socket and import shutdown diagnostics (27 ObjectDB instances,
12 resources and PagedAllocator messages) match the initial import; these are
separate from zero-orphan project test results. The project checker retains its
baseline 10 unrelated addon/menu/UID errors and 27 warnings. No existing campaign
was selected, and the normal four-service Docker stack remains running.

## Human acceptance — intentionally pending

- [ ] Play Missions 5–12 at normal speed: judge mission length, checkpoint spacing,
  boss phase pacing, encounter pressure and clarity of each required ability route.
- [ ] Use a physical controller for swaps, pod context attack, combat, Skip, results,
  both act transitions and final menu; verify focus and held-input release.
- [ ] Use touch hardware for pods, Skip and results; check target size and overlap
  at a smaller window and supported touch resolutions.
- [ ] Listen to every later cinematic naturally with speakers and headphones:
  confirm cast identity, delivery, full final words, subtitle timing, atmosphere
  separation and intelligibility. Decoder peaks do not establish subjective quality.
- [ ] Review all environments, boss silhouettes, landmark readability and the dawn
  ending in motion at the three requested aspect ratios.

Automated input invocation does not establish physical device usability; screenshots
and audio decoding do not establish pacing or subjective listening acceptance.
