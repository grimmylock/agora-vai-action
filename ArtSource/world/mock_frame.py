# In-game mock: a 1920x1080 frame of the dressed Perimeter at the game's zoom (14 tiles tall),
# from the harness render, with a squad, held guns, and the soft reach outline.
import sys
sys.path.insert(0, "/home/claude/push/ArtSource")
from PIL import Image
import make_armor_avatars as av

K = (20, 22, 23, 255)
def body(name):
    r = [a for a in av.ARMORS if a[0] == name][0]
    return av.build(r[0], r[3], r[4]).convert("RGBA")

def glove(c):
    g = Image.new("RGBA", (4, 3)); d = {"K": K, "c": c, "g": tuple(int(v * .8) for v in c[:3]) + (255,)}
    for y, row in enumerate([".KK.", "KcgK", ".KK."]):
        for x, ch in enumerate(row):
            if ch in d: g.putpixel((x, y), d[ch])
    return g.resize((8, 6), Image.NEAREST)

def unit(canvas, armor, wkey, grip, at, flip=False):
    """canvas is at 2x world px; at = (x, y) foot point in world px"""
    b = body(armor).resize((32, 32), Image.NEAREST)
    if flip: b = b.transpose(Image.FLIP_LEFT_RIGHT)
    bx, by = at[0] * 2 - 16, at[1] * 2 - 32
    sh = Image.new("RGBA", (22, 6), (0, 0, 0, 0))
    for y in range(6):
        for x in range(22):
            if ((x - 10.5) / 11) ** 2 + ((y - 2.5) / 3) ** 2 <= 1: sh.putpixel((x, y), (50, 33, 37, 140))
    canvas.alpha_composite(sh, (bx + 5, by + 28))
    canvas.alpha_composite(b, (bx, by))
    w = Image.open(f"/tmp/claude-0/weapons_hand_out/weapon_{wkey}.png").convert("RGBA")
    gx, gy, sx, sy = grip[:4]
    hand = (bx + 22, by + 20) if not flip else (bx + 10, by + 20)
    if flip: w = w.transpose(Image.FLIP_LEFT_RIGHT); gx = w.width - gx; sx = w.width - sx
    canvas.alpha_composite(w, (int(hand[0] - gx), int(hand[1] - gy)))
    gc = b.getpixel((14, 18))
    canvas.alpha_composite(glove(gc), (int(hand[0] - 4), int(hand[1] - 3)))
    canvas.alpha_composite(glove(gc), (int(hand[0] - gx + sx - 4), int(hand[1] - gy + sy - 3)))

def reach(canvas, cells, color=(255, 236, 160)):
    s = set(cells)
    for (cx, cy) in cells:
        for y in range(32):
            for x in range(32):
                border = (y < 2 and (cx, cy - 1) not in s) or (y > 29 and (cx, cy + 1) not in s) or (x < 2 and (cx - 1, cy) not in s) or (x > 29 and (cx + 1, cy) not in s)
                X, Y = cx * 32 + x, cy * 32 + y
                if 0 <= X < canvas.width and 0 <= Y < canvas.height:
                    o = canvas.getpixel((X, Y)); a = 150 / 255 if border else 22 / 255
                    canvas.putpixel((X, Y), tuple(int(o[i] * (1 - a) + color[i] * a) for i in range(3)) + (255,))

def main():
    world = Image.open(sys.argv[1] if len(sys.argv) > 1 else "/tmp/claude-0/perimeter_3q.png").convert("RGBA")
    x0, y0 = int(sys.argv[2]) if len(sys.argv) > 2 else 560, int(sys.argv[3]) if len(sys.argv) > 3 else 1180
    W, H = 384, 216                                    # 24 x 13.5 tiles, the game's zoom at 16:9
    view = world.crop((x0, y0, x0 + W, y0 + H)).resize((W * 2, H * 2), Image.NEAREST)
    # reach area around the player (cells relative to the view)
    pcx, pcy = 11, 8
    cells = [(pcx + dx, pcy + dy) for dx in range(-5, 6) for dy in range(-5, 6) if max(abs(dx), abs(dy)) + min(abs(dx), abs(dy)) // 2 <= 5]
    off = ((x0 % 16), (y0 % 16))
    ov = Image.new("RGBA", (W * 2 + 64, H * 2 + 64)); ov.paste(view, (off[0] * 2, off[1] * 2))
    reach(ov, cells)
    view = ov.crop((off[0] * 2, off[1] * 2, off[0] * 2 + W * 2, off[1] * 2 + H * 2))
    vk = [13.5, 7.5, 27.5, 5]
    unit(view, "Rookie Field Suit", "VK74", vk, (pcx * 16 + 8 - off[0], pcy * 16 + 15 - off[1]))
    unit(view, "Raider Jacket", "VK74", vk, (pcx * 16 + 8 - off[0] + 150, pcy * 16 + 15 - off[1] - 40), flip=True)
    unit(view, "Raider Stitched Coat", "VK74", vk, (pcx * 16 + 8 - off[0] + 175, pcy * 16 + 15 - off[1] + 22), flip=True)
    view = view.resize((1920, 1080), Image.NEAREST)
    view.save("/tmp/claude-0/mock_frame.png"); print("ok")

if __name__ == "__main__": main()
