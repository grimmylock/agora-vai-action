// Real-time fork, batch 1 (ACTION_DESIGN.md "Weapons"): how a gun handles, and one round fired for real.
// Per class: accuracy (spread once steadied), hip spread, handling (seconds to steady), recoil (degrees of kick a shot),
// recovery (degrees a second it settles back), fire rate, automatic or not, reach. Pellets for shotguns.
// Moving widens the spread (walking a little, sprinting a lot); crouching tightens it; kicks add up and recover.
// NPCs fire through the same code: their rank decides how steady they are and how often they go for the head.
// RT batch 6d (Yan: "every assault rifle is just firing at the same fire-rate, same reload speed, same accuracy"): every gun
// has its own fire rate, fire mode, reload, accuracy, handling and kick (Guns below); the class row keeps hip spread,
// recovery and reach. Numbers are a first pass from each gun's build (calibre, weight, action, magazine) - Unity tuning next.
using System.Collections.Generic;
using System.Linq;
using GameData;

namespace GameCore
{
    public class Handling
    {
        public float Steady, Hip, SteadyTime, Kick, Recover, Rpm, Reach, ReloadTime, Move = 1f, Choke, Speed;
        public bool Auto;
        public int Pellets = 1;
    }

    public static class Gunplay
    {
        // class: steadied spread, hip spread (degrees), seconds to steady, kick, recovery deg/s, rounds/min, auto, reach (tiles), reload s,
        // moving (x the spread walking or sprinting adds: RT 6d, SMGs and pistols shoot on the move, long guns don't),
        // bullet speed in tiles a second (RT 8; Velocity below for the guns that differ from their class)
        static readonly Dictionary<string, float[]> Table = new Dictionary<string, float[]>
        {
            ["sidearm"] = new[] { 1.2f, 3.5f, 0.45f, 2.6f, 14f, 320f, 0f, 40f, 1.3f, 0.6f, 40f },
            ["smg"]     = new[] { 1.9f, 3f, 0.4f, 1.5f, 16f, 720f, 1f, 50f, 1.7f, 0.5f, 45f },
            ["assault"] = new[] { 0.7f, 5f, 0.7f, 1.9f, 11f, 600f, 1f, 60f, 2.0f, 1f, 80f },
            ["sniper"]  = new[] { 0.12f, 8f, 1.2f, 7f, 9f, 50f, 0f, 100f, 2.6f, 1.6f, 100f },
            ["shotgun"] = new[] { 1.4f, 4f, 0.5f, 6f, 9f, 75f, 0f, 30f, 2.4f, 0.8f, 40f },
            ["heavy"]   = new[] { 1.4f, 7f, 0.9f, 2.2f, 8f, 560f, 1f, 60f, 3.2f, 1.5f, 85f },
        };


        // 2026-11-17 weapon pass (Yan: fix them one by one, every gun 2-4 s to kill a human of its own tier with every round landing on the torso; rifles differ by range role).
        // rate = share of the old rounds-per-minute (automatics only: the old 600-950 rpm made a rifle kill in half a second), vsHuman = damage multiplier against players and
        // human enemies only (mutants and bosses keep their old balance: against them the damage is 1 / rate, so a gun's dps against mutants did not change).
        // Solved per gun against its target time (see ArtSource/weapons/WEAPON_TUNING_RT.md); change a gun's row to retune it. A human dies when his torso pool
        // (80% of his HP) or head pool (50%) is gone (Body.cs), so "kill" means 800 torso damage after armor.
        public static readonly Dictionary<string, (float rate, float vsHuman)> Tune = new Dictionary<string, (float rate, float vsHuman)>
        {
            ["PSh-18 Pchela"] = (1.00f, 0.87f),
            ["Walden P9"] = (1.00f, 0.693f),
            ["Halden .45 Service"] = (1.00f, 0.83f),
            ["Halden Service Revolver"] = (1.00f, 1.117f),
            ["SR-21 Gadyuka"] = (1.00f, 0.62f),
            ["Black Harrier"] = (1.00f, 0.62f),
            ["Corvan 5.7"] = (1.00f, 0.66f),
            ["Sperling S-96"] = (1.00f, 0.69f),
            ["Rinaldi R6"] = (1.00f, 1.21f),
            ["Steppe Eagle"] = (1.00f, 0.792f),
            ["RK-12 Kuvalda"] = (1.00f, 1.68f),
            ["Vosa vz. 62"] = (0.80f, 0.48f),
            ["PPZ Zubr"] = (0.80f, 0.404f),
            ["Corvan Wasp-9"] = (0.80f, 0.43f),
            ["Corvan Hornet .45"] = (0.80f, 0.44f),
            ["Corvan Gnat PDW"] = (0.80f, 0.44f),
            ["VKS-74U"] = (0.80f, 0.24f),
            ["Smerch-3"] = (0.80f, 0.192f),
            ["VK-74"] = (0.70f, 0.22f),
            ["VKM"] = (0.70f, 0.23f),
            ["TS-9 Tishina"] = (0.65f, 0.22f),
            ["TR-30"] = (0.65f, 0.22f),
            ["Halden HR36K"] = (0.65f, 0.22f),
            ["VK-103"] = (0.65f, 0.24f),
            ["BR-86 Bulldog"] = (0.60f, 0.185f),
            ["VK-74 Obelisk"] = (0.60f, 0.25f),
            ["NA-94 Yenisei"] = (0.60f, 0.32f),
            ["Corvan Bullpup"] = (0.65f, 0.242f),
            ["Corvan Battle Rifle"] = (0.60f, 0.267f),
            ["Obrez-66"] = (1.00f, 0.70f),
            ["Halden Lever Shotgun"] = (1.00f, 0.58f),
            ["Hound 13"] = (1.00f, 0.59f),
            ["Ranger P8"] = (1.00f, 0.59f),
            ["Karusel-5"] = (1.00f, 0.57f),
            ["Bayou Pump"] = (1.00f, 0.51f),
            ["Sever-34 O/U"] = (1.00f, 0.81f),
            ["Tambour-12"] = (1.00f, 0.38f),
            ["SP-14 Warden"] = (1.00f, 0.37f),
            ["Sayan-12"] = (1.00f, 0.38f),
            ["Kaban-12"] = (1.00f, 0.33f),
            ["SKN Karabin"] = (1.00f, 0.29f),
            ["Frontier Lever Rifle"] = (1.00f, 0.43f),
            ["VSN Shepot"] = (1.00f, 0.22f),
            ["M91 Trekhlineyka"] = (1.00f, 0.494f),
            ["Nightwalker .45-70"] = (1.00f, 0.53f),
            ["Corvan Marksman"] = (1.00f, 0.31f),
            ["SV-2 Strela"] = (1.00f, 0.26f),
            ["Sever-98"] = (1.00f, 0.600f),
            ["VK-127 Grom"] = (1.00f, 0.783f),
            ["Halden AM-50 Sledge"] = (1.00f, 0.43f),
            ["G-7 Rail Prototype"] = (1.00f, 0.53f),
            ["PL-62 Kobza"] = (0.65f, 0.23f),
            ["Corvan LMG"] = (0.65f, 0.18f),
            ["RL-44"] = (0.65f, 0.22f),
            ["PL-74"] = (0.60f, 0.22f),
            ["PL-74P Polovets"] = (0.60f, 0.311f),
            ["PL-74P Polovets Zulu"] = (0.60f, 0.31f),
        };

