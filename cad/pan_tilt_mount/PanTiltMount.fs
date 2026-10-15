FeatureScript 3095;
import(path : "onshape/std/common.fs", version : "3095.0");

// Pan-tilt task-light mount: Base (E8 board + pan servo), Pan Yoke (tilt servo + pivot), Tilt Head (light ring + camera plate).
// Two DS3240MG servos. Frame: origin = centre of base underside, +Z up = pan axis, tilt axis parallel to X,
// light and camera look along +Y at tilt 0. Units mm. Sources / UNVERIFIED values: cad/pan_tilt_mount/brief.md

function bodies(context is Context, ids is array) returns Query
{
    var qs = [];
    for (var i in ids)
        qs = append(qs, qCreatedBy(i, EntityType.BODY));
    return qUnion(qs);
}

function box(context is Context, id is Id, x0, y0, z0, x1, y1, z1)
{
    fCuboid(context, id, { "corner1" : vector(x0, y0, z0), "corner2" : vector(x1, y1, z1) });
}

function cyl(context is Context, id is Id, p0 is Vector, p1 is Vector, d)
{
    fCylinder(context, id, { "bottomCenter" : p0, "topCenter" : p1, "radius" : d / 2 });
}

// BEGIN BUILD
export function buildPanTiltMount(context is Context, id is Id, d is map)
{
    const T = d.plateThickness;
    const wall = d.wallThickness;
    const clr = d.fitClearance;
    const z0 = 0 * millimeter;
    // servo geometry, local frame: case bottom at 0, spline on +Z, spline axis offset along local +X
    const aboveEar = d.servoHeight - d.servoEarZ;       // case top above the ear underside
    const earReach = d.servoSplineOffset + d.servoEarSpan / 2;   // spline axis to far ear tip

    // ---- derived layout ----
    const bx = d.boardLength + 2 * clr + 2 * wall;
    const by = d.boardWidth + 2 * clr + 2 * wall;
    const hIn = d.boardStandoff + d.boardHeight + d.servoClearance + d.servoEarZ - T;
    const zb = 2 * T + hIn;                              // base top surface (pan servo ears rest here)
    const sx = -d.servoSplineOffset;                     // pan servo case centre x (spline on the Z axis)
    const R = d.lightDiameter / 2 + d.lightClearance + d.ringWall;
    const xh = R + d.headClearance + T;                  // head side-plate outer face
    const xo = xh + aboveEar + d.hornOffset;             // yoke upright outer face
    const zy0 = zb + aboveEar + d.hornOffset;            // yoke plate underside (on pan horn)
    // head swing radius: camera plate top corner, or the flashlight tail sticking out behind the ring
    const rSweep = max(sqrt((d.ringLength / 2) ^ 2 + (R + T + d.cameraPlateHeight) ^ 2),
                       sqrt((d.ringLength / 2 + d.lightRearLength) ^ 2 + (d.lightDiameter / 2) ^ 2));
    const zt = zy0 + T + max(earReach, rSweep) + d.headClearance;   // tilt axis height
    const zc = zt - d.servoSplineOffset;                 // tilt servo case centre z
    const cableY = d.servoWidth / 2 + clr + wall + d.cableHoleDiameter / 2;

    if (bx / 2 - wall < earReach + clr)
        throw regenError("Base too short for the pan servo ears: increase board length.", ["boardLength"]);
    if (by / 2 - wall < d.servoEarWidth / 2 + clr || cableY + d.cableHoleDiameter / 2 > by / 2 - wall)
        throw regenError("Base too narrow for the pan servo and cable hole: increase board width.", ["boardWidth"]);
    if (d.killSwitchDiameter > zb - 2 * T)
        throw regenError("Kill-switch hole taller than the front wall.", ["killSwitchDiameter"]);
    if (d.hornHolePCD / 2 + d.hornHoleDiameter / 2 > d.uprightDepth / 2 || d.hornCenterHole >= d.hornHolePCD - d.hornHoleDiameter)
        throw regenError("Horn hole pattern does not fit the yoke/head plates.", ["hornHolePCD"]);
    if (d.uprightDepth < d.servoEarWidth)
        throw regenError("Upright narrower than the servo ears.", ["uprightDepth"]);
    if (d.uprightDepth > d.ringLength)
        throw regenError("Ring shorter than the horn plate: increase ring length.", ["ringLength"]);
    if (d.cameraHoleDX / 2 + d.cameraHoleDiameter >= d.cameraPlateWidth / 2 ||
        d.cameraLensHeight + d.cameraHoleDZ / 2 + d.cameraHoleDiameter >= d.cameraPlateHeight ||
        d.cameraLensHeight - d.cameraHoleDZ / 2 - d.cameraHoleDiameter <= T)
        throw regenError("Camera hole pattern does not fit the camera plate.", ["cameraPlateHeight"]);
    if (d.pivotBossDiameter <= d.pivotDiameter)
        throw regenError("Pivot boss must be larger than the pivot hole.", ["pivotBossDiameter"]);

    // ================= Base =================
    box(context, id + "bFloor", -bx / 2, -by / 2, z0, bx / 2, by / 2, T);
    box(context, id + "bTop", -bx / 2, -by / 2, T + hIn, bx / 2, by / 2, zb);
    box(context, id + "bFront", -bx / 2, by / 2 - wall, z0, bx / 2, by / 2, zb);
    box(context, id + "bLeft", -bx / 2, -by / 2, z0, -bx / 2 + wall, by / 2, zb);
    box(context, id + "bRight", bx / 2 - wall, -by / 2, z0, bx / 2, by / 2, zb);
    box(context, id + "bRail0", -d.boardLength / 2, -d.boardWidth / 2, z0, d.boardLength / 2, -d.boardWidth / 2 + d.railWidth, T + d.boardStandoff);
    box(context, id + "bRail1", -d.boardLength / 2, d.boardWidth / 2 - d.railWidth, z0, d.boardLength / 2, d.boardWidth / 2, T + d.boardStandoff);
    // UNION merges only "tools"; pieces overlap because face-touching bodies do not merge
    opBoolean(context, id + "bUnion", { "tools" : bodies(context, [id + "bFloor", id + "bTop", id + "bFront", id + "bLeft", id + "bRight", id + "bRail0", id + "bRail1"]),
                "operationType" : BooleanOperationType.UNION });

    var bCuts = [id + "bWin", id + "bCable", id + "bKill"];
    box(context, id + "bWin", sx - d.servoLength / 2 - clr, -d.servoWidth / 2 - clr, T + hIn - T, sx + d.servoLength / 2 + clr, d.servoWidth / 2 + clr, zb + T);
    cyl(context, id + "bCable", vector(z0, cableY, T + hIn - T), vector(z0, cableY, zb + T), d.cableHoleDiameter);
    cyl(context, id + "bKill", vector(bx / 4, by / 2 - 2 * wall, zb / 2), vector(bx / 4, by / 2 + wall, zb / 2), d.killSwitchDiameter);
    for (var i = 0; i < 4; i += 1)
    {
        const hx = sx + (i < 2 ? -1 : 1) * d.servoEarHoleDX / 2;
        const hy = (i % 2 == 0 ? -1 : 1) * d.servoEarHoleDY / 2;
        cyl(context, id + ("bScrew" ~ i), vector(hx, hy, T + hIn - T), vector(hx, hy, zb + T), d.servoScrewHole);
        bCuts = append(bCuts, id + ("bScrew" ~ i));
    }
    opBoolean(context, id + "bCut", { "tools" : bodies(context, bCuts), "targets" : qCreatedBy(id + "bFloor", EntityType.BODY),
                "operationType" : BooleanOperationType.SUBTRACTION });
    setProperty(context, { "entities" : qCreatedBy(id + "bFloor", EntityType.BODY), "propertyType" : PropertyType.NAME, "value" : "Base" });

    // ================= Pan Yoke =================
    const ud = d.uprightDepth;
    const zTopU = zc + d.servoEarSpan / 2 + wall;
    box(context, id + "yPlate", -xo, -ud / 2, zy0, xo, ud / 2, zy0 + T);
    box(context, id + "yRight", xo - T, -ud / 2, zy0, xo, ud / 2, zTopU);
    box(context, id + "yLeft", -xo, -ud / 2, zy0, -xo + T, ud / 2, zTopU);
    cyl(context, id + "yBoss", vector(-xo + T / 2, z0, zt), vector(-xh - d.pivotGap, z0, zt), d.pivotBossDiameter);
    opBoolean(context, id + "yUnion", { "tools" : bodies(context, [id + "yPlate", id + "yRight", id + "yLeft", id + "yBoss"]),
                "operationType" : BooleanOperationType.UNION });

    var yCuts = [id + "yWin", id + "yPivot", id + "yHornC"];
    box(context, id + "yWin", xo - 2 * T, -d.servoWidth / 2 - clr, zc - d.servoLength / 2 - clr, xo + T, d.servoWidth / 2 + clr, zc + d.servoLength / 2 + clr);
    cyl(context, id + "yPivot", vector(-xo - T, z0, zt), vector(-xh, z0, zt), d.pivotDiameter);
    cyl(context, id + "yHornC", vector(z0, z0, zy0 - T), vector(z0, z0, zy0 + 2 * T), d.hornCenterHole);
    for (var i = 0; i < 4; i += 1)
    {
        const hy = (i < 2 ? -1 : 1) * d.servoEarHoleDY / 2;
        const hz = zc + (i % 2 == 0 ? -1 : 1) * d.servoEarHoleDX / 2;
        cyl(context, id + ("yScrew" ~ i), vector(xo - 2 * T, hy, hz), vector(xo + T, hy, hz), d.servoScrewHole);
        yCuts = append(yCuts, id + ("yScrew" ~ i));
    }
    for (var k = 0; k < d.hornHoleCount; k += 1)
    {
        const a = d.hornHoleAngle + k * 360 * degree / d.hornHoleCount;
        const hx = d.hornHolePCD / 2 * cos(a);
        const hy = d.hornHolePCD / 2 * sin(a);
        cyl(context, id + ("yHorn" ~ k), vector(hx, hy, zy0 - T), vector(hx, hy, zy0 + 2 * T), d.hornHoleDiameter);
        yCuts = append(yCuts, id + ("yHorn" ~ k));
    }
    opBoolean(context, id + "yCut", { "tools" : bodies(context, yCuts), "targets" : qCreatedBy(id + "yPlate", EntityType.BODY),
                "operationType" : BooleanOperationType.SUBTRACTION });
    setProperty(context, { "entities" : qCreatedBy(id + "yPlate", EntityType.BODY), "propertyType" : PropertyType.NAME, "value" : "Pan Yoke" });

    // ================= Tilt Head =================
    const rl = d.ringLength / 2;
    const zShelf = zt + R + T;
    cyl(context, id + "hRing", vector(z0, -rl, zt), vector(z0, rl, zt), 2 * R);
    box(context, id + "hBridge", -xh, -rl, zt - R - T, xh, rl, zt - R + d.ringWall / 2);
    box(context, id + "hShelf", -xh, -rl, zt + R - d.ringWall / 2, xh, rl, zShelf);
    box(context, id + "hRight", xh - T, -rl, zt - R - T, xh, rl, zShelf);
    box(context, id + "hLeft", -xh, -rl, zt - R - T, -xh + T, rl, zShelf);
    box(context, id + "hCam", -d.cameraPlateWidth / 2, rl - T, zShelf - T, d.cameraPlateWidth / 2, rl, zShelf + d.cameraPlateHeight);
    opBoolean(context, id + "hUnion", { "tools" : bodies(context, [id + "hRing", id + "hBridge", id + "hShelf", id + "hRight", id + "hLeft", id + "hCam"]),
                "operationType" : BooleanOperationType.UNION });

    const zLens = zShelf + d.cameraLensHeight;
    var hCuts = [id + "hBore", id + "hRingScrew", id + "hPivot", id + "hHornC", id + "hLens"];
    cyl(context, id + "hBore", vector(z0, -rl - T, zt), vector(z0, rl + T, zt), d.lightDiameter + 2 * d.lightClearance);
    cyl(context, id + "hRingScrew", vector(z0, z0, zt - R - 2 * T), vector(z0, z0, zt), d.ringScrewHole);
    cyl(context, id + "hPivot", vector(-xh - T, z0, zt), vector(-xh + 2 * T, z0, zt), d.pivotDiameter);
    cyl(context, id + "hHornC", vector(xh - 2 * T, z0, zt), vector(xh + T, z0, zt), d.hornCenterHole);
    cyl(context, id + "hLens", vector(z0, rl - 2 * T, zLens), vector(z0, rl + T, zLens), d.cameraLensHole);
    for (var k = 0; k < d.hornHoleCount; k += 1)
    {
        const a = d.hornHoleAngle + k * 360 * degree / d.hornHoleCount;
        const hy = d.hornHolePCD / 2 * cos(a);
        const hz = zt + d.hornHolePCD / 2 * sin(a);
        cyl(context, id + ("hHorn" ~ k), vector(xh - 2 * T, hy, hz), vector(xh + T, hy, hz), d.hornHoleDiameter);
        hCuts = append(hCuts, id + ("hHorn" ~ k));
    }
    for (var i = 0; i < 4; i += 1)
    {
        const cx = (i < 2 ? -1 : 1) * d.cameraHoleDX / 2;
        const cz = zLens + (i % 2 == 0 ? -1 : 1) * d.cameraHoleDZ / 2;
        cyl(context, id + ("hCamHole" ~ i), vector(cx, rl - 2 * T, cz), vector(cx, rl + T, cz), d.cameraHoleDiameter);
        hCuts = append(hCuts, id + ("hCamHole" ~ i));
    }
    opBoolean(context, id + "hCut", { "tools" : bodies(context, hCuts), "targets" : qCreatedBy(id + "hRing", EntityType.BODY),
                "operationType" : BooleanOperationType.SUBTRACTION });
    setProperty(context, { "entities" : qCreatedBy(id + "hRing", EntityType.BODY), "propertyType" : PropertyType.NAME, "value" : "Tilt Head" });
}
// END BUILD

