"""Independent cadquery rebuild of PanTiltMount.fs (defaults parsed from the .fs) for verification.

Reports per-part volume and bounding box (compare with Onshape test_featurescript output) and
checks for interference between parts and servo/board/flashlight envelopes over the tilt and pan range.
Usage: .venv/bin/python cad/pan_tilt_mount/check_pan_tilt.py [--svg out_dir]
Units mm. Frame as in the .fs: origin at base underside centre, +Z pan axis, tilt axis parallel to X.
"""
import json
import math
import re
import sys
from pathlib import Path

import cadquery as cq

HERE = Path(__file__).parent
src = (HERE / "PanTiltMount.fs").read_text()
defaults = src.rsplit("}, {", 1)[1]
p = {k: float(v) for k, v in re.findall(r"(\w+)\s*:\s*([\d.]+)\s*\*?\s*(?:millimeter|degree)?", defaults)}
p.update({k: float(v) for k, v in re.findall(r"(\w+)\s*:\s*(\d+)\s*,", defaults)})


def box(x0, y0, z0, x1, y1, z1):
    return cq.Workplane("XY").box(x1 - x0, y1 - y0, z1 - z0, centered=False).translate((x0, y0, z0))


def cyl(p0, p1, d):
    v = cq.Vector(*p1) - cq.Vector(*p0)
    return cq.Workplane(obj=cq.Solid.makeCylinder(d / 2, v.Length, cq.Vector(*p0), v.normalized()))


T, wall, clr = p["plateThickness"], p["wallThickness"], p["fitClearance"]
above_ear = p["servoHeight"] - p["servoEarZ"]
ear_reach = p["servoSplineOffset"] + p["servoEarSpan"] / 2
bx = p["boardLength"] + 2 * clr + 2 * wall
by = p["boardWidth"] + 2 * clr + 2 * wall
h_in = p["boardStandoff"] + p["boardHeight"] + p["servoClearance"] + p["servoEarZ"] - T
zb = 2 * T + h_in
sx = -p["servoSplineOffset"]
R = p["lightDiameter"] / 2 + p["lightClearance"] + p["ringWall"]
xh = R + p["headClearance"] + T
xo = xh + above_ear + p["hornOffset"]
zy0 = zb + above_ear + p["hornOffset"]
r_sweep = max(math.hypot(p["ringLength"] / 2, R + T + p["cameraPlateHeight"]),
              math.hypot(p["ringLength"] / 2 + p["lightRearLength"], p["lightDiameter"] / 2))
zt = zy0 + T + max(ear_reach, r_sweep) + p["headClearance"]
zc = zt - p["servoSplineOffset"]
cable_y = p["servoWidth"] / 2 + clr + wall + p["cableHoleDiameter"] / 2
L, W = p["servoLength"], p["servoWidth"]

# ---- Base ----
base = (box(-bx/2, -by/2, 0, bx/2, by/2, T)
        .union(box(-bx/2, -by/2, T + h_in, bx/2, by/2, zb))
        .union(box(-bx/2, by/2 - wall, 0, bx/2, by/2, zb))
        .union(box(-bx/2, -by/2, 0, -bx/2 + wall, by/2, zb))
        .union(box(bx/2 - wall, -by/2, 0, bx/2, by/2, zb))
        .union(box(-p["boardLength"]/2, -p["boardWidth"]/2, 0, p["boardLength"]/2, -p["boardWidth"]/2 + p["railWidth"], T + p["boardStandoff"]))
        .union(box(-p["boardLength"]/2, p["boardWidth"]/2 - p["railWidth"], 0, p["boardLength"]/2, p["boardWidth"]/2, T + p["boardStandoff"])))
base = base.cut(box(sx - L/2 - clr, -W/2 - clr, h_in, sx + L/2 + clr, W/2 + clr, zb + T))
base = base.cut(cyl((0, cable_y, h_in), (0, cable_y, zb + T), p["cableHoleDiameter"]))
base = base.cut(cyl((bx/4, by/2 - 2*wall, zb/2), (bx/4, by/2 + wall, zb/2), p["killSwitchDiameter"]))
for ix in (-1, 1):
    for iy in (-1, 1):
        hx, hy = sx + ix * p["servoEarHoleDX"]/2, iy * p["servoEarHoleDY"]/2
        base = base.cut(cyl((hx, hy, h_in), (hx, hy, zb + T), p["servoScrewHole"]))