        /// <summary>Damage multiplier of one gun against one kind of target (see Tune): humans take `vsHuman`, everything else 1 / rate.</summary>
        public static float TuneMult(Weapon w, bool humanTarget)
        {
            if (w?.Db == null || !Tune.TryGetValue(w.Db.Name, out var t)) return 1f;
            return humanTarget ? t.vsHuman : 1f / t.rate;
        }

        // RT 6d: a shotgun's pellets leave as one shot (the aim spread moves the whole pattern) and spread round it by the choke, degrees
        public static readonly Dictionary<string, float> Choke = new Dictionary<string, float> { ["Obrez-66"] = 2.4f, ["Sever-34 O/U"] = 1.1f, ["Halden Lever Shotgun"] = 1.8f };

        // per gun: rounds/min, automatic, reload s, steadied spread (deg), seconds to steady, kick (deg a shot)
        public static readonly Dictionary<string, (float rpm, bool auto, float reload, float steady, float steadyTime, float kick)> Guns =
            new Dictionary<string, (float, bool, float, float, float, float)>
        {
            // sidearms: quick to raise; revolvers slow to reload, magnums kick
            ["PSh-18 Pchela"]           = (200, false, 1.4f, 1.4f, 0.4f, 2.2f),
            ["Walden P9"]               = (250, false, 1.6f, 1.1f, 0.4f, 2.2f),
            ["Halden .45 Service"]      = (290, false, 1.3f, 1f, 0.45f, 3f),
            ["Halden Service Revolver"] = (150, false, 2.6f, 1f, 0.5f, 3.2f),
            ["SR-21 Gadyuka"]           = (290, false, 1.3f, 1.1f, 0.45f, 2.6f),
            ["Black Harrier"]           = (250, false, 1.5f, 0.72f, 0.5f, 3.2f),
            ["Corvan 5.7"]              = (300, false, 1.2f, 1f, 0.35f, 1.8f),
            ["Rinaldi R6"]              = (190, false, 2.4f, 0.8f, 0.5f, 3.4f),
            ["Sperling S-96"]           = (300, false, 1.9f, 0.95f, 0.45f, 3f),   // long barrel steadies well, a stripper clip reloads slowly
            ["Steppe Eagle"]            = (200, false, 1.3f, 0.9f, 0.60f, 6.0f),
            ["RK-12 Kuvalda"]           = (75,  false, 3.0f, 0.8f, 0.65f, 7.5f),
            // SMGs: fast, light kick, wide
            ["Vosa vz. 62"]             = (850, true, 1.5f, 2.2f, 0.30f, 1.1f),
            ["PPZ Zubr"]                = (680, true, 2.6f, 1.8f, 0.45f, 1.2f),
            ["Corvan Wasp-9"]           = (800, true, 1.8f, 1.4f, 0.40f, 1.2f),
            ["Corvan Hornet .45"]       = (600, true, 1.9f, 1.6f, 0.45f, 2.0f),
            ["Corvan Gnat PDW"]         = (950, true, 1.7f, 1.5f, 0.30f, 1.0f),
            // rifles: short ones handle fast and spread; long and heavy ones steady slower and shoot tighter
            ["VKS-74U"]                 = (700, true, 1.9f, 1.1f, 0.50f, 2.2f),
            ["Smerch-3"]                = (800, true, 1.8f, 0.9f, 0.50f, 2.3f),
            ["VK-74"]                   = (620, true, 2.1f, 0.7f, 0.70f, 1.7f),
            ["VKM"]                     = (600, true, 2.1f, 0.8f, 0.70f, 2.4f),
            ["TS-9 Tishina"]            = (850, true, 2.0f, 0.75f, 0.60f, 2.0f),
            ["TR-30"]                   = (700, true, 2.0f, 0.6f, 0.70f, 1.7f),
            ["Halden HR36K"]            = (720, true, 2.0f, 0.65f, 0.60f, 1.6f),
            ["VK-103"]                  = (600, true, 2.1f, 0.7f, 0.75f, 2.3f),
            ["BR-86 Bulldog"]           = (650, true, 2.6f, 0.55f, 0.85f, 1.5f),
            ["VK-74 Obelisk"]           = (650, true, 2.0f, 0.5f, 0.55f, 1.6f),
            ["NA-94 Yenisei"]           = (600, true, 2.2f, 0.55f, 0.80f, 1.4f),
            ["Corvan Bullpup"]          = (750, true, 2.3f, 0.55f, 0.65f, 1.6f),
            ["Corvan Battle Rifle"]     = (600, true, 2.4f, 0.5f, 0.85f, 3.0f),
            // snipers: semis fire faster and wander more; bolts are the tightest
            ["SKN Karabin"]             = (120, false, 2.4f, 0.225f, 0.9f, 3.4f),
            ["Frontier Lever Rifle"]    = (70, false, 3.2f, 0.25f, 0.9f, 5.0f),
            ["VSN Shepot"]              = (150, false, 2.2f, 0.24f, 0.8f, 3.5f),
            ["M91 Trekhlineyka"]        = (40, false, 3.4f, 0.10f, 1.2f, 7.0f),
            ["Nightwalker .45-70"]      = (55, false, 3.0f, 0.20f, 1.1f, 8.0f),
            ["Corvan Marksman"]         = (110, false, 2.5f, 0.12f, 1.1f, 5f),
            ["SV-2 Strela"]             = (100, false, 2.6f, 0.1125f, 1.2f, 4.5f),
            ["Sever-98"]                = (45, false, 3.4f, 0.08f, 1.3f, 7f),
            ["VK-127 Grom"]             = (30, false, 3f, 0.1f, 1.4f, 9f),
            ["Halden AM-50 Sledge"]     = (70, false, 3.4f, 0.1f, 1.8f, 13.5f),
            ["G-7 Rail Prototype"]      = (40, false, 3.0f, 0.05f, 1.4f, 4.0f),
            // shotguns (spread = where the pattern goes; Choke = how wide it is): pumps and levers slow, semis and doubles quick
            ["Obrez-66"]                = (200, false, 2.0f, 1.8f, 0.35f, 8.0f),
            ["Halden Lever Shotgun"]    = (70, false, 2.2f, 1.4f, 0.5f, 6f),
            ["Hound 13"]                = (75, false, 2.6f, 1.3f, 0.50f, 6.0f),
            ["Ranger P8"]               = (80, false, 2.8f, 1.2f, 0.50f, 6.0f),
            ["Karusel-5"]               = (130, false, 2.4f, 1.3f, 0.50f, 6.0f),   // five shots from a cylinder: faster than a pump, a wider spread
            ["Bayou Pump"]              = (75, false, 3.0f, 1.2f, 0.55f, 6.0f),
            ["Sever-34 O/U"]            = (150, false, 2.2f, 1.0f, 0.50f, 7.0f),
            ["Tambour-12"]              = (180, false, 4.0f, 1.5f, 0.60f, 6.5f),
            ["SP-14 Warden"]            = (200, false, 3.2f, 1.3f, 0.55f, 5.5f),
            ["Sayan-12"]                = (180, false, 2.2f, 1.5f, 0.55f, 6.0f),
            ["Kaban-12"]                = (200, false, 2.4f, 1.3f, 0.60f, 5.5f),
            // machine guns: slow to steady, long belt changes
            ["PL-62 Kobza"]             = (650, true, 4.0f, 1.4f, 0.90f, 2.2f),
            ["Corvan LMG"]              = (750, true, 4.5f, 1.3f, 0.90f, 1.9f),
            ["RL-44"]                   = (600, true, 4.2f, 1.2f, 1.00f, 2.4f),
            ["PL-74"]                   = (650, true, 4.5f, 0.88f, 1f, 2.6f),
            ["PL-74P Polovets"]         = (650, true, 3.9f, 0.8f, 1f, 2.5f),
            ["PL-74P Polovets Zulu"]    = (650, true, 3.9f, 0.8f, 1.1f, 2.4f),
        };

