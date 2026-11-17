// Unity-only (left out of the offline harness, like Net/ and Editor/): URP 2D lights for the lights test (GameCore/Lighting2D.cs). F6 toggles it.
// What it makes while on: one Global light (the ambient: bright by day, ~10% at night and underground), a Spot light for the flashlight (follows the aim, only when the flashlight is on),
// a Point light for each of the map's placed lights near you (WorldArt.PlacedLights through Lighting.Placed: colour, radius, flicker), and every world SpriteRenderer switched to the lit
// sprite material (the overlays and effects drawn over everything keep theirs). No shadow casters yet: underground the sight mask hides what a light would leak into; outdoors it is a first look.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace GameCore
{
    public sealed class Light2DRig : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { var go = new GameObject("Light2DRig"); DontDestroyOnLoad(go); go.AddComponent<Light2DRig>(); }

        Light2D _global, _flash; readonly List<Light2D> _points = new List<Light2D>(); readonly List<float> _flicker = new List<float>(), _base = new List<float>();
        readonly Dictionary<SpriteRenderer, Material> _orig = new Dictionary<SpriteRenderer, Material>();
        Material _lit; float _scanAt, _lightsAt; object _lastList;

        void Update()
        {
            var k = Keyboard.current; if (k != null && k.f6Key.wasPressedThisFrame) Lighting2D.Toggle();
            if (!Lighting2D.On || Player.Current == null) { Release(); return; }
            if (_lit == null)
            {
                var sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
                if (sh == null) { Lighting2D.Status = "(no Sprite-Lit-Default shader found: is the URP 2D renderer on?)"; Lighting2D.On = false; GameLog.Write("[DEV] 2D lights: " + Lighting2D.Status); return; }
                _lit = new Material(sh);
            }
            var look = Atmosphere.Look(); var p = Player.Current;
            if (_global == null) { _global = Make("GlobalLight", Light2D.LightType.Global); }
            float dark = Mathf.Clamp01(look.Night / 0.85f);
            _global.intensity = Mathf.Lerp(1f, 0.10f, dark); _global.color = Color.Lerp(Color.white, new Color(0.62f, 0.70f, 1f), dark);
            _global.gameObject.SetActive(true);
            Flashlight(p, look);
            if (Time.unscaledTime >= _lightsAt) { _lightsAt = Time.unscaledTime + 0.3f; Points(p); }
            Flicker();
            if (Time.unscaledTime >= _scanAt) { _scanAt = Time.unscaledTime + 1f; Lit(); }
        }

        Light2D Make(string name, Light2D.LightType type)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            var l = go.AddComponent<Light2D>(); l.lightType = type; return l;
        }

        static Vector3 PlayerWorld(Player p) { var w = TacticalGridView.GridToWorld(p.X, p.Y); var o = Rt.Offset(p); return new Vector3(w.x + o.x, w.y - o.y, 0f); }
        static float TileUnits() => (TacticalGridView.GridToWorld(1, 0) - TacticalGridView.GridToWorld(0, 0)).magnitude;

        void Flashlight(Player p, AtmosphereLook look)
        {
            if (_flash == null) _flash = Make("Flashlight", Light2D.LightType.Point);
            bool on = p.FlashlightOn && look.Cone > 0f;
            _flash.gameObject.SetActive(on); if (!on) return;
            float u = TileUnits();
            _flash.transform.position = PlayerWorld(p); _flash.transform.rotation = Quaternion.Euler(0f, 0f, Vision.Aim * Mathf.Rad2Deg - 90f);
            _flash.pointLightOuterRadius = look.Cone * u; _flash.pointLightInnerRadius = 0.5f * u;
            _flash.pointLightOuterAngle = 52f; _flash.pointLightInnerAngle = 16f;
            _flash.falloffIntensity = 0.55f; _flash.intensity = 1.35f; _flash.color = new Color(1f, 0.96f, 0.85f);
        }

        void Points(Player p)
        {
            var src = Lighting.Placed(); float u = TileUnits(); var near = new List<(float d, WorldArt.PointLight l)>();
            foreach (var l in src) { float tx = l.X / WorldArt.Px, ty = l.Y / WorldArt.Px, d = Mathf.Sqrt((tx - p.X) * (tx - p.X) + (ty - p.Y) * (ty - p.Y)); if (d <= 36f) near.Add((d, l)); }
            near.Sort((a, b) => a.d.CompareTo(b.d));
            int n = Mathf.Min(near.Count, 32);
            while (_points.Count < n) { _points.Add(Make("Point" + _points.Count, Light2D.LightType.Point)); _flicker.Add(0f); _base.Add(1f); }
            for (int i = 0; i < _points.Count; i++)
            {
                var L = _points[i]; if (i >= n) { L.gameObject.SetActive(false); continue; }
                var l = near[i].l; L.gameObject.SetActive(true);
                L.transform.position = TacticalGridView.GridToWorld(Mathf.RoundToInt(l.X / WorldArt.Px), Mathf.RoundToInt(l.Y / WorldArt.Px));
                L.pointLightOuterRadius = l.R / WorldArt.Px * u; L.pointLightInnerRadius = 0.12f * L.pointLightOuterRadius; L.pointLightOuterAngle = 360f; L.pointLightInnerAngle = 360f;
                L.falloffIntensity = 0.6f; L.color = new Color(l.Cr, l.Cg, l.Cb); _base[i] = 0.9f * l.Power; L.intensity = _base[i]; _flicker[i] = l.Flicker;
            }
        }

        void Flicker()
        {
            for (int i = 0; i < _points.Count; i++)
                if (_flicker[i] > 0f && _points[i].gameObject.activeSelf) _points[i].intensity = _base[i] * (1f + _flicker[i] * (Mathf.PerlinNoise(Time.time * 7f, i * 3.1f) - 0.5f) * 2.4f);
        }

        void Lit()
        {
            foreach (var sr in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            {
                if (sr == null || _orig.ContainsKey(sr) || sr.sortingOrder >= WorldArt.SortTop - 40) continue;   // the overlays (wash, fog, rain, the night/sight mask) and the effects over everything stay unlit
                _orig[sr] = sr.sharedMaterial; sr.sharedMaterial = _lit;
            }
        }

        void Release()
        {
            if (_orig.Count > 0) { foreach (var kv in _orig) if (kv.Key != null) kv.Key.sharedMaterial = kv.Value; _orig.Clear(); }
            if (_global != null) _global.gameObject.SetActive(false);
            if (_flash != null) _flash.gameObject.SetActive(false);
            foreach (var l in _points) if (l != null) l.gameObject.SetActive(false);
        }
    }
}
#endif
