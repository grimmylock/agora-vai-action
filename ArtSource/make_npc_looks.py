"""The story's named people (Yan, 2026-11-18: "creating unique identifiable sprites for each unique npc ... the same style as the existing sprites ... they can differ by having unique helmets, maybe
no helmet and a hairstyle"). Built with the faction armour avatar parts (make_armor_avatars.py: 16x16, armless, outlined, lit left / shaded right) and the walk-frame trick of make_unique_armor.py,
each with its own head (a cap, a beret, a crown of scrap, a hood with goggles, a mohawk, long hair, glasses, a beard), colours and a few marks. Writes, per person,
  Assets/Resources/Sprites/armor_unique_<key>.png (96x16: front, walk a, walk b, back, back walk a, back walk b) and the doubled `_x2` copy (2x2 nearest, what the game loads),
and a preview sheet ArtSource/_out/npc_looks_preview.png (front and back, 6x, a Wanderer beside for scale).  UnitSprites.UniqueSuits lists the names; StoryNpcs picks them.
The minor people keep the game's own armour, a fixed suit each (StoryNpcs.Fixed).   python3 ArtSource/make_npc_looks.py"""
import os, sys
from PIL import Image, ImageDraw
HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE)
import make_armor_avatars as av
import make_walk_atlas as wa
SPR = os.path.join(HERE, "..", "Assets", "Resources", "Sprites"); OUT = os.path.join(HERE, "_out"); os.makedirs(OUT, exist_ok=True)

# heads: 8 columns, mirrored; rows 0..6 (the body starts at row 7). K outline, H/h headgear, A hair, F/f skin, e eye, W/w glasses, V goggles, N cloth, J metal.
HEADS = {
  "beard":     (["......KK", "....KHHH", "...KHhhh", "...KHHHH", "....KFeF", "....KAAA", ".....KAA"], ["......KK", "....KHHH", "...KHhhh", "...KHHHH", "....KAAA", "....KAAA", ".....KAA"]),   # flat cap, a full beard
  "glasses":   (["......KK", ".....KAA", "....KAAA", "....KAFF", "....KWwW", "....KFFf", ".....KFF"], ["......KK", ".....KAA", "....KAAA", "....KAAA", "....KAAA", "....KAAA", ".....Kff"]),   # round glasses, messy hair
  "longhair":  (["......KK", ".....KAA", "....KAAA", "....KAFF", "....KAeF", "....KAFf", ".....KAF"], ["......KK", ".....KAA", "....KAAA", "....KAAA", "....KAAA", "....KAAA", ".....KAA"]),   # hair falling past the face
  "scrapcrown":(["..K.K.KK", "....KJJH", "...KJHhh", "...KHFFF", "....KFeF", "....KFFf", ".....KFF"], ["..K.K.KK", "....KJJH", "...KJHhh", "...KHHHH", "....KAAA", "....KAAA", ".....KAA"]),   # a crown of scrap spikes
  "beret":     (["......KK", ".....KHH", "....KHHH", "...KHHHH", "....KFeF", "....KFFf", ".....KFF"], ["......KK", ".....KHH", "....KHHH", "...KHHHH", "....KAAA", "....KAAA", ".....KAA"]),   # a beret pulled low
  "sidepart":  (["......KK", ".....KAA", "....KAAA", "....KAFF", "....KFeF", "....KFFf", ".....KFF"], ["......KK", ".....KAA", "....KAAA", "....KAAA", "....KAAA", "....KAAA", ".....Kff"]),   # plain, neat hair
  "peakcap":   (["......KK", "....KHHH", "...KHhhh", "..KHHHHH", "....KFeF", "....KFFf", ".....KFF"], ["......KK", "....KHHH", "...KHhhh", "..KHHHHH", "....KAAA", "....KAAA", ".....KAA"]),   # a peaked cap, a wide brim
  "hoodgoggles":(["......KK", ".....KHh", "....KHhh", "....KVwV", "....KFeF", "....KNNN", ".....KNN"], ["......KK", ".....KHh", "....KHhh", "....KHHH", "....KHHH", "....KNNN", ".....KNN"]),   # a hood, goggles on the brow, a scarf over the mouth
  "mohawk":    (["......KA", ".....KFA", "....KFFA", "....KFFF", "....KFeF", "....KFFf", ".....KFF"], ["......KA", ".....KFA", "....KFFA", "....KFFA", "....KFFA", "....KFFf", ".....Kff"]),   # shaved sides, a crest
  "ragmask":   (["......KK", "....KHHH", "...KHhhh", "...KHJHH", "....KFeF", "....KNNN", ".....KNN"], ["......KK", "....KHHH", "...KHhhh", "...KHJHH", "...KHHHH", "....KNNN", ".....KNN"]),   # a grey rag wound round the head with a strand of barbed wire, a red scarf over the mouth (Spool)
  "shaggy":    (["......KK", ".....KAA", "....KAAA", "....KAFF", "....KFeF", "....KFAA", ".....KAA"], ["......KK", ".....KAA", "....KAAA", "....KAAA", "....KAAA", "....KAAA", ".....KAA"]),   # unkempt hair and stubble
}
for k, (f, b) in HEADS.items(): av.HEADS[k] = f; av.HEADS_BACK[k] = b
DARK = "#1c1812"   # eye colour (build() makes 'e' a little lighter)

