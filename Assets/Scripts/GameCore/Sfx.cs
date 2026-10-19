// Batch 30: sound. Clips are Resources/Audio/<name>.wav or <name>_0.._9 variants (one picked at
// random), all synthesized by ArtSource/audio/sfx.py. 2D audio: a sound placed in the world is
// panned left/right from the camera and fades with distance. One looping ambient bed per zone.
// No clips = silent, nothing breaks.
using System.Collections.Generic;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class Sfx
    {
        public static float Volume = 0.8f, AmbientVolume = 0.5f;
        const int Voices = 20;
        static GameObject _go;
        static AudioSource[] _pool;
        static AudioSource _amb;
        static int _next;
        static readonly Dictionary<string, AudioClip[]> _clips = new Dictionary<string, AudioClip[]>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCaches() { _go = null; _pool = null; _amb = null; _clips.Clear(); }

        static bool Ensure()
        {
            if (_go != null) return true;
            _go = new GameObject("Sfx");
            Object.DontDestroyOnLoad(_go);
            _pool = new AudioSource[Voices];
            for (int i = 0; i < Voices; i++) { _pool[i] = _go.AddComponent<AudioSource>(); _pool[i].playOnAwake = false; }
            _amb = _go.AddComponent<AudioSource>(); _amb.loop = true; _amb.playOnAwake = false;
            return true;
        }

        static AudioClip[] Clips(string name)
        {
            if (_clips.TryGetValue(name, out var c)) return c;
            var list = new List<AudioClip>();
            var one = Resources.Load<AudioClip>("Audio/" + name);
            if (one != null) list.Add(one);
            for (int i = 0; i < 10; i++) { var v = Resources.Load<AudioClip>($"Audio/{name}_{i}"); if (v == null) break; list.Add(v); }
            return _clips[name] = list.ToArray();
        }

        /// <summary>at = world position (panned + distance-faded from the camera), or null for UI/own sounds.</summary>
        public static void Play(string name, Vector3? at = null, float vol = 1f, float pitchJitter = 0.06f, float pitch = 1f)
        {
            if (name == null) return;
            var clips = Clips(name);
            if (clips.Length == 0 || !Ensure()) return;
            float v = vol * Volume, pan = 0f;
            if (at.HasValue && Camera.main != null)
            {
                var c = Camera.main.transform.position;
                float dx = at.Value.x - c.x, dy = at.Value.y - c.y, d = Mathf.Sqrt(dx * dx + dy * dy);
                v *= Mathf.Clamp01(1.25f - d / 24f);
                pan = Mathf.Clamp(dx / 14f, -0.75f, 0.75f);
            }
            if (v < 0.01f) return;
            AudioSource src = null;
            for (int i = 0; i < Voices && src == null; i++) { var s = _pool[(_next + i) % Voices]; if (!s.isPlaying) src = s; }
            if (src == null) src = _pool[_next];                  // all busy: steal the oldest slot
            _next = (_next + 1) % Voices;
            src.clip = clips[Random.Range(0, clips.Length)];
            src.volume = v; src.panStereo = pan;
            src.pitch = pitch * (1f + Random.Range(-pitchJitter, pitchJitter));
            src.Play();
        }

        public static void Ambient(string name)
        {
            var clips = name == null ? null : Clips(name);
            if (clips == null || clips.Length == 0 || !Ensure()) { if (_amb != null) _amb.Stop(); return; }
            if (_amb.clip == clips[0] && _amb.isPlaying) return;
            _amb.clip = clips[0]; _amb.volume = AmbientVolume * Volume; _amb.Play();
        }

        public static void StopAmbient() { if (_amb != null) _amb.Stop(); }

        // ---- what plays for what
        public static string Shot(WeaponDef db, WeaponFeel feel)
        {
            if (feel != null && feel.Rail) return "shot_rail";
            switch (CombatRules.Category(db))
            {
                case "sidearm": return "shot_pistol";
                case "smg": return "shot_smg";
                case "shotgun": return "shot_shotgun";
                case "sniper": return "shot_sniper";
                case "heavy": return "shot_heavy";
                default: return "shot_rifle";
            }
        }

        public static string Reload(WeaponFeel feel) =>
            feel == null ? null : feel.Reload == "tube" ? "reload_shell" : feel.Reload == "cylinder" || feel.Reload == "break" ? "reload_cylinder"
            : feel.Reload == "none" ? null : "reload_mag";

        public static string Step(GroundType g)
        {
            switch (g)
            {
                case GroundType.Grass: case GroundType.DarkGrass: return "step_grass";
                case GroundType.Dirt: case GroundType.Mud: case GroundType.Sand: case GroundType.Gravel: return "step_dirt";
                case GroundType.Floor: return "step_wood";
                case GroundType.Water: return "step_water";
                default: return "step_hard";
            }
        }

        public static string AmbientFor(string zoneId)
        {
            if (zoneId == null) return "amb_wind";
            if (zoneId == "marshes") return "amb_marsh";
            var theme = ZoneThemes.For(zoneId);
            if (theme.Mass == "cliff" && ZoneLayoutRecipes.IsInterior(zoneId)) return "amb_cave";
            if (ZoneLayoutRecipes.IsInterior(zoneId) || zoneId == "reactor_core") return "amb_hum";
            return "amb_wind";
        }
    }
}
