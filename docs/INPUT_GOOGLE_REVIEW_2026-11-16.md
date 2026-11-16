# Input prompt EGM review and proposed resolution — 2026-11-16

Status: first fresh round completed; resolution awaits owner choice.
Target: docs/INPUT_PROMPTS_AUDIT.md, full file, lines 1–84 at review time.
Scope: delivered input behavior, documentation accuracy and verification evidence.
Skills: egm-review-loop, egm-review and egm-resolve. Reviewers ran with no parent
conversation history; host instruction-file exclusion is not verified.

| Reviewer | Standard | Status | Result |
| --- | --- | --- | --- |
| codebase_details | re-implementation | completed | 1 Nit |
| implementation_readiness | implementation readiness, evidence scope | completed | 1 Major, 1 Nit |
| style | bundled style persona, evidence clarity | completed | 2 Nits |

The code/evidence source conclusions were checked against files after Graphify
lookups. No reviewer ran suites or modified runtime files. This is one review
round, not the five-stage design review and not two-round convergence.

## Prioritized findings and AI suggestions

### evidence-1 — Major — Controller routing coverage

Scope: audit lines 16/19/24–25/56–58/80–84, smoke harness lines 66–81,
input tests lines 106–117. Controller checks call ObserveInput directly;
they do not demonstrate controller actions passing through Godot event dispatch.
This can miss consumption-order and forced-Keyboard-style regressions.

AI suggestion: **PROPOSE FIX**. Root confirmed direct service calls. Preserve
the 58/0 result as valid assertion counts, explicitly label its limits, and add
viewport-injected pad press/release cases with observable menu, cinematic and
gameplay outcomes, including Keyboard style. Keep physical hardware unverified.
This is a coverage gap, not proof that gameplay is broken.

### evidence-2 — Nit — Weak menu prompt assertion

Scope: smoke harness lines 90–95/136–143. Searching every visible label for A
can match unrelated menu copy without proving the instruction prompt refreshed.

AI suggestion: **PROPOSE FIX**. Root confirmed this search. Locate the actual
instruction label and assert its expected keyboard/controller text transition.

### code-1-1 — Nit — Untranslated rebind action name

Scope: MenuInputManager.cs lines 87–91/134–135/180, audit line 20.
The catalog now retains English source keys but modal interpolation omits Tr.
Localized rows therefore open a modal naming the action in English.

AI suggestion: **PROPOSE FIX**. Root confirmed source and diff. Translate the
retained source title when displaying the modal, refresh it on locale invalidation
where applicable, and add a focused localized-modal regression check.

### style-1 — Nit — Stale pending-test wording

Scope: audit line 43 versus 50–55. A historical bullet says a final run is required
after the later section already records it passing.

AI suggestion: **PROPOSE FIX**. Change the sentence to past tense and point to
the completed resume results; preserve historical failures as history.

### style-2 — Nit — Ambiguous alias notation

Scope: audit line 19. X/B, Y/LB and Shift/C/RB mix naming schemes without labels.

AI suggestion: **PROPOSE FIX**. Identify pad names as Xbox-style examples and
separate keyboard Shift/C from pad RB. Do not change underlying bindings.

## Resolution boundary

No fixes applied during this review. No tests/import rerun was necessary for
review/report-only changes. Existing suite results remain backend 158, NUnit
454, Godot 385 with zero errors/failures/orphans. Raw reviewer reports are saved
alongside this file as INPUT_GOOGLE_CODE_REVIEW_2026-11-16.md,
INPUT_GOOGLE_EVIDENCE_REVIEW_2026-11-16.md and
INPUT_GOOGLE_DOCUMENT_REVIEW_2026-11-16.md.

Next owner choices: auto accept proposed fixes, continue without changes, or
review findings individually. After code/test changes, run required verification
and obtain a new fresh review; this round was not clean before fixes.
