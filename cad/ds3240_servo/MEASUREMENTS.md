# DS3240 MG digital servo (40 kg, standard size)

Built on 2026-10-15. **None of these values were measured on a real servo.** Check against your part before printing a mount.

Open in Onshape: **Create → Import** `ds3240_servo.step`. Units are mm.
Frame: the origin is the centre of the case bottom face. +Z points up through the output spline. +X runs along the length toward the output spline, and +Y runs across.

## Sources
- **[A]** DSSERVO DS3235 / DS3235-180 / DS3235-270 datasheet, §1 "Dimensions" (drawing, p. 2). https://hajim.rochester.edu/me/sites/kelley/me240/DS3235-270_datasheet.pdf
  This is a sibling model, not the DS3240. Its drawing shows a 40.4 mm height, but its own spec table says "Size 40\*20\*38.5mm".
- **[B]** Servo Database, DSSERVO DS3240 SG: "L 39.9 mm, W 20.1 mm, H 40.4 mm", "60.0 g", 25T spline. https://servodatabase.com/servo/dsservo/ds3240-sg
  This is the stainless-gear variant. Its size matches drawing [A] to within 0.1 mm, which is why [A] is used for the case.
- **[C]** eBay listing "DS3240 Digital Servo 180°/270° 40KG Metal Gear": 40 × 20 × 40.5 mm. https://www.ebay.de/itm/226244787627 (seen in search results only, not opened)

| Parameter | Value (mm) | Source |
|---|---|---|
| Case length | 40 | [A] labelled (39.9 in [B]) |
| Case width | 20 | [A] labelled (20.1 in [B]) |
| Case height, bottom to top of case, no horn | 40.4 | [A] labelled, [B] |
| Mounting ears, tip to tip | 54.5 | [A] labelled |
| Ear hole centres, lengthwise | 49.5 | [A] labelled |
| Ear hole centres, across | 10 | [A] labelled |
| Case bottom to underside of ears | 27.7 | [A] labelled |
| Ear thickness | ~3.1 | [A] scaled off the drawing |
| Ear width | ~19 | [A] scaled |
| Ear hole diameter | ~4.0 | [A] scaled |
| Output axis from case centre (toward the wire end) | ~9.8 (about 10.2 from that end) | [A] scaled |
| Output spline | 25T | [A] §7, [B] |
| Weight | 60 g | [B] |
| Wire length | 300 | [B] |

"Scaled" means read off the drawing at 6.25 px/mm. The labelled 40, 54.5 and 27.7 mm dimensions come out within 0.3 mm at that scale, so treat scaled values as ±0.3 mm.

## Modelling choices (not data)
- The output spline is modelled as a plain Ø5.9 × 4 mm cylinder with an M3 × 4 hole. The datasheet gives none of these values (UNVERIFIED). Only the axis position matters for mating.
- The sloped top cover is simplified to a 2 mm chamfer on the two short top edges.
- The ear holes are closed circles. On the real servo they are open notches to the outer edge.
- Not modelled: the wire and its grommet, the label recess, the horn and the bottom-cover lip.

Regenerate the model: `pip install cadquery`, then `python ds3240_servo.py`.
