"""Healing animation (Yan, 2026-10-29: "healing doesn't have an animation right now"): the items you hold while using them,
in the floating support glove. Weapon-B scale - half-size pixels (PPU 32) on a 16x16 canvas, the item centred (the glove holds
it at the middle). Locked style: 1 px #141617 outline, flat 2-3 tones. -> Assets/Resources/Sprites/Items/use_<kind>.png
    python3 ArtSource/make_use_items.py"""
import os
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "..", "Assets", "Resources", "Sprites", "Items")
def hexc(h, a=255): h = h.lstrip('#'); return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)
PAL = {"K": hexc("#141617"), "W": hexc("#d8d4c4"), "w": hexc("#a8a294"), "R": hexc("#b02a2a"), "r": hexc("#7a1c1c"),
       "b": hexc("#e4dcc8"), "B": hexc("#b8ae96"), "p": hexc("#4c5258"), "q": hexc("#727980"), "g": hexc("#8fd06a"),
       "G": hexc("#5a9a40"), "s": hexc("#d5dadd"), "o": hexc("#7a8256"), "O": hexc("#555c3a"), "y": hexc("#c9a63e"), "c": hexc("#c9b28a"), "C": hexc("#9a8462")}
ITEMS = {
    "medkit": [                 # an off-white case with a red cross and a carry handle
        "....KKKK....",
        "....K..K....",
        ".KKKKKKKKKK.",
        "KWWWWWWWWWwK",
        "KWWWWRrWWWwK",
        "KWWWRRRrWWwK",
        "KWWWWRrWWWwK",
        "KwwwwwwwwwwK",
        ".KKKKKKKKKK.",
    ],
    "bandage": [                # a gauze roll, its loose end trailing
        ".KKKKK....",
        "KbbbbbK...",
        "KbBBbbbKKK",
        "KbBbbbbbbbK",
        "KbbbbbbKKK.",
        ".KbbbbK....",
        "..KKKK.....",
    ],
    "syringe": [                # plunger, a barrel of green stim, the needle
        "KK.KKKKKKKK....",
        "KpKqggggggGKKss",
        "KK.KKKKKKKK....",
    ],
    "ration": [                 # a tan ration can with an olive band - pills, food, drink go to the mouth
        ".KKKKK.",
        "KWWWWwK",
        "KooooOK",
        "KccccCK",
        "KccccCK",
        "KccccCK",
        ".KKKKK.",
    ],
}
def render(rows):
    w = max(len(r) for r in rows); im = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    ox, oy = (16 - w) // 2, (16 - len(rows)) // 2
    for y, row in enumerate(rows):
        for x, ch in enumerate(row):
            if ch != ".": im.putpixel((ox + x, oy + y), PAL[ch])
    return im
if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    sheet = Image.new("RGBA", (4 * 16 * 8 + 50, 16 * 8 + 20), hexc("#1b1d1f"))
    for i, (k, rows) in enumerate(ITEMS.items()):
        im = render(rows); im.save(os.path.join(OUT, f"use_{k}.png"))
        sheet.alpha_composite(im.resize((128, 128), Image.NEAREST), (10 + i * 138, 10))
    sheet.save(os.path.join(HERE, "_out", "use_items_preview.png")); print("wrote", ", ".join(ITEMS))
