# Voice content review

The inventory follows the twelve LevelCatalog mission scenes, their code-built
arrivals and aftermaths, scorecard navigation into external arrival scenes, new
game deployment, and the four active briefing lines. Historical narrative JSON
events are listed as exclusions; they are not new runtime dialogue.

Every required recording was decoded and transcribed using Scribe. Exact matches
ignore punctuation/case. Remaining differences also have independent local
Whisper transcriptions in `independent_review.json`, attached to recording hashes.
Recognition agreement is evidence, not a substitute for subjective listening.

Confirmed repairs include the four obsolete briefing recordings and Titan's
Foundry aftermath slot, which contained a repeat of Oceanus's next line. Only
that mixed-track interval was replaced. Other takes retain their time positions;
the replacement fits its original window at natural speed. Kaito owns the
unattributed Tokyo internal line. Missing gameplay recordings include all three
Mission 5 testimonies and all later optional logs.

Two independent recognizers identified swallowed units and extra sounds around
slash separators in several new technical-log takes. Eight replacement takes
use spoken unit/number forms and punctuation pauses. Displayed dialogue stays
unchanged. Receipts distinguish the exact `input_text` sent for synthesis from
the original authored `text`, retaining both hashes/request settings.

Historical provenance was recovered without generating replacement performances.
Cyber-Grave source/request hashes match its original receipts. For 33 older cues,
authenticated ElevenLabs history downloads have identical MPEG audio frames to
the original production cache after stripping ID3 metadata. These recovered
receipts retain voice, model, settings, request/history IDs and source hashes.

Remaining ASR differences include numeric spelling, spelling variants, compounds,
names such as Xelthara/Stratos/Kaito, and ambiguous consonants or vowels. Listening
should prioritize `cinematic.shattered_aegis_intro.08` (Border/Order onset),
`cinematic.foundry_forge_intro.06` (route/road),
`cinematic.stratos_docks_intro.06` (flak/flat),
`cinematic.neon_rust_aftermath.05` (hauled/hold), and `m07.intro` (sever/severe).
They are deliberately still flagged; no replacement is justified solely by an
uncertain recognition result. The strict coverage gate remains nonzero until
these discrepancies and the other listed candidates receive acceptance.

`playback.json` records actual production collectible callbacks, FIFO ordering of
the three Mission 5 testimonies, a later mission log, pause/resume, natural
Foundry cinematic completion and briefing Continue advancement. Mixer capture
and playback positions establish that audio progressed; they do not establish
subjective listening acceptance on headphones or speakers.
