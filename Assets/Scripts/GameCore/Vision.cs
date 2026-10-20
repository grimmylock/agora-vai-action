// Batch 30-31: what the player can see right now (Zero Sievert style). Line of sight is worked out
// once per move for every cell within MaxRadius; what is actually seen also depends on the light
// (Atmosphere: a short radius at night or in fog, plus the flashlight cone toward where you aim).
// Unseen cells are drawn darker, and the enemies standing in them are not drawn at all
// (TacticalGridView), can't be hovered for info and can't be click-targeted.
namespace GameCore
{
    public static class Vision
    {
        public const int MaxRadius = 30;   // Batch 65: was 13; real time: 30, the far zoom's corners (the look cone reaches them)
        /// <summary>Batch 127c (Yan: "an awareness meter for the player (perception stat boosts it)"): how far a hostile you can see
        /// raises the ! over your head and pulls the camera out to it - 14 tiles at Perception 10, +1 a point, at most your sight.</summary>
        public static int SenseRange(Player p) => System.Math.Max(8, System.Math.Min(MaxRadius, 14 + (p.Stats.P - 10)));
        public static bool Senses(Player p, Mutant e) => e.IsAlive() && !Companion.Is(e) && !EnemyAI.FriendlyToPlayer(e) && Shows(e)
            && CombatantUtil.Distance(p, e) <= SenseRange(p);
        public const float ConeHalfAngle = 0.6f;            // radians, ~34 degrees either side of the aim
        // Yan's first real-time playtest ("make the entire screen seeable as long as im looking at it (so where the cursor is
        // pointing)"): a look cone toward the mouse, ~50 degrees either side, that sees as far as the day allows - the screen's
        // edge in daylight (30), less in rain, fog and dusk, nothing extra at night (the flashlight does that). All round you
        // the old radius stays. LookReach is also how far an enemy may shoot you from (RtAi: a fair shot).
        public const float LookHalfAngle = 0.87f;
        private static float _look;
        public static float LookReach => _look > 0f ? System.Math.Max(_look, _radius) : MaxRadius;
        const int LeanReach = 12;                           // leaning a step shows round corners this far (the cost was 4x the cone)
        private static bool[] _los, _show;
        private static int _w, _h, _px = int.MinValue, _py, _stamp = -1;
        private static object _tiles;
        private static float _radius = MaxRadius, _cone, _aim;
        public static int Version { get; private set; }      // bumps whenever what's seen changes (the overlay redraws then)
        public static float Radius => _radius;
        public static float ConeReach => _cone;
        public static float Aim => _aim;
        public static int PlayerX => _px;
        public static int PlayerY => _py;

        public static void Update(Player p, int worldStamp) => Update(p, worldStamp, MaxRadius, 0f, 0f);

