# 📦 Axiom Force: Asset Requirement Specification

This document outlines the visual and technical requirements for all 3D assets, textures, and VFX for the Axiom Force vertical slice. 

## 🎨 Visual Style Guide: "Neon Pulse"
The overall aesthetic is a high-contrast Cyberpunk vision. 
- **Primary Palette**: Matte Black, Dark Obsidian, and Industrial Grey.
- **Accents**: High-emissive Neon (Cyan, Magenta, Yellow, Orange).
- **Materials**: PBR (Physically Based Rendering). High-gloss surfaces for luxury corporate areas, matte carbon-fiber for tactical gear.

---

## 1. The Operative Roster (High-Fidelity Characters)
Each operative requires a full 3D model with a unique silhouette and a standard animation set: **Idle, Run, Jump, Attack_Melee, Attack_Special, Hit, and Death.**

| Operative | Ethnicity/Theme | Visual Key-Notes | Key Ability Visual |
| :--- | :--- | :--- | :--- |
| **Cyber Shogun** | Japanese | Traditional armor fused with carbon fiber; glowing katana. | White-hot blade streaks. |
| **Solaria** | Latina | Solar-themed plating; gold/white emissives; floating heat-sinks. | Magenta solar flares. |
| **Oceanus** | Polynesian | Fluid-metal armor; deep blue/cyan bioluminescent patterns. | Cyan plasma waves. |
| **Surya Prime** | Indian | Ornate high-tech gold armor; floating holographic mandalas. | Golden cosmic pulses. |
| **Mirage Blade** | Arab | Lightweight stealth suit; shifting mirrored surfaces. | Yellow holographic streaks. |
| **Apex Titan** | African | Heavy industrial power-armor; massive proportions; orange accents. | Orange EM pulses. |

## 2. Enemy Units (The Adversaries)
Enemies should look like standardized security drones or corporate mercenaries to contrast with the unique Operatives.

| Enemy Type | Role | Visual Requirement | Animation Focus |
| :--- | :--- | :--- | :--- |
| **Vanguard** | Standard | Sleek humanoid android; standard tactical gear. | Quick strikes, agile movement. |
| **Heavy** | Tank | Bulky armored frame; shoulder-mounted cannons. | Slow, heavy steps; recoil animations. |
| **Stalker** | Assassin | Quadrupedal or digitigrade legs; cloaking effect. | Prowling animation; sudden lunges. |

## 3. Tactical Environment (Interactive Objects)
These objects must be visually distinct to signal interactability.

| Object | Visual Requirement | Interaction Cue |
| :--- | :--- | :--- |
| **Explosive Barrel** | Industrial canisters with "Hazard" warnings. | Pulsing red light when health is low. |
| **Hazard Zone** | Floor-grid patterns or leaking pipes/cables. | Emissive floor glow (Red=Burn, Blue=Slow). |
| **Timed Trap** | Ceiling-mounted spikes or floor-plates. | Yellow "Warning" $\rightarrow$ Red "Active". |
| **Switch Object** | Cyber-terminals or physical pressure plates. | Changes color from Red to Green when active. |
| **Destructible Cover** | Carbon-fiber crates, server racks, concrete pillars. | Fragmented "shatter" mesh for destruction. |

## 4. Architecture & Level Assets (The World)
Modular kit for building futuristic urban environments.

*   **Modular Kit**:
    *   Wall panels (Matte black with neon trim).
    *   Floors (Industrial grating, polished obsidian).
    *   Ceilings (Exposed cabling, ventilation shafts).
*   **Hero Assets**:
    *   **The Virtual Forge**: A digital, grid-like arena for the Training Protocol.
    *   **The Extraction Point**: A high-tech landing pad with holographic markers.
    *   **Corporate Hubs**: High-ceiling lobbies with holographic advertisements.

## 5. "Neon Pulse" VFX & UI (The Polish)
Diegetic elements integrated into the 3D world.

*   **Combat VFX**:
    *   **Ability Pulses**: Spherical shockwaves matching Operative colors.
    *   **Damage Pop-ups**: Floating holographic numbers with a "glitch" effect.
    *   **Hit Flash**: Sharp, white emissive flash on enemy meshes.
*   **Diegetic HUD**:
    *   **Player Health Bar**: Floating 3D bar following the character.
    *   **Ability Cooldowns**: Holographic rings filling up around the character's feet.

---

## 🛠️ Technical Requirements for Codex
- **Recommended Tooling**: Blender (for modeling, rigging, and animation).
- **Format**: `.glb` or `.gltf`.
- **Scale**: 1 unit = 1 meter.
- **Materials**: PBR. Emissives must be in a separate map for pulsing effects.
- **Animations**: Use a standard rig for all Operatives where possible to allow for animation blending.
