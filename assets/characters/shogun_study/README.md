# Cyber Shogun selection-screen study

This is a separate, experimental hand-drawn art variant. The original artwork in
`assets/characters/rendered` is preserved and still used by campaign gameplay.
Only Cyber Shogun's selection-screen card is replaced with this interactive study.
Other operatives keep their existing cards.

Select Cyber Shogun, press Right to enter the preview controls, then use arrows
or Tab and Enter to activate a movement or ability. Existing tactical loadout
buttons also trigger the corresponding study pose.

The preview uses illustrated key poses, procedural displacement, jump arcs and a
slam effect. It is an animation/design audition, not a combat simulator.
Walking/running use dedicated eight-frame cycles in `locomotion.png`, with a
fixed drawing anchor and separate walk/run cadence. They preview in place,
without the old sideways oscillation. Parry and ground slam are
explicitly labeled proposed ability animations: they are listed in selection
but are not implemented by the campaign's CyberShogun controller.
This preview does not write progression, sessions, stats or selected-character
save data. It does not deploy the new design into campaign scenes.

`concept.png` and `poses.png` were created using the built-in image generation
tool, using the user-reviewed futuristic human Cyber Shogun concept as reference.
No historical 3D model was used.

Art prompt: consistent hand-drawn human cyber-samurai with black swept hair,
visible face, charcoal combat tunic, red sash, restrained futuristic shoulder
armor and cyan katana; polished ink contours and cel shading; sixteen full-body
right-facing key poses in a transparent four-column, four-row sheet. Pose order:
idle x2, walk x2, run x2, jump launch/airborne, katana windup/slash, martial
windup/kick, blade dash, parry, slam airborne/landing. Keep weapons and cloth
inside each cell with transparent gutters.

Locomotion revision prompt (built-in image generation, referencing `poses.png`):
same character and costume, sheathed sword, strict right-facing side view;
eight sequential walk poses (contact/down/passing/up on each leg) followed by
eight run poses (contact/compression/passing/flight on each leg); opposing arm
swing, consistent proportions, fixed hip alignment and contact baseline;
transparent four-by-four sheet. Existing combat, jump and idle artwork is kept.