        // RT 8 (Yan: "i want real bullets for everyone, guns will need a bullet velocity stat too"): tiles a second, game time (bullet
        // time slows them with everything else). Real muzzle speeds scaled down ~1:5 so a round is something you can see and step
        // out of: a pistol crosses 6 tiles in 0.15 s (0.5 s in bullet time), a rifle 30 tiles in 0.4 s. Subsonic guns are slow.
        public static readonly Dictionary<string, float> Velocity = new Dictionary<string, float>
        {
            ["Corvan 5.7"] = 55f, ["Sperling S-96"] = 52f, ["Steppe Eagle"] = 50f, ["RK-12 Kuvalda"] = 50f, ["Black Harrier"] = 48f, ["Vosa vz. 62"] = 35f,
            ["Corvan Gnat PDW"] = 60f, ["VKS-74U"] = 70f, ["TS-9 Tishina"] = 32f, ["VSN Shepot"] = 32f, ["Frontier Lever Rifle"] = 60f,
            ["Nightwalker .45-70"] = 55f, ["VK-127 Grom"] = 80f, ["Halden AM-50 Sledge"] = 110f, ["G-7 Rail Prototype"] = 250f, ["Obrez-66"] = 35f,
        };

        // RT 6h (Yan: "individual bullet reloading for revolvers and lever actions/ shotguns"): tube shotguns, levers, revolvers
        // and the bolt rifles without a box magazine load a round at a time; shooting stops a tube/lever/bolt reload with what's in,
        // a revolver finishes the round in hand and closes first. The per-gun reload time is a full load from empty.
        public static bool ByRound(Weapon w) { var r = WeaponFeel.For(w?.Db).Reload; return r == "tube" || r == "cylinder"; }
        public static bool Revolver(Weapon w) => WeaponFeel.For(w?.Db).Reload == "cylinder";
        public const float CylinderOpen = 0.4f;               // swing it out, dump the brass
        public const float BreakOpenClose = 0.6f;             // RT 6k: a break-action opens, dumps and closes; the rest is per shell fired

        /// <summary>A whole-magazine reload's length (RT 6k: a break-action's depends on how many shells it fired).</summary>
        public static float ReloadSeconds(Weapon w)
        {
            float full = For(w).ReloadTime;
            if (WeaponFeel.For(w?.Db).Reload != "break") return full;
            int cap = System.Math.Max(1, w.MagCapacity);
            return BreakOpenClose + (cap - w.CurrentMagazine) * (full - BreakOpenClose) / cap;
        }
        public static float PerRound(Weapon w) =>
            System.Math.Max(0.25f, System.Math.Min(0.6f, (For(w).ReloadTime - (Revolver(w) ? CylinderOpen : 0f)) / System.Math.Max(1, w.MagCapacity)));

        // Fire modes (Yan, 2026-11-08: "yes [a fire-mode key], for more modern assault rifles and SMGs"): the modern guns - the ones with a selector - can be switched with X between full AUTO (hold to spray), a 3-round BURST
        // (one pull, three rounds at the gun's own rate, a short pause) and SEMI (one round a pull). The old designs (VKS-74U, Smerch-3, VK-74, VKM, Vosa vz. 62, PPZ Zubr) keep their one mode. The probe (Program.cs
        // RtLoadoutProbe) showed why it matters: held down the starting rifle hit 17% at 12 tiles, in bursts 40%.
        public static readonly HashSet<string> Modern = new HashSet<string> { "TS-9 Tishina", "TR-30", "Halden HR36K", "VK-103", "BR-86 Bulldog", "VK-74 Obelisk", "NA-94 Yenisei", "Corvan Bullpup", "Corvan Battle Rifle", "Corvan Wasp-9", "Corvan Hornet .45", "Corvan Gnat PDW" };
        public const int BurstRounds = 3; public const float BurstGap = 0.18f;
        public static bool HasModes(Weapon w) => w?.Db != null && Modern.Contains(w.Db.Name);
        public static string ModeLabel(Weapon w, int mode) => !HasModes(w) ? (w != null && For(w).Auto ? "AUTO" : "SINGLE") : mode == 1 ? "BURST" : mode == 2 ? "SEMI" : "AUTO";

