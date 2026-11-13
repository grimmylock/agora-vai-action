// 2026-11-12: the Pallid Prowler's acid spit (MutantAttack): the glob in flight from its muzzle to the target and the splash where it lands. The damage and the timing live in MutantAttack; this only draws.
using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    public partial class CombatFxView
    {
        readonly List<SpriteRenderer> _acid = new List<SpriteRenderer>();

        void DrawAcid()
        {
            int n = 0;
            if (Rt.On)
            {
                var glob = UnitSprites.HDFx("pallid_acid_glob", 6, new Vector2(13f / 18f, 0.5f));      // the head is 5 px from the right edge, the tail streams left
                var splat = UnitSprites.HDFx("pallid_acid_splat", 6, new Vector2(0.5f, 4f / 14f));      // bottom centre on the ground
                var rock = UnitSprites.HDFx("brute_boulder", 6, new Vector2(0.5f, 0.5f)); var rockHit = UnitSprites.HDFx("brute_rock_hit", 6, new Vector2(0.5f, 8f / 40f)); var dust = UnitSprites.HDFx("brute_slam_dust", 6, new Vector2(0.5f, 5f / 24f));      // the Blight Brute's boulder, its shatter and the slam dust
                foreach (var e in EncounterState.Enemies)
                {
                    var m = e as Mutant; if (m == null || !MutantAttack.Has(m)) continue;
                    if (m.Db.Name == "Blight Brute")
                    {
                        if (rock != null && MutantAttack.BoulderAt(m, Time.time, out float bx, out float by, out float bh, out int bf))
                        {
                            var sr = Pooled(_acid, n++, "Boulder", 9); if (sr == null) continue;
                            sr.sprite = rock[bf % rock.Length]; sr.color = Color.white; sr.transform.position = W(bx, by, bh); sr.transform.rotation = Quaternion.identity; sr.transform.localScale = Vector3.one;
                        }
                        if (rockHit != null && MutantAttack.SplatAt(m, Time.time, out float hx, out float hy, out int hf))
                        {
                            var sr = Pooled(_acid, n++, "RockHit", 8); if (sr == null) continue;
                            sr.sprite = rockHit[Mathf.Clamp(hf, 0, rockHit.Length - 1)]; sr.color = Color.white; sr.transform.position = W(hx, hy, 0.1f); sr.transform.rotation = Quaternion.identity; sr.transform.localScale = Vector3.one;
                        }
                        if (dust != null && MutantAttack.DustAt(m, Time.time, out int df))
                        {
                            var (dox, doy) = Rt.Offset(m); var sr = Pooled(_acid, n++, "SlamDust", 8); if (sr == null) continue;
                            sr.sprite = dust[Mathf.Clamp(df, 0, dust.Length - 1)]; sr.color = Color.white; sr.transform.position = W(m.X + 0.5f + dox, m.Y + 0.5f + doy, 0.1f); sr.transform.rotation = Quaternion.identity; sr.transform.localScale = Vector3.one;
                        }
                        continue;
                    }
                    if (glob != null && MutantAttack.GlobAt(m, Time.time, out float gx, out float gy, out float ang))
                    {
                        var sr = Pooled(_acid, n++, "Glob", 9); if (sr == null) continue;
                        sr.sprite = glob[(int)(Time.time * 14f) % glob.Length]; sr.color = Color.white;
                        sr.transform.position = W(gx, gy, 0.45f); sr.transform.rotation = Quaternion.Euler(0f, 0f, -ang); sr.transform.localScale = Vector3.one;
                    }
                    if (splat != null && MutantAttack.SplatAt(m, Time.time, out float sx, out float sy, out int fi))
                    {
                        var sr = Pooled(_acid, n++, "Splat", 8); if (sr == null) continue;
                        sr.sprite = splat[Mathf.Clamp(fi, 0, splat.Length - 1)]; sr.color = Color.white;
                        sr.transform.position = W(sx, sy, 0.1f); sr.transform.rotation = Quaternion.identity; sr.transform.localScale = Vector3.one;
                    }
                }
            }
            HideFrom(_acid, n);
        }
    }
}
