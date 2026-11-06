# Written localization and review

The structural catalog is complete: 1,492 required entries for each of seventeen
languages, zero missing cells, damaged format contracts or unexplained English
matches. The scanner conservatively roots all main menus, UI/results, cutscenes
and twelve mission scenes. It does not prove that every possible runtime branch
has been visually inspected. See coverage.json/coverage.md for per-key evidence.

Spanish campaign prose is directly authored. Fifteen other campaign additions
are machine-assisted Google Translate drafts with protected names/numbers/fields,
short-result and numeric-difference review, contextual combat corrections and
agent semantic samples. Native-speaker certification is not claimed. Earlier
rejected local-model drafts were not reused. Review title puns, specialist combat
terms and Hebrew/Thai/CJK phrasing with native speakers before release.

The authoritative CSV retains original keys/English. Authored exact-source JSON
files are in tools/localization/authored; review_corrections.json,
combat_review.json and review_equality.json explain explicit interventions.
The builder rejects conflicting sources and changed fields before opening the
CSV, fills empty cells only, and reuses exact English aliases. Correcting an
already populated translation requires an intentional catalog edit; changing an
authoring file alone does not overwrite it. No fuzzy matching of retired prose.

```powershell
python tools/localization/build_assets.py --seed-english
python tools/localization/catalog.py --write --check
python -m unittest discover -s tests -p test_written_localization.py -v
```

The optional prepare_drafts.py requires --fetch-drafts, writes ignored review
checkpoints only and stops on access/rate refusal. It is not called by the game,
importer, tests or normal build. No runtime model, credentials or translation
service are required. Shared word/proper-name/control exceptions are explicit in
exemptions.json; meaningful English prose is not blanket-exempted.

To rescan C# display syntax without rewriting code:

```powershell
dotnet run --project tests/LocalizationSyntax -- . docs/localization/reachable_sources.json docs/localization/syntax_inventory.json
```

Run build_assets and the gate after scanning. The --apply scanner option mutates
bindings and requires deliberate diff review and full project verification.

Bindings retain canonical display names, semantic category keys and nested damage
templates; arbitrary user/API data remains literal. Locale changes retain active
missions, preview state, recording streams and speech position. Picker fonts cover
all displayed scripts; Arabic/Hebrew direction is explicit. The loading tagline
is a native translated label. A Blender/Docker audit covered 40 directly referenced
raster assets, with proper brand logos retained. Generated-stage/artwork coverage
and fluent editorial polish are broader release QA, not implied by a green gate.

Python tests preserve original English cells and recording hashes/cue words/order/
intervals. Godot checks selectors, live speech, result bindings, script glyphs,
roster nested fields and stable preview commands. Final import, full-suite and
representative screen results are recorded in root HANDOFF.md and MEMORY.MD.

## Verification on 2026-11-06

The final gate has 1,492 required entries per locale and 301 subtitle mappings.
Build passes; import exits 0 with existing editor-addon/shutdown diagnostics.
Full suites pass Django 104, NUnit 290 and Godot report_514 317, with zero suite
failures/errors/orphans. Nineteen offline Python checks pass; the fourteen written
localization checks also pass under Docker Python 3.15.2.

MCP captured all seventeen login locales and checked selection, direction, picker
bounds and mirrored frame styles. Representative visual inspection covered Latin,
Arabic/Hebrew, Japanese/Korean and Thai login text, French/Arabic roster, German
results, Japanese Mission 1 and Thai loading. Final screenshots confirm that a
live Japanese-to-Thai loading switch updates status/logo without changing target,
and Arabic stat fractions retain value/maximum order. Diagnostic images and raw
logs are ignored under .validation; original assets were not overwritten.

This is representative technical QA. Native editorial review and exhaustive
branch-by-branch visual approval remain release work. Earlier transient engine
font-metric diagnostics are recorded in HANDOFF, not declared resolved here.