        public const float MaxBloom = 10f;                   // degrees of kick at most, however long the trigger is held
        /// <summary>Task #36 ("why Yan loses to humans as Ex-Military", measured with Program.cs RtLoadoutProbe): a bot holding the trigger with the starting rifle hit 17% of its rounds at 12 tiles (the starting pistol 50%) because a 600 rpm gun
        /// kicks 19 degrees a second and recovers 11, so the kick sat at the 10 degree cap. YOUR kick tops out lower (the enemies' keep MaxBloom, Yan's rule 6: no enemy nerfs, tools for the player): holding the trigger is still worse than
        /// bursts, just not worse than a pistol.</summary>
        public const float PlayerMaxBloom = 6f;

        public static Handling For(Weapon w)
        {
            string cat = CombatRules.Category(w?.Db);
            if (!Table.TryGetValue(cat, out var t)) t = Table["sidearm"];
            var h = new Handling { Steady = t[0], Hip = t[1], SteadyTime = t[2], Kick = t[3], Recover = t[4], Rpm = t[5], Auto = t[6] > 0f, Reach = t[7], ReloadTime = t[8], Move = t[9], Speed = t[10] };
            if (w?.Db != null)
            {
                if (Velocity.TryGetValue(w.Db.Name, out var v)) h.Speed = v;
                if (Guns.TryGetValue(w.Db.Name, out var g)) { h.Rpm = g.rpm; h.Auto = g.auto; h.ReloadTime = g.reload; h.Steady = g.steady; h.SteadyTime = g.steadyTime; h.Kick = g.kick; }
                if (Tune.TryGetValue(w.Db.Name, out var tu)) h.Rpm *= tu.rate;
                if (CombatRules.IsShotgun(w)) { if (w.CurrentAmmoType == "Slug") { h.Pellets = 1; h.Choke = 0f; } else { h.Pellets = 6; h.Choke = Choke.TryGetValue(w.Db.Name, out var c) ? c : 1.5f; } }   // a slug is one projectile
            }
            if (ItemCondition.IsDamaged(w?.Condition ?? ItemCondition.Working)) h.Steady *= 1.4f;
            if (w != null && w.Upgrades.Contains("Calibrated")) h.Steady *= 0.9f;                     // a calibrated barrel groups tighter (it was +1 to hit)
            if (w?.Owner is Player pl) PerkTune(h, CombatRules.Category(w.Db), pl);
            return h;
        }

        /// <summary>The gun perks in real time (2026-11-17: they only existed in the 2d10 roll, so in real time they did nothing): each one tunes the
        /// handling of the gun its player holds. Marksman's Focus steadies a sniper rifle, Recoil Control and Bullet Hose tame the kick of rifles and SMGs,
        /// Shredder steadies a machine gun, Point-Blank Execution tightens a sidearm fired from the hip. (High-Velocity Impact, Breacher and Staggering
        /// Blast act on the hit: Resolve.)</summary>
        static void PerkTune(Handling h, string cat, Player pl)
        {
            if (cat == "sniper" && CombatantUtil.HasPerk(pl, "MarksmansFocus")) h.Steady *= 0.8f;
            if (cat == "assault" && CombatantUtil.HasPerk(pl, "RecoilControl")) h.Kick *= 0.7f;
            if (cat == "smg" && CombatantUtil.HasPerk(pl, "BulletHose")) h.Kick *= 0.7f;
            if (cat == "heavy" && CombatantUtil.HasPerk(pl, "Shredder")) { h.Steady *= 0.75f; h.Kick *= 0.85f; }
            if (cat == "sidearm" && CombatantUtil.HasPerk(pl, "PointBlankExecution")) h.Hip *= 0.75f;
        }

        /// <summary>The live state of one shooter's aim: how steady it is and how much kick it still carries.</summary>
        public class Aim
        {
            public float Steady;          // 0 = hip, 1 = fully steadied
            public float Bloom;           // degrees of kick not yet recovered
            public float NextShot;        // time the gun can fire again
            public float ReloadUntil = -1f;
            public int Loading;           // RT 6h: rounds still to go in, one at a time (0 = a whole-magazine reload, or none)
        }
        static readonly Dictionary<ICombatant, Aim> Aims = new Dictionary<ICombatant, Aim>();
        public static Aim AimOf(ICombatant u) { if (!Aims.TryGetValue(u, out var a)) Aims[u] = a = new Aim(); return a; }
        public static void Reset() { Aims.Clear(); OffAims.Clear(); Flying.Clear(); Track.Clear(); }
        static readonly Dictionary<ICombatant, Aim> OffAims = new Dictionary<ICombatant, Aim>();
        /// <summary>RT 7z-c: a Gunslinger's left-hand gun keeps its own steadiness, kick, fire rate and reload.</summary>
        public static Aim OffAimOf(ICombatant u) { if (!OffAims.TryGetValue(u, out var a)) OffAims[u] = a = new Aim(); return a; }

        /// <summary>RT 7z (Yan: "weapon dispersion seems very high even for the starter pistol ... past a few 'me's' bullets
        /// already fly high or low"): the player's spread is halved. The degrees were set when one shot was a turn; in real time a
        /// hip-fired pistol hit 24% at 8 tiles (Program.cs AccuracyProbe). Enemies keep theirs (NpcAim) - Yan's rule 6.</summary>
        public const float PlayerSpread = 0.5f;

        /// <summary>The player's spread (degrees, one standard deviation) right now. move: 0 still, 1 walking, 2 sprinting.</summary>
        public static float Spread(Handling h, Aim a, int move, bool crouched)
        {
            float s = h.Hip + (h.Steady - h.Hip) * a.Steady;
            s += (move == 1 ? 1.2f : move >= 2 ? 6f : 0f) * h.Move;
            if (crouched) s *= 0.8f;
            return (s + a.Bloom) * PlayerSpread;
        }

        /// <summary>Per frame: steady up while aiming (and not sprinting), the kick settles back.</summary>
        public static void Tick(Handling h, Aim a, bool aiming, int move, float dt)
        {
            float target = aiming && move < 2 ? 1f : 0.25f;
            float rate = 1f / System.Math.Max(0.1f, h.SteadyTime);
            a.Steady = a.Steady < target ? System.Math.Min(target, a.Steady + rate * dt) : System.Math.Max(target, a.Steady - 2f * rate * dt);
            a.Bloom = System.Math.Max(0f, a.Bloom - h.Recover * dt);
        }

        static float Gauss() { float u1 = System.Math.Max(1e-6f, UnityEngine.Random.value), u2 = UnityEngine.Random.value; return (float)(System.Math.Sqrt(-2.0 * System.Math.Log(u1)) * System.Math.Cos(6.2831853 * u2)); }

