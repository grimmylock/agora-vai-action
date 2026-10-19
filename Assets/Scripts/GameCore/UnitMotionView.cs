// New (Batch 24): how units move and die on the map.
//  - Walking: the rules move a unit tile by tile in one frame (TacticalGridSystem.AttemptMove records
//    every step in CombatFx.Steps); here each unit walks that route at walking pace, alternating
//    its feet (armor_walk_a/b.png, ArtSource/make_walk_atlas.py). Mutants slide (no walk frames yet).
//    Teleports that skip AttemptMove (an anomaly throw, a squad placed on entry) slide if short, snap if far.
//  - Dying: the body splits into head / torso / two legs (sliced from its own sprite, no extra art),
//    the floating gloves stay with it, and it falls over away from the killing blow as a floppy ragdoll
//    that is simply left where it comes to rest. The gun drops next to it and blood pools underneath.
//    Big overkill (heavy rounds, explosions) tears the head and/or legs off and throws gibs.
//    Corpses still sit on their tile, so clicking them to loot works as before.
// Everything here is presentation: nothing in the rules reads it back.
using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    public class UnitMotionView
    {
        public const float WalkSpeed = 3f;          // tiles per second (Yan: 5 was too fast to see the steps)
        public const float ExploreSpeed = 4.2f;     // Batch 30: free walking out of combat
        /// <summary>Steps the player's sprite still has to walk (PlayerController paces held-key walking on it).</summary>
        public static int PlayerQueue;
        private const float SettleTime = 1.1f;
        /// <summary>Batch 102: how close (tiles) a walker gets to a waypoint before turning for the next one, so routes read as
        /// straight lines and curves instead of tile-centre zigzags. Purely visual.</summary>
        private const float CornerCut = 0.45f;

        private class Part
        {
            public SpriteRenderer Sr;
            public Vector3 P, V;                    // relative to the corpse anchor
            public float Rot, W;
            public float Jx, Jy;                    // joint on the torso (torso frame)
            public float RotOff;                    // how this limb lies relative to the torso (random per death)
            public bool Free;                       // torn off: no joint holding it
            public int Row0, Col0, H, Wd;           // the rectangle of the body sprite this piece was cut from
        }

        private class Ragdoll
        {
            public Vector3 Anchor;
            public Part Torso, Head, Gun;
            public readonly List<Part> Pieces = new List<Part>();   // head, left leg, right leg, gloves
            public float Born, Fall;                // Fall = where the torso ends up (degrees)
            public float K = 1f;                    // joint stiffness: low = floppy, high = stiff (random per death)
            public bool Settled;
        }

        private class State
        {
            public bool Init;
            public Vector3 Vis;
            public int LastX, LastY;
            public float Stride;
            public readonly Queue<Vector3> Path = new Queue<Vector3>();
            public Ragdoll Rag;
            public readonly List<(int x, int y, int v)> Blood = new List<(int, int, int)>();   // stains on the body (sprite px)
        }

        private readonly Transform _root;
        private readonly CombatFxView _fx;
        private readonly Dictionary<ICombatant, State> _s = new Dictionary<ICombatant, State>();

        public UnitMotionView(Transform root, CombatFxView fx)
        {
            _root = root; _fx = fx;
            CombatFx.Steps.Clear();
        }

        private State Get(ICombatant u)
        {
            if (!_s.TryGetValue(u, out var s)) _s[u] = s = new State();
            return s;
        }

        // ---------------- walking ----------------

        /// <summary>Hand the recorded steps to each unit's walk queue. Call once per frame before Place.</summary>
        public void DrainSteps()
        {
            foreach (var (u, x, y) in CombatFx.Steps)
            {
                if (!_s.TryGetValue(u, out var s) || !s.Init) continue;     // not on screen yet: Place snaps it
                s.Path.Enqueue(TacticalGridView.GridToWorld(x, y));
                s.LastX = x; s.LastY = y;
            }
            CombatFx.Steps.Clear();
        }

        /// <summary>Where to draw the unit this frame (snapped to the character pixel grid).</summary>
        public Vector3 Place(ICombatant u, Vector3 logical)
        {
            var s = Get(u);
            if (!s.Init || s.Rag != null)
            {
                s.Init = true; s.Vis = logical; s.LastX = u.X; s.LastY = u.Y; s.Path.Clear();
                return logical;
            }
            if (u.X != s.LastX || u.Y != s.LastY)                          // moved without AttemptMove
            {
                s.LastX = u.X; s.LastY = u.Y; s.Path.Clear();
                if (Dist(s.Vis, logical) > 3.5f) { s.Vis = logical; return logical; }
                s.Path.Enqueue(logical);
            }
            if (s.Path.Count == 0 && Dist(s.Vis, logical) > 0.01f) s.Path.Enqueue(logical);

            float pace = (EncounterState.InCombat ? WalkSpeed : ExploreSpeed) * (u == Player.Current && Player.Current.Sneaking ? 0.5f : 1f);   // Batch 66
            float budget = (pace + (u == Player.Current ? 0 : Mathf.Max(0, s.Path.Count - 4) * 0.5f)) * Time.deltaTime;   // others catch up a little on long routes; the player keeps one pace however far the click (Yan, b67)
            while (budget > 0f && s.Path.Count > 0)
            {
                if (s.Path.Count > 1 && Dist(s.Vis, s.Path.Peek()) < CornerCut) s.Path.Dequeue();   // Batch 102: round the corner, don't touch the tile centre
                var t = s.Path.Peek();
                float d = Dist(s.Vis, t);
                if (d <= budget) { s.Vis = t; s.Path.Dequeue(); budget -= d; s.Stride += d; }
                else
                {
                    s.Vis = new Vector3(s.Vis.x + (t.x - s.Vis.x) / d * budget, s.Vis.y + (t.y - s.Vis.y) / d * budget, 0f);
                    s.Stride += budget; budget = 0f;
                }
            }
            if (u == Player.Current) PlayerQueue = s.Path.Count;
            return new Vector3(Mathf.Round(s.Vis.x * 16f) / 16f, Mathf.Round(s.Vis.y * 16f) / 16f, 0f);
        }

        public bool Known(ICombatant u) => u != null && _s.TryGetValue(u, out var s) && s.Init;

        public bool IsMoving(ICombatant u) => u != null && _s.TryGetValue(u, out var s) && s.Path.Count > 0;

        /// <summary>-1 standing, 0 = left foot up, 1 = right foot up. Stand, left, stand, right per tile.</summary>
        public int WalkFrame(ICombatant u)
        {
            if (!_s.TryGetValue(u, out var s) || s.Path.Count == 0) return -1;
            return HeldPoseMath.WalkFrame(s.Stride);
        }

        // ---------------- blood on the body ----------------

        // Where a stain can sit on a human sprite without spilling off the silhouette (inside the outline).
        private static readonly HashSet<(int, int)> BodyPx = BuildBodyPx();
        private static HashSet<(int, int)> BuildBodyPx()
        {
            var h = new HashSet<(int, int)>();
            for (int y = 2; y <= 6; y++) for (int x = 6; x <= 9; x++) h.Add((x, y));     // head / mask
            for (int y = 8; y <= 11; y++) for (int x = 5; x <= 10; x++) h.Add((x, y));    // torso
            for (int y = 12; y <= 14; y++) foreach (int x in new[] { 5, 6, 9, 10 }) h.Add((x, y));   // legs
            return h;
        }
        private static readonly (int dx, int dy)[][] StainShapes =
        {
            new[] { (0, 0) }, new[] { (0, 0), (1, 0) }, new[] { (0, 0), (0, 1) }, new[] { (0, 0), (1, 0), (0, 1) },
        };
        private readonly Dictionary<SpriteRenderer, List<SpriteRenderer>> _stainPool = new Dictionary<SpriteRenderer, List<SpriteRenderer>>();
        private const int MaxStains = 10;

        /// <summary>A hit leaves a stain on a human: head 15%, torso 55%, legs 30%. It stays for the rest of
        /// the visit and goes with the body when it falls.</summary>
        public void Bleed(ICombatant u)
        {
            if (!GameConstants.Gore || !(u is Player || u is HumanEnemy)) return;
            var s = Get(u);
            if (s.Blood.Count >= MaxStains) return;
            float r = Random.value;
            int y0 = r < 0.15f ? 2 : r < 0.7f ? 8 : 12, y1 = r < 0.15f ? 6 : r < 0.7f ? 11 : 14;
            for (int tries = 0; tries < 12; tries++)
            {
                int v = Random.Range(0, StainShapes.Length), x = Random.Range(5, 11), y = Random.Range(y0, y1 + 1);
                bool fits = true;
                foreach (var (ox, oy) in StainShapes[v]) fits &= BodyPx.Contains((x + ox, y + oy));
                if (fits) { s.Blood.Add((x, y, v)); return; }
            }
        }

        public int StainCount(ICombatant u) => _s.TryGetValue(u, out var s) ? s.Blood.Count : 0;

        /// <summary>Show this unit's stains on whatever marker draws it this frame (markers are reused by index).</summary>
        public void SyncStains(ICombatant u, SpriteRenderer marker, bool show)
        {
            if (!_stainPool.TryGetValue(marker, out var pool)) _stainPool[marker] = pool = new List<SpriteRenderer>();
            var blood = show && _s.TryGetValue(u, out var s) ? s.Blood : null;
            int n = blood?.Count ?? 0;
            while (pool.Count < n) pool.Add(NewStain(marker.transform, marker.sortingOrder));
            for (int i = 0; i < pool.Count; i++)
            {
                bool on = i < n;
                pool[i].gameObject.SetActive(on);
                if (!on) continue;
                var (x, y, v) = blood[i];
                pool[i].sprite = Stain(v);
                pool[i].transform.localPosition = new Vector3((x - 8f) / 16f, (8f - y) / 16f, -0.01f);
            }
        }

        private static SpriteRenderer NewStain(Transform parent, int order)
        {
            var go = new GameObject("Blood");
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = order;            // same layer as the body, nudged toward the camera (z) so it draws on top
            return sr;
        }

        private static readonly Dictionary<int, Sprite> StainCache = new Dictionary<int, Sprite>();
        // Batch 29: the project enters Play mode without a domain reload, so statics survive between
        // runs while the sprites/textures they point at are destroyed - clear the caches each run.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCaches() { StainCache.Clear(); }

        /// <summary>Stain sprites built in code (dark-red blood palette), pivot at the top-left pixel. v 4 = a torn edge.</summary>
        private static Sprite Stain(int v)
        {
            if (StainCache.TryGetValue(v, out var hit)) return hit;
            var cols = new[] { new Color(0.54f, 0.1f, 0.13f), new Color(0.7f, 0.13f, 0.16f), new Color(0.43f, 0.07f, 0.09f) };
            var shape = v < StainShapes.Length ? StainShapes[v] : new[] { (0, 0), (1, 0), (2, 0), (3, 0) };
            int w = 1, h = 1;
            foreach (var (ox, oy) in shape) { w = Mathf.Max(w, ox + 1); h = Mathf.Max(h, oy + 1); }
            var tex = new Texture2D(w, h) { filterMode = FilterMode.Point };
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) tex.SetPixel(x, y, new Color(0f, 0f, 0f, 0f));
            int i = 0;
            foreach (var (ox, oy) in shape) tex.SetPixel(ox, h - 1 - oy, cols[(i++ + v) % cols.Length]);
            tex.Apply();
            return StainCache[v] = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0f, 1f), 16f);
        }

        // ---------------- dying ----------------

        public bool Ragdolled(ICombatant u) => u != null && _s.TryGetValue(u, out var s) && s.Rag != null;

        /// <summary>Start the death fall. (dx, dy) = direction of the killing blow (world, y up).
        /// gib: 0 = whole body, 1 = one piece comes off (head or a leg), 2 = several (a blast, a huge hit).
        /// settled = a body that was already dead before this view existed: drawn where it would have come to rest.
        /// Every death rolls its own push, spin, floppiness and limb angles, so no two falls look the same:
        /// most topple over, some spin round as they go, some just crumple where they stood.</summary>
        public void Kill(ICombatant u, Sprite body, HeldWeaponView held, Vector3 at, float dx, float dy, int gib, bool settled)
        {
            var s = Get(u);
            if (s.Rag != null || body == null || body.rect.width != 16f || body.rect.height != 16f) return;
            float l = Mathf.Sqrt(dx * dx + dy * dy);
            float ang = l < 0.01f ? Random.Range(0f, 6.283f) : Mathf.Atan2(dy, dx) + Random.Range(-0.45f, 0.45f);
            dx = Mathf.Cos(ang); dy = Mathf.Sin(ang);
            if (!GameConstants.Gore) gib = 0;

            var r = new Ragdoll { Anchor = at, Born = Time.time, K = Random.Range(0.6f, 1.4f) };
            float side = Mathf.Abs(dx) > 0.3f ? (dx > 0f ? -1f : 1f) : (Random.value < 0.5f ? -1f : 1f);
            if (Random.value < 0.15f) side = -side;                        // sometimes twists the "wrong" way
            float style = Random.value;
            bool crumple = gib == 0 && style > 0.75f, spin = !crumple && style > 0.55f;
            r.Fall = crumple ? Random.Range(-30f, 30f) : side * (spin ? Random.Range(200f, 290f) : Random.Range(60f, 140f));

            var tex = body.texture; var rc = body.rect;
            Part Cut(string n, int col, int row, int w, int h, float jx, float jy)
            {
                var p = NewPart(u.Name + n, Sprite.Create(tex, new Rect(rc.x + col, rc.y + 16 - row - h, w, h), new Vector2(0.5f, 0.5f), 16), jx, jy, false);
                p.Col0 = col; p.Row0 = row; p.H = h; p.Wd = w;
                return p;
            }
            r.Torso = Cut("_Torso", 0, 7, 16, 5, 0f, 0f);
            r.Torso.P = new Vector3(0f, -0.09f, 0f);
            r.Pieces.Add(r.Head = Cut("_Head", 0, 0, 16, 7, 0f, 0.37f));
            r.Pieces.Add(Cut("_LegL", 0, 12, 8, 4, -0.25f, -0.28f));
            r.Pieces.Add(Cut("_LegR", 8, 12, 8, 4, 0.25f, -0.28f));
            if (u is Player || u is HumanEnemy)                             // the floating gloves stay with the body
            {
                var glove = HeldWeaponView.Glove(u is Player pl ? pl.EquippedArmor : ((HumanEnemy)u).EquippedArmor, held != null && held.Visible);   // small if it died holding something
                var hl = NewPart(u.Name + "_HandL", glove, -0.4f, -0.06f, false); hl.Sr.flipX = true;
                r.Pieces.Add(hl);
                r.Pieces.Add(NewPart(u.Name + "_HandR", glove, 0.4f, -0.06f, false));
            }
            if (crumple)                                                    // slumps: head sinks, legs splay, arms drop
                foreach (var p in r.Pieces) { p.Jx *= p.Jy < -0.2f ? 1.5f : 0.8f; p.Jy *= p.Jy > 0.2f ? 0.7f : p.Jy < -0.2f ? 0.7f : 1f; if (p.Jy > -0.2f && p.Jy < 0.2f) p.Jy -= 0.1f; }
            foreach (var p in r.Pieces)
            {
                p.P = new Vector3(r.Torso.P.x + p.Jx, r.Torso.P.y + p.Jy, 0f);
                p.RotOff = Random.Range(-25f, 25f);
            }

            float push = (gib > 0 ? 1.4f : 0.9f) * Random.Range(0.6f, 1.35f) * (crumple ? 0.35f : 1f);
            r.Torso.V = new Vector3(dx * push, dy * push, 0f);
            r.Torso.W = spin ? side * Random.Range(900f, 1400f) : r.Fall * Random.Range(3f, 8f);
            foreach (var p in r.Pieces)
            {
                p.V = new Vector3(dx * push * (1f + p.Jy) + Random.Range(-0.6f, 0.6f), dy * push * (1f + p.Jy) + Random.Range(-0.6f, 0.6f), 0f);
                p.W = Random.Range(-250f, 250f);
            }

            var torn = Dismember(r, gib);
            foreach (var p in torn)
            {
                p.Free = true;
                float sp = Random.Range(2.5f, 4.5f), sd = Random.Range(-1f, 1f);
                p.V = new Vector3((dx - dy * sd) * sp, (dy + dx * sd) * sp, 0f);
                p.W = Random.Range(-900f, 900f);
            }
            if (held != null && held.Visible && held.GunSprite != null)
            {
                r.Gun = NewPart(u.Name + "_Gun", held.GunSprite, 0f, 0f, true);
                r.Gun.P = held.Gun.position - at;
                r.Gun.Rot = held.Angle; r.Gun.Sr.flipY = held.AimLeft;
                r.Gun.V = new Vector3(dx * 0.8f + Random.Range(-0.5f, 0.5f), dy * 0.8f + Random.Range(-0.5f, 0.5f), 0f);
                r.Gun.W = Random.Range(-500f, 500f);
            }
            s.Rag = r;
            MoveStainsToPieces(s, r, torn);

            if (GameConstants.Gore)
            {
                _fx.Pool(new Vector3(at.x + dx * 0.25f * push, at.y + dy * 0.25f * push, 0f), settled ? 0f : 0.25f);   // roughly where the body ends up
                if (torn.Count > 0) _fx.Gibs(at, dx, dy, torn.Count + 2);
            }
            if (settled) { for (int i = 0; i < 70; i++) Step(r, 1f / 60f); r.Settled = true; }   // play the fall out instantly
            Draw(r);
        }

        /// <summary>The body's stains go onto the piece they were on; torn pieces and the torso get a bloody edge where they parted.</summary>
        private void MoveStainsToPieces(State s, Ragdoll r, List<Part> torn)
        {
            if (!GameConstants.Gore) return;
            var cut = new List<Part> { r.Torso };
            cut.AddRange(r.Pieces);
            foreach (var (x, y, v) in s.Blood)
            {
                var p = cut.Find(c => c.H > 0 && y >= c.Row0 && y < c.Row0 + c.H && x >= c.Col0 && x < c.Col0 + c.Wd);
                if (p != null) AddStain(p, x, y, v);
            }
            foreach (var p in torn)
            {
                if (p.H == 0) continue;                                    // gloves: no stump
                bool head = p.Row0 == 0;
                int row = head ? p.Row0 + p.H - 1 : p.Row0;                // the edge that was attached
                AddStain(p, head ? 6 : p.Col0 + (p.Col0 == 0 ? 5 : 1), row, head ? 4 : 1);
                AddStain(r.Torso, head ? 6 : (p.Col0 == 0 ? 5 : 9), head ? 7 : 11, head ? 4 : 1);
            }
        }

        private static void AddStain(Part p, int x, int y, int v)
        {
            var sr = NewStain(p.Sr.transform, p.Sr.sortingOrder);
            sr.sprite = Stain(v);
            sr.color = p.Sr.color;
            sr.transform.localPosition = new Vector3((x - p.Col0 - p.Wd / 2f) / 16f, (p.Row0 + p.H / 2f - y) / 16f, -0.01f);
        }

        /// <summary>Which pieces come off. 1: the head or one leg. 2: two or more, sometimes everything.</summary>
        private static List<Part> Dismember(Ragdoll r, int gib)
        {
            var torn = new List<Part>();
            if (gib <= 0) return torn;
            Part head = r.Pieces[0], legL = r.Pieces[1], legR = r.Pieces[2];
            int roll = Random.Range(0, gib == 1 ? 3 : 4);
            if (gib == 1) torn.Add(roll == 0 ? head : roll == 1 ? legL : legR);
            else if (roll == 0) { torn.Add(head); torn.Add(Random.value < 0.5f ? legL : legR); }
            else if (roll == 1) { torn.Add(legL); torn.Add(legR); }
            else if (roll == 2) { torn.Add(head); torn.Add(legL); torn.Add(legR); }
            else torn.AddRange(r.Pieces);                                  // blown apart, gloves and all
            return torn;
        }

        public void TickRagdolls()
        {
            float now = Time.time, dt = Mathf.Min(Time.deltaTime, 0.05f);
            foreach (var s in _s.Values)
            {
                var r = s.Rag;
                if (r == null || r.Settled || r.Torso.Sr == null) continue;
                if (now - r.Born >= SettleTime) { r.Settled = true; continue; }   // just stops: no snapping back
                Step(r, dt);
                Draw(r);
            }
        }

        private static void Step(Ragdoll r, float dt)
        {
            var t = r.Torso;
            Damp(t, 6f, dt);
            t.W += (r.Fall - t.Rot) * 90f * dt; t.W *= Mathf.Exp(-9f * dt); t.Rot += t.W * dt;
            foreach (var p in r.Pieces) Joint(p, t, r.K, dt);
            if (r.Gun != null) { Damp(r.Gun, 5f, dt); r.Gun.W *= Mathf.Exp(-4f * dt); r.Gun.Rot += r.Gun.W * dt; }
        }

        private static void Damp(Part p, float friction, float dt)
        {
            p.V *= Mathf.Exp(-friction * dt);
            p.P += p.V * dt;
            float lim = p.Free ? 1.4f : 0.5f;                               // a corpse stays on its tile
            p.P = new Vector3(Mathf.Clamp(p.P.x, -lim, lim), Mathf.Clamp(p.P.y, -lim, lim), 0f);
        }

        /// <summary>Pieces hang off the torso on soft springs and lag its rotation (the floppiness).</summary>
        private static void Joint(Part p, Part torso, float k, float dt)
        {
            if (p.Free) { Damp(p, 4f, dt); p.W *= Mathf.Exp(-3f * dt); p.Rot += p.W * dt; return; }
            float a = torso.Rot * Mathf.Deg2Rad, c = Mathf.Cos(a), sn = Mathf.Sin(a);
            var target = new Vector3(torso.P.x + p.Jx * c - p.Jy * sn, torso.P.y + p.Jx * sn + p.Jy * c, 0f);
            p.V += (target - p.P) * (140f * k * dt);
            Damp(p, 10f, dt);
            p.W += (torso.Rot + p.RotOff - p.Rot) * 70f * k * dt; p.W *= Mathf.Exp(-8f * dt); p.Rot += p.W * dt;
        }

        private static void Draw(Ragdoll r)
        {
            Draw(r, r.Torso);
            foreach (var p in r.Pieces) Draw(r, p);
            if (r.Gun != null) Draw(r, r.Gun);
        }

        private static void Draw(Ragdoll r, Part p)
        {
            if (p.Sr == null) return;
            p.Sr.transform.position = new Vector3(Mathf.Round((r.Anchor.x + p.P.x) * 16f) / 16f, Mathf.Round((r.Anchor.y + p.P.y) * 16f) / 16f, 0f);
            p.Sr.transform.localRotation = Quaternion.Euler(0f, 0f, p.Rot);
        }

        private Part NewPart(string name, Sprite sprite, float jx, float jy, bool free)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 4;                                           // under the living (5+)
            sr.color = new Color(0.82f, 0.8f, 0.78f, 1f);                   // a touch darker than the living
            return new Part { Sr = sr, Jx = jx, Jy = jy, Free = free };
        }

        private static float Dist(Vector3 a, Vector3 b) => Mathf.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y));
    }
}
