"""Greartisan 12V 100RPM 37mm gearmotor -> STEP for Onshape.

Dimensions copied from RoboTrashCan trash-can-robot/cad/params.scad (see
MEASUREMENTS.md for each value's source tag). Units: mm.
Frame: shaft axis = +Z, gearbox face at Z = 0, 12 o'clock hole on +Y.
Usage: python dc_motor.py  ->  dc_motor.step next to this file
"""
from pathlib import Path

import cadquery as cq

gearbox_d = 37.0
gearbox_l = 24.5
motor_d = 36.2
motor_l = 33.3
motor_tail_l = 6.0
shaft_d = 6.0
shaft_flat = 5.5      # across the D flat
shaft_l = 14.0
shaft_offset = 0.0    # shaft is centred on the gearbox
gb_holes_pcd = 28.0
gb_hole_n = 6
gb_hole_d = 3.0       # M3 nominal (modelled plain, not threaded)
gb_hole_depth = 4.0   # = gb_thread, the safe screw depth

gearbox = (
    cq.Workplane("XY").circle(gearbox_d / 2).extrude(-gearbox_l)
    .faces(">Z").workplane()
    .polarArray(gb_holes_pcd / 2, 90, 360, gb_hole_n)
    .hole(gb_hole_d, gb_hole_depth)
)
motor = (
    cq.Workplane("XY").workplane(offset=-gearbox_l)
    .circle(motor_d / 2).extrude(-(motor_l + motor_tail_l))
)
# D-shaft: circle minus everything past the flat (flat faces +X)
shaft = (
    cq.Workplane("XY").center(0, shaft_offset).circle(shaft_d / 2).extrude(shaft_l)
    .cut(cq.Workplane("XY").center(shaft_flat, shaft_offset)
         .rect(shaft_d, shaft_d).extrude(shaft_l))
)

asm = (
    cq.Assembly(name="greartisan_37mm_gearmotor")
    .add(gearbox, name="gearbox", color=cq.Color(0.75, 0.75, 0.75))
    .add(motor, name="motor_can", color=cq.Color(0.55, 0.55, 0.6))
    .add(shaft, name="d_shaft", color=cq.Color(0.3, 0.3, 0.3))
)
out = Path(__file__).with_name("dc_motor.step")
asm.export(str(out))
print("wrote", out)
