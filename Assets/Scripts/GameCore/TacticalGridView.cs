// Top-down grid renderer, built at runtime (no scene wiring). One marker per tile/unit; each
// loads real pixel art from Assets/Resources/Sprites/ (see PixelSpriteImporter for import
// settings) if present, tinted per type/faction via SpriteRenderer.color same as before - falls
// back automatically to a flat-colored square for any sprite that isn't dropped in yet, so this
// keeps working with zero art. This is the only view - there's no separate Overworld screen to
// hide behind (see ZoneTravel.EnterZone) - and it rebuilds itself whenever the current zone
// changes. GridToWorld/WorldToGrid is the stable contract other systems build on.
// Added automatically by GameBootstrap.
using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    public class TacticalGridView : MonoBehaviour
    {
        private const float TileWorldSize = 1f;

        private static Sprite _whiteSprite;

        // Real art, loaded once from Assets/Resources/Sprites/ if present (see
        // PixelSpriteImporter for import settings). Any sprite that's still null just falls back
        // to the flat white square + solid color it always used - dropping the art in upgrades
        // the look automatically, nothing else has to change.
        private static bool _spritesLoaded;
        private static Sprite _spriteBeing, _spriteAnomaly, _spriteLoot, _spriteExit, _spriteGroundItem;
        // Batch 29: the project enters Play mode without a domain reload, so statics survive between
        // runs while the sprites/textures they point at are destroyed - clear the caches each run.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCaches() { _whiteSprite = _spriteBeing = _spriteAnomaly = _spriteLoot = _spriteExit = _spriteGroundItem = _shadowSprite = null; _spritesLoaded = false; }


        private static void LoadSpritesOnce()
        {
            if (_spritesLoaded) return;
            _spritesLoaded = true;
            _spriteBeing = Resources.Load<Sprite>("Sprites/unit_being");
            _spriteAnomaly = Resources.Load<Sprite>("Sprites/icon_anomaly");
            _spriteLoot = Resources.Load<Sprite>("Sprites/icon_loot");
            _spriteExit = Resources.Load<Sprite>("Sprites/icon_exit");
            // New (Batch 12): dropped items (GroundItem) - a distinct sack icon so a loose item on
            // the ground doesn't read as the same "lootable container" icon interactive objects use.
            _spriteGroundItem = Resources.Load<Sprite>("Sprites/icon_ground_item");
        }
        private GameObject _root;
        private SpriteRenderer _playerMarker;
        // New (Batch 21): gun + floating hands per human marker (HeldWeapon.cs), keyed by the body marker
        private readonly Dictionary<SpriteRenderer, HeldWeaponView> _held = new Dictionary<SpriteRenderer, HeldWeaponView>();
        private HeldWeaponView Held(SpriteRenderer body)
        {
            if (!_held.TryGetValue(body, out var h)) _held[body] = h = new HeldWeaponView(body);
            return h;
        }
        private readonly List<SpriteRenderer> _enemyMarkers = new List<SpriteRenderer>();
        private CombatFxView _fx;
        private UnitMotionView _motion;   // Batch 24: walking, ragdolls   // Batch 22: muzzle flashes, tracers, recoil, hit flashes, reloads
        private SpriteRenderer MarkerOf(ICombatant unit)
        {
            if (unit == null) return null;
            if (unit == Player.Current) return _playerMarker;
            int i = unit is Mutant m ? EncounterState.Enemies.IndexOf(m) : -1;
            return i >= 0 && i < _enemyMarkers.Count ? _enemyMarkers[i] : null;
        }
        private readonly List<SpriteRenderer> _anomalyMarkers = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> _lootMarkers = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> _groundItemMarkers = new List<SpriteRenderer>();
        private readonly List<(SpriteRenderer sr, string name)> _landmarkMarkers = new List<(SpriteRenderer, string)>();
        private int _exitCount;
        private object _builtTiles;   // the TerrainMap.Tiles array the view was drawn from
        private Sprite _mapSprite;
        private TMPro.TextMeshProUGUI _hoverText;
        // The floor is one composed texture (WorldArt ground layer, or TerrainArt's flat tiles when
        // the world atlas is missing); the reachable area is one soft overlay sprite on top of it.
        private Texture2D _mapTex;
        private Color32[] _mapPx;
        private readonly List<SpriteRenderer> _smokePool = new List<SpriteRenderer>();
        // Batch 30: 3/4 world (WorldArt) - ground texture + y-sorted standing sprites, faded when they hide a unit
        private bool _world;
        private Transform _standingRoot;
        private readonly Dictionary<int, (WorldArt.Standing st, SpriteRenderer sr)> _standing = new Dictionary<int, (WorldArt.Standing, SpriteRenderer)>();
        private readonly HashSet<int> _fires = new HashSet<int>();   // standing cells drawn with the animated bonfire
        private readonly HashSet<SpriteRenderer> _faded = new HashSet<SpriteRenderer>();
        private readonly Dictionary<SpriteRenderer, int> _lastFoot = new Dictionary<SpriteRenderer, int>();
        private readonly Dictionary<SpriteRenderer, SpriteRenderer> _shadows = new Dictionary<SpriteRenderer, SpriteRenderer>();
        private static Sprite _shadowSprite;

        /// <summary>A soft ellipse under a standing unit's feet (same colour as the props' baked shadows).</summary>
        private void UnitShadow(SpriteRenderer marker, bool on)
        {
            if (!_shadows.TryGetValue(marker, out var s) || s == null)
            {
                if (_shadowSprite == null)
                {
                    var tex = new Texture2D(12, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                    var px = new Color32[48];
                    for (int y = 0; y < 4; y++)
                        for (int x = 0; x < 12; x++)
                        {
                            float ex = (x + 0.5f - 6f) / 6f, ey = (y + 0.5f - 2f) / 2f;
                            px[y * 12 + x] = ex * ex + ey * ey <= 1f ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
                        }
                    tex.SetPixels32(px); tex.Apply();
                    _shadowSprite = Sprite.Create(tex, new Rect(0, 0, 12, 4), new Vector2(0.5f, 0.5f), TerrainArt.Px);
                }
                var go = new GameObject("Shadow");
                go.transform.SetParent(marker.transform, false);
                go.transform.localPosition = new Vector3(0f, -7f / TerrainArt.Px, 0f);
                s = go.AddComponent<SpriteRenderer>();
                s.sprite = _shadowSprite;
                s.color = new Color(0.196f, 0.129f, 0.145f, 0.55f);
                s.sortingOrder = marker.sortingOrder - 1;          // under the body inside its sorting group
                _shadows[marker] = s;
            }
            if (s.gameObject.activeSelf != on) s.gameObject.SetActive(on);
            // in clear daylight the blob leans away from the sun and stretches like the props' cast shadows
            float kx = 0f, ky = 0f; bool sun = _world && WorldArt.Sun(out kx, out ky);
            float lean = sun ? kx : 0f;
            s.transform.localPosition = new Vector3(lean * 6f / TerrainArt.Px, (-7f - (sun ? ky * 4f : 0f)) / TerrainArt.Px, 0f);
            s.transform.localScale = new Vector3(1f + Mathf.Abs(lean) * 0.9f, 1f, 1f);
        }
        private readonly HashSet<SpriteRenderer> _fadeNow = new HashSet<SpriteRenderer>();
        private SpriteRenderer _reachSr;
        private Texture2D _reachTex;
        // Batch 31: time of day, weather and line of sight are drawn by AtmosphereView
        private AtmosphereView _atmo;
        private AtmosphereLook _look;
        private string _lookZone;
        private RoofView _roofs;          // Batch 34
        private float _aim;
        private int _worldStamp;
        private System.Threading.Tasks.Task<Color32[]> _bake;
        private int _sunKey = -1;

        private bool _built;
        private string _builtZoneId;
        private int _highlightX = int.MinValue, _highlightY = int.MinValue; private float _highlightMp = float.MinValue;

        private static TacticalGridView _self;
        private void Awake() { _self = this; }
        /// <summary>Batch 129: where a unit is drawn right now (smooth walking included), for lines over its head; null when not shown.</summary>
        public static Vector3? UnitPos(ICombatant u)
        {
            var m = _self != null ? _self.MarkerOf(u) : null;
            return m != null && m.gameObject.activeInHierarchy ? m.transform.position : (Vector3?)null;
        } // camera is sized per-zone now, see Rebuild() -> SetupCamera()

        private void Update()
        {
            bool tactical = GameState.Current.Mode == GameMode.Tactical;
            if (GameState.Current.Mode == GameMode.MainMenu && Player.Current == null) { TickBackdrop(); return; }   // Batch 123
            string zoneId = TerrainMap.ZoneId ?? MacroState.Current.CurrentZoneId;   // the map overhaul: walking into the next region of a tier map doesn't rebuild the view

            if (tactical && (!_built || zoneId != _builtZoneId || TerrainMap.Exits.Count != _exitCount
                || !ReferenceEquals(TerrainMap.Tiles, _builtTiles)))   // a load / restart in the same zone regenerates it   // a route found mid-visit shows up right away
            {
                Rebuild();
                _built = true;
                _builtZoneId = zoneId;
            }

            if (_root != null) _root.SetActive(tactical);
            if (!tactical && _root != null) Sfx.StopAmbient();
            if (!tactical) return;

            var player = Player.Current;
            if (player == null) return;

            // Doors opened / cover destroyed since last frame: recompose just those spots.
            BakeTick();

            if (TerrainMap.DirtyTiles.Count > 0 && _mapTex != null && _bake == null)   // during a re-bake they wait a frame or two
            {
                _worldStamp++;
                foreach (var d in TerrainMap.DirtyTiles)
                {
                    if (!_world) { TerrainArt.Recompose(_mapPx, _builtZoneId, d.x, d.y); continue; }
                    for (int y = d.y - 1; y <= d.y + 1; y++) for (int x = d.x - 1; x <= d.x + 1; x++) WorldArt.Refresh(x, y);
                    WorldArt.PaintGround(_mapPx, d.x - 2, d.y - 2, d.x + 2, d.y + 2);
                    for (int y = d.y - 1; y <= d.y + 1; y++) for (int x = d.x - 1; x <= d.x + 1; x++) PlaceStanding(x, y);
                }
                TerrainMap.DirtyTiles.Clear();
                _mapTex.SetPixels32(_mapPx);
                _mapTex.Apply();
                _highlightX = int.MinValue; // move costs may have changed
            }

            SlideProps();
            _motion.DrainSteps();
            bool playerUp = DrawUnit(player, _playerMarker, new Color(0.2f, 0.6f, 1f));
            // Batch 31: time of day / weather decide how far you see; the flashlight follows your aim
            var real = Atmosphere.Look();
            var look = _lookZone != _builtZoneId ? real : Atmosphere.Ease(_look, real, 1f - Mathf.Exp(-Time.deltaTime / Atmosphere.FadeSeconds));   // Batch 67: fades, a new zone starts as it is
            _look = look; _lookZone = _builtZoneId;
            var aimAt = MouseWorld();
            if (aimAt.HasValue) _aim = Mathf.Atan2(aimAt.Value.y - _playerMarker.transform.position.y, aimAt.Value.x - _playerMarker.transform.position.x);
            Vision.Update(player, _worldStamp * 131 + TerrainMap.Smoke.Count, look.Vision, player.FlashlightOn ? look.Cone : 0f, _aim);
            _roofs?.Tick(player);
            _atmo?.Tick(player, look);
            ColorGrade.Apply(look);
            if (look.Rain > 0.75f) Sfx.Ambient("amb_storm"); else if (look.Rain > 0f) Sfx.Ambient("amb_rain"); else Sfx.Ambient(Sfx.AmbientFor(MacroState.Current.CurrentZoneId));   // the map overhaul: the region you stand in
            bool downNow = Body.Down(player);   // RT batch 2: down - lying on your side, gun dropped to the ground beside you
            float tilt = downNow ? 90f : Rt.Diving ? (Rt.DiveRight ? -70f : 70f) : Rt.Low(player) ? (Rt.DiveRight ? -90f : 90f) : Held(_playerMarker).Lean;   // RT batch 3: into the dive, sprawled after it; RT 7c: leaning into a swing
            _playerMarker.transform.localRotation = Quaternion.Euler(0f, 0f, tilt);
            Held(_playerMarker).MeleeCharge = Melee.ChargeOf(player);                     // RT 7e: the player's wind-ups drive the pose
            Held(_playerMarker).Show(player.EquippedWeapon, player.EquippedArmor, _playerMarker.transform.position, AimPoint(player, _playerMarker), playerUp && !downNow, Away(_playerMarker));
            // Batch 123: while someone on the map talks to you, the camera frames you both (above the dialog panel) and a
            // chevron bobs over the speaker
            var speaker = ChoiceUI.Speaker; var spm = speaker != null && speaker != player ? MarkerOf(speaker) : null;
            FollowCamera(_playerMarker.transform.position, spm != null ? spm.transform.position : (Vector3?)null);
            if (spm != null && _speakerMark == null) _speakerMark = CreateMarker("Speaker", Color.white, 1f, WorldArt.SortTop, _root.transform, Chevron());
            if (_speakerMark != null)
            {
                _speakerMark.gameObject.SetActive(spm != null);
                if (spm != null) _speakerMark.transform.position = spm.transform.position + new Vector3(0f, 1.05f + 0.08f * Mathf.Sin(Time.time * 5f), 0f);
            }

            // Rebuilt against the live enemy roster every frame — cheap at "a handful of enemies
            // per encounter" scale (ponytail: revisit if encounters ever get into the hundreds).
            while (_enemyMarkers.Count < EncounterState.Enemies.Count)
            {
                var em = CreateMarker("Enemy", new Color(1f, 0.25f, 0.2f), TileWorldSize * 0.7f, 5, _root.transform, _spriteBeing);
                em.gameObject.AddComponent<UnityEngine.Rendering.SortingGroup>();
                _enemyMarkers.Add(em);
                _alertMarks.Add(CreateMarker("Alert", Color.white, 1f, WorldArt.SortTop, _root.transform, Bang()));
            }
            while (_enemyMarkers.Count > EncounterState.Enemies.Count)
            {
                var extra = _enemyMarkers[_enemyMarkers.Count - 1];
                _enemyMarkers.RemoveAt(_enemyMarkers.Count - 1);
                _held.Remove(extra);
                Destroy(extra.gameObject);
                Destroy(_alertMarks[_alertMarks.Count - 1].gameObject); _alertMarks.RemoveAt(_alertMarks.Count - 1);
            }
            if (_playerAlert == null) _playerAlert = CreateMarker("PlayerAlert", BangYou, 1f, WorldArt.SortTop, _root.transform, Bang());
            bool sensing = !EncounterState.InCombat && EncounterState.Enemies.Exists(e => Vision.Senses(player, e));
            _playerAlert.gameObject.SetActive(sensing);
            if (sensing) _playerAlert.transform.position = Above(_playerMarker);
            for (int i = 0; i < EncounterState.Enemies.Count; i++)
            {
                var enemy = EncounterState.Enemies[i];
                // Fix: dead enemies used to just be hidden (SetActive(false)) - that made a
                // HumanEnemy corpse invisible right when CorpseLootUI made it worth clicking on.
                // Keep the marker up so a body stays a visible thing in the world (Batch 24: as a
                // fallen ragdoll; the corpse is still clickable on its tile).
                bool shown = Vision.Shows(enemy);                // out of sight: not drawn at all
                _enemyMarkers[i].gameObject.SetActive(shown);
                bool red = Time.time - enemy.SpottedAt < 2.5f, yellow = !enemy.IsAggroed && enemy.Alert > 0f;
                _alertMarks[i].gameObject.SetActive(shown && enemy.IsAlive() && (red || yellow));
                if (!shown) continue;
                bool alive = DrawUnit(enemy, _enemyMarkers[i], Companion.Is(enemy) ? new Color(0.45f, 1f, 0.55f) : new Color(1f, 0.25f, 0.2f));
                if (_alertMarks[i].gameObject.activeSelf) { _alertMarks[i].color = red ? BangRed : BangYellow; _alertMarks[i].transform.position = Above(_enemyMarkers[i]); }
                if (enemy is HumanEnemy he)   // aware enemies point at the player, the rest where they look (Batch 128); corpses and the surrendered drop it
                    Held(_enemyMarkers[i]).Show(he.EquippedWeapon, he.EquippedArmor, _enemyMarkers[i].transform.position,
                        AimPoint(he, _enemyMarkers[i]), alive && he.Morale != "Surrendered", Away(_enemyMarkers[i]));
                else if (_held.TryGetValue(_enemyMarkers[i], out var stale)) stale.Show(null, null, Vector3.zero, null, false);   // slot reused by a mutant
            }

            // Anomalies stay invisible (per AnomalyEngine.RevealAnomaly) until something fails a
            // check stepping onto one — only draw the ones that have actually been revealed.
            var revealedAnomalies = EncounterState.Anomalies.FindAll(a => a.Revealed);
            while (_anomalyMarkers.Count < revealedAnomalies.Count)
                _anomalyMarkers.Add(CreateMarker("Anomaly", _spriteAnomaly != null ? Color.white : new Color(0.7f, 0.2f, 0.9f), TileWorldSize * 0.5f, 4, _root.transform, _spriteAnomaly));
            while (_anomalyMarkers.Count > revealedAnomalies.Count)
            {
                var extra = _anomalyMarkers[_anomalyMarkers.Count - 1];
                _anomalyMarkers.RemoveAt(_anomalyMarkers.Count - 1);
                Destroy(extra.gameObject);
            }
            AnimateFires();
            for (int i = 0; i < revealedAnomalies.Count; i++)
            {
                var an = revealedAnomalies[i];
                _anomalyMarkers[i].transform.position = GridToWorld(an.X, an.Y);
                _anomalyMarkers[i].sortingOrder = SortAt(_anomalyMarkers[i].transform.position.y);
                // Batch 22: each type has its own 6-frame animation (ArtSource/make_anomalies.py),
                // offset per tile so neighbouring anomalies don't pulse in lockstep
                var frames = AnomalyFrames(an.AnomalyType);
                if (frames != null)
                {
                    _anomalyMarkers[i].sprite = frames[(int)(Time.time / 0.11f + an.X * 7 + an.Y * 3) % frames.Length];
                    _anomalyMarkers[i].transform.localScale = Vector3.one * TileWorldSize;
                }
                var baseCol = _spriteAnomaly != null || frames != null ? Color.white : new Color(0.7f, 0.2f, 0.9f);
                baseCol.a = revealedAnomalies[i].Searched ? 0.4f : 1f;   // already searched: dimmed
                _anomalyMarkers[i].color = baseCol;
            }

            // Story / legend objectives pulse gold so it's clear where E does something.
            float pulse = 0.6f + 0.4f * Mathf.Sin(Time.time * 4f);
            foreach (var (sr, name) in _landmarkMarkers)
            {
                bool goal = StorySystem.IsObjective(zoneId, name);
                sr.color = goal ? new Color(1f, 0.8f, 0.2f, pulse) : new Color(0.85f, 0.85f, 0.6f);
                sr.transform.localScale = Vector3.one * TileWorldSize * (goal ? 0.6f : 0.4f);
            }
            UpdateHover(player);

            // Interactive objects (loot/lore) - unlike exits, these can disappear mid-visit once
            // interacted with (InteractionEngine removes them), so sync by count every frame the
            // same way enemies/anomalies do rather than only at Rebuild().
            var loot = TerrainMap.InteractiveObjects.FindAll(o => TerrainMap.GetTile(o.X, o.Y).Look?.StartsWith("stash_") != true);   // Batch 124: stashes are drawn as props
            while (_lootMarkers.Count < loot.Count)
                _lootMarkers.Add(CreateMarker("Loot", _spriteLoot != null ? Color.white : new Color(0.9f, 0.75f, 0.2f), TileWorldSize * 0.5f, 3, _root.transform, _spriteLoot));
            while (_lootMarkers.Count > loot.Count)
            {
                var extra = _lootMarkers[_lootMarkers.Count - 1];
                _lootMarkers.RemoveAt(_lootMarkers.Count - 1);
                Destroy(extra.gameObject);
            }
            for (int i = 0; i < loot.Count; i++)
            {
                _lootMarkers[i].transform.position = GridToWorld(loot[i].X, loot[i].Y);
                _lootMarkers[i].sortingOrder = SortAt(_lootMarkers[i].transform.position.y);
            }

            // New (Batch 12): dropped items - removed on pickup (PlayerController), so synced by
            // count every frame the same way interactive-object loot already is above.
            var groundItems = TerrainMap.GroundItems;
            while (_groundItemMarkers.Count < groundItems.Count)
                _groundItemMarkers.Add(CreateMarker("GroundItem", _spriteGroundItem != null ? Color.white : new Color(0.6f, 0.45f, 0.25f), TileWorldSize * 0.5f, 3, _root.transform, _spriteGroundItem));
            while (_groundItemMarkers.Count > groundItems.Count)
            {
                var extra = _groundItemMarkers[_groundItemMarkers.Count - 1];
                _groundItemMarkers.RemoveAt(_groundItemMarkers.Count - 1);
                Destroy(extra.gameObject);
            }
            for (int i = 0; i < groundItems.Count; i++)
            {
                _groundItemMarkers[i].transform.position = GridToWorld(groundItems[i].X, groundItems[i].Y);
                _groundItemMarkers[i].sortingOrder = SortAt(_groundItemMarkers[i].transform.position.y) - 1;
            }

            // smoke clouds: a grey haze over each smoked tile
            int smoked = 0;
            foreach (var cell in TerrainMap.Smoke.Keys)
            {
                if (smoked == _smokePool.Count)
                    _smokePool.Add(CreateMarker("Smoke", new Color(0.75f, 0.75f, 0.72f, 0.7f), TileWorldSize, WorldArt.SortTop, _root.transform));   // over everything
                var sr = _smokePool[smoked++];
                sr.gameObject.SetActive(true);
                sr.transform.position = GridToWorld(cell.x, cell.y);
            }
            for (int i = smoked; i < _smokePool.Count; i++) _smokePool[i].gameObject.SetActive(false);

            _fx.Tick();
            _motion.TickRagdolls();
            _fx.ApplyTo(player, _playerMarker);
            for (int i = 0; i < EncounterState.Enemies.Count && i < _enemyMarkers.Count; i++) _fx.ApplyTo(EncounterState.Enemies[i], _enemyMarkers[i]);
            if (Camera.main != null) Camera.main.transform.position += _fx.ShakeOffset;
            UpdateFades();

            if (Rt.On) { Reticle(player); return; }   // real-time fork: no move range, no path dots; a crosshair instead
            if (GameState.Current.CurrentTurn == Turn.Player
                && (player.X != _highlightX || player.Y != _highlightY || player.Mp != _highlightMp))
            {
                RecomputeReachableHighlight(player);
                _highlightX = player.X; _highlightY = player.Y; _highlightMp = player.Mp;
            }
            UpdatePathPreview(player);
        }

        // Real-time fork: four ticks round the mouse, as far apart as the round can stray at that distance (one spread),
        // so steadying the aim visibly closes them. Red while reloading or empty; the centre dot turns red over a body.
        private readonly List<SpriteRenderer> _reticle = new List<SpriteRenderer>();
        private void Reticle(Player player)
        {
            while (_reticle.Count < 6)
                _reticle.Add(CreateMarker("Reticle", Color.white, 1f / 16f, WorldArt.SortTop, _root.transform, GetWhiteSprite()));
            var mt = Rt.MouseTile(UnityEngine.InputSystem.Mouse.current);
            var w = player.EquippedWeapon;
            bool show = mt.HasValue && w != null && player.IsAlive();
            foreach (var r in _reticle) r.gameObject.SetActive(show);
            if (!show) return;
            var (mx, my) = mt.Value;
            var (px, py) = Ballistics.Pos(player);
            float dist = Mathf.Sqrt((mx - px) * (mx - px) + (my - py) * (my - py));
            var aim = Gunplay.AimOf(player);
            float spread = Gunplay.Spread(Gunplay.For(w), aim, Rt.Moving, player.Sneaking);
            float gap = Mathf.Clamp(dist * Mathf.Sin(spread * Mathf.Deg2Rad) + 2f / 16f, 2f / 16f, 3f);
            bool dry = aim.ReloadUntil >= 0f || (!CombatRules.IsMelee(w) && w.CurrentMagazine <= 0) || w.IsJammed;
            var col = dry ? new Color(1f, 0.35f, 0.3f, 0.9f) : new Color(1f, 1f, 1f, 0.85f);
            var c = new Vector3(mx, -my, 0f);
            Vector3[] at = { c + new Vector3(gap, 0f, 0f), c + new Vector3(-gap, 0f, 0f), c + new Vector3(0f, gap, 0f), c + new Vector3(0f, -gap, 0f), c };
            Vector3[] size = { new Vector3(3f, 1f, 1f), new Vector3(3f, 1f, 1f), new Vector3(1f, 3f, 1f), new Vector3(1f, 3f, 1f), Vector3.one };
            var over = Rt.UnitUnder(mx, my);
            for (int i = 0; i < 5; i++)
            {
                _reticle[i].transform.position = at[i];
                _reticle[i].transform.localScale = size[i] * (1f / 16f);
                _reticle[i].color = i == 4 && over != null ? new Color(1f, 0.3f, 0.25f, 1f) : col;
            }
            // RT batch 3: in bullet time the part under the crosshair lights up (head, torso or legs band of the sprite)
            var box = _reticle[5];
            bool lit = Rt.SlowMo && over != null && Body.Human(over);
            box.gameObject.SetActive(lit);
            if (lit)
            {
                float h = Ballistics.AimHeight(over, my), sc = Ballistics.Crouched(over) ? Ballistics.CrouchScale : 1f;
                float lo = h >= Ballistics.HeadFrom * sc ? Ballistics.HeadFrom * sc : h >= Ballistics.TorsoFrom * sc ? Ballistics.TorsoFrom * sc : 0f;
                float hi = lo >= Ballistics.HeadFrom * sc ? sc : lo >= Ballistics.TorsoFrom * sc ? Ballistics.HeadFrom * sc : Ballistics.TorsoFrom * sc;
                var (ux, uy) = Ballistics.Pos(over);
                float feet = -(uy + 0.44f), wide = lo >= Ballistics.HeadFrom * sc ? 0.28f : 0.4f;
                box.transform.position = new Vector3(ux, feet + (lo + hi) * 0.5f * Ballistics.BodyTiles, 0f);
                box.transform.localScale = new Vector3(wide, (hi - lo) * Ballistics.BodyTiles, 1f);
                box.color = new Color(1f, 0.9f, 0.4f, 0.35f);
            }
        }

        // Batch 116 (Yan: "a line indicating the path the character is gonna take if you click there, it could be green when in
        // range and turn to red when out of range (not enough MP)"): a dotted line along the route a click on the hovered tile would
        // walk (the same A* and smoothing). In a fight the dots are green while your MP lasts and red past it; exploring they're pale.
        private readonly List<SpriteRenderer> _pathDots = new List<SpriteRenderer>();
        private (int x, int y, int px, int py, float mp, bool fight) _pathKey = (int.MinValue, 0, 0, 0, 0, false);
        static readonly Color PathGreen = new Color(0.45f, 0.95f, 0.45f, 0.9f), PathRed = new Color(1f, 0.32f, 0.28f, 0.9f), PathPale = new Color(0.9f, 0.9f, 0.8f, 0.45f);
        private void UpdatePathPreview(Player player)
        {
            var m = MouseWorld();
            int x = 0, y = 0;
            bool show = m.HasValue && _root != null && GameState.Current.CurrentTurn == Turn.Player && player.IsAlive()
                && player.ArmedThrowable == null && player.ArmedAction == null && !HudView.TargetingOpen
                && !(UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                && WorldToGrid(m.Value, out x, out y) && (x != player.X || y != player.Y) && TerrainMap.CanEnterTile(x, y)
                && !EncounterState.Enemies.Exists(e => e.IsAlive() && e.X == x && e.Y == y && Vision.Shows(e));   // hovering an enemy is a shot, not a walk
            bool fight = EncounterState.InCombat;
            var key = (show ? x : int.MinValue, y, player.X, player.Y, player.Mp, fight);
            if (key == _pathKey) return;
            _pathKey = key;
            int used = 0;
            if (show)
            {
                var grid = GameServices.Grid;
                var path = grid?.FindPath(player.X, player.Y, x, y, player);
                if (path != null && path.Count > 1)
                {
                    float spent = 0f, step = 4f / TerrainArt.Px;          // a 2 px dot every 4 px
                    var prev = _playerMarker != null ? _playerMarker.transform.position : GridToWorld(player.X, player.Y);
                    prev.y -= 5f / TerrainArt.Px;                          // from the feet, not the chest
                    for (int i = 1; i < path.Count; i++)
                    {
                        spent += grid.StepCost(player, path[i - 1].x, path[i - 1].y, path[i].x, path[i].y);
                        var col = !fight ? PathPale : spent <= player.Mp + 0.001f ? PathGreen : PathRed;
                        var next = GridToWorld(path[i].x, path[i].y); next.y -= 5f / TerrainArt.Px;
                        if (i == path.Count - 1) { next.x += Mathf.Clamp(m.Value.x - next.x, -0.3f, 0.3f); next.y += Mathf.Clamp(m.Value.y - (next.y + 5f / TerrainArt.Px), -0.3f, 0.3f); }   // ends where you'd stop
                        float len = Mathf.Sqrt((next.x - prev.x) * (next.x - prev.x) + (next.y - prev.y) * (next.y - prev.y));
                        for (float t = step; t <= len + 0.001f; t += step)
                        {
                            if (used == _pathDots.Count) _pathDots.Add(CreateMarker("PathDot", Color.white, 2f / TerrainArt.Px, 2, _root.transform));
                            var d = _pathDots[used++];
                            d.gameObject.SetActive(true); d.color = col;
                            var at = new Vector3(prev.x + (next.x - prev.x) * t / len, prev.y + (next.y - prev.y) * t / len, 0f);
                            d.transform.position = new Vector3(Mathf.Round(at.x * TerrainArt.Px) / TerrainArt.Px, Mathf.Round(at.y * TerrainArt.Px) / TerrainArt.Px, 0f);
                        }
                        prev = next;
                    }
                }
            }
            for (int i = used; i < _pathDots.Count; i++) if (_pathDots[i] != null) _pathDots[i].gameObject.SetActive(false);
        }

        /// <summary>The sun moved (or the weather cleared / clouded over): re-bake the ground off the main thread.</summary>
        private void BakeTick()
        {
            if (_world && _bake == null && WorldArt.SunKey() != _sunKey) { _sunKey = WorldArt.SunKey(); _bake = System.Threading.Tasks.Task.Run(WorldArt.ComposeGround); }
            if (_bake != null && _bake.IsCompleted)
            {
                if (_bake.Status == System.Threading.Tasks.TaskStatus.RanToCompletion) { _mapPx = _bake.Result; _mapTex.SetPixels32(_mapPx); _mapTex.Apply(); }
                else Debug.LogWarning($"[World] shadow re-bake failed: {_bake.Exception?.GetBaseException().Message}");
                _bake = null;
            }
        }

        private void AnimateFires()
        {
            // Batch 68: campfires burn with the Pyre's flame (ArtSource/make_bonfire.py), offset per tile
            var bf = BonfireFrames();
            if (bf == null) return;
            foreach (int k in _fires)
                if (_standing.TryGetValue(k, out var f) && f.sr != null)
                    f.sr.sprite = bf[(int)(Time.time / 0.11f + (k % TerrainMap.Width) * 7 + (k / TerrainMap.Width) * 3) % bf.Length];
        }

        /// <summary>Batch 123: behind the main menu, Bonfire Village by its fire with the menu's fast clock (MenuBackdrop):
        /// the real ground, props, roofs, sky, rain and night, no units, no input.</summary>
        private void TickBackdrop()
        {
            if (!MenuBackdrop.Ready) MenuBackdrop.Prepare();
            if (!_built || _builtZoneId != "menu" || !ReferenceEquals(TerrainMap.Tiles, _builtTiles))
            {
                Rebuild();
                _built = true; _builtZoneId = "menu"; _lookZone = null;
            }
            if (_root == null) return;
            _root.SetActive(true);
            if (_playerMarker != null) _playerMarker.gameObject.SetActive(false);
            MenuBackdrop.Advance(Time.unscaledDeltaTime);
            BakeTick();
            AnimateFires();
            var real = Atmosphere.Look();
            var look = _lookZone != "menu" ? real : Atmosphere.Ease(_look, real, 1f - Mathf.Exp(-Time.unscaledDeltaTime / Atmosphere.FadeSeconds));
            _look = look; _lookZone = "menu";
            _atmo?.Tick(null, look);
            ColorGrade.Apply(look);
            if (look.Rain > 0.75f) Sfx.Ambient("amb_storm"); else if (look.Rain > 0f) Sfx.Ambient("amb_rain"); else Sfx.Ambient(Sfx.AmbientFor(MenuBackdrop.Region));
            var cam = Camera.main;
            if (cam == null) return;
            // a slow drift, the fire right of centre (the menu column covers the left third), the huts above it
            float t = Time.unscaledTime;
            var c = GridToWorld(MenuBackdrop.FireX - 4, MenuBackdrop.FireY - 3);
            cam.transform.position = new Vector3(c.x + 2f * Mathf.Sin(t * 0.031f), c.y + 1.2f * Mathf.Sin(t * 0.023f + 1f), -10f);
            cam.orthographicSize = 9f;
        }

        private void Rebuild()
        {
            LoadSpritesOnce();
            SetupCamera(); // grid size is per-zone now (TerrainMap.Width/Height), reframe each time

            if (_root != null) Destroy(_root);
            _root = new GameObject("TacticalGridRoot");
            _root.transform.SetParent(transform, false);
            _smokePool.Clear();
            _speakerMark = null; _playerAlert = null; _alertMarks.Clear(); _standing.Clear(); _fires.Clear(); _faded.Clear(); _reachSr = null; _pathDots.Clear(); _pathKey = (int.MinValue, 0, 0, 0, 0, false);
            Vision.Reset(); _lastFoot.Clear(); _shadows.Clear();
            _atmo = new AtmosphereView(_root.transform);
            _enemyMarkers.Clear();
            _held.Clear(); _away.Clear();
            _anomalyMarkers.Clear();
            _lootMarkers.Clear();
            _groundItemMarkers.Clear();
            _landmarkMarkers.Clear();
            _exitCount = TerrainMap.Exits.Count;
            _builtTiles = TerrainMap.Tiles;
            _highlightX = int.MinValue; // force a highlight recompute against the fresh tiles

            _world = WorldArt.Ready;
            _bake = null;                                         // an unfinished re-bake belongs to the old map
            string mapId = TerrainMap.ZoneId ?? MacroState.Current.CurrentZoneId;
            if (_world) { WorldArt.Begin(mapId); _mapPx = WorldArt.ComposeGround(); _sunKey = WorldArt.SunKey(); }
            else _mapPx = TerrainArt.Compose(mapId);
            TerrainMap.DirtyTiles.Clear();
            if (_mapTex != null) Destroy(_mapTex);
            if (_mapSprite != null) Destroy(_mapSprite);
            _mapTex = new Texture2D(TerrainMap.Width * TerrainArt.Px, TerrainMap.Height * TerrainArt.Px, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            _mapTex.SetPixels32(_mapPx);
            _mapTex.Apply();
            var mapGo = new GameObject("Map");
            mapGo.transform.SetParent(_root.transform, false);
            var mapSr = mapGo.AddComponent<SpriteRenderer>();
            // pivot top-left at world (0,0): grid (x,y) lands exactly where GridToWorld puts it
            mapSr.sprite = _mapSprite = Sprite.Create(_mapTex, new Rect(0, 0, _mapTex.width, _mapTex.height), new Vector2(0f, 1f), TerrainArt.Px / TileWorldSize);
            mapSr.sortingOrder = 0;
            if (_world)
            {
                _standingRoot = new GameObject("Standing").transform;
                _standingRoot.SetParent(_root.transform, false);
                for (int y = 0; y < TerrainMap.Height; y++) for (int x = 0; x < TerrainMap.Width; x++) PlaceStanding(x, y);
                _roofs = new RoofView(_root.transform);
            }
            else _roofs = null;
            HideStrayTexts();

            // Exits are fixed for the duration of this visit (recomputed only on zone entry / a
            // successful Search Area), so they're drawn once here rather than synced per-frame.
            foreach (var exit in TerrainMap.Exits)
            {
                var marker = CreateMarker($"Exit_{exit.TargetZoneId}", _spriteExit != null ? Color.white : new Color(0.2f, 0.9f, 0.3f), TileWorldSize * 0.85f, 2, _root.transform, _spriteExit);
                marker.transform.position = GridToWorld(exit.X, exit.Y);
                marker.sortingOrder = SortAt(marker.transform.position.y) - 1;
            }

            // New (Batch 7): named landmarks - also fixed for the visit (never removed, unlike
            // loot), so drawn once here alongside exits rather than synced per-frame. No dedicated
            // sprite yet - falls back to a small white marker, same convention as every other
            // Resources.Load miss in this file.
            foreach (var landmark in TerrainMap.Landmarks)
            {
                var marker = CreateMarker($"Landmark_{landmark.Name}", new Color(0.85f, 0.85f, 0.6f), TileWorldSize * 0.4f, 1, _root.transform, null);
                marker.transform.position = GridToWorld(landmark.X, landmark.Y);
                marker.sortingOrder = SortAt(marker.transform.position.y) - 1;
                _landmarkMarkers.Add((marker, landmark.Name));
            }

            _playerMarker = CreateMarker("Player", new Color(0.2f, 0.6f, 1f), TileWorldSize * 0.7f, 5, _root.transform, _spriteBeing);
            _playerMarker.gameObject.AddComponent<UnityEngine.Rendering.SortingGroup>();
            _fx = new CombatFxView(_root.transform, MarkerOf, Held);
            _motion = new UnitMotionView(_root.transform, _fx);
            _fx.IsMoving = _motion.IsMoving;
            _fx.OnWound = _motion.Bleed;
            _fx.OnKill = (u, dx, dy, gib) =>
            {
                var m = MarkerOf(u);
                _motion.Kill(u, UnitSprites.For(u), m != null && _held.TryGetValue(m, out var h) ? h : null, GridToWorld(u.X, u.Y) + StandOffset(u), dx, dy, gib, false);
            };
        }

        /// <summary>Batch 114 (free movement, look): nobody stands on the exact middle of a tile. The player stands where you
        /// clicked; everyone else has a fixed spot of their own, up to a third of a tile off. Rules still use the tile.</summary>
        public static Vector3 StandOffset(ICombatant u)
        {
            if (u is Player && !Rt.On) return PlayerController.StandAt;
            var (ox, oy) = Rt.Offset(u);                                   // real-time fork: where the body really is (tile space, y down)
            return new Vector3(ox, -oy, 0f);
        }

        /// <summary>Batch 24: positions (walking), art (walk frames) and death (ragdoll) for one unit.
        /// Returns whether it is drawn standing - a unit whose killing blow hasn't played yet still is.</summary>
        /// <summary>Batch 128: where a unit points its gun and looks - you: the mouse (the speaker while one talks to you); a
        /// human enemy: you once it's fighting you, else the way it faces (its last step, or where it turned to look). It decides
        /// the back sprite (UnitSprites.Away) and the held gun together, so the gun always points where the body faces.</summary>
        private Vector3? AimPoint(ICombatant u, SpriteRenderer m)
        {
            if (u is Player)
            {
                var sp = ChoiceUI.Speaker; var sm = sp != null && sp != u ? MarkerOf(sp) : null;
                return sm != null ? sm.transform.position : MouseWorld();
            }
            if (u is HumanEnemy h)
                return h.IsAggroed && h.Morale != "Surrendered" && _playerMarker != null ? _playerMarker.transform.position
                    : m.transform.position + new Vector3(h.Facing.x, -h.Facing.y, 0f);
            return null;
        }
        private readonly Dictionary<SpriteRenderer, bool> _away = new Dictionary<SpriteRenderer, bool>();
        private bool Away(SpriteRenderer m) => _away.TryGetValue(m, out var b) && b;

        private bool DrawUnit(ICombatant u, SpriteRenderer marker, Color fallback)
        {
            bool known = _motion.Known(u);
            var logical = GridToWorld(u.X, u.Y) + StandOffset(u);
            marker.transform.position = _motion.Place(u, logical);
            var group = marker.GetComponent<UnityEngine.Rendering.SortingGroup>();
            if (group != null) group.sortingOrder = SortAt(marker.transform.position.y);
            bool up = u.IsAlive() || _fx.DeathPending(u);
            ApplyUnitArt(marker, u, up, fallback);
            var aim = AimPoint(u, marker);
            if (aim.HasValue) _away[marker] = UnitSprites.Away(marker.transform.position, aim.Value);   // no mouse: keep the last
            bool away = up && Away(marker);
            if (away && UnitSprites.Back(u) is Sprite backArt) marker.sprite = backArt;
            // standing in a cast shadow (a wall's, a tree's) dims the body; faction colours stay readable
            var foot = marker.transform.position;
            if (_world && up && WorldArt.InShade(Mathf.RoundToInt(foot.x * TerrainArt.Px), Mathf.RoundToInt(-foot.y * TerrainArt.Px + 6f)))
                marker.color *= new Color(0.64f, 0.62f, 0.68f, 1f);
            if (!up && !_motion.Ragdolled(u))                     // died without a blow to wait for (bleeding, an anomaly), or already dead on arrival
            {
                var (hx, hy) = _fx.LastHitDir(u);
                _motion.Kill(u, UnitSprites.For(u), known && _held.TryGetValue(marker, out var h) ? h : null, logical, hx, hy, 0, !known);
            }
            bool down = _motion.Ragdolled(u);
            _motion.SyncStains(u, marker, !down);
            UnitShadow(marker, !down);
            if (down) { marker.color = new Color(0f, 0f, 0f, 0f); return false; }
            int wf = up ? _motion.WalkFrame(u) : -1;
            if (wf >= 0 && UnitSprites.Walk(u, wf, away) is Sprite ws) marker.sprite = ws;
            // a footstep each time a foot comes down (Batch 30)
            _lastFoot.TryGetValue(marker, out int lastWf);
            if (wf >= 0 && wf != lastWf && (u is Player || u is HumanEnemy))
                Sfx.Play(Sfx.Step(TerrainMap.GetTile(u.X, u.Y).Ground), marker.transform.position, u is Player ? 0.6f : 0.4f, 0.12f);
            _lastFoot[marker] = wf;
            return up;
        }

        /// <summary>Tints tiles reachable within the player's current MP, XCOM-style, so clicking to
        /// move isn't a guess. ponytail: plain BFS relaxation, not a real priority-queue Dijkstra —
        /// fine at this grid size (<=800 cells, move costs of 1-2); upgrade if costs get more varied.</summary>
        private void RecomputeReachableHighlight(Player player)
        {
            var grid = GameServices.Grid;
            if (grid == null || _root == null) return;

            var costSoFar = new Dictionary<(int x, int y), float> { [(player.X, player.Y)] = 0 };
            var frontier = new Queue<(int x, int y)>();
            frontier.Enqueue((player.X, player.Y));

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();
                float currentCost = costSoFar[current];
                foreach (var next in TerrainMap.GetAdjacentCoords(current.x, current.y))
                {
                    if (!grid.CanEnterTile(next.x, next.y)) continue;
                    float cost = currentCost + grid.StepCost(player, current.x, current.y, next.x, next.y);
                    if (cost > player.Mp) continue;
                    if (costSoFar.TryGetValue(next, out var existing) && existing <= cost) continue;
                    costSoFar[next] = cost;
                    frontier.Enqueue(next);
                }
            }

            DrawReach(costSoFar.Keys, player.X, player.Y);
        }

        /// <summary>Y-sort key for something whose sprite is centred at world y (feet at its bottom edge):
        /// the lower on screen, the later it draws. Matches WorldArt.Standing.Sort.</summary>
        public static int SortAt(float worldY) => WorldArt.SortBase + Mathf.RoundToInt(-worldY * TerrainArt.Px + 7f);

        /// <summary>(Re)creates the standing sprite of one cell (wall, tree, car...), or removes it.</summary>
        // Batch 103: shoved props slide (or roll) from where they were to their new tile, easing out
        private readonly List<(SpriteRenderer sr, Vector3 home, Vector3 off, float t0, float dur)> _slides = new List<(SpriteRenderer, Vector3, Vector3, float, float)>();
        private void SlideProps()
        {
            if (Props.Slides.Count > 0 && TerrainMap.DirtyTiles.Count == 0)   // once the tiles are redrawn
            {
                foreach (var s in Props.Slides)
                    if (_standing.TryGetValue(s.y * TerrainMap.Width + s.x, out var e) && e.sr != null)
                        _slides.Add((e.sr, e.sr.transform.position, new Vector3(s.fromX - s.x, s.y - s.fromY, 0f) * TileWorldSize, Time.time,
                            Mathf.Max(Mathf.Abs(s.fromX - s.x), Mathf.Abs(s.fromY - s.y)) / 9f));
                Props.Slides.Clear();
            }
            for (int i = _slides.Count - 1; i >= 0; i--)
            {
                var s = _slides[i];
                float k = s.sr == null ? 1f : Mathf.Clamp01((Time.time - s.t0) / s.dur);
                if (s.sr != null) s.sr.transform.position = s.home + s.off * ((1f - k) * (1f - k));
                if (k >= 1f) _slides.RemoveAt(i);
            }
        }

        private void PlaceStanding(int x, int y)
        {
            if (x < 0 || y < 0 || x >= TerrainMap.Width || y >= TerrainMap.Height || _standingRoot == null) return;
            int key = y * TerrainMap.Width + x;
            bool has = WorldArt.StandingAt(x, y, out var st);
            _standing.TryGetValue(key, out var old);
            if (!has)
            {
                if (old.sr != null) { _faded.Remove(old.sr); Destroy(old.sr.gameObject); }
                _standing.Remove(key); _fires.Remove(key);
                return;
            }
            var sr = old.sr;
            if (sr == null)
            {
                var go = new GameObject("S");
                go.transform.SetParent(_standingRoot, false);
                sr = go.AddComponent<SpriteRenderer>();
            }
            sr.sprite = WorldArt.SpriteOf(st.Spr);
            sr.sortingOrder = st.Sort;
            sr.color = Color.white;
            sr.transform.position = new Vector3((st.X + 0.5f) / TerrainArt.Px, -(st.Y + 1f) / TerrainArt.Px, 0f);
            _standing[key] = (st, sr);
            if (TerrainMap.GetTile(x, y).Look == "campfire" && BonfireFrames() != null) _fires.Add(key); else _fires.Remove(key);
        }

        /// <summary>Walls, trees and other tall things in front of a unit go see-through while they hide it.</summary>
        private void UpdateFades()
        {
            if (!_world) return;
            _fadeNow.Clear();
            void Check(SpriteRenderer m)
            {
                if (m == null || !m.gameObject.activeSelf) return;
                var p = m.transform.position;
                int ux = Mathf.RoundToInt(p.x * TerrainArt.Px), foot = Mathf.RoundToInt(-p.y * TerrainArt.Px + 7f);
                int cx = Mathf.FloorToInt(p.x), cy = Mathf.FloorToInt(-p.y);
                for (int y = cy; y <= cy + 3; y++)
                    for (int x = cx - 3; x <= cx + 3; x++)
                    {
                        if (x < 0 || y < 0 || x >= TerrainMap.Width || y >= TerrainMap.Height) continue;
                        if (!_standing.TryGetValue(y * TerrainMap.Width + x, out var e) || !e.st.Fades || e.st.Sort <= WorldArt.SortBase + foot) continue;
                        if (e.st.Left < ux + 6 && e.st.Left + e.st.Spr.W > ux - 6 && e.st.Top < foot && e.st.Top + e.st.Spr.H > foot - 14)
                            _fadeNow.Add(e.sr);
                    }
            }
            Check(_playerMarker);
            for (int i = 0; i < _enemyMarkers.Count && i < EncounterState.Enemies.Count; i++)
                if (EncounterState.Enemies[i].IsAlive()) Check(_enemyMarkers[i]);
            foreach (var sr in _faded) if (sr != null && !_fadeNow.Contains(sr)) sr.color = Color.white;
            foreach (var sr in _fadeNow) sr.color = new Color(1f, 1f, 1f, 0.45f);
            _faded.Clear();
            foreach (var sr in _fadeNow) _faded.Add(sr);
        }

        /// <summary>The cells the player can reach this turn as one soft area with an outline (no grid squares).</summary>
        private void DrawReach(ICollection<(int x, int y)> cells, int px0, int py0)
        {
            if (_reachSr == null)
            {
                _reachSr = CreateMarker("Reach", Color.white, 1f, 1, _root.transform);
            }
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            foreach (var c in cells) { minX = Mathf.Min(minX, c.x); minY = Mathf.Min(minY, c.y); maxX = Mathf.Max(maxX, c.x); maxY = Mathf.Max(maxY, c.y); }
            if (cells.Count <= 1) { _reachSr.gameObject.SetActive(false); return; }
            _reachSr.gameObject.SetActive(true);
            const int P = 16;
            int w = (maxX - minX + 1) * P, h = (maxY - minY + 1) * P;
            var set = new HashSet<(int, int)>(cells);
            var px = new Color32[w * h];
            var fill = new Color32(255, 236, 160, 22); var edge = new Color32(255, 230, 140, 150);
            foreach (var (cx, cy) in cells)
            {
                bool n = set.Contains((cx, cy - 1)), s = set.Contains((cx, cy + 1)), wv = set.Contains((cx - 1, cy)), e = set.Contains((cx + 1, cy));
                for (int y = 0; y < P; y++)
                    for (int x = 0; x < P; x++)
                    {
                        bool border = (!n && y == 0) || (!s && y == P - 1) || (!wv && x == 0) || (!e && x == P - 1);
                        int tx = (cx - minX) * P + x, ty = h - 1 - ((cy - minY) * P + y);
                        px[ty * w + tx] = border ? edge : fill;
                    }
            }
            if (_reachTex == null || _reachTex.width != w || _reachTex.height != h)
            {
                if (_reachTex != null) Destroy(_reachTex);
                _reachTex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            }
            _reachTex.SetPixels32(px);
            _reachTex.Apply();
            if (_reachSr.sprite != null && _reachSr.sprite.texture == _reachTex) Destroy(_reachSr.sprite);
            _reachSr.sprite = Sprite.Create(_reachTex, new Rect(0, 0, w, h), new Vector2(0f, 1f), P);
            _reachSr.transform.localScale = Vector3.one;
            _reachSr.transform.position = new Vector3(minX * TileWorldSize, -minY * TileWorldSize, 0f);
        }

        /// <summary>The scene still has a few default "New Text" TMP objects from early prototyping - hide them.</summary>
        private static void HideStrayTexts()
        {
            foreach (var t in FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None))
                if (t != null && t.text == "New Text") t.gameObject.SetActive(false);
        }

        // Fixed exploration zoom (half-height, world units = tiles) instead of one derived from
        // the zone's size - an exploration-style map should reveal itself as you walk it, not sit
        // fully visible from the moment you enter. Also the whole reason finer pixel-art detail
        // is worth doing: at the old whole-map-fit zoom, a 60-tile zone squeezed every tile down
        // to a handful of screen pixels, too small for extra sprite detail to actually read.
        private const float CameraViewHalfHeightTiles = 7f;
        private const float CameraMaxHalfHeightTiles = 14f;   // Batch 65: scroll out to twice as far
        private static float _zoom = CameraViewHalfHeightTiles;

        private static void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;
            cam.orthographic = true;
            cam.orthographicSize = CameraViewHalfHeightTiles; // width follows from Unity's aspect automatically
            cam.backgroundColor = Color.black;
        }

        /// <summary>Keeps the camera centered on the player every frame. Doesn't clamp to the
        /// zone's edges - near a border you'll see black past the map, same as this view already
        /// showed beyond a zone's edge before this change (backgroundColor = black).</summary>
        private SpriteRenderer _speakerMark;
        static Sprite _chevron;
        /// <summary>A 7x5 yellow chevron pointing down, dark rim (the speaker's marker).</summary>
        static Sprite Chevron()
        {
            if (_chevron != null) return _chevron;
            string[] rows = { "kkkkkkk", "kyyyyyk", ".kyyyk.", "..kyk..", "...k..." };
            var px = new Color32[7 * 5];
            for (int y = 0; y < 5; y++)
                for (int x = 0; x < 7; x++)
                {
                    char ch = rows[y][x];
                    px[(4 - y) * 7 + x] = ch == 'y' ? new Color32(232, 197, 71, 255) : ch == 'k' ? new Color32(58, 44, 18, 255) : new Color32(0, 0, 0, 0);
                }
            var tex = new Texture2D(7, 5, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            tex.SetPixels32(px); tex.Apply();
            return _chevron = Sprite.Create(tex, new Rect(0, 0, 7, 5), new Vector2(0.5f, 0f), TerrainArt.Px);
        }

        // Batch 127c (Yan: "a small ! appears on his head, similar to the metal gear !... enemy npcs can have this as well, yellow
        // when they detect something and red when they confirm"): white over you while you sense a hostile (Vision.Senses, even
        // off screen; out of a fight), yellow over an enemy whose Alert is filling (Contact.Notice), red for a moment when it spots you.
        private readonly List<SpriteRenderer> _alertMarks = new List<SpriteRenderer>();
        private SpriteRenderer _playerAlert;
        static Sprite _bang;
        static Sprite Bang()
        {
            if (_bang != null) return _bang;
            string[] rows = { "kkk", "kwk", "kwk", "kwk", "kwk", "kkk", "kwk", "kkk" };
            var px = new Color32[3 * 8];
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 3; x++)
                    px[(7 - y) * 3 + x] = rows[y][x] == 'w' ? new Color32(255, 255, 255, 255) : new Color32(40, 34, 28, 255);
            var tex = new Texture2D(3, 8, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            tex.SetPixels32(px); tex.Apply();
            return _bang = Sprite.Create(tex, new Rect(0, 0, 3, 8), new Vector2(0.5f, 0f), TerrainArt.Px);
        }
        static readonly Color BangYellow = new Color(0.91f, 0.77f, 0.28f), BangRed = new Color(0.86f, 0.24f, 0.2f), BangYou = new Color(0.95f, 0.94f, 0.86f);
        Vector3 Above(SpriteRenderer m) => m.transform.position + new Vector3(0f, 1.1f + 0.05f * Mathf.Sin(Time.time * 6f), 0f);

        static Vector3 _focus, _lookAhead;   // eased camera offset from the player (a speaker pulls it over)

        private static void FollowCamera(Vector3 target, Vector3? speaker = null)
        {
            var cam = Camera.main;
            if (cam == null) return;
            var me = target;
            var off = speaker.HasValue ? (speaker.Value - target) * 0.5f + new Vector3(0f, -cam.orthographicSize * 0.3f, 0f) : Vector3.zero;   // the dialog covers the bottom third
            _focus = Vector3.Lerp(_focus, off, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 6f));
            target += _focus;
            _lookAhead = Vector3.Lerp(_lookAhead, Rt.On && !speaker.HasValue ? Rt.Look : Vector3.zero, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 4f));   // real-time fork: look toward the mouse
            target += _lookAhead;
            cam.transform.position = new Vector3(target.x, target.y, -10f);
            // Batch 65: mouse wheel zooms between the close view and twice as far (long guns reach past the close view)
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null && !GameState.Current.ModalOpen)
            {
                float wheel = mouse.scroll.ReadValue().y;
                if (wheel != 0f) _zoom = Mathf.Clamp(_zoom - Mathf.Sign(wheel), CameraViewHalfHeightTiles, CameraMaxHalfHeightTiles);
            }
            // Batch 73 (Yan: "the enemies aggro from outside of the screen"): enemies see 12 tiles, the close view shows ~7
            // up and down, so in a fight (or while a gang walks over) the view eases out until every hostile you can see
            // fits, up to the far zoom; it eases back to your own zoom afterwards.
            float need = 0f;
            var p = Player.Current;
            if (p != null)
                foreach (var e in EncounterState.Enemies)
                {
                    if (!e.IsAlive() || !(e.IsAggroed || Shakedown.Approaching(e) || Vision.Senses(p, e)) || Companion.Is(e) || !Vision.Shows(e)) continue;   // Batch 127c: and anyone you sense
                    float dx = Mathf.Abs(e.X - p.X) + 1.5f, dy = Mathf.Abs(e.Y - p.Y) + 1.5f;
                    need = Mathf.Max(need, Mathf.Max(dy, dx / Mathf.Max(0.5f, cam.aspect)));
                }
            if (speaker.HasValue) need = Mathf.Max(need, Mathf.Max(Mathf.Abs(speaker.Value.y - me.y) * 2f + 2f, Mathf.Abs(speaker.Value.x - me.x) / Mathf.Max(0.5f, cam.aspect) + 2f));
            float want = Mathf.Clamp(Mathf.Max(_zoom, need), CameraViewHalfHeightTiles, CameraMaxHalfHeightTiles);
            _shown = _shown <= 0f ? want : Mathf.MoveTowards(_shown, want, Time.deltaTime * 6f);
            cam.orthographicSize = _shown;
        }
        private static float _shown;

        /// <summary>New: draws a unit with its own pixel art (UnitSprites - humans in the armor they
        /// wear, mutants as themselves) at full tile size, untinted; a corpse is darkened. Units with
        /// no art keep the old flat marker in `fallbackColor`. Re-applied every frame, so armor swaps,
        /// cloaking and deaths show up immediately.</summary>
        private static void ApplyUnitArt(SpriteRenderer marker, ICombatant unit, bool alive, Color fallbackColor)
        {
            var art = UnitSprites.For(unit);
            marker.sprite = art != null ? art : (_spriteBeing != null ? _spriteBeing : GetWhiteSprite());
            marker.transform.localScale = Vector3.one * (art != null ? TileWorldSize : TileWorldSize * 0.7f);
            Color live = art != null ? Color.white : fallbackColor;
            if (unit is HumanEnemy h && h.Morale == "Surrendered") live = new Color(0.7f, 0.75f, 0.9f, 0.85f);   // hands up
            if (unit is Mutant nm && nm.Named) { live = Color.Lerp(Color.white, new Color(1f, 0.55f, 0.2f), 0.35f + 0.25f * Mathf.Sin(Time.time * 3f)); marker.transform.localScale *= 1.2f; }
            if (unit is Mutant hm && !(unit is HumanEnemy) && art != null && art == UnitSprites.HD(hm.Db?.Name ?? hm.Name))
            {
                // they're drawn facing right: turn toward you in a fight, toward where they walk otherwise (kept between moves)
                var p = Player.Current;
                int? tx = hm.IsAggroed && p != null ? p.X : hm.PatrolTarget?.x;
                if (alive && tx.HasValue && tx.Value != unit.X) marker.flipX = tx.Value < unit.X;
                if (hm.IsCloaked) live.a = 0.3f;                                  // the Veil Prowler's shimmer (no cloaked cell at this size)
            }
            else marker.flipX = false;
            marker.color = alive ? live : new Color(0.35f, 0.32f, 0.3f, 0.9f);
        }

        // 16x24 frames, foot at pixel (7.5, 22) like the static p_campfire's pivot
        private static Sprite[] BonfireFrames() => CombatFxView.Strip("Sprites/bonfire_strip", 6, TerrainArt.Px, new Vector2(7.5f / 16f, 1f / 24f));

        public static string AnomalyArtKey(string type) => type?.ToLowerInvariant().Replace(' ', '_');
        private static Sprite[] AnomalyFrames(string type)
        {
            string key = AnomalyArtKey(type);
            return key == null ? null : CombatFxView.Strip($"Sprites/anomaly_{key}_strip", 6, 16, new Vector2(0.5f, 0.5f));
        }

        /// <summary>Where the player aims: the mouse over the map, or null (keep the last angle) when a
        /// panel is open or there's no mouse.</summary>
        private static Vector3? MouseWorld()
        {
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse == null || Camera.main == null || GameState.Current.ModalOpen) return null;
            return Camera.main.ScreenToWorldPoint(mouse.position.ReadValue());
        }

        /// <summary>A one-line readout of whatever is under the mouse: unit (rank, morale, HP),
        /// anomaly, landmark (and whether it's a story objective), object or exit.</summary>
        private void UpdateHover(Player player)
        {
            if (_hoverText == null)
            {
                var canvas = FindAnyObjectByType<Canvas>();
                if (canvas == null) return;
                _hoverText = UIBuildUtil.CreateText(canvas.transform, "HoverInfo", new Vector2(0.25f, 0.94f), new Vector2(0.75f, 0.995f),
                    "", 15, new Color(1f, 0.95f, 0.8f), TMPro.TextAlignmentOptions.Center);
                _hoverText.raycastTarget = false;
            }
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse == null || Camera.main == null || GameState.Current.ModalOpen) { _hoverText.text = ""; return; }
            var world = Camera.main.ScreenToWorldPoint(mouse.position.ReadValue());
            if (!WorldToGrid(world, out int x, out int y)) { _hoverText.text = ""; return; }
            _hoverText.text = DescribeTile(x, y, player);
        }

        public static string DescribeTile(int x, int y, Player player)
        {
            var unit = EncounterState.Enemies.Find(e => e.X == x && e.Y == y && e.IsAlive() && Vision.Shows(e)) ?? EncounterState.Enemies.Find(e => e.X == x && e.Y == y && !e.IsAlive());
            if (unit != null)
            {
                if (!unit.IsAlive()) return $"{unit.Name} (dead){(unit is HumanEnemy ? " - click to loot" : "")}";
                string extra = unit is HumanEnemy h ? $"{h.FactionName} {h.Rank}, {h.Morale}{(EnemyAI.FriendlyToPlayer(h) ? ", friendly" : "")}" : $"Tier {unit.Db?.Tier ?? 1} mutant{(unit.Named ? ", NAMED" : "")}";
                return $"{unit.Name} - {extra} - HP {unit.Hp}/{unit.MaxHp}{(unit.IsAggroed ? "" : " (unaware)")}";
            }
            var a = EncounterState.Anomalies.Find(an => an.Revealed && an.X == x && an.Y == y);
            if (a != null) return $"{a.DisplayName}{(a.Searched ? " - searched" : " - F next to it to search for an artifact")}";
            var lm = TerrainMap.Landmarks.Find(l => l.X == x && l.Y == y);
            if (lm != null) return $"{lm.Name}{(StorySystem.IsObjective(MacroState.Current.CurrentZoneId, lm.Name) ? " - [OBJECTIVE] E next to it" : "")}";
            var obj = TerrainMap.InteractiveObjects.Find(o => o.X == x && o.Y == y);
            if (obj != null) return $"{obj.Name} - E next to it";
            var exit = TerrainMap.Exits.Find(e => e.X == x && e.Y == y);
            if (exit != null) return $"To {GameData.MacroMapDatabase.Find(exit.TargetZoneId)?.Name ?? exit.TargetZoneId}";
            return "";
        }

        private static SpriteRenderer CreateMarker(string name, Color color, float scale, int sortingOrder, Transform parent, Sprite sprite = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite != null ? sprite : GetWhiteSprite();
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            sr.transform.localScale = Vector3.one * scale;
            return sr;
        }

        private static Sprite GetWhiteSprite()
        {
            if (_whiteSprite != null) return _whiteSprite;
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            _whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            return _whiteSprite;
        }

        /// <summary>Grid (x,y), y=0 at the top (matches TerrainMap's [y,x] convention), to Unity world space.</summary>
        /// <summary>Batch 123: is the cell inside the camera's view (with a margin in tiles)? No camera (harness): yes.</summary>
        public static bool OnScreen(int x, int y, float margin = 1f)
        {
            var cam = Camera.main;
            if (cam == null || cam.orthographicSize <= 0f) return true;
            var w = GridToWorld(x, y); var c = cam.transform.position;
            float hh = cam.orthographicSize - margin, hw = cam.orthographicSize * cam.aspect - margin;
            return Mathf.Abs(w.x - c.x) <= hw && Mathf.Abs(w.y - c.y) <= hh;
        }

        public static Vector3 GridToWorld(int x, int y) =>
            new Vector3((x + 0.5f) * TileWorldSize, -(y + 0.5f) * TileWorldSize, 0f);

        /// <summary>Inverse of GridToWorld — turns a mouse click's world position into a grid cell.</summary>
        public static bool WorldToGrid(Vector3 world, out int x, out int y)
        {
            x = Mathf.FloorToInt(world.x / TileWorldSize);
            y = Mathf.FloorToInt(-world.y / TileWorldSize);
            return x >= 0 && x < TerrainMap.Width && y >= 0 && y < TerrainMap.Height;
        }
    }
}
