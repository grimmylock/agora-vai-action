# Fetch a finished Pixel Lab animate-pixminimax job's frames: python3 ArtSource/pixellab_fetch.py <job_id> <outdir>   (the helper pixellab_anim.py does this itself; use this when a job was started elsewhere)
import sys, os, io, json, base64, time, urllib.request
from PIL import Image
jid, out = sys.argv[1], sys.argv[2]; os.makedirs(out, exist_ok=True)
def call(url): return json.load(urllib.request.urlopen(urllib.request.Request('https://api.pixellab.ai' + url, headers={'Content-Type': 'application/json'}), timeout=120))
for _ in range(90):
    j = call('/v2/background-jobs/' + jid)
    if j.get('status') in ('completed', 'failed'): break
    time.sleep(5)
json.dump({k: v for k, v in j.items() if k != 'last_response'}, open(os.path.join(out, 'job.json'), 'w'))
res = j.get('last_response') or j; frames = res.get('images') or res.get('frames') or []
print(j.get('status'), 'frames', len(frames))
for i, f in enumerate(frames):
    d = f.get('base64') if isinstance(f, dict) else f
    Image.open(io.BytesIO(base64.b64decode(d.split(',')[-1]))).convert('RGBA').save(os.path.join(out, 'f_%02d.png' % i))
