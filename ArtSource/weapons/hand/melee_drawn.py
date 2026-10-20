"""RT 7u: the melee weapons drawn at their final sizes (Yan: references in ArtSource/refs/melee/, colours taken from them).
Shrinking a reference can't keep one-pixel details (teeth, wire, a knuckle guard), so each weapon is drawn from a few shapes -
handle, guard, blade, head - in in-hand units (the 32x16 canvas) and painted twice: x1 for the hand, x2 for the menu (64x32),
with the small details re-spaced by hand for each size. Handle on the left, head/blade on the right, edge down (the game's
convention, as the Tactical Hatchet). Light from the top-left; the outline is the weapons' #141617.
  python3 ArtSource/weapons/hand/melee_drawn.py [--install]   -> ArtSource/_out/melee/ (or the game's Weapons folder)
                                                               + ArtSource/_out/melee_drawn_sheet.png"""
import os, sys, math
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))
WEAP = os.path.join(ROOT, "Assets/Resources/Sprites/Weapons")
K = (20, 22, 23, 255)

def hx(h): v = int(h.lstrip("#"), 16); return ((v >> 16) & 255, (v >> 8) & 255, v & 255, 255)
def ramp(*hs): return [hx(h) for h in hs]                       # dark -> light

class Canvas:
    """A sprite drawn in in-hand units: s = 1 (32x16) or 2 (64x32). Everything is filled pixel by pixel."""
    def __init__(self, s):
        self.s = s; self.im = Image.new("RGBA", (int(34 * s) + 4, int(18 * s) + 4)); self.px = self.im.load()
    def put(self, x, y, c):
        if 0 <= x < self.im.width and 0 <= y < self.im.height: self.px[x, y] = c
    def get(self, x, y): return self.px[x, y] if 0 <= x < self.im.width and 0 <= y < self.im.height else (0, 0, 0, 0)
    def P(self, u): return int(round(u * self.s))                # units -> pixels

    def rod(self, x0, x1, yc, th, cols, th_at=None):
        """A round bar from x0 to x1 (units), centred on yc, th units thick (or th_at(u) for a taper): lit top, dark bottom."""
        for px in range(self.P(x0), self.P(x1)):
            u = (px + 0.5) / self.s
            t = max(1, int(round((th_at(u) if th_at else th) * self.s)))
            top = int(round(yc * self.s - t / 2))
            for k in range(t):
                f = (k + 0.5) / t                                     # 0 top .. 1 bottom
                c = cols[3] if f < 0.25 else cols[2] if f < 0.55 else cols[1] if f < 0.85 else cols[0]
                if t <= 2: c = cols[2] if k == 0 else cols[1]
                self.put(px, top + k, c)

    def grain(self, x0, x1, yc, th, c, every):
        """Wood grain / scuffs: short streaks along the bar."""
        for i, px in enumerate(range(self.P(x0), self.P(x1))):
            if i % every in (0, 1) and (px * 7 + i) % 3 == 0:
                t = max(1, int(round(th * self.s)))
                self.put(px, int(round(yc * self.s - t / 2)) + (i // every) % max(1, t - 1) + (1 if t > 2 else 0), c)

    def wrap(self, x0, x1, yc, th, c, every):
        """Tape or cord round a grip: slanted bands every `every` pixels."""
        t = max(1, int(round(th * self.s))); top = int(round(yc * self.s - t / 2))
        for px in range(self.P(x0), self.P(x1)):
            for k in range(t):
                if (px - k) % every == 0: self.put(px, top + k, c)

    def blade(self, x0, x1, spine, depth_at, cols, teeth=0, edge_light=True):
        """A blade lying along x: from the spine row down `depth_at(u)` units. cols = (spine, flat, bevel, edge highlight).
        teeth: a serrated spine, one tooth every `teeth` units."""
        for px in range(self.P(x0), self.P(x1)):
            u = (px + 0.5) / self.s
            d = depth_at(u)
            if d <= 0: continue
            top = int(round(spine * self.s)); n = max(1, int(round(d * self.s)))
            for k in range(n):
                f = k / max(1, n - 1)
                c = cols[0] if k == 0 else cols[3] if (k == n - 1 and edge_light and n > 1) else cols[2] if f > 0.6 else cols[1]
                self.put(px, top + k, c)
            if teeth and int(u / teeth * 2) % 2 == 0 and u < x1 - 2: self.put(px, top - 1, cols[0])

    def poly(self, pts, cols):
        """A filled shape (units), shaded the pixel-art way: flat mid tone, a lit rim on the top/left edges, a dark rim on the
        bottom/right ones (cols: dark, mid-dark, mid, light)."""
        P = [(x * self.s, y * self.s) for x, y in pts]
        mask = Image.new("L", self.im.size); ImageDraw.Draw(mask).polygon(P, fill=255); m = mask.load()
        W, H = self.im.size
        inside = lambda x, y: 0 <= x < W and 0 <= y < H and m[x, y]
        for y in range(H):
            for x in range(W):
                if not m[x, y]: continue
                c = cols[2]
                if not inside(x, y + 1) or not inside(x + 1, y): c = cols[0]
                elif not inside(x, y + 2) or not inside(x + 2, y): c = cols[1]
                if not inside(x, y - 1) or not inside(x - 1, y): c = cols[3]
                self.put(x, y, c)

    def line(self, pts, c):
        P = [(int(round(x * self.s)), int(round(y * self.s))) for x, y in pts]
        ImageDraw.Draw(self.im).line(P, fill=c, width=1)

    def finish(self, w, h):
        """Outline, then centre the drawing on a w x h canvas."""
        self.outline()
        im = self.im.crop(self.im.getbbox())
        out = Image.new("RGBA", (w, h)); out.alpha_composite(im, ((w - im.width) // 2, (h - im.height) // 2))
        return out

    def outline(self):
        add = [(x, y) for y in range(self.im.height) for x in range(self.im.width) if not self.px[x, y][3]
               and any(self.get(x + dx, y + dy)[3] and self.get(x + dx, y + dy) != K for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
        for p in add: self.px[p] = K
        return self.im

# ---------------- the weapons (in-hand units: 32 x 16, y = 8 is the middle) ----------------
WOOD = ramp("#623e30", "#9f6a46", "#b07a52", "#c9a07c")
STEEL = ramp("#5c5c61", "#727275", "#a9a8ad", "#e4e4e8")

def hunting_knife(c):
    c.rod(8.5, 15.0, 8.2, 2.4, WOOD); c.grain(9.2, 14.5, 8.2, 2.4, hx("#623e30"), 3)
    c.put(c.P(9.0), c.P(8.2), hx("#b59577"))                              # the butt cap
    c.rod(14.6, 15.4, 8.0, 3.6, ramp("#20161f", "#3c2d2f", "#726059", "#8c7e77"))   # guard
    def d(u):                                                             # a clip point: deepest mid-blade, the spine dips at the tip
        t = (u - 15.4) / 8.4
        return 2.6 if t < 0.65 else 2.6 * (1 - (t - 0.65) / 0.35) ** 0.8
    c.blade(15.4, 23.8, 7.0, d, STEEL)
    for px in range(c.P(20.5), c.P(23.8)):                                 # the clip: spine bevel bright
        c.put(px, c.P(7.0), hx("#e4e4e8"))
    if c.s >= 1.5: c.put(c.P(16.5), c.P(6.6), STEEL[0]); c.put(c.P(17.5), c.P(6.6), STEEL[0])   # two jimping notches

def switchblade(c):
    c.rod(8.5, 15.0, 8.2, 2.0, ramp("#020216", "#1f1c23", "#36252c", "#473e48"))
    c.rod(8.3, 9.1, 8.2, 2.2, ramp("#5a5c68", "#9193a0", "#a0a3af", "#dfe2e8"))            # bolster
    c.rod(14.6, 15.4, 8.2, 3.0, ramp("#5a5c68", "#9193a0", "#a0a3af", "#dfe2e8"))          # guard
    c.put(c.P(11.6), c.P(8.2), hx("#9193a0"))                                             # the button
    c.blade(15.4, 23.8, 7.5, lambda u: 1.6 if u < 21.0 else 1.6 * (23.8 - u) / 2.8, ramp("#5a5c68", "#9193a0", "#a0a3af", "#dfe2e8"))

def machete(c):
    c.rod(3.0, 9.0, 8.3, 2.2, ramp("#090a1b", "#202028", "#27282d", "#3f4045"))
    c.wrap(3.4, 8.8, 8.3, 2.2, hx("#090a1b"), 3 if c.s >= 1.5 else 2)
    c.rod(9.0, 9.7, 8.2, 3.0, ramp("#090a1b", "#27272c", "#3f4045", "#87878c"))            # guard
    def d(u):
        t = (u - 9.7) / 19.6
        return 2.4 + 0.7 * t if t < 0.86 else (3.1 * (1 - (t - 0.86) / 0.14)) + 0.4
    c.blade(9.7, 29.3, 7.0, d, ramp("#202028", "#27282d", "#3f4045", "#87878c"))

def trench_knife(c):
    BRASS = ramp("#523b26", "#756752", "#8a7e66", "#c5b893")
    c.rod(7.5, 13.6, 8.0, 2.2, ramp("#161424", "#363539", "#464444", "#505154"))
    c.wrap(7.8, 13.4, 8.0, 2.2, hx("#161424"), 3 if c.s >= 1.5 else 2)
    c.put(c.P(7.1), c.P(8.0), BRASS[2])                                                   # skull-crusher pommel
    # the knuckle guard: brass finger rings under the grip, see-through, joined to the guard
    if c.s < 1.5:                                                                          # in the hand: a brass bar, two dark holes
        c.poly([(8.0, 9.0), (14.0, 9.0), (14.0, 11.8), (8.0, 11.8)], BRASS)
        for u in (9.6, 11.8): c.put(c.P(u), c.P(10.4), K)
    else:
        for i, u in enumerate([8.0, 9.8, 11.6, 13.2]):
            P = [int(round(v * c.s)) for v in (u - 0.95, 9.0, u + 0.95, 12.0)]
            ImageDraw.Draw(c.im).ellipse(P, outline=BRASS[3] if i == 0 else BRASS[2], width=1)
            c.put(P[0] + (P[2] - P[0]) // 2, P[3], BRASS[0])                              # the ring's lower rim, darker
    c.rod(13.6, 14.4, 8.0, 3.6, BRASS)                                                     # guard
    c.blade(14.4, 24.2, 6.9, lambda u: 2.5 if u < 21.5 else 2.5 * (24.2 - u) / 2.7,
            ramp("#363539", "#505154", "#8a7e66", "#c5b893"), teeth=1.0 if c.s >= 1.5 else 0)
    if c.s < 1.5:                                                                          # in the hand: three notches on the spine
        for u in (17.0, 19.0, 21.0): c.put(c.P(u), c.P(6.9) - 1, hx("#505154"))

def hatchet(c):
    c.rod(6.0, 22.5, 8.6, 2.2, ramp("#97644a", "#b57951", "#d5a87a", "#cfbdaa"),
          th_at=lambda u: 2.6 if u < 8 else 2.0 if u < 18 else 2.4)          # a swell at the butt and under the head
    c.grain(7.0, 21.5, 8.6, 2.2, hx("#b57951"), 4)
    c.poly([(20.4, 4.6), (24.2, 4.4), (24.6, 7.8), (26.6, 12.8), (22.2, 12.8), (22.8, 9.8), (20.4, 9.6)],
           ramp("#4a4c55", "#74767f", "#97989e", "#cfd1d6"))
    for px in range(c.P(22.4), c.P(26.4)): c.put(px, c.P(12.8) - 1, hx("#e4e4e8"))      # the edge, honed bright

def fire_axe(c):
    c.rod(1.5, 26.0, 8.4, 2.2, ramp("#7c0e22", "#d75427", "#dc6a27", "#d08a62"), th_at=lambda u: 2.6 if u < 4 else 2.1)
    c.rod(1.5, 7.5, 8.4, 2.7, ramp("#050404", "#1f1a1c", "#3a2d2c", "#5f3930"))          # black grip
    RED = ramp("#7c0e22", "#b30e25", "#ee1916", "#f26a5c")
    c.poly([(24.4, 1.2), (25.2, 0.6), (26.2, 5.8), (24.2, 5.8)], RED)                          # the pick, up
    c.poly([(23.4, 5.6), (26.6, 5.6), (26.6, 10.8), (23.4, 10.8)], RED)                        # the eye round the handle
    c.poly([(24.2, 10.4), (26.6, 10.4), (28.4, 12.2), (29.6, 14.8), (24.0, 14.8), (24.6, 12.4)], RED)   # the blade, down and out
    for px in range(c.P(24.2), c.P(29.6)): c.put(px, c.P(14.8) - 1, hx("#d9d6d2"))       # bare steel edge

def baseball_bat(c):
    c.rod(1.0, 30.0, 8.2, 2.0, ramp("#9a6c52", "#bf8459", "#e5b57c", "#f5e3cb"),
          th_at=lambda u: 2.2 if u < 2 else 1.4 if u < 10 else 1.4 + (u - 10) / 20 * 1.8)
    c.wrap(2.4, 8.6, 8.2, 1.4, hx("#f5e3cb"), 3 if c.s >= 1.5 else 2)                     # tape on the handle
    c.grain(14, 29, 8.2, 2.6, hx("#cf996a"), 5)

def hammer(c):
    c.rod(7.0, 21.5, 8.6, 2.0, ramp("#6e574f", "#917561", "#a08670", "#b39a85"))
    c.rod(7.0, 14.5, 8.6, 2.5, ramp("#04081b", "#212231", "#2a3144", "#4c5261"))
    c.wrap(7.3, 14.3, 8.6, 2.5, hx("#04081b"), 3 if c.s >= 1.5 else 2)
    STEELH = ramp("#3d343e", "#6e6f78", "#a59c98", "#d0cbc8")
    c.poly([(20.6, 5.0), (23.0, 5.0), (23.0, 9.4), (22.6, 10.0), (23.4, 10.4), (23.4, 13.0), (21.4, 13.0), (21.4, 10.4), (22.0, 10.0), (20.6, 9.6)], STEELH)   # head, neck, face
    c.poly([(20.6, 5.4), (20.2, 4.0), (19.2, 2.8), (17.8, 2.0), (18.2, 1.2), (19.8, 1.6), (21.2, 2.8), (22.0, 4.2), (22.4, 5.4)], STEELH)          # the claw, curving back

def sledgehammer(c):
    c.rod(1.0, 25.0, 8.4, 2.0, ramp("#554442", "#745b52", "#8c7669", "#aea8a4"))
    c.rod(1.0, 9.5, 8.4, 2.4, ramp("#07060e", "#25212c", "#38323a", "#5b5a5f"))
    c.wrap(1.2, 9.3, 8.4, 2.4, hx("#07060e"), 3 if c.s >= 1.5 else 2)
    c.rod(0.5, 1.4, 8.4, 2.8, ramp("#25212c", "#38323a", "#5b5a5f", "#aea8a4"))           # end cap
    HEAD = ramp("#25212c", "#38323a", "#5b5a5f", "#aea8a4")
    c.poly([(24.8, 3.4), (30.2, 3.4), (30.2, 13.4), (24.8, 13.4)], HEAD)                  # the head
    for y0 in (2.6, 12.4): c.poly([(24.4, y0), (30.6, y0), (30.6, y0 + 1.6), (24.4, y0 + 1.6)], HEAD)   # its two faces, a rim wider
    c.poly([(24.6, 7.2), (30.4, 7.2), (30.4, 9.6), (24.6, 9.6)], HEAD)                    # the collar round the handle
    c.put(c.P(27.5), c.P(8.4), hx("#07060e"))                                             # the bolt

def barbed_bat(c):
    c.rod(1.0, 30.0, 8.2, 2.0, ramp("#59392d", "#7d472a", "#a56839", "#cbb08f"),
          th_at=lambda u: 2.2 if u < 2 else 1.4 if u < 10 else 1.4 + (u - 10) / 20 * 1.8)
    c.wrap(2.4, 8.6, 8.2, 1.4, hx("#cbb08f"), 3 if c.s >= 1.5 else 2)
    c.grain(12, 29, 8.2, 2.8, hx("#59392d"), 4)
    WIRE, SHINE = hx("#2a2a2e"), hx("#9c9ca2")
    th = lambda u: 1.4 + (u - 10) / 20 * 1.8                                               # the barrel's thickness (as above)
    step = 2.0 if c.s >= 1.5 else 3.0
    u = 13.5
    while u < 28.5:                                                                        # strands wrapped across the barrel...
        top, bot = 8.2 - th(u) / 2, 8.2 + th(u) / 2
        c.line([(u, top), (u + 0.9, bot)], WIRE)
        c.put(c.P(u) - 1, c.P(top) - 1, SHINE)                                             # ...barbs poking out above and below
        c.put(c.P(u + 0.9) + 1, c.P(bot) + 1, WIRE)
        u += step

def ritual_blade(c):
    """No reference (Yan): the old sprite's design redrawn - a wooden grip in a red binding, a dark blade with a fin and a red eye
    at the guard, the spine chipped into steps."""
    c.rod(2.0, 9.0, 8.4, 2.4, ramp("#4c2812", "#743f1c", "#a0602f", "#b97a45")); c.grain(2.6, 8.6, 8.4, 2.4, hx("#4c2812"), 3)
    c.rod(8.4, 9.6, 8.4, 2.8, ramp("#7a1a17", "#a02620", "#c0302a", "#e06a50"))           # red binding
    c.put(c.P(8.6), c.P(7.4), hx("#f0d878"))                                               # its brass tie
    DARK = ramp("#22262a", "#33373b", "#4c5258", "#727980")
    c.poly([(9.6, 7.0), (10.4, 3.6), (11.2, 4.0), (13.2, 7.0), (13.2, 10.2), (9.6, 10.2)], DARK)   # the fin over the guard
    c.put(c.P(11.0), c.P(6.2), hx("#c0302a"))                                              # the red eye
    def d(u):                                                                              # stepped spine, a thin tip
        return 2.6 if u < 16 else 2.2 if u < 22 else 1.8 if u < 27 else 1.8 * (30.5 - u) / 3.5
    c.blade(13.2, 30.5, 7.4, d, ramp("#33373b", "#4c5258", "#727980", "#a9b0b5"))
    for u in (16.0, 22.0, 27.0): c.put(c.P(u), c.P(7.4) - 1, DARK[1])                     # chips on the spine

DRAW = {"HuntingKnife": hunting_knife, "SpetsnazSwitchblade": switchblade, "SteppeMachete": machete, "SerratedTrenchKnife": trench_knife,
        "Hatchet": hatchet, "FireAxe": fire_axe, "WoodenBaseballBat": baseball_bat, "Hammer": hammer, "Sledgehammer": sledgehammer,
        "BarbedWireBat": barbed_bat, "ObeliskRitualBlade": ritual_blade}

def make(key):
    hand = Canvas(1); DRAW[key](hand); hand = hand.finish(32, 16)
    w, h = hand.getbbox()[2] - hand.getbbox()[0], hand.getbbox()[3] - hand.getbbox()[1]
    s = min(60 / w, 30 / h, 3.6)                                                           # the menu fills its 64x32 canvas
    menu = Canvas(s); DRAW[key](menu)
    return hand, menu.finish(64, 32)

if __name__ == "__main__":
    install = "--install" in sys.argv
    dest = WEAP if install else os.path.join(ROOT, "ArtSource/_out/melee"); os.makedirs(dest, exist_ok=True)
    refs = os.path.join(ROOT, "ArtSource/refs/melee")
    REF = {"HuntingKnife": "hunting_knife", "SpetsnazSwitchblade": "switchblade", "SteppeMachete": "machete", "SerratedTrenchKnife": "serrated_trench_knife",
           "Hatchet": "hatchet", "FireAxe": "fire_axe", "WoodenBaseballBat": "baseball_bat", "Hammer": "hammer", "Sledgehammer": "sledge_hammer", "BarbedWireBat": "barbed_wire_bat", "ObeliskRitualBlade": None}
    f = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 16); bg = (46, 42, 34, 255)
    rows = []
    for k in DRAW:
        hand, menu = make(k)
        hand.save(os.path.join(dest, f"weapon_{k}.png")); menu.save(os.path.join(dest, f"weapon_{k}_hd.png"))
        rows.append((k, hand, menu))
    rh = 16 * 10 + 40
    sheet = Image.new("RGBA", (330 + 340 + 340 + 30, 40 + rh * len(rows)), (27, 29, 31, 255)); d = ImageDraw.Draw(sheet)
    for x, t in ((10, "reference"), (350, "in the hand (32x16) x10"), (690, "menu (64x32) x5")): d.text((x, 10), t, font=f, fill=(235, 235, 235))
    for r, (k, hand, menu) in enumerate(rows):
        y = 40 + r * rh
        d.text((10, y), k, font=f, fill=(200, 200, 190))
        ref = Image.open(os.path.join(refs, REF[k] + ".png") if REF[k] else os.path.join(WEAP, "weapon_ObeliskRitualBlade_hd.png")).convert("RGBA")  # no reference: the old sprite; ref.thumbnail((320, 140)); sheet.alpha_composite(ref, (10, y + 22))
        for im, s, x in ((hand, 10, 350), (menu, 5, 690)):
            t = Image.new("RGBA", im.size, bg); t.alpha_composite(im)
            sheet.alpha_composite(t.resize((im.width * s, im.height * s), Image.NEAREST), (x, y + 20))
    os.makedirs(os.path.join(ROOT, "ArtSource/_out"), exist_ok=True)
    sheet.save(os.path.join(ROOT, "ArtSource/_out/melee_drawn_sheet.png")); print("->", dest, "+ ArtSource/_out/melee_drawn_sheet.png")
