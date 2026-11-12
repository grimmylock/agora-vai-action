# The Veil Prowler's cloak dissolve (2026-11-12, Yan: "a short pixel-noise dissolve from full opacity down to the shimmer"): 6 frames of its stand sprite with a growing share of pixels gone, always the same
# noise field so the pattern is stable. The game plays them while the cloak fades in or out (MutantAttack.CloakFade); the shimmer itself (alpha 0.3) is the game's.   python3 make_veil_dissolve.py
import os, numpy as np
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__)); R = os.path.join(HERE, "..", "Assets", "Resources", "Sprites", "Mutants")
st = np.array(Image.open(os.path.join(R, "mutant_hd_veil_prowler.png")).convert("RGBA")); h, w = st.shape[:2]
rng = np.random.RandomState(11); noise = rng.rand(h, w)
TH = [0.06, 0.22, 0.42, 0.62, 0.80, 0.93]                    # the share of pixels gone in each frame
strip = Image.new("RGBA", (w * len(TH), h), (0, 0, 0, 0))
for i, t in enumerate(TH):
    a = st.copy(); a[noise < t] = 0; strip.alpha_composite(Image.fromarray(a), (i * w, 0))
strip.save(os.path.join(R, "anim", "veil_prowler_dissolve.png")); os.replace(os.path.join(R, "anim", "veil_prowler_dissolve.png"), os.path.join(R, "anim", "veil_prowler_dissolve.bytes"))
sc = 8; sheet = Image.new("RGB", (w * len(TH) * sc, h * sc), (105, 115, 90)); t = strip.resize((strip.width * sc, strip.height * sc), Image.NEAREST); sheet.paste(t, (0, 0), t)
sheet.save(os.path.join(HERE, "mutants_anim", "veil_dissolve_sheet.png")); print("frames", len(TH), "frame", w, "x", h)
