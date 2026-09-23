# 🚚 Codex Asset Delivery Guide

This guide provides the exact directory structure and naming conventions required for assets to be integrated into the Axiom Force project without requiring code changes.

## 📂 Directory Structure
Please save all generated assets into the following paths. **Case sensitivity matters.**

### 1. Operatives (Hero Characters)
**Path**: `assets/models/hq/`
**Naming Convention**: `[operative_name].glb` (Lowercase, underscores for spaces)
- `cyber_shogun.glb`
- `solaria.glb`
- `oceanus.glb`
- `surya_prime.glb`
- `mirage_blade.glb`
- `apex_titan.glb`

### 2. Enemy Units
**Path**: `assets/models/enemies/`
**Naming Convention**: `[enemy_type].glb`
- `vanguard.glb`
- `heavy.glb`
- `stalker.glb`

### 3. Tactical Props & Environment
**Path**: `assets/models/props/`
**Naming Convention**: `[object_name].glb`
- `explosive_barrel.glb`
- `hazard_zone_marker.glb`
- `timed_trap.glb`
- `switch_object.glb`
- `destructible_crate.glb`

---

## 🎬 Animation Naming Convention
To ensure animations trigger correctly in the game engine, please use these exact names for the animation clips within the `.glb` files:

### Operative Standard Set:
- `Idle`
- `Running`
- `Walking`
- `Jumping`
- `Death`
- `Hit`

### Operative Combat Set:
- `Thrust_Slash` (Primary Melee)
- `Karate_Strike` (Secondary Melee)
- `Attack` (Generic ability animation for non-melee)

---

## 🛠️ Technical Checklist for Delivery
- [ ] **Format**: `.glb` (Binary GLTF).
- [ ] **Materials**: PBR standard.
- [ ] **Emissives**: Must be on a separate map or clearly defined so the game can pulse them.
- [ ] **Scale**: 1 unit = 1 meter.
- [ ] **Origin**: Model origin must be at the base of the character/object (feet/bottom).
- [ ] **Rigging**: Standard humanoid rig for all operatives.

## 🎨 Visual Reference
For the visual style, colors, and themes, please refer to:
`docs/ASSET_SPEC.md`
