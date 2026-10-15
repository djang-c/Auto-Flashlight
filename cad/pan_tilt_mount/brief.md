# Pan–tilt task-light mount: design brief

- Date: 2026-10-15 · Onshape doc `8e416b4f63997dc15a7ebf6e`, workspace `2dc80b108362e6f175e0e9be`
- Feature Studio: `PanTiltMount` (eid: _set after create_)
- Units: mm. Frame: origin at the centre of the base underside. +Z is up and is the pan axis. The tilt axis is parallel to X. At tilt 0 the light and camera look along +Y.
- Naming follows the build plan ("Safety and presentation"): pan–tilt platform / task light, not "sentry" or "turret".

## Purpose
Two-axis aiming mount for the Voice-Directed Robotic Task Light. The camera rides on the tilt head with the light, so the plan's centring loop (`e_x`, `e_y` → Δθ) sees the error relative to the beam.

| Part | What it does |
|---|---|
| **Base** | Box for the Alif E8 DevKit. The board sits on two rails, raised to clear the bottom-side J16 camera connector. The back is open for USB, power and the camera ribbon. The pan servo drops through a window in the top plate and hangs above the board on its ears (4 × M3 self-tapping screws). There's a Ø10 cable hole and a Ø12 front-wall hole for the physical kill switch the plan requires. |
| **Pan Yoke** | U-bracket bolted to the pan servo horn. The tilt servo goes through a window in the +X upright, with its ears screwed to the outside face. The −X upright has an M4 pivot bolt and a spacer boss. |
| **Tilt Head** | Flashlight clamp ring on the tilt axis (balanced), with a thumb-screw hole underneath. The +X side plate bolts to the tilt horn; the −X side plate rides on the pivot. A camera plate above the ring has a lens hole and 4 mounting holes. |

Derived, not set directly: base height (from board, standoff and servo depth), yoke width (from ring size and servo protrusion), and **tilt-axis height**. The tilt axis is set high enough that the head, including a flashlight sticking out behind the ring, can turn a full 360° without hitting the yoke. Tilt limits are therefore enforced in software (plan: angle clamps, slew limit); geometry can't jam the mechanism.

## Inputs and sources
| Item | Value | Status | Source |
|---|---|---|---|
| DS3240 case | 40 × 20 × 40.4, ears at 27.7, span 54.5, holes 49.5 × 10 | datasheet drawing of a sibling model plus Servo Database | `cad/ds3240_servo/MEASUREMENTS.md` [A][B] |
| DS3240 ear width, spline offset | 19, 9.8 (±0.3, scaled off the drawing) | scaled | same |
| DS3240MG vs DS3240SG | MG is assumed to share the SG case | **UNVERIFIED** | |
| Horn face above case top 6; centre hole 7; 4 × Ø3.2 on Ø14 | — | **UNVERIFIED**, measure the horn you'll use | none |
| Alif E8 DevKit outline | 100 × 70 × 15 placeholder | **UNVERIFIED**, measure | DK-E8 user guide not readable (2026-10-15) |
| J16 camera connector on the bottom side | standoff 8 mm placeholder | location from build plan; height **UNVERIFIED** | build plan, "Start from proven code" |
| Flashlight | Ø25.4 body, 40 mm behind the ring | **UNVERIFIED**, measure | none |
| MT9M114 camera module | lens hole Ø10, 4 × Ø2.2 on 20 × 20 | **UNVERIFIED**, measure | none |
| Kill switch | Ø12 panel hole | assumed (common 12 mm pushbutton) | |

## Verification (2026-10-15)
Independent cadquery rebuild (`check_pan_tilt.py`, defaults parsed from the `.fs`):
- Volumes: Base 106,565.63 mm³, Pan Yoke 21,951.49 mm³, Tilt Head 32,569.04 mm³. One solid each.
- Base is 106.8 × 76.8 × 58.7 mm; tilt axis at Z = 139.85 mm; overall top at Z = 191.85 mm.
- Interference against stand-in shapes for the servo cases and ears, the board and the flashlight: 0.000 mm³ for every static pair. Also 0.000 for head and flashlight vs yoke and tilt servo over tilt −90…+90° (15° steps), and for the moving assembly vs the base over pan 0…180° (30° steps).
- The first run found the flashlight tail grazing the yoke at ±90° (0.24 mm³). Fixed by adding the `lightRearLength` setting to the swing-radius calculation.
- Onshape FeatureScript run: pending. The MCP needed re-authentication.

## Open questions / assumptions
1. Measure: board outline and the height of its bottom-side parts; the horn's bolt circle and its face height above the case; flashlight diameter and length; camera module holes and lens.
2. Camera ribbon length from J16 to the head: it must allow for pan rotation. Not checked.
3. Servo wiring twists with pan. Limit pan in software to about ±150° from centre.
4. Strength, tipping stability under servo torque (DS3240 is rated 40 kg·cm class; the base is not weighted), and printability are **UNVERIFIED**. Clamp or weight the base for the demo.
5. Hardware safety: these are mechanical parts only. Servo limits, slew limits, the e-stop, and powering the servos from an external 5 V supply with a shared ground are covered in the build plan and the `hardware-safety` skill. Nothing here commands hardware.