        /// <summary>Call once per frame; cheap when nothing changed. aim = radians, world angle (y up).</summary>
        public static void Update(Player p, int worldStamp, float radius, float cone, float aim, float look = 0f)
        {
            if (p == null) return;
            bool moved = _los == null || p.X != _px || p.Y != _py || worldStamp != _stamp || !ReferenceEquals(_tiles, TerrainMap.Tiles);
            bool lit = System.Math.Abs(radius - _radius) > 0.25f || System.Math.Abs(cone - _cone) > 0.25f
                       || System.Math.Abs(System.Math.Min(look, MaxRadius) - _look) > 0.25f
                       || (cone > 0f || look > 0f) && System.Math.Abs(System.Math.IEEERemainder(aim - _aim, 2 * System.Math.PI)) > 0.08f;
            if (!moved && !lit) return;
            _radius = System.Math.Min(radius, MaxRadius); _cone = System.Math.Min(cone, MaxRadius); _aim = aim; _look = System.Math.Min(look, MaxRadius);
            Version++;
            if (!moved) return;
            _px = p.X; _py = p.Y; _stamp = worldStamp; _tiles = TerrainMap.Tiles;
            _w = TerrainMap.Width; _h = TerrainMap.Height;
            if (_los == null || _los.Length != _w * _h) _los = new bool[_w * _h]; else System.Array.Clear(_los, 0, _los.Length);
            var grid = GameServices.Grid;
            for (int y = p.Y - MaxRadius; y <= p.Y + MaxRadius; y++)
                for (int x = p.X - MaxRadius; x <= p.X + MaxRadius; x++)
                {
                    if (x < 0 || y < 0 || x >= _w || y >= _h) continue;
                    int dx = x - p.X, dy = y - p.Y;
                    if (dx * dx + dy * dy > MaxRadius * MaxRadius) continue;
                    _los[y * _w + x] = dx * dx + dy * dy <= 2 || grid == null || grid.CheckLineOfSight(p.X, p.Y, x, y);
                }
            // what's drawn as seen also counts what you'd see leaning one step (into a doorway, round a corner):
            // a single cell's line of sight hugs walls and leaves room corners dark. Gameplay (Sees) stays strict.
            if (_show == null || _show.Length != _los.Length) _show = new bool[_los.Length];
            System.Array.Copy(_los, _show, _los.Length);
            if (grid != null)
                foreach (var (ox, oy) in new[] { (p.X + 1, p.Y), (p.X - 1, p.Y), (p.X, p.Y + 1), (p.X, p.Y - 1) })
                {
                    if (ox < 0 || oy < 0 || ox >= _w || oy >= _h || TerrainMap.BlocksSight(ox, oy)) continue;
                    for (int y = oy - LeanReach; y <= oy + LeanReach; y++)
                        for (int x = ox - LeanReach; x <= ox + LeanReach; x++)
                        {
                            if (x < 0 || y < 0 || x >= _w || y >= _h || _show[y * _w + x]) continue;
                            int dx = x - p.X, dy = y - p.Y;
                            if (dx * dx + dy * dy > LeanReach * LeanReach) continue;
                            if (grid.CheckLineOfSight(ox, oy, x, y)) _show[y * _w + x] = true;
                        }
                }
        }

        public static void Reset() { _los = null; _show = null; _px = int.MinValue; _radius = MaxRadius; _cone = 0f; _look = 0f; Version++; }

        /// <summary>0..1 how lit a cell is from the player's own light (1 inside the radius, the cone
        /// fading toward its edges and its reach). Line of sight not included.</summary>
        public static float Light(float x, float y)
        {
            float dx = x - _px, dy = y - _py, d = (float)System.Math.Sqrt(dx * dx + dy * dy);
            float near = System.Math.Max(Clamp01((_radius - d) / 1.5f + 0.3f), LightMap.At((int)System.Math.Round(x), (int)System.Math.Round(y)));   // Batch 92: lamps
            if (_cone <= 0f && _look <= 0f || d < 0.5f) return near;
            float ang = (float)System.Math.Atan2(-dy, dx);                      // grid y points down; aim is world (y up)
            float da = (float)System.Math.Abs(System.Math.IEEERemainder(ang - _aim, 2 * System.Math.PI));
            float cone = _cone <= 0f ? 0f : Clamp01((ConeHalfAngle - da) / 0.25f) * Clamp01((_cone - d) / 3f);
            float look = _look <= 0f ? 0f : Clamp01((LookHalfAngle - da) / 0.25f + 0.3f) * Clamp01((_look - d) / 1.5f + 0.3f);
            return System.Math.Max(near, System.Math.Max(cone, look));
        }

        static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;

        /// <summary>Batch 123: false until line of sight has been worked out for the current map (Sees answers "yes" before
        /// that, which let a toll be asked from off screen right after the view was rebuilt).</summary>
        public static bool Ready => _los != null;

        public static bool Sees(int x, int y)
        {
            if (_los == null) return true;
            if (x < 0 || y < 0 || x >= _w || y >= _h || !_los[y * _w + x]) return false;
            return Light(x, y) > 0.35f;
        }

        /// <summary>Living enemies are only shown, described and targetable while seen; bodies stay put.</summary>
        public static bool Shows(ICombatant u) => u == null || !u.IsAlive() || Sees(u.X, u.Y) && !(u is Mutant m && m.HiddenByCloak());   // Batch 49: cloak hides

        /// <summary>Drawn as in sight (ignoring light): line of sight from the player's cell or one step from it.
        /// The overlay keeps lit-but-blocked cells dark; roofs come off rooms seen into.</summary>
        public static bool InSight(int x, int y) => _show == null || x >= 0 && y >= 0 && x < _w && y < _h && _show[y * _w + x];
    }
}