# ---- Pan Yoke ----
ud = p["uprightDepth"]
z_top_u = zc + p["servoEarSpan"]/2 + wall
yoke = (box(-xo, -ud/2, zy0, xo, ud/2, zy0 + T)
        .union(box(xo - T, -ud/2, zy0, xo, ud/2, z_top_u))
        .union(box(-xo, -ud/2, zy0, -xo + T, ud/2, z_top_u))
        .union(cyl((-xo + T/2, 0, zt), (-xh - p["pivotGap"], 0, zt), p["pivotBossDiameter"])))
yoke = yoke.cut(box(xo - 2*T, -W/2 - clr, zc - L/2 - clr, xo + T, W/2 + clr, zc + L/2 + clr))
yoke = yoke.cut(cyl((-xo - T, 0, zt), (-xh, 0, zt), p["pivotDiameter"]))
yoke = yoke.cut(cyl((0, 0, zy0 - T), (0, 0, zy0 + 2*T), p["hornCenterHole"]))
for iy in (-1, 1):
    for iz in (-1, 1):
        hy, hz = iy * p["servoEarHoleDY"]/2, zc + iz * p["servoEarHoleDX"]/2
        yoke = yoke.cut(cyl((xo - 2*T, hy, hz), (xo + T, hy, hz), p["servoScrewHole"]))
n_h = int(p["hornHoleCount"])
for k in range(n_h):
    a = math.radians(p["hornHoleAngle"] + k * 360 / n_h)
    hx, hy = p["hornHolePCD"]/2 * math.cos(a), p["hornHolePCD"]/2 * math.sin(a)
    yoke = yoke.cut(cyl((hx, hy, zy0 - T), (hx, hy, zy0 + 2*T), p["hornHoleDiameter"]))

# ---- Tilt Head ----
rl = p["ringLength"]/2
z_shelf = zt + R + T
head = (cyl((0, -rl, zt), (0, rl, zt), 2*R)
        .union(box(-xh, -rl, zt - R - T, xh, rl, zt - R + p["ringWall"]/2))
        .union(box(-xh, -rl, zt + R - p["ringWall"]/2, xh, rl, z_shelf))
        .union(box(xh - T, -rl, zt - R - T, xh, rl, z_shelf))
        .union(box(-xh, -rl, zt - R - T, -xh + T, rl, z_shelf))
        .union(box(-p["cameraPlateWidth"]/2, rl - T, z_shelf - T, p["cameraPlateWidth"]/2, rl, z_shelf + p["cameraPlateHeight"])))
z_lens = z_shelf + p["cameraLensHeight"]
head = head.cut(cyl((0, -rl - T, zt), (0, rl + T, zt), p["lightDiameter"] + 2*p["lightClearance"]))
head = head.cut(cyl((0, 0, zt - R - 2*T), (0, 0, zt), p["ringScrewHole"]))
head = head.cut(cyl((-xh - T, 0, zt), (-xh + 2*T, 0, zt), p["pivotDiameter"]))
head = head.cut(cyl((xh - 2*T, 0, zt), (xh + T, 0, zt), p["hornCenterHole"]))
head = head.cut(cyl((0, rl - 2*T, z_lens), (0, rl + T, z_lens), p["cameraLensHole"]))
for k in range(n_h):
    a = math.radians(p["hornHoleAngle"] + k * 360 / n_h)
    hy, hz = p["hornHolePCD"]/2 * math.cos(a), zt + p["hornHolePCD"]/2 * math.sin(a)
    head = head.cut(cyl((xh - 2*T, hy, hz), (xh + T, hy, hz), p["hornHoleDiameter"]))
for ix in (-1, 1):
    for iz in (-1, 1):
        cx, cz = ix * p["cameraHoleDX"]/2, z_lens + iz * p["cameraHoleDZ"]/2
        head = head.cut(cyl((cx, rl - 2*T, cz), (cx, rl + T, cz), p["cameraHoleDiameter"]))

