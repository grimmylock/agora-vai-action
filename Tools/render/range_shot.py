# A camera-sized frame of the real map render, with the game's own character sprites stood where units stand (feet 7 px under
# the tile middle), to show what 12 tiles looks like. Default zoom = 7 tiles half-height (TacticalGridView), max = 14.
import sys
from PIL import Image, ImageDraw, ImageFont
R = "/home/user/agora-vai-action"
world = Image.open(R + "/Tools/render/_render/perimeter.png").convert("RGBA")
atlas = Image.open(R + "/Assets/Resources/Sprites/armor_atlas.png").convert("RGBA")
def body(i): return atlas.crop(((i % 16) * 16, (i // 16) * 16, (i % 16) * 16 + 16, (i // 16) * 16 + 16))
you, raider = body(30), body(0).transpose(Image.FLIP_LEFT_RIGHT)          # Leather Jacket facing right, Raider Jacket facing left
PX, PY, D = 70, 170, 12
f = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 22); fs = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", 18)
def frame(half, title, scale):
    w = world.copy()
    def stand(img, tx, ty):
        cx, feet = (tx + 0.5) * 16, (ty + 0.5 + 0.44) * 16
        w.alpha_composite(img, (int(cx - 8), int(feet - 15)))
    d = ImageDraw.Draw(w)
    for t, c in ((5, (240, 220, 120, 255)), (8, (240, 170, 90, 255)), (12, (230, 90, 70, 255))):   # ground ticks at 5, 8, 12 tiles
        x = (PX + 0.5 + t) * 16; y = (PY + 0.5 + 0.44) * 16 + 2
        d.line([(x, y - 3), (x, y + 3)], fill=c, width=1)
    d.line([((PX + 0.5) * 16, (PY + 0.94) * 16 + 2), ((PX + 0.5 + D) * 16, (PY + 0.94) * 16 + 2)], fill=(255, 255, 255, 110), width=1)
    stand(you, PX, PY); stand(raider, PX + D, PY)
    hh = half * 16; hw = hh * 16 / 9
    cx, cy = (PX + 0.5 + D / 2) * 16, (PY + 0.5) * 16                 # framed on the pair (the camera leans toward the mouse)
    crop = w.crop((int(cx - hw), int(cy - hh), int(cx + hw), int(cy + hh))).resize((int(2 * hw * scale), int(2 * hh * scale)), Image.NEAREST)
    out = Image.new("RGBA", (crop.width, crop.height + 70), (24, 26, 28, 255)); out.alpha_composite(crop, (0, 0))
    dd = ImageDraw.Draw(out)
    dd.text((12, crop.height + 8), title, font=f, fill=(235, 235, 225))
    dd.text((12, crop.height + 38), "ticks on the ground: 5 (yellow), 8 (orange), 12 tiles (red). Map render + the game's body sprites (no held guns).", font=fs, fill=(170, 170, 160))
    return out
a = frame(7, f"Default zoom (14 tiles tall, ~25 wide): you and a Raider {D} tiles apart", 4)
b = frame(14, f"Zoomed all the way out (28 tiles tall): the same {D} tiles", 2)
a.convert("RGB").save(R + "/maps/renders/range/range_12_tiles_default_zoom.png")
b.convert("RGB").save(R + "/maps/renders/range/range_12_tiles_zoomed_out.png")
print(a.size, b.size)
