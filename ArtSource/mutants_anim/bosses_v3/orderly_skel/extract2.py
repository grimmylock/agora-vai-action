# resp_<clip>.json -> raw_<clip>/f_NN.png (188x148) + padded 224x176 into ../orderly/<clip>/ (same placement as walk) + preview gif
import json, base64, io, sys, os
from PIL import Image
for c in sys.argv[1:]:
    r = json.load(open(f"resp_{c}.json")); ims = [Image.open(io.BytesIO(base64.b64decode(i["base64"]))).convert("RGBA") for i in r["images"]]
    os.makedirs(f"raw_{c}", exist_ok=True); os.makedirs(f"../orderly/{c}", exist_ok=True)
    ref = Image.open("../orderly/walk/f_00.png").convert("RGBA"); W, H = ref.size
    out = []
    for k, im in enumerate(ims):
        im.save(f"raw_{c}/f_{k:02d}.png"); cv = Image.new("RGBA", (W, H), (0, 0, 0, 0)); cv.paste(im, ((W - im.width) // 2, H - 6 - im.height), im); cv.save(f"../orderly/{c}/f_{k:02d}.png"); out.append(cv)
    bg = [Image.alpha_composite(Image.new("RGBA", (W, H), (40, 40, 46, 255)), f).convert("RGB").resize((W * 2, H * 2), Image.NEAREST) for f in out]
    bg[0].save(f"../../../../maps/renders/bosses_v3/orderly_{c}.gif", save_all=True, append_images=bg[1:], duration=110, loop=0); print(c, len(ims), ims[0].size)
