// The 2D lights TEST (Yan, 2026-11-17: "actual lights instead of turning the brightness up in select spots"). F6 (editor / development build) switches Unity's own URP 2D lights on
// (Assets/Scripts/Unity2D/Light2DRig.cs: a dim global light, the flashlight as a spot light, every placed lamp, campfire, ceiling and emergency light as a point light, the sprites on the lit material).
// While it is on, AtmosphereView stops darkening and brightening spots of the picture (the lights do that) and keeps only the sight mask: what is out of your line of sight is hidden/dimmed as before.
// This file is plain C# so the offline harness compiles it; the Unity-only rig reads and sets it.
namespace GameCore
{
    public static class Lighting2D
    {
        public static bool On;
        public static string Status = "";
        public static void Toggle() { On = !On; GameLog.Write("[DEV] 2D lights " + (On ? "on (F6 to switch off)." : "off.") + (string.IsNullOrEmpty(Status) ? "" : " " + Status)); }
    }
}
