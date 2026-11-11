# Builds the four-canine walk GIF from the strips the game uses (Assets/Resources/Sprites/Mutants/anim/*_walk.bytes).  python3 ArtSource/make_canines_gif.py <out.gif>
import sys
from PIL import Image, ImageDraw, ImageFont
R = "Assets/Resources/Sprites/Mutants/"
names = [("pallid_prowler", "Pallid Prowler"), ("veil_prowler", "Veil Prowler"), ("blind_maw", "Blind Maw"), ("dread_prowler", "Dread Prowler")]
strips = {}
for k, _ in names:
    w = Image.open(R + f"anim/{k}_walk.bytes").convert("RGBA"); fw = w.width // 8
    strips[k] = [w.crop((i * fw, 0, (i + 1) * fw, w.height)) for i in range(8)]
cw = max(f[0].width for f in strips.values()) + 10; ch = max(f[0].height for f in strips.values()) + 4
sc = 8; lab = 6; W = 2 * cw; H = 2 * (ch + lab)
font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 24)
frames = []
for t in range(32):
    base = Image.new("RGB", (W, H), (46, 43, 38)); d = ImageDraw.Draw(base)
    for n, (k, label) in enumerate(names):
        cx = (n % 2) * cw; cy = (n // 2) * (ch + lab); gy = cy + lab + ch - 2
        d.rectangle([cx, gy, cx + cw - 1, gy + 1], fill=(60, 56, 48)); off = int(t * 2.3) % 10
        for gx in range(-10, cw, 10): d.point((cx + gx - off + 10, gy), fill=(82, 76, 64))
        f = strips[k][t % 8]; base.paste(f, (cx + (cw - f.width) // 2, gy - f.height), f)
    big = base.resize((W * sc, H * sc), Image.NEAREST); d = ImageDraw.Draw(big)
    for n, (k, label) in enumerate(names): d.text(((n % 2) * cw * sc + 16, (n // 2) * (ch + lab) * sc + 8), label, fill=(214, 204, 180), font=font)
    frames.append(big.quantize(colors=96, method=Image.MEDIANCUT, dither=Image.NONE))
frames[0].save(sys.argv[1], save_all=True, append_images=frames[1:], duration=125, loop=0)
