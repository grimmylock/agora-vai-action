# Pixel Lab edit-image-pixen (2026-11-21): python3 pixellab_edit.py <in.png> <out.png> <seed> "<what to change>"   (the canvas is padded with transparent pixels to a multiple of 4; never rescaled)
import sys, io, json, base64, time, urllib.request
from PIL import Image
src, out, seed, desc = sys.argv[1], sys.argv[2], int(sys.argv[3]), sys.argv[4]
im = Image.open(src).convert("RGBA"); W = (im.width + 3) // 4 * 4; Hh = (im.height + 3) // 4 * 4
cv = Image.new("RGBA", (W, Hh), (0, 0, 0, 0)); cv.alpha_composite(im, (0, Hh - im.height)); b = io.BytesIO(); cv.save(b, "PNG")
body = {"image": {"type": "base64", "base64": base64.b64encode(b.getvalue()).decode()}, "description": desc, "width": W, "height": Hh, "seed": seed, "no_background": True}
def call(url, data=None):
    req = urllib.request.Request("https://api.pixellab.ai" + url, data=json.dumps(data).encode() if data else None, headers={"Content-Type": "application/json"})
    try: return json.load(urllib.request.urlopen(req, timeout=120))
    except urllib.error.HTTPError as e: print(e.code, e.read()[:500]); raise
r = call("/v2/edit-image-pixen", body); jid = r.get("background_job_id"); print({k: v for k, v in r.items() if k != "image"})
for _ in range(90):
    time.sleep(5); j = call("/v2/background-jobs/" + jid)
    if j.get("status") in ("completed", "failed"): break
r = j.get("last_response") or j
img = r.get("image") or (r.get("images") or [None])[0]; d = img["base64"] if isinstance(img, dict) else img
Image.open(io.BytesIO(base64.b64decode(d.split(",")[-1]))).convert("RGBA").save(out); print("saved", out)
