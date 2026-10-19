// New (Batch 22): plays CombatFx events back on the tactical view - muzzle flash, tracer, recoil,
// ejected casings, hit flash + knockback, mutant lunges, reload animations, camera shake.
// The rules have already resolved by the time anything here runs (see CombatFx.cs); events are
// spaced a beat apart so a whole enemy turn reads as a sequence instead of one frame of noise.
// Art: Sprites/Fx/fx_*.png (ArtSource/make_fx.py); missing art falls back to flat coloured quads.
using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    public class CombatFxView
    {
        private class Particle
        {
            public SpriteRenderer Sr;
            public float Born, Life, Gravity, Alpha = 1f;
            public Vector3 Vel;
            public Sprite[] Frames;
            public bool Fade;
            public float Friction;   // top-down slide that slows down (gibs)
            public bool Stick;       // stays on the ground as a decal when its life ends (blood, gibs)
        }

        /// <summary>Batch 24: called when a killing blow lands visually (direction, gib?). The grid view
        /// starts the ragdoll there; until then the victim keeps standing.</summary>
        public System.Action<ICombatant, float, float, int> OnKill;
        /// <summary>Batch 24: an event whose actor is still walking waits until it arrives.</summary>
        public System.Func<ICombatant, bool> IsMoving;
        /// <summary>Batch 26: a physical hit landed on this unit (leaves a stain on its body).</summary>
        public System.Action<ICombatant> OnWound;
        private readonly HashSet<ICombatant> _dying = new HashSet<ICombatant>();
        private readonly Queue<SpriteRenderer> _decals = new Queue<SpriteRenderer>();
        private const int MaxDecals = 120;

        /// <summary>True while the unit's killing blow is still waiting to be played.</summary>
        public bool DeathPending(ICombatant u) => _dying.Contains(u);

        /// <summary>Direction of the last blow this unit took (world, y up), or (0, 0).</summary>
        public (float, float) LastHitDir(ICombatant u) => u != null && _hurt.TryGetValue(u, out var h) ? (h.dx, h.dy) : (0f, 0f);

        private readonly Transform _root;
        private readonly System.Func<ICombatant, SpriteRenderer> _markerOf;
        private readonly System.Func<SpriteRenderer, HeldWeaponView> _heldOf;
        private readonly List<(float at, System.Action act)> _timers = new List<(float, System.Action)>();
        private readonly List<Particle> _live = new List<Particle>();
        private readonly Dictionary<ICombatant, (float at, float dx, float dy, bool psi)> _hurt = new Dictionary<ICombatant, (float, float, float, bool)>();
        private readonly Dictionary<ICombatant, (float at, float dx, float dy)> _lunge = new Dictionary<ICombatant, (float, float, float)>();
        private float _nextFree, _shakeAt = -99f, _shakeAmp;
        private static Sprite _white;
        private static readonly Dictionary<string, Sprite[]> Strips = new Dictionary<string, Sprite[]>();
        // Batch 29: the project enters Play mode without a domain reload, so statics survive between
        // runs while the sprites/textures they point at are destroyed - clear the caches each run.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCaches() { Strips.Clear(); _white = null; }


        private const float HurtTime = 0.15f, LungeTime = 0.18f, ShakeTime = 0.15f;
        private static readonly Color TracerColor = new Color(1f, 0.93f, 0.6f, 1f), RailColor = new Color(0.62f, 0.9f, 1f, 1f);

        public CombatFxView(Transform root, System.Func<ICombatant, SpriteRenderer> markerOf, System.Func<SpriteRenderer, HeldWeaponView> heldOf)
        {
            _root = root; _markerOf = markerOf; _heldOf = heldOf;
            CombatFx.Pending.Clear();   // whatever happened before this view existed (previous zone, load) is stale
        }

        public Vector3 ShakeOffset { get; private set; }

        public void Tick()
        {
            float now = Time.time;
            foreach (var e in CombatFx.Pending)
            {
                if (e.Killed && e.Target != null) _dying.Add(e.Target);
                if (e.Now) { var rt = e; At(now + e.Delay, () => Play(rt)); continue; }   // real-time fork: no queue, no waiting for walkers
                float start = Mathf.Max(now, _nextFree);
                float gap = e.Kind == "shot" ? 0.14f + 0.07f * (e.Rounds - 1) : e.Kind == "reload" ? 0.15f : 0.22f;
                if (_nextFree - now > 1.2f) gap *= 0.4f;            // a big enemy turn: catch up instead of lagging
                _nextFree = start + gap;
                var ev = e;
                At(start, () => PlayWhenReady(ev, 0));
            }
            CombatFx.Pending.Clear();

            for (int i = 0; i < _timers.Count; i++)
            {
                if (_timers[i].at > now) continue;
                var act = _timers[i].act;
                _timers.RemoveAt(i--);
                act();
            }

            float dt = Time.deltaTime;
            for (int i = 0; i < _live.Count; i++)
            {
                var p = _live[i];
                float t = now - p.Born;
                if (p.Sr == null || t >= p.Life)
                {
                    if (p.Sr != null && p.Stick) Decal(p.Sr);
                    else if (p.Sr != null) Object.Destroy(p.Sr.gameObject);
                    _live.RemoveAt(i--);
                    continue;
                }
                if (p.Frames != null) p.Sr.sprite = p.Frames[Mathf.Min(p.Frames.Length - 1, (int)(t / p.Life * p.Frames.Length))];
                p.Vel.y -= p.Gravity * dt;
                if (p.Friction > 0f) p.Vel *= Mathf.Exp(-p.Friction * dt);
                p.Sr.transform.position += p.Vel * dt;
                if (p.Fade) { var c = p.Sr.color; c.a = p.Alpha * (1f - t / p.Life); p.Sr.color = c; }
            }

            float st = now - _shakeAt;
            float amp = st < ShakeTime ? _shakeAmp * (1f - st / ShakeTime) : 0f;
            ShakeOffset = amp > 0f ? new Vector3(Random.Range(-amp, amp), Random.Range(-amp, amp), 0f) : Vector3.zero;
        }

        /// <summary>Hit flash (red, or violet for psi) and a 1-pixel knockback, and a mutant's lunge.
        /// Call after the marker's position and colour are set for the frame.</summary>
        public void ApplyTo(ICombatant unit, SpriteRenderer marker)
        {
            float now = Time.time;
            var pos = marker.transform.position;
            if (_hurt.TryGetValue(unit, out var h) && now - h.at < HurtTime)
            {
                float k = 1f - (now - h.at) / HurtTime;
                var c = h.psi ? new Color(0.8f, 0.5f, 1f, marker.color.a) : new Color(1f, 0.5f, 0.45f, marker.color.a);
                marker.color = c;
                pos.x += h.dx * k / 16f; pos.y += h.dy * k / 16f;
            }
            if (_lunge.TryGetValue(unit, out var l) && now - l.at < LungeTime)
            {
                float s = Mathf.Sin((now - l.at) / LungeTime * Mathf.PI) * 0.25f;
                pos.x += l.dx * s; pos.y += l.dy * s;
            }
            marker.transform.position = pos;
        }

        private void At(float t, System.Action act) => _timers.Add((t, act));

        private void PlayWhenReady(FxEvent e, int waited)
        {
            if (waited < 60 && e.Actor != null && IsMoving != null && IsMoving(e.Actor))   // max ~3 s
            {
                At(Time.time + 0.05f, () => PlayWhenReady(e, waited + 1));
                _nextFree = Mathf.Max(_nextFree, Time.time + 0.1f);
                return;
            }
            Play(e);
        }

        private void Play(FxEvent e)
        {
            float now = Time.time;
            var marker = e.Actor != null ? _markerOf(e.Actor) : null;
            var held = marker != null && e.Kind != "strike" ? _heldOf(marker) : null;
            if (held != null && !held.Visible) held = null;
            Vector3 from = marker != null ? marker.transform.position : TacticalGridView.GridToWorld(e.FromX, e.FromY);
            var targetMarker = e.Target != null ? _markerOf(e.Target) : null;
            Vector3 to = targetMarker != null ? targetMarker.transform.position : TacticalGridView.GridToWorld(e.ToX, e.ToY);
            if (e.EndX >= 0f) to = new Vector3(e.EndX, -e.EndY, 0f);   // real-time fork: where the round really stopped
            var (dx, dy) = Dir(from, to);
            var feel = WeaponFeel.For(e.Weapon?.Db);

            switch (e.Kind)
            {
                case "shot":
                    held?.AimAt(to, now + 0.35f + 0.07f * e.Rounds);
                    if (e.Jammed) { At(now + 0.03f, () => held?.Twitch()); Sfx.Play("dryfire", from); return; }
                    for (int i = 0; i < e.Rounds; i++)
                    {
                        int n = i;
                        At(now + 0.03f + 0.07f * i, () => SubShot(e, feel, held, from, to, n));
                    }
                    break;
                case "melee":
                    held?.AimAt(to, now + 0.3f);
                    held?.Swing();
                    Sfx.Play("swing", from);
                    if (e.Hit) At(now + 0.1f, () => Wound(e, to, dx, dy, false));
                    break;
                case "strike":
                    if (e.Actor != null) _lunge[e.Actor] = (now, dx, dy);
                    Sfx.Play(e.Psi ? "psi" : "claw", from);
                    if (e.Psi) Puff("fx_hit_psi", from, 0.25f);
                    if (e.Hit) At(now + 0.09f, () => Wound(e, to, dx, dy, e.Psi));
                    break;
                case "kill":                                    // blown up: outward from the blast centre
                    var (bx, by) = Dir(TacticalGridView.GridToWorld(e.FromX, e.FromY), to);
                    Wound(e, to, bx, by, false);
                    break;
                case "reload":
                    if (held == null) break;
                    float len = e.Seconds > 0f ? e.Seconds : feel.ReloadTime;
                    held.StartReload(feel, e.Weapon.MagCapacity, len);
                    Sfx.Play(Sfx.Reload(feel), from, 0.8f);
                    At(now + 0.2f * len, () =>
                    {
                        if (held == null || !held.Visible) return;
                        var at = held.Gun.position;
                        if (feel.Reload == "mag") Bit(at, new Color(0.2f, 0.22f, 0.23f), 2, 3, new Vector3(0f, -0.2f, 0f), 4f, 0.45f, 4);
                        else if (feel.Reload == "cylinder" || feel.Reload == "break")
                            for (int i = 0; i < Mathf.Min(e.Weapon.MagCapacity, 6); i++)
                                Bit(at, feel.Reload == "break" ? new Color(0.69f, 0.16f, 0.16f) : new Color(0.79f, 0.65f, 0.24f), 1, 1,
                                    new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(0.2f, 0.8f), 0f), 5f, 0.4f, 4);
                    });
                    break;
            }
        }

        private void SubShot(FxEvent e, WeaponFeel feel, HeldWeaponView held, Vector3 from, Vector3 to, int i)
        {
            if (held != null && !held.Visible) held = null;
            Vector3 muzzle = held != null ? held.Muzzle() : from;
            bool last = i == e.Rounds - 1;
            if (e.Now)
            {
                // real-time fork: one real round. Flash, sound and casing once a trigger pull; the tracer to where it stopped.
                if (!e.Pellet)
                {
                    held?.Recoil(feel, 1f);
                    Sfx.Play(Sfx.Shot(e.Weapon?.Db, feel), muzzle, feel.Flash == 0 ? 0.35f : 1f, 0.07f, feel.Flash == 0 ? 1.35f : 1f);
                    if (feel.Rail) Flash(held, "fx_muzzle_rail", 0.1f, muzzle);
                    else if (feel.Flash == 0) Puff("fx_muzzle_puff", muzzle, 0.3f, 0.3f);
                    else Flash(held, "fx_muzzle_" + feel.Flash, 0.06f, muzzle);
                    if (e.Actor is Player && feel.Shake > 0f) { _shakeAt = Time.time; _shakeAmp = feel.Shake; }
                }
                Tracer(muzzle, to, feel);
                float rx = to.x - muzzle.x, ry = to.y - muzzle.y, rl = Mathf.Max(0.01f, Mathf.Sqrt(rx * rx + ry * ry));
                if (e.CoverHit || e.Through) CoverImpact(e, rx / rl, ry / rl);
                if (e.Hit && e.Target != null) Wound(e, to, rx / rl, ry / rl, false);
                return;
            }
            held?.Recoil(feel, i == 0 ? 1f : 0.7f);

            Sfx.Play(Sfx.Shot(e.Weapon?.Db, feel), muzzle, feel.Flash == 0 ? 0.35f : 1f, 0.07f, feel.Flash == 0 ? 1.35f : 1f);   // suppressed: quieter, thinner
            if (feel.Rail) Flash(held, "fx_muzzle_rail", 0.1f, muzzle);
            else if (feel.Flash == 0) Puff("fx_muzzle_puff", muzzle, 0.3f, 0.3f);
            else Flash(held, "fx_muzzle_" + feel.Flash, 0.06f, muzzle);

            var end = to;
            float mx = to.x - muzzle.x, my = to.y - muzzle.y, len = Mathf.Max(0.01f, Mathf.Sqrt(mx * mx + my * my));
            float nx = mx / len, ny = my / len;
            if (e.NoLos) { end.x = muzzle.x + mx * 0.55f; end.y = muzzle.y + my * 0.55f; }
            else if (e.CoverHit && last) { var c = TacticalGridView.GridToWorld(e.CoverX, e.CoverY); end.x = c.x + Random.Range(-0.25f, 0.25f); end.y = c.y + Random.Range(-0.1f, 0.3f); }   // Batch 105: into the cover
            else if (!e.Hit) { float s = Random.Range(-0.45f, 0.45f); end.x = to.x + nx * 1.5f - ny * s; end.y = to.y + ny * 1.5f + nx * s; }
            else if (i > 0) { float s = Random.Range(-0.15f, 0.15f); end.x += -ny * s; end.y += nx * s; }
            Tracer(muzzle, end, feel);

            if (feel.Casing != null && held != null)
            {
                float side = held.AimLeft ? -1f : 1f;
                Bit(held.Gun.position, feel.Casing == "shell" ? new Color(0.69f, 0.16f, 0.16f) : new Color(0.79f, 0.65f, 0.24f),
                    feel.Casing == "shell" ? 2 : 1, 1, new Vector3(-side * Random.Range(0.4f, 0.9f), Random.Range(1.2f, 1.8f), 0f), 6f, 0.35f, 7);
            }
            if (e.Actor is Player && feel.Shake > 0f) { _shakeAt = Time.time; _shakeAmp = feel.Shake; }

            if (!last) return;
            if (e.CoverHit || e.Through) CoverImpact(e, nx, ny);
            if (e.Hit && e.Target != null) Wound(e, to, nx, ny, false);
            else if ((e.Hit || e.NoLos) && !e.CoverHit) { Puff("fx_hit_spark", end, 0.18f); Sfx.Play("hit_hard", end, 0.6f, 0.15f); }     // a door, a barrel, a wall
        }

        /// <summary>A blow lands on a unit: flash, knockback, blood puff, a splatter on the ground behind
        /// it, and the death fall if this was the killing blow.</summary>
        private void Wound(FxEvent e, Vector3 at, float dx, float dy, bool psi)
        {
            Hurt(e.Target, dx, dy, psi);
            Sfx.Play(e.Target is Player ? "hurt" : "hit_flesh", at, 0.9f, 0.1f);
            Puff(psi ? "fx_hit_psi" : "fx_hit_blood", at, 0.2f);
            if (!psi && GameConstants.Gore) { Splat(at, dx, dy); if (e.Target != null) OnWound?.Invoke(e.Target); }
            if (e.Killed && e.Target != null && _dying.Remove(e.Target)) OnKill?.Invoke(e.Target, dx, dy, e.Overkill >= GibOverkill * 3 ? 2 : e.Overkill >= GibOverkill ? 1 : 0);
        }
        public const int GibOverkill = 150;

        /// <summary>Batch 105: a round hits a piece of cover. What flies off and the sound follow its material: splinters off
        /// wood, dust and chips off concrete, sparks off metal, earth off sandbags, leaves off brush.</summary>
        private void CoverImpact(FxEvent e, float dx, float dy)
        {
            var at = TacticalGridView.GridToWorld(e.CoverX, e.CoverY);
            at.y += 0.25f;
            var m = e.CoverMat;
            var (r, g, b) = Materials.Chip(m);
            if (Materials.Sparks(m)) Puff("fx_hit_spark", at, 0.18f);
            for (int k = 0; k < (e.Through ? 6 : 4); k++)   // chips fly back toward the shooter, or out the far side when it goes through
            {
                float side = e.Through ? 1f : -1f, sp = Random.Range(0.6f, 1.6f);
                var col = new Color(r * Random.Range(0.8f, 1.1f), g * Random.Range(0.8f, 1.1f), b * Random.Range(0.8f, 1.1f));
                Bit(at, col, 1, 1, new Vector3(side * dx * sp + Random.Range(-0.4f, 0.4f), side * dy * sp + Random.Range(0.4f, 1.2f), 0f), 5f, 0.45f, 6);
            }
            Sfx.Play(Materials.Sound(m), at, 0.6f, 0.15f);
        }

        private void Hurt(ICombatant unit, float dx, float dy, bool psi)
        {
            if (unit != null) _hurt[unit] = (Time.time, dx, dy, psi);
        }

        private static (float, float) Dir(Vector3 a, Vector3 b)
        {
            float x = b.x - a.x, y = b.y - a.y, l = Mathf.Sqrt(x * x + y * y);
            return l < 0.001f ? (1f, 0f) : (x / l, y / l);
        }

        // ---- spawners ----

        private void Flash(HeldWeaponView held, string strip, float life, Vector3 muzzle)
        {
            var frames = Strip("Sprites/Fx/" + strip, 2, WeaponArt.PixelsPerUnit, new Vector2(0f, 0.5f));
            var sr = NewSprite("MuzzleFlash", frames?[0], new Color(1f, 0.8f, 0.3f, 1f), held != null ? held.Gun : _root, 9);
            if (held != null)
            {
                sr.transform.localPosition = held.MuzzleLocal();
                sr.flipY = held.AimLeft;
            }
            else sr.transform.position = muzzle;
            if (frames == null) sr.transform.localScale = Vector3.one * 0.15f;
            _live.Add(new Particle { Sr = sr, Born = Time.time, Life = life, Frames = frames });
        }

        private void Puff(string strip, Vector3 at, float life, float rise = 0f)
        {
            var frames = Strip("Sprites/Fx/" + strip, 3, strip == "fx_muzzle_puff" ? WeaponArt.PixelsPerUnit : 16, new Vector2(0.5f, 0.5f));
            var sr = NewSprite(strip, frames?[0], strip.Contains("blood") ? new Color(0.7f, 0.13f, 0.16f) : strip.Contains("psi") ? new Color(0.75f, 0.42f, 1f) : new Color(1f, 0.8f, 0.3f), _root, 9);
            sr.transform.position = new Vector3(at.x + Random.Range(-0.06f, 0.06f), at.y + Random.Range(-0.06f, 0.06f), 0f);
            if (frames == null) sr.transform.localScale = Vector3.one * 0.2f;
            _live.Add(new Particle { Sr = sr, Born = Time.time, Life = life, Frames = frames, Vel = new Vector3(0f, rise, 0f), Fade = rise > 0f });
        }

        private void Tracer(Vector3 a, Vector3 b, WeaponFeel feel)
        {
            float x = b.x - a.x, y = b.y - a.y, len = Mathf.Sqrt(x * x + y * y);
            if (len < 0.05f) return;
            var col = feel.Rail ? RailColor : TracerColor;
            col.a = feel.Tracer;
            var sr = NewSprite("Tracer", White(), col, _root, 8);
            sr.transform.position = new Vector3((a.x + b.x) * 0.5f, (a.y + b.y) * 0.5f, 0f);
            sr.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(y, x) * Mathf.Rad2Deg);
            sr.transform.localScale = new Vector3(len, (feel.Rail ? 2f : 1f) / WeaponArt.PixelsPerUnit, 1f);
            _live.Add(new Particle { Sr = sr, Born = Time.time, Life = feel.Rail ? 0.3f : 0.06f, Fade = true, Alpha = col.a });
        }

        /// <summary>A small solid chunk (casing, shell, dropped magazine) in weapon pixels that flies and falls.</summary>
        private void Bit(Vector3 at, Color col, int w, int h, Vector3 vel, float gravity, float life, int order)
        {
            var sr = NewSprite("Bit", White(), col, _root, order);
            sr.transform.position = at;
            sr.transform.localScale = new Vector3(w / (float)WeaponArt.PixelsPerUnit, h / (float)WeaponArt.PixelsPerUnit, 1f);
            _live.Add(new Particle { Sr = sr, Born = Time.time, Life = life, Vel = vel, Gravity = gravity, Fade = true });
        }

        // ---- gore (Batch 24) ----

        /// <summary>Blood sprayed on the ground behind a hit, pointing the way the shot travelled.</summary>
        public void Splat(Vector3 at, float dx, float dy)
        {
            var frames = Strip("Sprites/Fx/fx_blood_splat", 4, 16, new Vector2(0f, 0.5f));
            var sr = NewSprite("Splat", frames?[Random.Range(0, 4)], new Color(0.55f, 0.08f, 0.1f, 0.9f), _root, 1);
            float d = Random.Range(0.15f, 0.4f);
            sr.transform.position = new Vector3(at.x + dx * d, at.y + dy * d, 0f);
            sr.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dy, dx) * Mathf.Rad2Deg);
            if (frames == null) sr.transform.localScale = new Vector3(0.4f, 0.12f, 1f);
            Decal(sr);
        }

        /// <summary>A pool spreading under a body over a second, then left on the ground.</summary>
        public void Pool(Vector3 at, float delay)
        {
            At(Time.time + delay, () =>
            {
                var f = Strip("Sprites/Fx/fx_blood_pool", 5, 16, new Vector2(0.5f, 0.5f));
                var sr = NewSprite("BloodPool", f?[0], new Color(0.43f, 0.07f, 0.09f, 0.9f), _root, 1);
                sr.transform.position = new Vector3(at.x, at.y - 0.1f, 0f);
                if (f == null) sr.transform.localScale = new Vector3(0.6f, 0.35f, 1f);
                _live.Add(new Particle { Sr = sr, Born = Time.time, Life = 1.2f, Frames = f, Stick = true });
            });
        }

        /// <summary>Chunks thrown along the blow, each leaving a smear where it lands.</summary>
        public void Gibs(Vector3 at, float dx, float dy, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float sp = Random.Range(2f, 4.5f), sx = Random.Range(-0.8f, 0.8f);
                var vel = new Vector3((dx - dy * sx) * sp, (dy + dx * sx) * sp, 0f);
                var col = i % 3 == 0 ? new Color(0.43f, 0.07f, 0.09f) : i % 3 == 1 ? new Color(0.7f, 0.13f, 0.16f) : new Color(0.55f, 0.42f, 0.38f);
                var sr = NewSprite("Gib", White(), col, _root, 2);
                sr.transform.position = at;
                sr.transform.localScale = Vector3.one * ((Random.value < 0.5f ? 1f : 2f) / 16f);
                _live.Add(new Particle { Sr = sr, Born = Time.time, Life = 0.6f, Vel = vel, Friction = 4f, Stick = true });
                At(Time.time + 0.25f, () => Splat(new Vector3(at.x + vel.x * 0.2f, at.y + vel.y * 0.2f, 0f), dx, dy));
            }
        }

        private void Decal(SpriteRenderer sr)
        {
            _decals.Enqueue(sr);
            while (_decals.Count > MaxDecals) { var old = _decals.Dequeue(); if (old != null) Object.Destroy(old.gameObject); }   // ponytail: oldest blood goes first
        }

        private static SpriteRenderer NewSprite(string name, Sprite sprite, Color fallback, Transform parent, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite != null ? sprite : White();
            sr.color = sprite != null && sprite != _white ? Color.white : fallback;
            sr.sortingOrder = order >= 8 ? WorldArt.SortTop + order : order;   // tracers, flashes, impacts over the y-sorted world
            return sr;
        }

        /// <summary>A horizontal strip of `count` frames from Resources (cached), or null if the art is missing.</summary>
        public static Sprite[] Strip(string path, int count, float ppu, Vector2 pivot)
        {
            if (Strips.TryGetValue(path, out var hit)) return hit;
            var tex = Resources.Load<Texture2D>(path);
            Sprite[] frames = null;
            if (tex != null)
            {
                tex.filterMode = FilterMode.Point;
                int w = tex.width / count;
                frames = new Sprite[count];
                for (int i = 0; i < count; i++) frames[i] = Sprite.Create(tex, new Rect(i * w, 0, w, tex.height), pivot, ppu);
            }
            return Strips[path] = frames;
        }

        private static Sprite White()
        {
            if (_white != null) return _white;
            var tex = new Texture2D(1, 1) { filterMode = FilterMode.Point };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            return _white = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
    }
}
