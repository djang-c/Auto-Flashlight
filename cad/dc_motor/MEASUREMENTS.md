# DC drive motor: Greartisan 12 V 100 RPM, 37 mm gearbox

Copied on 2026-10-14 from `RoboTrashCan/trash-can-robot/cad/params.scad` (section "Drive motors").
The source tags are carried over from that file. Only the `[MEASURED]` values were checked on the real part.

Open in Onshape: **Create → Import** (or drag and drop) `dc_motor.step`. Units are mm.
Frame: the shaft axis is +Z, the gearbox face is at Z = 0, and the 12 o'clock hole is on +Y. The D flat faces +X.

| Parameter | Value (mm) | Source tag |
|---|---|---|
| Gearbox diameter | 37 | [DEFAULT] spec |
| Gearbox length | 24.5 | [DEFAULT] spec |
| Motor can diameter | 36.2 | [DEFAULT] spec |
| Motor can length | 33.3 | [DEFAULT] spec |
| Tail (brush cap / terminals) | 6 | [DEFAULT] |
| **Overall body length** (gearbox + can + tail) | **63.8** | derived |
| Shaft diameter (D-shaft) | 6 | [DEFAULT] spec |
| Shaft across the D flat | 5.5 | [DEFAULT] spec |
| Shaft length past the face | 14 | [DEFAULT] spec |
| Shaft offset from gearbox axis | 0 (centred) | [MEASURED] photo |
| Gearbox face | flush | [MEASURED] |
| Face mounting holes | 6× M3, 60° apart, one at 12 o'clock | [MEASURED] |
| Hole PCD | ~28 (bracket slots cover 27–32) | [MEASURED ~28, tape] |
| Safe screw depth into gearbox | 4 | [LISTING] deeper screws can hit the gears |
| Raised boss around shaft | Ø12 | [PHOTO] |

## Modelling choices (not measurements)
- The holes are modelled as plain Ø3 × 4 mm deep, not threaded.
- The D flat runs the full 14 mm of the shaft. Its real length was not recorded.
- The tail uses the can's Ø36.2. Terminals are not modelled.
- The Ø12 boss is **not modelled** because its height was never recorded.

Regenerate the model: `pip install cadquery`, then `python dc_motor.py`.