        /// <summary>The bodies a round can hit: you and everyone on the map.</summary>
        /// <summary>What a shot or a blade from `shooter` can hit. Yan: "turn friendly fire off for now for players, make it so bullets
        /// go through allied players" - a player's rounds, swings and throws pass through the other players.</summary>
        public static IEnumerable<ICombatant> Targets(ICombatant shooter) => shooter is Player ? Bodies().Where(u => !(u is Player)) : Bodies();

        /// <summary>On the same side: players together; people of one faction or of factions at peace.</summary>
        public static bool Allies(ICombatant a, ICombatant b) =>
            a is Player && b is Player
            || a is HumanEnemy x && b is HumanEnemy y && (x.FactionName == y.FactionName || !FactionDatabase.AreHostile(x.FactionName, y.FactionName));

        /// <summary>Yan: "enemies make no effort to not shoot their friends ... they need to at least try to not shoot each other in the
        /// back": a living ally stands within ~half a tile of the line from the shooter to the target, before the target.</summary>
        public static bool AllyInLine(ICombatant shooter, ICombatant target)
        {
            var (sx, sy) = Ballistics.Pos(shooter); var (tx, ty) = Ballistics.Pos(target);
            float dx = tx - sx, dy = ty - sy, len = (float)System.Math.Sqrt(dx * dx + dy * dy);
            if (len < 0.01f) return false;
            dx /= len; dy /= len;
            foreach (var u in Bodies())
            {
                if (u == shooter || u == target || !u.IsAlive() || !Allies(shooter, u)) continue;
                var (ux, uy) = Ballistics.Pos(u);
                float t = (ux - sx) * dx + (uy - sy) * dy, side = (ux - sx) * dy - (uy - sy) * dx;
                if (t > 0.3f && t < len && System.Math.Abs(side) < 0.6f) return true;
            }
            return false;
        }

        public static IEnumerable<ICombatant> Bodies()
        {
            foreach (var p in Player.All) yield return p;                  // co-op: every player has a body rounds can hit
            foreach (var e in EncounterState.Enemies) yield return e;
        }

        /// <summary>RT 8: a round in the air. It flies `Speed` tiles a second (game time) and lands on the first body, wall or
        /// blocking cover its path crosses (Ballistics.Trace a frame's length at a time); its height drifts with the up/down scatter.</summary>
        public class Round
        {
            public ICombatant Shooter; public Weapon W; public string Map;
            public float X, Y, Dx, Dy, H, Up, Gone, Range, Speed, Wait, X0, Y0, Through = 1f;
            public int CoverX = -1, CoverY = -1, Pellets = 1;
            public bool Ambush, First, AtEel;                  // AtEel: aimed at the eel (it may hit the parts of its body that are in the air)
            public Ballistics.Hit Hit;                         // set when it lands
            public int Pierce;                                 // 2026-11-17: targets it can still pass through (Pierce table)
            public float PierceMul = 0.75f;                    // damage left after each body it passes
            public HashSet<ICombatant> Hurt;                   // bodies it already went through
        }

        // 2026-11-17 (Yan's sheet: "shots pen targets"): rounds of these guns pass through bodies and keep flying. (targets it can hit with the gun's own rounds, with armor-piercing rounds, damage left
        // after each body). A round stops at walls and cover as before.
        public static readonly Dictionary<string, (int targets, int ap, float mul)> Pierce = new Dictionary<string, (int, int, float)>
        {
            ["RK-12 Kuvalda"] = (2, 3, 0.75f), ["Steppe Eagle"] = (2, 3, 0.75f),
            ["SKN Karabin"] = (2, 2, 0.75f), ["VSN Shepot"] = (2, 2, 0.75f), ["Frontier Lever Rifle"] = (2, 2, 0.75f), ["M91 Trekhlineyka"] = (2, 2, 0.75f), ["Nightwalker .45-70"] = (2, 2, 0.75f), ["SV-2 Strela"] = (2, 2, 0.75f),
            ["Corvan Marksman"] = (3, 3, 0.75f), ["Sever-98"] = (3, 3, 0.75f), ["VK-127 Grom"] = (3, 3, 0.75f), ["Halden AM-50 Sledge"] = (3, 3, 0.75f),
            ["G-7 Rail Prototype"] = (99, 99, 0.9f), ["PL-74P Polovets Zulu"] = (2, 2, 0.75f),
        };

        /// <summary>How many bodies a round of this gun can go through (1 = it stops in the first), by the load in the chamber.</summary>
        public static bool NoPierceForReport;                  // the harness (RTBAL=9) switches penetration off to see how much of a result it explains; nothing in the game sets it
        public static (int targets, float mul) PierceOf(Weapon w)
        {
            if (NoPierceForReport) return (1, 1f);
            if (w?.Db != null && w.CurrentAmmoType == "Slug" && CombatRules.IsShotgun(w)) return (2, 0.75f);          // a slug passes through 2 bodies
            if (w?.Db == null || !Pierce.TryGetValue(w.Db.Name, out var p)) return (1, 1f);
            return (w.CurrentAmmoType == "ArmorPiercing" ? p.ap : p.targets, p.mul);
        }
        // (a slug goes through 2 bodies: see PierceOf below)

        /// <summary>Lever guns (2026-11-17, Yan: "can be levered when hip-fired, hip fire speed increased by 200%"): worked from the hip they fire three times as fast; aimed, they cycle at their listed rate.</summary>
        public static readonly HashSet<string> Levers = new HashSet<string> { "Nightwalker .45-70", "Frontier Lever Rifle", "Halden Lever Shotgun" };
        /// <summary>Effective range (2026-11-17, Yan: "make it so the effective range of each weapon is half of its total range"): a round keeps all its damage out to half the gun's reach, then loses it in a straight line to
        /// half damage at the reach itself (pistol 40: 20..40, SMG 50: 25..50, rifle 60: 30..60, heavy MG 60: 30..60, sniper 100: 50..100). Buckshot has no fall-off (its spread is the fall-off); a slug (reach 30) fades from 15.</summary>
        public static float EffectiveRange(Weapon w) => w?.Db == null ? 1f : For(w).Reach * 0.5f;
        public static float FalloffMult(Weapon w, float dist)
        {
            if (w?.Db == null) return 1f;
            if (CombatRules.IsShotgun(w) && w.CurrentAmmoType != "Slug") return 1f;
            if (CombatRules.IsMelee(w)) return 1f;
            float reach = For(w).Reach, start = reach * 0.5f; if (reach <= 1f || dist <= start) return 1f;
            return 1f - 0.5f * System.Math.Min(1f, (dist - start) / (reach - start));
        }

