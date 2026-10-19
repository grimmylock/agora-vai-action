// Batch 35: colour grading by hour and weather. One global URP Volume, built in code, takes the grade
// in Atmosphere.Look() (saturation, colour filter, haze, vignette: the same moves as concept.py's post())
// and follows the clock every frame. AtmosphereView's wash quad stays as the fallback: it runs at half
// strength while the grade is on and at full strength when there is no URP (or with F8 in the editor).
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GameCore
{
    public static class ColorGrade
    {
        static Volume _vol;
        static ColorAdjustments _adj;
        static LiftGammaGain _lgg;
        static Vignette _vig;
        public static bool Off;                                   // F8 (editor): compare with and without

        public static bool Active => !Off && _vol != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { _vol = null; Off = false; }

        public static void Apply(AtmosphereLook l)
        {
            if (_vol == null && !Build()) return;
            _vol.enabled = !Off;
            _adj.saturation.value = (l.Saturation - 1f) * 100f;
            _adj.colorFilter.value = l.Filter;                    // URP linearises it, so it multiplies like the concept's display-space filter
            // ponytail: URP's LDR lift is roughly lift*(1-c) in linear after a x0.2 scale, so a display-space
            // haze h is about h^2.2/0.2 here. Tune in Unity with F8 if the darks look off.
            _lgg.lift.value = new Vector4(1f, 1f, 1f, Mathf.Pow(Mathf.Max(0f, l.Haze), 2.2f) / 0.2f);
            _vig.intensity.value = l.Vignette;
        }

        static bool Build()
        {
            var cam = Camera.main;
            if (cam == null || GraphicsSettings.currentRenderPipeline == null) return false;   // not URP: the wash works alone
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _adj = profile.Add<ColorAdjustments>(true);
            _lgg = profile.Add<LiftGammaGain>(true);
            _vig = profile.Add<Vignette>(true);
            _adj.contrast.value = 0f; _adj.postExposure.value = 0f; _adj.hueShift.value = 0f;
            _lgg.gamma.value = new Vector4(1f, 1f, 1f, 0f); _lgg.gain.value = new Vector4(1f, 1f, 1f, 0f);
            _vig.color.value = Color.black; _vig.smoothness.value = 0.3f; _vig.center.value = new Vector2(0.5f, 0.5f);
            var go = new GameObject("ColorGrade");
            _vol = go.AddComponent<Volume>();
            _vol.isGlobal = true; _vol.priority = 10f; _vol.profile = profile;
            return true;
        }
    }
}
