# Pixel Lab animate-pixminimax helper (2026-11-20). python3 ArtSource/pixellab_anim.py <sprite.png> <canvasW> <canvasH> <outdir> <frames> <seed> <loop 0/1> "<motion description>"
# The sprite is centred on the canvas bottom (like the game cell), sent as first_frame (and last_frame when loop=1: the trick that makes a cycle). Token comes from the environment's network secret (proxy). Saves frames f_00.png.. + job.json.
import sys, os, io, json, base64, time, urllib.request
from PIL import Image
sprite, cw, ch, out, n, seed, loop, desc = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4], int(sys.argv[5]), int(sys.argv[6]), sys.argv[7] == '1', sys.argv[8]
os.makedirs(out, exist_ok=True)
im = Image.open(sprite).convert('RGBA'); cv = Image.new('RGBA', (cw, ch), (0, 0, 0, 0)); cv.alpha_composite(im, ((cw - im.width) // 2, ch - im.height - 6)); cv.save(os.path.join(out, 'base_canvas.png'))
b = io.BytesIO(); cv.save(b, 'PNG'); img = {"type": "base64", "base64": base64.b64encode(b.getvalue()).decode()}
body = {"first_frame": img, "description": desc, "frame_count": n, "seed": seed, "no_background": True, "enhance_prompt": True}
if loop: body["last_frame"] = img
def call(url, data=None):
    req = urllib.request.Request('https://api.pixellab.ai' + url, data=json.dumps(data).encode() if data else None, headers=__import__('pixellab_auth').HEADERS)
    return json.load(urllib.request.urlopen(req, timeout=120))
r = call('/v2/animate-pixminimax', body); jid = r['background_job_id']; print('job', jid, r.get('enhanced_prompt'))
for _ in range(90):
    time.sleep(5); j = call('/v2/background-jobs/' + jid)
    if j.get('status') in ('completed', 'failed'): break
json.dump({k: v for k, v in j.items() if k != 'last_response'} if isinstance(j, dict) else j, open(os.path.join(out, 'job.json'), 'w'))
print(j.get('status'), list(j.keys()))
res = j.get('last_response') or j; frames = res.get('images') or res.get('frames') or []
print('frames', len(frames))
for i, f in enumerate(frames):
    d = f.get('base64') if isinstance(f, dict) else f
    Image.open(io.BytesIO(base64.b64decode(d.split(',')[-1]))).convert('RGBA').save(os.path.join(out, 'f_%02d.png' % i))