annotation { "Feature Type Name" : "Pan Tilt Task Light Mount" }
export const panTiltMount = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Plate thickness" }
        isLength(definition.plateThickness, { (millimeter) : [2, 4, 15] } as LengthBoundSpec);
        annotation { "Name" : "Wall thickness" }
        isLength(definition.wallThickness, { (millimeter) : [1.2, 3, 10] } as LengthBoundSpec);
        annotation { "Name" : "Fit clearance (servo windows)" }
        isLength(definition.fitClearance, { (millimeter) : [0, 0.4, 3] } as LengthBoundSpec);

        annotation { "Group Name" : "Board (Alif E8) and base", "Collapsed By Default" : false }
        {
            annotation { "Name" : "Board length (UNVERIFIED, measure)" }
            isLength(definition.boardLength, { (millimeter) : [20, 100, 300] } as LengthBoundSpec);
            annotation { "Name" : "Board width (UNVERIFIED, measure)" }
            isLength(definition.boardWidth, { (millimeter) : [20, 70, 300] } as LengthBoundSpec);
            annotation { "Name" : "Board height incl. top parts" }
            isLength(definition.boardHeight, { (millimeter) : [2, 15, 60] } as LengthBoundSpec);
            annotation { "Name" : "Board standoff (clears J16 camera connector)" }
            isLength(definition.boardStandoff, { (millimeter) : [0, 8, 40] } as LengthBoundSpec);
            annotation { "Name" : "Rail width" }
            isLength(definition.railWidth, { (millimeter) : [1, 4, 20] } as LengthBoundSpec);
            annotation { "Name" : "Servo-to-board clearance" }
            isLength(definition.servoClearance, { (millimeter) : [0, 4, 30] } as LengthBoundSpec);
            annotation { "Name" : "Cable hole diameter" }
            isLength(definition.cableHoleDiameter, { (millimeter) : [4, 10, 30] } as LengthBoundSpec);
            annotation { "Name" : "Kill-switch hole diameter" }
            isLength(definition.killSwitchDiameter, { (millimeter) : [3, 12, 30] } as LengthBoundSpec);
        }

        annotation { "Group Name" : "Servo (DS3240MG)", "Collapsed By Default" : true }
        {
            annotation { "Name" : "Case length" }
            isLength(definition.servoLength, { (millimeter) : [10, 40, 80] } as LengthBoundSpec);
            annotation { "Name" : "Case width" }
            isLength(definition.servoWidth, { (millimeter) : [5, 20, 50] } as LengthBoundSpec);
            annotation { "Name" : "Case height (no horn)" }
            isLength(definition.servoHeight, { (millimeter) : [10, 40.4, 80] } as LengthBoundSpec);
            annotation { "Name" : "Case bottom to ear underside" }
            isLength(definition.servoEarZ, { (millimeter) : [5, 27.7, 70] } as LengthBoundSpec);
            annotation { "Name" : "Ear tip-to-tip span" }
            isLength(definition.servoEarSpan, { (millimeter) : [10, 54.5, 100] } as LengthBoundSpec);
            annotation { "Name" : "Ear width (scaled, +-0.3)" }
            isLength(definition.servoEarWidth, { (millimeter) : [5, 19, 50] } as LengthBoundSpec);
            annotation { "Name" : "Ear hole spacing, lengthwise" }
            isLength(definition.servoEarHoleDX, { (millimeter) : [10, 49.5, 100] } as LengthBoundSpec);
            annotation { "Name" : "Ear hole spacing, across" }
            isLength(definition.servoEarHoleDY, { (millimeter) : [2, 10, 50] } as LengthBoundSpec);
            annotation { "Name" : "Spline offset from case centre (scaled)" }
            isLength(definition.servoSplineOffset, { (millimeter) : [0, 9.8, 30] } as LengthBoundSpec);
            annotation { "Name" : "Screw pilot hole (M3 self-tap)" }
            isLength(definition.servoScrewHole, { (millimeter) : [1, 2.8, 6] } as LengthBoundSpec);
        }

        annotation { "Group Name" : "Servo horn (UNVERIFIED, measure)", "Collapsed By Default" : true }
        {
            annotation { "Name" : "Horn face above case top" }
            isLength(definition.hornOffset, { (millimeter) : [0, 6, 30] } as LengthBoundSpec);
            annotation { "Name" : "Horn centre hole" }
            isLength(definition.hornCenterHole, { (millimeter) : [1, 7, 20] } as LengthBoundSpec);
            annotation { "Name" : "Horn bolt circle diameter" }
            isLength(definition.hornHolePCD, { (millimeter) : [4, 14, 40] } as LengthBoundSpec);
            annotation { "Name" : "Horn bolt count" }
            isInteger(definition.hornHoleCount, { (unitless) : [1, 4, 8] } as IntegerBoundSpec);
            annotation { "Name" : "Horn bolt hole diameter" }
            isLength(definition.hornHoleDiameter, { (millimeter) : [1, 3.2, 6] } as LengthBoundSpec);
            annotation { "Name" : "Horn bolt angle offset" }
            isAngle(definition.hornHoleAngle, ANGLE_360_ZERO_DEFAULT_BOUNDS);
        }

        annotation { "Group Name" : "Yoke and pivot", "Collapsed By Default" : true }
        {
            annotation { "Name" : "Upright depth" }
            isLength(definition.uprightDepth, { (millimeter) : [10, 26, 80] } as LengthBoundSpec);
            annotation { "Name" : "Pivot hole (M4 bolt)" }
            isLength(definition.pivotDiameter, { (millimeter) : [2, 4.3, 12] } as LengthBoundSpec);
            annotation { "Name" : "Pivot boss diameter" }
            isLength(definition.pivotBossDiameter, { (millimeter) : [4, 10, 30] } as LengthBoundSpec);
            annotation { "Name" : "Pivot boss gap to head" }
            isLength(definition.pivotGap, { (millimeter) : [0, 0.5, 5] } as LengthBoundSpec);
            annotation { "Name" : "Head swing clearance" }
            isLength(definition.headClearance, { (millimeter) : [0.5, 2, 20] } as LengthBoundSpec);
        }

        annotation { "Group Name" : "Light ring (UNVERIFIED, measure flashlight)", "Collapsed By Default" : false }
        {
            annotation { "Name" : "Flashlight body diameter" }
            isLength(definition.lightDiameter, { (millimeter) : [5, 25.4, 80] } as LengthBoundSpec);
            annotation { "Name" : "Flashlight clearance (radial)" }
            isLength(definition.lightClearance, { (millimeter) : [0, 0.3, 3] } as LengthBoundSpec);
            annotation { "Name" : "Ring wall" }
            isLength(definition.ringWall, { (millimeter) : [1.2, 3, 10] } as LengthBoundSpec);
            annotation { "Name" : "Ring length" }
            isLength(definition.ringLength, { (millimeter) : [10, 30, 100] } as LengthBoundSpec);
            annotation { "Name" : "Flashlight length behind ring" }
            isLength(definition.lightRearLength, { (millimeter) : [0, 40, 300] } as LengthBoundSpec);
            annotation { "Name" : "Ring thumb-screw pilot hole" }
            isLength(definition.ringScrewHole, { (millimeter) : [1, 2.8, 6] } as LengthBoundSpec);
        }

        annotation { "Group Name" : "Camera plate (UNVERIFIED, measure MT9M114 module)", "Collapsed By Default" : true }
        {
            annotation { "Name" : "Plate width" }
            isLength(definition.cameraPlateWidth, { (millimeter) : [10, 36, 100] } as LengthBoundSpec);
            annotation { "Name" : "Plate height above shelf" }
            isLength(definition.cameraPlateHeight, { (millimeter) : [10, 32, 100] } as LengthBoundSpec);
            annotation { "Name" : "Lens centre above shelf" }
            isLength(definition.cameraLensHeight, { (millimeter) : [5, 17, 90] } as LengthBoundSpec);
            annotation { "Name" : "Lens hole diameter" }
            isLength(definition.cameraLensHole, { (millimeter) : [2, 10, 40] } as LengthBoundSpec);
            annotation { "Name" : "Mount hole spacing, across" }
            isLength(definition.cameraHoleDX, { (millimeter) : [2, 20, 90] } as LengthBoundSpec);
            annotation { "Name" : "Mount hole spacing, vertical" }
            isLength(definition.cameraHoleDZ, { (millimeter) : [2, 20, 90] } as LengthBoundSpec);
            annotation { "Name" : "Mount hole diameter" }
            isLength(definition.cameraHoleDiameter, { (millimeter) : [1, 2.2, 6] } as LengthBoundSpec);
        }
    }
    {
        buildPanTiltMount(context, id, definition);
    }, {
        plateThickness : 4 * millimeter, wallThickness : 3 * millimeter, fitClearance : 0.4 * millimeter,
        boardLength : 100 * millimeter, boardWidth : 70 * millimeter, boardHeight : 15 * millimeter, boardStandoff : 8 * millimeter,
        railWidth : 4 * millimeter, servoClearance : 4 * millimeter, cableHoleDiameter : 10 * millimeter, killSwitchDiameter : 12 * millimeter,
        servoLength : 40 * millimeter, servoWidth : 20 * millimeter, servoHeight : 40.4 * millimeter, servoEarZ : 27.7 * millimeter,
        servoEarSpan : 54.5 * millimeter, servoEarWidth : 19 * millimeter, servoEarHoleDX : 49.5 * millimeter, servoEarHoleDY : 10 * millimeter,
        servoSplineOffset : 9.8 * millimeter, servoScrewHole : 2.8 * millimeter,
        hornOffset : 6 * millimeter, hornCenterHole : 7 * millimeter, hornHolePCD : 14 * millimeter, hornHoleCount : 4,
        hornHoleDiameter : 3.2 * millimeter, hornHoleAngle : 45 * degree,
        uprightDepth : 26 * millimeter, pivotDiameter : 4.3 * millimeter, pivotBossDiameter : 10 * millimeter, pivotGap : 0.5 * millimeter,
        headClearance : 2 * millimeter,
        lightDiameter : 25.4 * millimeter, lightClearance : 0.3 * millimeter, ringWall : 3 * millimeter, ringLength : 30 * millimeter,
        lightRearLength : 40 * millimeter, ringScrewHole : 2.8 * millimeter,
        cameraPlateWidth : 36 * millimeter, cameraPlateHeight : 32 * millimeter, cameraLensHeight : 17 * millimeter, cameraLensHole : 10 * millimeter,
        cameraHoleDX : 20 * millimeter, cameraHoleDZ : 20 * millimeter, cameraHoleDiameter : 2.2 * millimeter
    });
