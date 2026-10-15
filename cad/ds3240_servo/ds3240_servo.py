"""DS3240 MG 40 kg standard-size digital servo -> STEP for Onshape.

Dimensions and their sources are in MEASUREMENTS.md. Units: mm.
Frame: origin = centre of the case bottom face, +Z up through the output
spline, +X along the length toward the output spline (and wire exit), +Y across.
Usage: python ds3240_servo.py  ->  ds3240_servo.step next to this file
"""
from pathlib import Path

import cadquery as cq

# case (labelled on the datasheet drawing)
case_l = 40.0
case_w = 20.0
case_h = 40.4         # bottom to the top of the case, horn not included
ear_span = 54.5       # tip to tip
ear_hole_dx = 49.5    # hole centres, along X
ear_hole_dy = 10.0    # hole centres, along Y
ear_z = 27.7          # bottom of the case to the underside of the ears
# scaled off the same drawing (about +-0.3 mm)
ear_w = 19.0
ear_t = 3.1
ear_hole_d = 4.0
spline_x = 9.8        # output axis from the case centre, toward +X
top_chamfer = 2.0     # simplification of the sloped top cover
# assumed, not on the drawing
spline_d = 5.9        # 25T spline, modelled as a plain cylinder
spline_h = 4.0
spline_screw_d = 3.0  # M3 horn screw hole
spline_screw_depth = 4.0

case = (
    cq.Workplane("XY").rect(case_l, case_w).extrude(case_h)
    .faces(">Z").edges("|Y").chamfer(top_chamfer)
)
ears = (
    cq.Workplane("XY").workplane(offset=ear_z)
    .rect(ear_span, ear_w).extrude(ear_t)
    .cut(cq.Workplane("XY").rect(case_l, case_w).extrude(case_h))
    .faces(">Z").workplane()
    .pushPoints([(sx * ear_hole_dx / 2, sy * ear_hole_dy / 2)
                 for sx in (-1, 1) for sy in (-1, 1)])
    .hole(ear_hole_d)
)
body = case.union(ears)
spline = (
    cq.Workplane("XY").workplane(offset=case_h).center(spline_x, 0)
    .circle(spline_d / 2).extrude(spline_h)
    .faces(">Z").workplane().hole(spline_screw_d, spline_screw_depth)
)

asm = (
    cq.Assembly(name="ds3240mg_servo")
    .add(body, name="case", color=cq.Color(0.1, 0.1, 0.1))
    .add(spline, name="output_spline_25T", color=cq.Color(0.8, 0.8, 0.8))
)
out = Path(__file__).with_name("ds3240_servo.step")
asm.export(str(out))
print("wrote", out)