# name: (look, marks) - marks is a list of (x, y, colour) pixels painted on the finished front (and mirrored ones on the back where noted)
LOOKS = {
  "Yefim":         dict(head="beard", body="coat", H="#6a7058", S="#6b5a3e", D="#3c342a", A="#b9b9b2", E=DARK, arm="#5a6a8a"),                                   # old trader: grey beard, wool cap, brown wool coat
  "Pavel":         dict(head="glasses", body="jacket", S="#8794a0", D="#4a5260", A="#3a2c22", E=DARK, F="#dcc0aa", f="#b89a86", arm="#3a7a9a"),                # thin engineer, pale, glasses, messy hair, a Surveyor-grey jacket
  "Lev Marchuk":   dict(head="shaggy", body="heavy", S="#4a4036", P="#6e5a3a", A="#7a7066", E=DARK, F="#b88a68", f="#8e664a"),                                # the big man in a patched coat, grey and unkempt
  "Dr. Priya Osei":dict(head="longhair", body="coat", S="#e0e2e0", D="#454d5a", A="#2a2020", E=DARK, F="#b88060", f="#8e5e44", arm="#3a7a9a"),                 # a white lab coat, long dark hair
  "The Scrap King":dict(head="scrapcrown", body="heavy", H="#9a6a3a", S="#2a2420", P="#b8642a", A="#3a2a22", E=DARK, arm="#d4a62a"),                          # a crown of scrap, rust plates on black leather
  "Voss":          dict(head="beret", body="vest", H="#3a3c34", S="#6a5c42", P="#3e3a2c", D="#2d2f2e", A="#8a8a84", E=DARK, arm="#c8c8c0"),                    # the Contractor boss: a low beret, grey hair, a tan carrier
  "Emil Rask":     dict(head="sidepart", body="jacket", S="#aab8c4", D="#3e4650", A="#6a4a2e", E=DARK, arm="#3a7a9a"),                                         # neat hair, a clean pale lab jacket
  "Konrad Hale":   dict(head="peakcap", body="jacket", H="#2a3450", S="#586038", D="#3a3a2a", A="#4a3a2a", E=DARK, arm="#3a7a9a"),                              # a peaked cap, an olive supply jacket
  "The Runner":    dict(head="hoodgoggles", body="jacket", H="#6b6a3a", S="#4a3a2a", D="#2e2a26", N="#a83a2a", E=DARK),                                       # hood, goggles, a red scarf
  "Barbwire Coat": dict(head="ragmask", body="heavy", H="#8a8478", S="#46372c", P="#7a5a38", D="#2a2018", N="#a83a2a", J="#b8bcc0", E=DARK, F="#b8845e", f="#8e6244", arm="#b8bcc0"),   # Spool's: a patched leather coat, wire round the shoulders
  "Bogdan":        dict(head="mohawk", body="vest", S="#1c1c1e", P="#8a3a2a", D="#2a2a2a", A="#a02020", E=DARK, F="#c0866a", arm="#b02a2a"),                    # a Raider warlord: a red crest, black leather, rust plates
}
MARKS = {   # extra pixels (front only): scars, a bandage, a stubble line
  "Barbwire Coat": [(3, 8, "#b8bcc0"), (5, 9, "#b8bcc0"), (7, 8, "#b8bcc0"), (9, 9, "#b8bcc0"), (11, 8, "#b8bcc0"), (4, 11, "#8e949a"), (6, 12, "#b8bcc0"), (10, 11, "#8e949a")],   # barbed wire wound round the shoulders and chest  "Pavel":       [(5, 12, "#d8d4c8"), (5, 13, "#d8d4c8"), (5, 13, "#b0504a")],                                   # a bandage on the thigh (a soaked one)
  "Voss":        [(9, 5, "#a8685a"), (9, 6, "#a8685a")],                                                         # a scar down the cheek
  "Bogdan":      [(6, 4, "#a8685a"), (6, 5, "#a8685a"), (10, 5, "#a8685a")],                                     # scars
  "Lev Marchuk": [(7, 6, "#6a5a4a"), (8, 6, "#6a5a4a")],                                                         # stubble at the chin
}
def strip(name, L):
    f = av.build(name, 0.2, 2, L); b = av.build(name, 0.2, 2, L, back=True)
    px = f.load()
    for x, y, c in MARKS.get(name, []): px[x, y] = av.hexc(c)
    cells = [f, f.copy(), f.copy(), b, b.copy(), b.copy()]
    for base, a, bb in ((0, 1, 2), (3, 4, 5)):
        top = wa.leg_top(cells[base].load(), 0, 0)
        if top is not None:
            wa.lift(cells[a], 0, 0, top, left=True); wa.lift(cells[bb], 0, 0, top, left=False)
    s = Image.new("RGBA", (96, 16), (0, 0, 0, 0))
    for i, c in enumerate(cells): s.alpha_composite(c, (i * 16, 0))
    return s
def main():
    sheet = Image.new("RGBA", (len(LOOKS) * 16 * 6 + 16 * 6 + 20, 16 * 6 * 2 + 10), (66, 66, 66, 255))
    for i, (name, L) in enumerate(LOOKS.items()):
        s = strip(name, L); k = av.key(name)
        s.save(os.path.join(SPR, f"armor_unique_{k}.png"))
        s.resize((s.width * 2, s.height * 2), Image.NEAREST).save(os.path.join(SPR, f"armor_unique_{k}_x2.png"))
        sheet.alpha_composite(s.crop((0, 0, 16, 16)).resize((96, 96), Image.NEAREST), (i * 96, 0))
        sheet.alpha_composite(s.crop((48, 0, 64, 16)).resize((96, 96), Image.NEAREST), (i * 96, 106))
    ref = Image.open(os.path.join(SPR, "armor_atlas.png")).convert("RGBA").crop((16, 0, 32, 16))
    sheet.alpha_composite(ref.resize((96, 96), Image.NEAREST), (len(LOOKS) * 96 + 20, 0))
    sheet.save(os.path.join(OUT, "npc_looks_preview.png")); print("wrote", len(LOOKS), "strips and the preview")
    print("names:", list(LOOKS))
if __name__ == "__main__": main()
