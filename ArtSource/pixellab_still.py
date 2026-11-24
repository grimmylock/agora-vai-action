# Pixel Lab bitforge still (2026-11-20): python3 pixellab_still.py <out.png> <w> <h> <seed> "<description>" [style.png] [init.png init_strength]
import sys, io, json, base64, time, urllib.request
from PIL import Image
out, w, h, seed, desc = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), int(sys.argv[4]), sys.argv[5]
def b64(p, size=None):
    im = Image.open(p).convert('RGBA')
    if size: im = im.resize(size, Image.NEAREST)
    b = io.BytesIO(); im.save(b, 'PNG'); return {"type": "base64", "base64": base64.b64encode(b.getvalue()).decode()}
body = {"description": desc, "image_size": {"width": w, "height": h}, "no_background": True, "seed": seed, "text_guidance_scale": 8, "view": __import__("os").environ.get("VIEW", "side"), "detail": "highly detailed", "shading": "detailed shading", "outline": "single color black outline"}
if len(sys.argv) > 6: body["style_image"] = b64(sys.argv[6], (w, h)); body["style_strength"] = 60
if len(sys.argv) > 7: body["init_image"] = b64(sys.argv[7], (w, h)); body["init_image_strength"] = int(sys.argv[8])
def call(url, data=None):
    req = urllib.request.Request('https://api.pixellab.ai' + url, data=json.dumps(data).encode() if data else None, headers=__import__('pixellab_auth').HEADERS)
    try: return json.load(urllib.request.urlopen(req, timeout=120))
    except urllib.error.HTTPError as e: print(e.code, e.read()[:400]); raise
r = call('/v2/create-image-bitforge', body); print({k: v for k, v in r.items() if k != 'image'} if isinstance(r, dict) else r)
jid = r.get('background_job_id')
if jid:
    for _ in range(60):
        time.sleep(4); j = call('/v2/background-jobs/' + jid)
        if j.get('status') in ('completed', 'failed'): break
    r = j.get('last_response') or j
img = r.get('image') or (r.get('images') or [None])[0]
d = img['base64'] if isinstance(img, dict) else img
Image.open(io.BytesIO(base64.b64decode(d.split(',')[-1]))).convert('RGBA').save(out); print('saved', out)
