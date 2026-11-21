# Pixel Lab interpolation-v2 (2026-11-21): python3 pixellab_interp.py <start.png> <end.png> <outdir> <seed> "<action>"   (keyframes > 64 px give only 2 in-betweens; the keyframes are not returned)
import sys, io, json, base64, time, os, urllib.request
from PIL import Image
a, b, out, seed, act = sys.argv[1], sys.argv[2], sys.argv[3], int(sys.argv[4]), sys.argv[5]
def enc(p):
    im = Image.open(p).convert("RGBA"); buf = io.BytesIO(); im.save(buf, "PNG"); return {"image": {"type": "base64", "base64": base64.b64encode(buf.getvalue()).decode()}, "size": {"width": im.width, "height": im.height}}, im.size
ea, sa = enc(a); eb, sb = enc(b); W = max(sa[0], sb[0]); Hh = max(sa[1], sb[1])
body = {"start_image": ea, "end_image": eb, "action": act, "image_size": {"width": min(128, W), "height": min(128, Hh)}, "seed": seed, "no_background": True}
def call(url, data=None):
    req = urllib.request.Request("https://api.pixellab.ai" + url, data=json.dumps(data).encode() if data else None, headers={"Content-Type": "application/json"})
    try: return json.load(urllib.request.urlopen(req, timeout=120))
    except urllib.error.HTTPError as e: print(e.code, e.read()[:600]); raise
r = call("/v2/interpolation-v2", body); jid = r.get("background_job_id"); print({k: v for k, v in r.items() if k != "images"})
for _ in range(120):
    time.sleep(6); j = call("/v2/background-jobs/" + jid)
    if j.get("status") in ("completed", "failed"): break
r = j.get("last_response") or j; os.makedirs(out, exist_ok=True)
imgs = r.get("images") or r.get("frames") or []; print("frames", len(imgs), list(r)[:6])
for i, d in enumerate(imgs):
    d = d["base64"] if isinstance(d, dict) else d; Image.open(io.BytesIO(base64.b64decode(d.split(",")[-1]))).convert("RGBA").save(os.path.join(out, "i_%02d.png" % i))
