# The same attacks seen the way the game draws them: a top-down grid of square tiles, a profile sprite that only flips left/right toward its target (TacticalGridView.ApplyUnitArt), its feet on the bottom edge of its
# tile, the pounce sliding diagonally by the strip's travel curve and the charge running straight down the screen. Hand-built timeline, not a Unity capture.   python3 render_attack_topdown.py <out_dir>
import os, sys
from PIL import Image, ImageDraw, ImageFont
import render_attacks_vs_pc as R
T = R.T; SC = 4; GW, GH = 14, 8; STEP = R.STEP
font = R.font; small = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", 11)
def base(caption):
    im = Image.new("RGBA", (GW * T, GH * T), (58, 54, 46, 255)); d = ImageDraw.Draw(im)
    for x in range(GW + 1): d.line([(x * T, 0), (x * T, GH * T)], fill=(70, 66, 58))
    for y in range(GH + 1): d.line([(0, y * T), (GW * T, y * T)], fill=(70, 66, 58))
    return im
def place(im, img, tile_x, tile_y, flip, lowest_row, alpha=1.0):
    """tile_x/tile_y are float tile coordinates of the unit's tile centre; the feet sit on the tile's bottom edge"""
    if flip: img = img.transpose(Image.FLIP_LEFT_RIGHT)
    im.alpha_composite(img, (int((tile_x + 0.5) * T - img.width / 2), int((tile_y + 1.0) * T - (lowest_row + 1))))
def finish(frames, im, caption):
    big = im.resize((im.width * SC, im.height * SC), Image.NEAREST); ImageDraw.Draw(big).text((16, 10), caption, font=font, fill=(235, 228, 205)); frames.append(big.convert("RGB"))
def save(frames, path): frames[0].save(path, save_all=True, append_images=frames[1:], duration=STEP, loop=0); print(path, len(frames), frames[0].size)

def dread_diagonal(out):
    key = "dread_prowler"; a = R.TABLE[key]["pounce"]; fr = R.strip_of(key, "pounce"); bite = R.strip_of(key, "bite"); frames = []
    sx, sy, px_, py_ = 2, 1, 10, 5; lx, ly = px_ - 1, py_ - 1; flip = False                       # the target is to the right and below: the sprite faces right
    t_hit = a["contact_ms"]
    for now in range(0, a["total_ms"] + 1200, STEP):
        k = R.travel_at(key, "pounce", now); x = sx + (lx - sx) * k; y = sy + (ly - sy) * k
        im = base(""); e = now - t_hit
        pcimg, dx, dy = R.pc_state(t_hit, now, "knockdown")
        place(im, fr[R.attack_frame(key, "pounce", now)], x, y, flip, R.L_attack[key])
        place(im, pcimg, px_ + dx / T, py_, False, 30)
        finish(frames, im, "Dread Prowler: a pounce from the upper left, seen top-down (knocks the target down)")
    save(frames, os.path.join(out, "dread_topdown.gif"))

def maw_vertical(out):
    key = "blind_maw"; bite = R.strip_of(key, "bite"); wk = R.walk[key]; st = R.stand[key]; L = R.lowest(st); frames = []
    cx, y0, py_ = 6, 0.0, 6; flip = False; spd = 7.0; hit_t = None; pos = y0
    for now in range(0, 3000, STEP):
        if now < 300: img = bite[2]; pos = y0
        elif hit_t is None:
            pos = y0 + spd * (now - 300) / 1000.0; img = wk[int((now - 300) / 62) % 8]
            if abs(pos - (py_ - 0.95)) < 0.06 or pos >= py_ - 0.95: hit_t = now; pos = py_ - 0.95
        im = base("")
        if hit_t is not None: bi = 4 + min(5, int((now - hit_t) / 90)); pic = bite[bi]; low = R.L_attack[key]
        else: pic = img; low = R.L_attack[key] if img in bite else L
        place(im, pic, cx, pos, flip, low); pcimg, dx, dy = R.pc_state(hit_t, now, "bleed"); place(im, pcimg, cx + dx / T, py_, False, 30)
        finish(frames, im, "Blind Maw: charging straight down the screen (it keeps facing right)")
        if hit_t is not None and now - hit_t > 1500: break
    save(frames, os.path.join(out, "maw_topdown.gif"))

if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else "mutants_anim/vs_pc"; os.makedirs(out, exist_ok=True); dread_diagonal(out); maw_vertical(out)