        public static float HipRate(Weapon w, bool aiming) => !aiming && w?.Db != null && Levers.Contains(w.Db.Name) ? 3f : 1f;
        public static readonly List<Round> Flying = new List<Round>();
        public static float AimUp;                             // extra climb (tan) of the rounds being made: the eel shot rises to the height of the part under the crosshair
        public static bool AimAtEel;                           // set by the player's fire code while the crosshair is over the eel, read when the rounds are made

        /// <summary>One trigger pull: rounds leave the muzzle, scattered by `spread` degrees sideways and up/down, at aim
        /// height h. Ammo, jams and the fire rate are the caller's (PlayerShoot / NpcShoot). RT 8: they fly (Fly), nothing lands yet.</summary>
        public static List<Round> Shoot(ICombatant shooter, Weapon w, float dx, float dy, float h, float spread, bool ambush, float delay = 0f)
        {
            var rounds = new List<Round>();
            var hd = For(w);
            var (x0, y0) = Ballistics.Pos(shooter);
            float aimSide = Gauss() * spread, aimUp = Gauss() * spread;
            for (int p = 0; p < hd.Pellets; p++)
            {
                float side = (aimSide + Gauss() * hd.Choke) * 0.0174533f, up = (aimUp + Gauss() * hd.Choke) * 0.0174533f;
                float c = (float)System.Math.Cos(side), s = (float)System.Math.Sin(side);
                var r = new Round { Shooter = shooter, W = w, Map = TerrainMap.ZoneId, X = x0, Y = y0, X0 = x0, Y0 = y0, Dx = dx * c - dy * s, Dy = dx * s + dy * c,
                    H = h, Up = (float)System.Math.Tan(up) + AimUp, Range = hd.Reach, Speed = System.Math.Max(5f, hd.Speed), Wait = delay, Pellets = hd.Pellets, Ambush = ambush, First = p == 0, Pierce = PierceOf(w).targets - 1, PierceMul = PierceOf(w).mul, AtEel = AimAtEel };
                Flying.Add(r); rounds.Add(r);
            }
            // the trigger pull itself: flash, sound, kick, casing (CombatFxView); the gun points along the shot
            CombatFx.Add(new FxEvent { Kind = "shot", Actor = shooter, Weapon = w, Now = true, Delay = delay, Flying = true, AtEel = AimAtEel,
                EndX = x0 + dx * 6f, EndY = y0 + dy * 6f, FromX = shooter.X, FromY = shooter.Y });
            if (!CombatRules.IsMelee(w)) Noise.Make(shooter.X, shooter.Y, w.Db != null && w.Db.InnateSuppressor ? Noise.Suppressed : Noise.Shot, shooter);
            return rounds;
        }

        /// <summary>RT 8: every round in the air moves one frame (Rt.WorldTick, the host's world). dt is game time.</summary>
        public static void Fly(float dt)
        {
            if (dt <= 0f) return;
            foreach (var u in Bodies())                       // how everyone is moving, for leading a shot (NpcLead)
            {
                var (x, y) = Ballistics.Pos(u);
                Track[u] = Track.TryGetValue(u, out var t) ? (x, y, t.vx + ((x - t.x) / dt - t.vx) * 0.3f, t.vy + ((y - t.y) / dt - t.vy) * 0.3f) : (x, y, 0f, 0f);
            }
            for (int i = 0; i < Flying.Count; i++)
            {
                var r = Flying[i];
                if (r.Map != TerrainMap.ZoneId) { Flying.RemoveAt(i--); continue; }   // the map changed under it
                if (r.Wait > 0f) { r.Wait -= dt; continue; }
                float step = System.Math.Min(r.Speed * dt, r.Range - r.Gone);
                float h = r.H + r.Up * (r.Gone + step * 0.5f) / Ballistics.BodyTiles;
                var hit = Ballistics.Trace(r.Shooter, r.X, r.Y, r.Dx, r.Dy, step, h, r.Hurt == null ? Targets(r.Shooter) : Targets(r.Shooter).Where(u => !r.Hurt.Contains(u)), r.AtEel);
                r.Through *= hit.Through;
                if (hit.CoverX >= 0) { r.CoverX = hit.CoverX; r.CoverY = hit.CoverY; }
                r.X = hit.EndX; r.Y = hit.EndY; r.Gone += hit.Dist;
                if (hit.Unit == null && !hit.Wall && !hit.Cover && r.Gone < r.Range - 0.001f) continue;
                hit.Through = r.Through; hit.CoverX = r.CoverX; hit.CoverY = r.CoverY; hit.Dist = r.Gone;
                if (hit.Unit != null && r.Pierce > 0 && !hit.Wall && !Boss.Deflects(hit.Unit))     // through the body and on (a wall or a deflecting boss stops it)
                {
                    Resolve(r.Shooter, r.W, hit, r.Ambush, r.First, 0f, r.Pellets);
                    (r.Hurt ?? (r.Hurt = new HashSet<ICombatant>())).Add(hit.Unit); r.Pierce--; r.Through *= r.PierceMul; r.First = false;
                    continue;
                }
                Flying.RemoveAt(i--);
                Land(r, hit);
            }
        }

        /// <summary>Test and probe helper: fly every round until it lands.</summary>
        public static void LandAll() { for (int k = 0; k < 2000 && Flying.Count > 0; k++) Fly(0.02f); }

        static void Land(Round r, Ballistics.Hit hit)
        {
            r.Hit = hit;
            var shooter = r.Shooter;
            Resolve(shooter, r.W, hit, r.Ambush, r.First, 0f, r.Pellets);
            if (!r.First) return;
            if (!(shooter is Player))
                foreach (var pl in Player.All)                                                  // co-op: whoever it just missed
                    if (hit.Unit != pl && NearMiss(r.X0, r.Y0, hit.EndX, hit.EndY, pl))
                    {
                        Rt.AddFocus(pl, CombatantUtil.HasPerk(pl, "ColdBlood") ? 12f : 6f);    // RT batch 3: a round that just missed you
                        if (!Rt.Of(pl).Diving) Rt.Of(pl).PinnedUntil = UnityEngine.Time.time + 1.5f;   // RT batch 4: it pins you (RT 8, Yan: not mid-dive)
                    }
            if (shooter is Player && Rt.On)                                                    // RT batch 4: your near misses pin them
                foreach (var e in EncounterState.Enemies)
                    if (e != hit.Unit && e.IsAlive() && NearMiss(r.X0, r.Y0, hit.EndX, hit.EndY, e)) RtAi.Suppress(e);
        }

