using UnityEngine;

// 2026-11-23 (Yan: "lets establish resting positions for all weapons, when a character is stationary, walking and running all weapon types need a resting, walking, running and walk while aiming animations"):
// one row per weapon category (the carry of a pistol is not the carry of a machine gun). A character with a weapon is in one of four states, all presentation only:
//   REST   (stationary, not aiming/shooting): the weapon sits in its carry pose: RestDeg (degrees, counter-clockwise for a character facing right; + = muzzle up), ShiftPx in toward the torso, DropPx lower.
//   WALK   the carry pose with a small pendulum across the body: WalkAmp px each way, WalkLift px up at the ends of the swing.
//   RUN    the same, bigger and with the gun leaning into it: RunAmp, RunLift, RunTilt degrees.
//   AIM-WALK (aiming or shooting while moving): the weapon stays on the aim (no carry, no swing); only a 1 px bob on each step.
// HeldWeaponView reads this (HeldWeapon.cs, Show wrapper and ShowCore); Tools/render/weapon_states.py draws every row.
namespace GameCore
{
    public static class WeaponCarry
    {
        public struct Def { public float RestDeg, ShiftPx, DropPx, WalkAmp, WalkLift, RunAmp, RunLift, RunTilt; public bool FreeArm; }   // FreeArm: the other hand is off the gun, hanging and swinging with the steps
        static Def D(float rest, float shift, float drop, float wa, float wl, float ra, float rl, float rt) => new Def { RestDeg = rest, ShiftPx = shift, DropPx = drop, WalkAmp = wa, WalkLift = wl, RunAmp = ra, RunLift = rl, RunTilt = rt };
        //                                              rest    shift drop  walk  walk  run   run   run
        //                                              deg     px    px    amp   lift  amp   lift  tilt
        // 2026-11-23 EXPERIMENT (Yan: "make the character hold the pistol muzzle UP like james bond in the movie posters, and when running he runs holding the pistol muzzle up and swinging the other arm" ... "the pistol's in the wrong hand, change it to the character's right hand and match the orientation accordingly"): held up beside the face, muzzle up, one-handed in the character's RIGHT hand (he faces the camera, so that is the screen's LEFT: the gun is mirrored, HeldWeaponView forces _restLeft for a FreeArm weapon); the other arm hangs and swings with the steps (FreeArm). The old low-ready was D(-28, 2, 2, 1, 1, 2, 1, 7).
        static readonly Def Sidearm = new Def { RestDeg = 78f, ShiftPx = -5f, DropPx = -1f, WalkAmp = 1f, WalkLift = 0f, RunAmp = 1f, RunLift = 1f, RunTilt = 2f, FreeArm = true };
                // 2026-11-23 (Yan: "try it on the smg's as well"): the SMG in the Bond carry too: one-handed in the character's RIGHT hand (the screen's left, he faces us), muzzle up, the other arm swinging. The old carry: D(-12, 3, 1, 1, 1, 3, 2, 5).
        static readonly Def Smg = new Def { RestDeg = 74f, ShiftPx = -5f, DropPx = -1f, WalkAmp = 1f, WalkLift = 0f, RunAmp = 1f, RunLift = 1f, RunTilt = 2f, FreeArm = true };
        static readonly Def Assault = D(0f, 3f, 0f, 1f, 1f, 3f, 2f, 5f);       // a rifle: level across the front (the stance Yan approved)
        static readonly Def Shotgun = D(14f, 3f, 0f, 1f, 1f, 3f, 2f, 5f);      // a shotgun: across the chest, muzzle a little up
        static readonly Def Sniper = D(30f, 4f, 0f, 1f, 1f, 3f, 2f, 4f);       // a long rifle: port arms, muzzle up
        static readonly Def Heavy = D(-6f, 2f, 1f, 1f, 1f, 2f, 1f, 3f);        // a machine gun: heavy at the hip, swings little
        static readonly Def Melee = D(0f, 0f, 0f, 1f, 1f, 3f, 2f, 5f);         // melee keeps its own stance (HeldPoseMath.MeleeFrame); only the walk and run swing ride on it
        public static Def Of(string cat) { switch (cat) { case "sidearm": return Sidearm; case "smg": return Smg; case "shotgun": return Shotgun; case "sniper": return Sniper; case "heavy": return Heavy; case "melee": return Melee; default: return Assault; } }
        /// <summary>The resting angle for a character facing left or right (degrees, world, y up).</summary>
        public static float RestAngle(string cat, bool left) { float r = Of(cat).RestDeg; return left ? 180f - r : r; }
    }
}
