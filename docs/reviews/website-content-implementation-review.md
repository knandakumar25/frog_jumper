# Website implementation content review

Reviewed 2026-11-15 against the approved redesign plan and `website-content-baseline.md`. A separate fresh-context Codebase Details Goldfish inspected the implementation independently and returned no Major or Blocker findings. Graphify was queried first; it still identified the old website pages, so current source was used for conclusions.

## Findings

No Major or Blocker content findings in the reviewed implementation.

Nit: `website/src/translations/ui_translations.json:252` explains export targets and development tools in the player-facing platforms answer. “Release platforms have not been confirmed” communicates the relevant outcome more directly. This is a clarity improvement, not an incorrect promise.

## Verified evidence

- `website/src/components/Showcase.jsx:5` lists all six canonical operatives and their matching recruitment missions; `:13` lists the twelve catalog mission titles. `website/src/pages/Gameplay.jsx:6` groups Act I as 1–4, Act II as 5–7, and Act III as 8–12.
- `website/src/pages/Squad.jsx:11` renders registry-aligned roles, conservative signature ability descriptions and recruitment information. The `showcase.abilities.surya` value includes the Mission 5 restriction. Simulated deployment, invented percentage statistics and the undefined hallRoster dependency have been removed from this page.
- `website/src/translations/ui_translations.json:236` begins the English showcase content. Flattening every locale's showcase subtree found 72 nonempty leaf keys in each of the 17 locales, with zero missing English reference keys. Each locale includes all twelve mission translations and all six roles and ability descriptions. Shared English strings were limited to expected menu/mission cognates. Key parity proves coverage, not native-language quality.
- The English world copy (`ui_translations.json:243`) explicitly limits the incomplete region activities statement to Los Angeles; it does not incorrectly restrict the entire game's other cities to eight quests. It gives Mission 1 as the world-exploration unlock.
- `website/src/components/Showcase.jsx:28` supplies the FAQ answers through showcase translations. Release timing, release platforms and hardware requirements remain unconfirmed. Current answers make no early-access, newsletter, cooperative-play, certified-platform or performance promise.
- `website/src/pages/Home.jsx:12` and `website/src/pages/Squad.jsx:11` label the hero and dossier art as game artwork. The seven files under `website/public/images/showcase/` have SHA-256 hashes identical to the existing six portraits in `assets/characters/rendered/` and `assets/images/menu_backdrops/city_base.png`. The source paths are recorded in `spritecook-assets.json`; no new art generation was needed.
- `website/src/components/Footer.jsx:7` links FAQ and Development, with no invented live status or console compatibility list. `website/src/pages/TechHub.jsx:5` uses the current Godot/C#, React/Vite, Express.js/Node, Clerk and PostgreSQL stack and omits the prior simulated telemetry.

## Limits

Static source and content review only. This report does not certify browser layout, accessibility, authentication regression behavior, native-speaker translation accuracy or platform readiness. Existing unused legacy translation entries still contain historical copy; the reviewed pages now use the showcase subtree. Tests are owned by the implementation task. This review changed Markdown only and did not require a Godot refresh or unit-suite rerun.
