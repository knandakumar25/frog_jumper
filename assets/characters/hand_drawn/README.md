# Hand-drawn operative selection studies

These five transparent pose sheets extend the approved Cyber Shogun hand-drawn
style to Surya Prime, Oceanus, Mirage Blade, Apex Titan, and Aura Phoenicia.
Cyber Shogun's existing sheets remain in `../shogun_study`.

All six original flat designs, animations and generator files are preserved in
`../rendered` and `tools/art`. Campaign rendering still uses those original
SpriteFrames. The hand-drawn rollout currently applies to character selection,
where movement and each operative's four named abilities can be auditioned.

The shared preview keeps the existing jump arcs, dash travel, action durations,
walk/run stride durations and impact effects. Each new sheet supplies sixteen
key poses: idle; four walk poses; four run poses; jump; primary windup/release;
secondary; dash; slam preparation/landing. Four locomotion keys span the same
stride duration as the original eight frames. This is an art/animation preview,
not an implementation of combat damage, cooldowns or character-specific mechanics.

`OperativeStudyAtlas` reads transparent gutters and visible pose bounds from the
PNG without changing the file. Fixed source anchors and grounded row baselines
preserve drawing scale across standing, crouching and airborne poses. Texture
and extraction results are cached so switching operatives does not rescan PNGs.

## Art provenance and prompts

Generated with the built-in image-generation tool. The only visual reference was
the approved `../shogun_study/concept.png`, used for ink/cel-shading style. No
deleted model, legacy portrait or original flat image was used as an art input.

Shared prompt: original futuristic human operative, professional hand-drawn ink
contours, restrained cel shadows, visible expressive face, consistent anatomy,
flowing cloth, practical combat equipment; sixteen right-facing full-body poses
on a transparent four-by-four sheet with padded cells and a fixed foot baseline.
Pose order: idle, walk contact/passing/opposite contact/opposite passing, run
contact/passing/opposite contact/flight, jump, primary windup/release, secondary,
dash, slam preparation/landing. No labels, borders, scenery or 3D rendering.

Character-specific prompts:

- **Surya Prime / Arjun Varma:** Indian man, broad athletic defender, warm brown
  skin, short wavy black hair and neat beard; ivory/amber armor over navy, saffron
  waist cloth, sun emblem, forearm hard-light shield. Shield bash, golden Aegis,
  armored charge, amber ground impact.
- **Oceanus / Kiana Vaea:** Polynesian woman, athletic build, medium-deep brown
  skin, long dark braid; marine blue suit, teal armor, seafoam sash, hydro emitters.
  Water torrent, teal vortex, aqua propulsion, watery ground impact.
- **Mirage Blade / Zayd Al-Mansoor:** Middle Eastern man, lean build, olive skin,
  black hair, stubble, short hood; graphite suit, emerald-lined cloak, silver
  bracers, twin phase daggers. Teleport slash, decoy, phase lunge, dagger plunge.
- **Apex Titan / King Tariq Nzinga:** Black African man, powerful build, deep brown
  skin, cropped hair and beard; graphite/steel-blue armor, brass fittings, violet
  gauntlets and waist sash. Tectonic slam, magnetic pull, heavy charge, seismic drop.
- **Aura Phoenicia / Maya Khoury:** Lebanese woman, graceful athletic build, olive
  skin, swept dark auburn hair; plum suit, magenta/copper armor, violet coat tails,
  prism bracelets. Sonic pulse, resonant field, wave glide, concentric impact rings.