# ---- Envelopes (not printed parts) ----
ear_t = 3.1  # scaled off the datasheet drawing (MEASUREMENTS.md)
pan_servo = (box(sx - L/2, -W/2, zb - p["servoEarZ"], sx + L/2, W/2, zb + above_ear)
             .union(box(sx - p["servoEarSpan"]/2, -p["servoEarWidth"]/2, zb, sx + p["servoEarSpan"]/2, p["servoEarWidth"]/2, zb + ear_t)))
tilt_servo = (box(xo + p["servoEarZ"] - p["servoHeight"], -W/2, zc - L/2, xo + p["servoEarZ"], W/2, zc + L/2)
              .union(box(xo, -p["servoEarWidth"]/2, zc - p["servoEarSpan"]/2, xo + ear_t, p["servoEarWidth"]/2, zc + p["servoEarSpan"]/2)))
board = box(-p["boardLength"]/2, -p["boardWidth"]/2, T + p["boardStandoff"], p["boardLength"]/2, p["boardWidth"]/2, T + p["boardStandoff"] + p["boardHeight"])
light = cyl((0, -rl - p["lightRearLength"], zt), (0, rl + 10, zt), p["lightDiameter"])  # stand-in, 10 mm proud at the front


def vol(a, b):
    try:
        return a.intersect(b).val().Volume()
    except Exception:
        return 0.0


def rot_tilt(obj, deg):
    return obj.rotate((0, 0, zt), (1, 0, zt), deg)


def rot_pan(obj, deg):
    return obj.rotate((0, 0, 0), (0, 0, 1), deg)


report = {"layout_mm": {"base": [round(bx, 2), round(by, 2), round(zb, 2)], "tilt_axis_z": round(zt, 2),
                        "yoke_outer_half_width": round(xo, 2), "head_half_width": round(xh, 2)}}
for name, s in [("Base", base), ("Pan Yoke", yoke), ("Tilt Head", head)]:
    bb = s.val().BoundingBox()
    report[name] = {"volume_mm3": round(s.val().Volume(), 2), "solids": len(s.solids().vals()),
                    "bbox": [[round(bb.xmin, 2), round(bb.ymin, 2), round(bb.zmin, 2)], [round(bb.xmax, 2), round(bb.ymax, 2), round(bb.zmax, 2)]]}

static = {"base~panServo": vol(base, pan_servo), "base~board": vol(base, board), "panServo~board": vol(pan_servo, board),
          "yoke~tiltServo": vol(yoke, tilt_servo), "head~light": vol(head, light)}
worst_tilt = {}
for deg in range(-90, 91, 15):
    h, lt = rot_tilt(head, deg), rot_tilt(light, deg)
    for pair, v in {"head~yoke": vol(h, yoke), "head~tiltServo": vol(h, tilt_servo),
                    "light~yoke": vol(lt, yoke), "light~tiltServo": vol(lt, tilt_servo)}.items():
        worst_tilt[pair] = max(worst_tilt.get(pair, 0.0), v)
worst_pan = 0.0
moving = yoke.union(tilt_servo).union(head)
for deg in range(0, 181, 30):
    worst_pan = max(worst_pan, vol(rot_pan(moving, deg), base))
report["interference_mm3"] = {"static": {k: round(v, 3) for k, v in static.items()},
                              "max_over_tilt_-90..90": {k: round(v, 3) for k, v in worst_tilt.items()},
                              "max_moving~base_over_pan_0..180": round(worst_pan, 3)}
print(json.dumps(report, indent=1))

if "--svg" in sys.argv:
    out = Path(sys.argv[sys.argv.index("--svg") + 1])
    out.mkdir(parents=True, exist_ok=True)
    asm = base.union(yoke).union(head)
    for nm, d in [("iso", (1, -1.4, 0.9)), ("front", (0, -1, 0)), ("side", (1, 0, 0))]:
        cq.exporters.export(asm, str(out / f"{nm}.svg"), opt={"projectionDir": d, "width": 700, "height": 600,
                                                               "showHidden": False, "strokeWidth": 0.4})
    cq.exporters.export(base, str(HERE / "base.step"))
    cq.exporters.export(yoke, str(HERE / "pan_yoke.step"))
    cq.exporters.export(head, str(HERE / "tilt_head.step"))
    print("wrote SVGs to", out, "and STEP files next to this script")
