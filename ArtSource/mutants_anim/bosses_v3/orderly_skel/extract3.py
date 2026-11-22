# resp_<clip>_s<seed>.json -> cand/<clip>_s<seed>/f_NN.png on the Orderly's 276x178 canvas (the 236x172 frame at 0,0) + a labelled sheet for frame-by-frame review
import json, base64, io, sys, os
from PIL import Image, ImageDraw
for tag in sys.argv[1:]:
    r = json.load(open(f"resp_{tag}.json")); ims = [Image.open(io.BytesIO(base64.b64decode(i["base64"]))).convert("RGBA") for i in r["images"]]
    d = f"cand/{tag}"; os.makedirs(d, exist_ok=True); out = []
    for k, im in enumerate(ims):
        cv = Image.new("RGBA", (276, 178), (0, 0, 0, 0)); cv.paste(im, (0, 0), im); cv.save(f"{d}/f_{k:02d}.png"); out.append(cv)
    s = Image.new("RGB", (276 * 4 * 2 // 2 * 1, 178 * 2 * 1), (40, 40, 46))
    s = Image.new("RGB", (276 * 4, 178 * 2), (40, 40, 46))
    for k, f in enumerate(out): s.paste(f, ((k % 4) * 276, (k // 4) * 178), f); ImageDraw.Draw(s).text(((k % 4) * 276 + 4, (k // 4) * 178 + 4), f"{tag} f{k}", fill=(255, 255, 0))
    s = s.resize((s.width * 2 // 1, s.height * 2 // 1), Image.NEAREST) if False else s
    s.save(f"cand/{tag}_sheet.png"); print(tag, len(ims), ims[0].size)
