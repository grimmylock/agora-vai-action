# Pixel Lab inpaint (2026-11-21): python3 pixellab_inpaint.py <in.png> <mask.png (white = repaint)> <out.png> <seed> "<description>"
import sys, io, json, base64, time, urllib.request
from PIL import Image
src, mask, out, seed, desc = sys.argv[1], sys.argv[2], sys.argv[3], int(sys.argv[4]), sys.argv[5]
def b64(p, mode='RGBA'):
    b = io.BytesIO(); Image.open(p).convert(mode).save(b, 'PNG'); return {"type": "base64", "base64": base64.b64encode(b.getvalue()).decode()}
im = Image.open(src)
body = {"description": desc, "image_size": {"width": im.width, "height": im.height}, "inpainting_image": b64(src), "mask_image": b64(mask, 'RGB'), "no_background": True, "seed": seed, "text_guidance_scale": 8, "detail": "highly detailed", "shading": "detailed shading", "outline": "single color black outline"}
def call(url, data=None):
    req = urllib.request.Request('https://api.pixellab.ai' + url, data=json.dumps(data).encode() if data else None, headers=__import__('pixellab_auth').HEADERS)
    try: return json.load(urllib.request.urlopen(req, timeout=120))
    except urllib.error.HTTPError as e: print(e.code, e.read()[:600]); raise
r = call('/v2/inpaint', body); print({k: v for k, v in r.items() if k != 'image'} if isinstance(r, dict) else r)
jid = r.get('background_job_id')
if jid:
    for _ in range(60):
        time.sleep(4); j = call('/v2/background-jobs/' + jid)
        if j.get('status') in ('completed', 'failed'): break
    r = j.get('last_response') or j
img = r.get('image') or (r.get('images') or [None])[0]
d = img['base64'] if isinstance(img, dict) else img
Image.open(io.BytesIO(base64.b64decode(d.split(',')[-1]))).convert('RGBA').save(out); print('saved', out)
