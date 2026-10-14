# Axiom Force — Story Bible (v3.0, character-driven restructure)

> Written 2026-10-13. This is the working canon for the campaign narrative in
> `assets/narrative/*.json`. It replaces the v2.0 ensemble framing.

## The premise

The Xelthara occupation runs Earth through a simulation grid. The Homeland
Liberation Front fights it from inside, and the squad's guide is **AXIOM-7**
— an AI that narrates the war, runs the training simulations, and is slowly
discovering that it has preferences of its own.

## The four leads

| Character | Role in the ensemble | Axis of contrast | Arc (level 1 → 9) |
|---|---|---|---|
| **Cyber Shogun** (Kaito Kurogane) | The sword and the standard. Stoic, protocol-bound, expresses everything through movement (rules: **0 particle VFX**). | Tradition vs. machine | Obeys the war protocol blindly → begins to question AXIOM-7's orders (L4, L7) → chooses to call the machine an ally by his own code (L9) |
| **Oceanus** (Kiana Vaea) | The emotional heart. Tidecaller; thinks in tides, rescue, return. | Warmth vs. protocol | Saves refugees by reflex (L2) → the bio-labs turn grief into resolve (L6) → "the tide has come home" (L9) |
| **Aura Phoenicia** (Maya Khoury) | The voice. Harmonics, prism art, aerial broadcast. | Art vs. war | Art as escape (L3's comms hack = a song) → art as communication (L8's sky-broadcast) → the anthem that ends the war (L9) |
| **AXIOM-7** | The narrator/guide — the machine lead. Runs the simulations, gives the mission briefs. | Machine protocol vs. emerging self | "Simulation" framing throughout → the first unauthorized feeling (L7) → at L9, for the first time, chooses to call something *hope* |

The trio are named humans on first mention; afterwards the story may use
their titles. **AXIOM-7's existing campaign lines are kept word-for-word**
(some are backed by recorded VO at `assets/speech/AXIOM-7/`), which is why
they read as mission beats — the character lines were written around them.

## Functional cast (no arcs, limited lines)

- **Mirage Blade** (Zayd Al-Mansoor) — the scout. Terse intel drops: L3, L5.
- **Apex Titan** (King Tariq Nzinga) — the wall. Breacher: L2, L4, L7.

Two lines each at most. They remain fully selectable operatives in the game
(model, VFX, registry untouched) — only their narrative weight is reduced.

## Antagonist voices (kept, not "mains")

- **Border Commander Voss** — Act I obstacle (L4).
- **The Void Commander** — Act II final enemy (L9).

## REMOVED from this version

`Ninja Frog`, `Mask Dude`, `Pink Man`, `Virtual Guy` (2D frog-jumper relics
that still spoke mid-campaign lines in v2.0), and `Aegis Ronin` / `Oni
Matrix` (relic bosses doubling as heroes). All replaced by the leads,
functional cast, or antagonist voices — no story information lost except
the joke lines, which die with the frog.

## One deliberate exception (recording debt)

L9 adds ONE new AXIOM-7 line ("...For the first time, I have no protocol
for what I feel..."). The AXIOM-7 voice files exist per level for the
original lines only; this line is unrecorded. It is the arc's payoff, and
worth one future recording session.

## Level-by-level focus

| Level | Focus | Beats |
|---|---|---|
| 0 | Tutorial (untouched) | The recorded training script, verbatim |
| 1 | Shogun | Culture purge; the archive grief beat with Oceanus |
| 2 | **Oceanus** | Refugee defense; Titan holds the breach |
| 3 | **Phoenicia** | The comms blackout as a broadcast; Blade delivers the route |
| 4 | Ensemble + Titan | The wall; Voss; Shogun claims the line |
| 5 | Mirage Blade (functional) | The surveillance spire dark; Blade disappears |
| 6 | **Oceanus** | The bio-labs; grief becomes resolve |
| 7 | Shogun + Titan | The foundry; Shogun's discipline cracks; first AXIOM-7 doubt comes from Oceanus's question |
| 8 | **Phoenicia** | The docks; the sky is a radio |
| 9 | All four | The Citadel; the Void Commander; AXIOM-7's choice; Oceanus closes the campaign |

## Consequences and deferred work

- `english.json` is v3.0. The other 14 locale files still hold the v2.0
  story and a mechanical re-translation pass — ideal fragmented-downtime
  work; until then the game falls back to english for any locale whose file
  lags (NarrativeManager falls back to english when a locale file is
  missing, not when it is stale — stale files still load).
- No code changes were required for this restructure; dialogue flows as
  text (the VO mp3 files are wired to nothing yet). When VO playback gets
  wired, per-level audio must be re-cut for the added character lines.