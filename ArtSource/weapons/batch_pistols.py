import sys; sys.path.insert(0, '/home/claude/art/weapons')
from weapon_pixel import *
from PIL import ImageDraw, ImageFont
P = {
 "PSh18Pchela":           dict(A=28),
 "WaldenP9":              dict(A=28, olive_s=0.08),
 "Halden45Service":       dict(A=30, flip=True),
 "HaldenServiceRevolver": dict(A=32, flip=True, force=[(9, 40, 0, 6, "metal")]),
 "SR21Gadyuka":           dict(A=28, flip=True),
 "BlackHarrier":          dict(A=34),
 "Corvan57":              dict(A=30),
 "RinaldiR6":             dict(A=32, flip=True, rot=14),
 "SteppeEagle":           dict(A=32, flip=True, rot=27),
}
def sheet(P, path, title):
    font = ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf', 14)
    rows = []
    for n, cfg in P.items():
        A, B = make(n, cfg); rows.append((n, render(A), render(B)))
    S = 6; cw = 64 * S + 32 * S + 60
    out = Image.new('RGBA', (2 * cw + 20, ((len(rows) + 1) // 2) * (32 * S + 40) + 40), (27, 29, 31, 255)); d = ImageDraw.Draw(out)
    d.text((10, 8), title, font=font, fill=(220, 220, 220))
    for i, (n, a, b) in enumerate(rows):
        x0, y0 = 10 + (i % 2) * cw, 34 + (i // 2) * (32 * S + 40)
        d.text((x0, y0), n, font=font, fill=(184, 188, 192))
        bg = Image.new('RGBA', (64, 32), (42, 38, 32, 255)); bg.alpha_composite(a); out.alpha_composite(bg.resize((64 * S, 32 * S), Image.NEAREST), (x0, y0 + 20))
        bg = Image.new('RGBA', (32, 16), (42, 38, 32, 255)); bg.alpha_composite(b); out.alpha_composite(bg.resize((32 * S, 16 * S), Image.NEAREST), (x0 + 64 * S + 20, y0 + 20))
    out.save(path)
if __name__ == '__main__':
    sheet(P, '/tmp/claude-0/pistols_px.png', 'pistols v1: A 64x32 (left), B 32x16 (right), both x6')