        static readonly Dictionary<ICombatant, (float x, float y, float vx, float vy)> Track = new Dictionary<ICombatant, (float, float, float, float)>();

        /// <summary>RT 8: where to aim at a moving target so a round of this gun meets it - its position plus its velocity times
        /// the flight time. NPCs lead their shots fully (Yan's rule 6: real bullets mustn't make them worse shots); a dive or a
        /// sudden turn after the trigger is pulled is what makes them miss.</summary>
        public static (float x, float y) Lead(ICombatant shooter, Weapon w, ICombatant target)
        {
            var (sx, sy) = Ballistics.Pos(shooter); var (tx, ty) = Ballistics.Pos(target);
            if (!Track.TryGetValue(target, out var t)) return (tx, ty);
            float d = (float)System.Math.Sqrt((tx - sx) * (tx - sx) + (ty - sy) * (ty - sy)), fly = d / System.Math.Max(5f, For(w).Speed);
            return (tx + t.vx * fly, ty + t.vy * fly);
        }

        /// <summary>Damage (armor as before: CombatRules.PhysicalDamage), aggro, death hooks and the shot's effects.</summary>
        public static System.Action<ICombatant, Weapon, Ballistics.Hit> Probe;   // the harness listens to where every round ends (RTBAL=7); nothing in the game sets it
        static void Resolve(ICombatant shooter, Weapon w, Ballistics.Hit hit, bool ambush, bool first, float delay, int pellets)
        {
            Probe?.Invoke(shooter, w, hit);
            var fx = new FxEvent { Kind = "shot", Actor = shooter, Weapon = w, Now = true, Delay = delay, Pellet = !first, Landed = true,
                EndX = hit.EndX, EndY = hit.EndY, FromX = shooter.X, FromY = shooter.Y };
            if (hit.CoverX >= 0)
            {
                fx.CoverX = hit.CoverX; fx.CoverY = hit.CoverY; fx.CoverMat = Materials.At(hit.CoverX, hit.CoverY);
                fx.CoverHit = hit.Cover || hit.Wall; fx.Through = hit.Through < 1f;
                if (hit.Cover || hit.Through < 1f) TerrainMap.DamageTile(hit.CoverX, hit.CoverY, Barrier.Sturdy(hit.CoverX, hit.CoverY) ? UnityEngine.Mathf.Max(1, UnityEngine.Mathf.RoundToInt(RoundDamage(w, "Torso", hit.Dist, pellets) * Barrier.RoundWear)) : 1, shooter);
            }
            var target = hit.Unit;
            if (target != null && (Boss.Deflects(target) || Tier2Story.ShieldsElder(shooter, target)))   // Boss 1: he swats it away unless he's staggered; "Taken": the Elder Gaunt shrugs off the soldiers' rounds
            {
                fx.Kind = "deflect"; fx.Target = target; fx.Landed = false;
                CombatFx.Add(fx);
                return;
            }
            if (target != null)
            {
                fx.Target = target; fx.ToX = target.X; fx.ToY = target.Y; fx.Hit = true;
                // RT batch 6: a helmet can turn the round away
                if (hit.Part == "Head" && UnityEngine.Random.value < HeadGear.DeflectChance(target, w))
                {
                    fx.Hit = false; fx.CoverHit = true; fx.CoverX = target.X; fx.CoverY = target.Y; fx.CoverMat = Material.Steel;
                    Body.Concuss(target, 2f);
                    if (first) GameLog.Write($"The round pings off {(target is Player ? "your" : target.Name + "'s")} helmet!");
                    CombatFx.Add(fx);
                    return;
                }
                float raw = RoundDamage(w, hit.Part, hit.Dist, pellets) * hit.Through;
                raw *= TuneMult(w, target is Player || target is HumanEnemy) * FalloffMult(w, hit.Dist);   // the 2026-11-17 weapon pass (Tune) and the fall-off with distance
                if (ambush) raw *= 1.3f;                                       // they never saw it coming (Batch 64, as a bonus now)
                if (EelSump.IsEelHead(target, hit.Part)) raw *= EelSump.HeadBonus;   // Boss 3: only the eel's head takes extra damage (Yan, 2026-11-06)
                if (ItemCondition.IsDamaged(w.Condition)) raw *= 0.75f;
                if (shooter is Player) raw *= CombatRules.PlayerDamage;
                raw *= CombatRules.Lethality;
                float ignore = w.Db?.IgnorePhysDRPercent ?? 0f;
                if (shooter is Player pk)
                {
                    string cat = CombatRules.Category(w.Db);
                    if (cat == "sniper" && hit.Dist >= 12f && CombatantUtil.HasPerk(pk, "HighVelocityImpact")) ignore = 1f - (1f - ignore) * 0.8f;   // High-Velocity Impact
                    if (cat == "shotgun" && hit.Dist <= 4f)
                    {
                        if (CombatantUtil.HasPerk(pk, "Breacher")) raw *= 1.25f;                                                                       // Breacher
                        if (CombatantUtil.HasPerk(pk, "StaggeringBlast") && first && target is Mutant reeling) RtAi.Pin(reeling, 1.0f);                  // Staggering Blast
                    }
                }
                if (shooter is Player sp && sp.ArtifactBypassPhysDR > 0) ignore = 1f - (1f - ignore) * (1f - sp.ArtifactBypassPhysDR);
                float dr = hit.Part == "Head" ? HeadGear.Of(HeadGear.ArmorOf(target)).dr : target.PhysDR * CombatRules.ArmorScale(target);   // RT batch 6: helmets
                var load = AmmoDatabase.Load(w.Db, w.CurrentAmmoType);                       // what this caliber's load does (AmmoDatabase)
                int dmg = load != null ? CombatRules.PhysicalDamage(raw, load, dr, ignore) : CombatRules.PhysicalDamage(raw, w.CurrentAmmoType, dr, ignore);
                int before = target.Hp;
                target.Hp = System.Math.Max(0, target.Hp - dmg);
                Boss.OnDamaged(target);
                fx.Killed = !target.IsAlive(); fx.Overkill = dmg - before;
                if (first || pellets == 1) GameLog.Write($"{Who(shooter)} hits {Who(target)} in the {hit.Part.ToLowerInvariant()} for {dmg}.");
                TestingGround.Note(target, dmg, hit.Part);                    // the testing ground's damage numbers
                if (target is Mutant tm && !TestingGround.IsDummy(tm)) { tm.IsAggroed = true; }
                if (shooter is Player && target is HumanEnemy he && !TestingGround.IsDummy(he)) foreach (var o in EnemyAI.Squad(he)) if (o.IsAlive()) o.IsAggroed = true;
                if (target.IsAlive()) Body.Hit(target, hit.Part, hit.Side, dmg, w.CurrentAmmoType, shooter is Player && Rt.SlowMo ? 1.5f : 1f);   // RT batch 3: bullet time wounds more
                if (shooter is Player sf) Rt.AddFocus(sf, target.IsAlive() ? 4f : 25f);   // RT batch 2: limb health, wounds, down
                if (!target.IsAlive()) Weapon.OnTargetKilled(shooter, target);
                if (target is Player p && p.IsAlive()) Perks.TryAdrenaline(p);
            }
            CombatFx.Add(fx);
        }

        // RT batch 6 (Yan's targets, weapon vs armor of its own tier: rifles 2-4 head hits, snipers 1 head / 2 chest, pistols 2-4,
        // SMGs 4-6, shotguns 1 to many by range, machine guns like rifles): the per-round damage was tuned for one shot per turn,
        // so each class gets a real-time multiplier. Shotguns have no range fall-off any more (2026-11-17): their pellet spread does it.
        public static readonly Dictionary<string, float> ClassDamage = new Dictionary<string, float>
            { ["sidearm"] = 1f, ["smg"] = 0.8f, ["assault"] = 1f, ["sniper"] = 1.8f, ["shotgun"] = 1f, ["heavy"] = 1f };

        /// <summary>One round's (or one pellet's) damage before armor: base x class x part x shotgun range fall-off.</summary>
        public static float RoundDamage(Weapon w, string part, float dist, int pellets)
        {
            string cat = CombatRules.Category(w?.Db);
            float raw = (w.BaseDamage + (w.Upgrades.Contains("Reinforced") ? 30 : 0)) * (ClassDamage.TryGetValue(cat, out var m) ? m : 1f) * Ballistics.PartMult(part);
            if (pellets > 1) raw = raw / pellets * 1.5f;                      // 2026-11-17 (Yan): no damage fall-off for shotguns, the spread of the pellets is the fall-off
            return raw;
        }

        /// <summary>The round's path (x0,y0)-(x1,y1) passed within 0.6 tiles of the unit.</summary>
        public static bool NearMiss(float x0, float y0, float x1, float y1, ICombatant u)
        {
            var (ux, uy) = Ballistics.Pos(u);
            float vx = x1 - x0, vy = y1 - y0, l2 = vx * vx + vy * vy;
            if (l2 < 1e-4f) return false;
            float t = System.Math.Max(0f, System.Math.Min(1f, ((ux - x0) * vx + (uy - y0) * vy) / l2));
            float dx = x0 + vx * t - ux, dy = y0 + vy * t - uy;
            return dx * dx + dy * dy < 0.36f;
        }

        static string Who(ICombatant u) => u is Player ? "You" : u.Name;

        /// <summary>Where an NPC aims: at the body, or (sometimes, more with rank) at the head; how steady it holds.</summary>
        public static (float h, float spread) NpcAim(ICombatant shooter, Weapon w, ICombatant target)
        {
            int r = shooter is HumanEnemy he ? CombatRules.RankIndex(he.Rank) : 0;      // 0 Rookie .. 6 Legend
            float h = UnityEngine.Random.value < 0.08f + 0.07f * r ? 0.8f : 0.45f;
            var hd = For(w);
            float spread = hd.Steady * (2.6f - 0.25f * r) + 0.8f + (shooter.StatusEffects?.Suppressed == true ? 2.5f : 0f);
            if (shooter.StatusEffects?.Wound == "Arm") spread += 1.5f;
            if (Husk.Is(shooter)) { spread += Husk.AimSpread; h = 0.45f; }   // Husks: very bad accuracy
            if (Body.Concussed(shooter)) spread += 2f;
            return (h, spread);
        }

        /// <summary>The bridge while enemies still think in turns (EnemyAI.TakeTurn every few seconds, RT batch 4 replaces
        /// it): Weapon.Fire hands NPC gunfire here, and each round is a real one at the target.</summary>
        public static void NpcShoot(ICombatant shooter, Weapon w, ICombatant target, int rounds)
        {
            if (Body.Down(target)) return;                     // RT batch 2: they leave someone who's down to bleed
            if (AllyInLine(shooter, target)) return;           // a friend in the way: hold fire
            var (sx, sy) = Ballistics.Pos(shooter); var (tx, ty) = Lead(shooter, w, target);
            float dx = tx - sx, dy = ty - sy, len = (float)System.Math.Sqrt(dx * dx + dy * dy);
            if (len < 0.01f) return;
            dx /= len; dy /= len;
            var (h, spread) = NpcAim(shooter, w, target);
            var hd = For(w);
            for (int i = 0; i < rounds; i++)
                Shoot(shooter, w, dx, dy, h, spread + i * hd.Kick * 0.6f, false, i * 60f / System.Math.Max(60f, hd.Rpm));
        }

        /// <summary>2026-11-17 (Yan: enemies should not waste rounds into walls and cover): can a round fired from (sx, sy) (tile space) reach `target` at chest or head height, i.e. is the first thing on the line the target?
        /// Bodies other than the target are ignored here (AllyInLine handles friends).</summary>
        public static bool ClearShotFrom(ICombatant shooter, float sx, float sy, ICombatant target)
        {
            var (tx, ty) = Ballistics.Pos(target); float dx = tx - sx, dy = ty - sy, len = (float)System.Math.Sqrt(dx * dx + dy * dy);
            if (len < 0.5f) return true;
            var one = new[] { target };
            foreach (float h in new[] { 0.45f, 0.8f })
                if (Ballistics.Trace(shooter, sx, sy, dx / len, dy / len, len + 0.5f, h, one).Unit == target) return true;
            return false;
        }

        /// <summary>RT batch 4: one trigger pull by an NPC fighting in real time (RtAi), extra = spread from moving or being pinned.</summary>
        public static void NpcShootOnce(ICombatant shooter, Weapon w, ICombatant target, float extra)
        {
            var (sx, sy) = Ballistics.Pos(shooter); var (tx, ty) = Lead(shooter, w, target);
            float dx = tx - sx, dy = ty - sy, len = (float)System.Math.Sqrt(dx * dx + dy * dy);
            if (len < 0.01f) return;
            var (h, spread) = NpcAim(shooter, w, target);
            Shoot(shooter, w, dx / len, dy / len, h, spread + extra, false);
        }
    }
}
