"""Check (2026-11-13): every gun listed in WeaponArt.Dense, in the character's hands with its double-size picture, using the game's own pose frames (`dotnet /tmp/test.dll heldframes frames.json`) and the grip row
and density k read from HeldWeapon.cs. One row per gun: aiming, then the reload at 4 points (hands only, no cut pieces). Look for: the glove on the grip, the support hand under the fore-end, the muzzle where the barrel ends.
Usage: python3 Tools/render/held_hires_check.py frames.json out.png"""
import json, os, re, sys
from PIL import Image, ImageDraw, ImageFont
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import held_anim as H
import held_hires_preview as P
S = H.S; SPR = H.SPR; ROOT = H.ROOT
def dense():
    s = open(os.path.join(ROOT, "Assets/Scripts/GameCore/HeldWeapon.cs")).read(); m = re.search(r"Dense = new Dictionary<string, float>\s*\{(.*?)\};", s, re.S)
    return {k: float(v) for k, v in re.findall(r'\["(\w+)"\] = ([0-9.]+)f', m.group(1))}
def main(src, out):
    D = dense(); anims = [a for a in json.load(open(src)) if a["key"] in D and a["style"] != "jam"]
    atlas = Image.open(os.path.join(SPR, "armor_atlas.png")).convert("RGBA"); font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 16)
    cw, ch = H.W * S, H.H * S; sheet = Image.new("RGBA", (170 + 5 * (cw + 8), 30 + len(anims) * (ch + 26)), (27, 29, 31, 255)); d = ImageDraw.Draw(sheet)
    d.text((8, 6), "Held with the double-size pictures (the game's own pose code, the grip rows and densities from HeldWeapon.cs)", font=font, fill=(235, 235, 235))
    for r, a in enumerate(anims):
        k = D[a["key"]]; i = a["armor"]; body = atlas.crop(((i % 16) * 16, (i // 16) * 16, (i % 16) * 16 + 16, (i // 16) * 16 + 16)).resize((16 * H.S, 16 * H.S), Image.NEAREST); gl = H.glove(a["cuff"], a["glove"])
        big = Image.open(os.path.join(SPR, "Weapons", f"weapon_{a['key']}_hd.png")).convert("RGBA"); grip = (a["gx"] * k, a["gy"] * k)
        n = len(a["frames"]); picks = [n - 1, n // 6, n // 3, n // 2, 3 * n // 4]; labels = ["aiming", "reload 15 %", "33 %", "50 %", "75 %"]; y = 30 + r * (ch + 26)
        d.text((8, y + ch // 2 - 22), a["name"], font=font, fill=(255, 255, 0)); d.text((8, y + ch // 2), f"k = {k:.2f}", font=font, fill=(200, 200, 200))
        for j, p in enumerate(picks): sheet.alpha_composite(P.frame_new(a, a["frames"][p], body, big, gl, grip, k), (170 + j * (cw + 8), y)); d.text((174 + j * (cw + 8), y + ch + 3), labels[j], font=font, fill=(200, 200, 200))
    sheet.convert("RGB").save(out); print("saved", out, sheet.size, [a["name"] for a in anims])
if __name__ == "__main__": main(sys.argv[1], sys.argv[2])
