#!/usr/bin/env python3
"""One batch of guns, start to finish (2026-11-15, the --native method of WEAPON_SPRITE_RULES.md):
   python3 -I ArtSource/weapons/batch_native.py submit <batch.json>     builds the reference sheet, imports it, starts the SpriteCook job (24 credits), writes <batch>.job.json
   python3 -I ArtSource/weapons/batch_native.py fetch  <batch.json>     polls the job; when done downloads both variations and slices them (rows by height) into <outdir>/<Key>_A.png / _B.png
   python3 -I ArtSource/weapons/batch_native.py install <batch.json> A|B,A|B,...   (one letter per gun) copies the chosen stills to <outdir>/<Key>_spritecook.png and runs make_gun_x2.py --native, printing the HeldWeapon.cs lines
batch.json: {"name": "b3", "outdir": "ArtSource/weapons/spritecook_pilot/batch3", "refs": [["VKM", "path/ref.png (RGBA, muzzle right, level)"], ...], "rows": ["one-line SHAPE description per gun, same order", ...]}"""
import sys, os, json, base64, io, time, subprocess, urllib.request
import numpy as np
from PIL import Image
from scipy import ndimage as nd
HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, os.path.join(HERE, '..'))
import spritecook_mcp as mcp
def text(d): return d['result']['content'][0]['text']
def sheet(refs):
    def crop(im): a = np.array(im)[..., 3] > 40; ys, xs = np.where(a); return im.crop((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1))
    W = 640; rows = []
    for _, p in refs:
        g = crop(Image.open(p).convert('RGBA')); g = g.resize((W, max(1, int(g.height * W / g.width))), Image.LANCZOS); bg = Image.new('RGBA', g.size, (255, 255, 255, 255)); bg.alpha_composite(g); rows.append(bg.convert('RGB'))
    out = Image.new('RGB', (W + 40, sum(r.height for r in rows) + 20 * (len(rows) + 1)), (255, 255, 255)); y = 20
    for r in rows: out.paste(r, (20, y)); y += r.height + 20
    return out
def submit(b):
    sh = sheet(b['refs']); buf = io.BytesIO(); sh.save(buf, 'PNG'); sh.save(b['_path'].replace('.json', '.sheet.png'))
    d = mcp.call('import_asset', {"image": "data:image/png;base64," + base64.b64encode(buf.getvalue()).decode(), "pixel": False, "display_name": "gun reference sheet " + b['name']})
    ref = d['result']['structuredContent']['id']; n = len(b['rows'])
    prompt = (f"{n} separate firearms side view, all facing right, one above the other in the same order as the reference photo sheet, each as its own clean pixel-art sprite, same pixel scale, plain transparent background. "
              + " ".join(f"Row {i + 1}: {r}" for i, r in enumerate(b['rows'])) + " Keep each gun's proportions and colours from the reference. No text, no lettering, no logos, no hands, no shadows, no scopes or silencers unless named.")
    d = mcp.call('generate_game_art', {"prompt": prompt, "width": 256, "height": 256, "variations": 2, "pixel": True, "bg_mode": "transparent", "aspect_ratio": "1:1", "reference_asset_id": ref, "wait_seconds": 0})
    t = json.loads(text(d)); json.dump(t, open(b['_path'].replace('.json', '.job.json'), 'w')); print('job', t['job_id'], 'credits left', t.get('credits_remaining'))
def fetch(b):
    job = json.load(open(b['_path'].replace('.json', '.job.json')))['job_id']; os.makedirs(b['outdir'], exist_ok=True)
    for _ in range(30):
        t = json.loads(text(mcp.call('check_job_status', {"job_id": job, "wait_seconds": 0}))); print(t['status'])
        if t['status'] == 'succeeded': break
        if t['status'] in ('failed', 'cancelled'): sys.exit('job ' + t['status'])
        time.sleep(20)
    keys = [k for k, _ in b['refs']]
    for v, a in zip('AB', t['assets']):
        path = os.path.join(b['outdir'], f'sheet_{v}.png'); urllib.request.urlretrieve(a['sprite_url'], path)
        A = np.array(Image.open(path).convert('RGBA')); m = A[..., 3] > 0
        for it in (0, 1, 2):                                                   # the pixels' own islands first; bridge small gaps only when a gun is in pieces
            lab, n = nd.label(nd.binary_dilation(m, iterations=it) if it else m, structure=np.ones((3, 3))); lab = lab * m
            sizes = nd.sum(m, lab, range(1, n + 1)); big = [i + 1 for i in np.argsort(sizes)[::-1] if sizes[i] > 800][:len(keys)]
            if len(big) == len(keys): break
        comps = sorted(big, key=lambda l: np.where(lab == l)[0].mean())
        if len(comps) != len(keys):                                            # guns touching (bipods, magazines): cut the sheet at the emptiest row near each 1/n of its height instead
            print('variation', v, 'found', len(comps), 'blobs for', len(keys), 'guns: cutting by rows')
            rs = m.sum(1).astype(float); H = len(rs); cuts = [0]
            for i in range(1, len(keys)):
                c = int(H * i / len(keys)); w = max(4, H // (3 * len(keys))); lo, hi = max(cuts[-1] + 5, c - w), min(H - 5, c + w); cuts.append(lo + int(np.argmin(rs[lo:hi])))
            cuts.append(H)
            for k, y0, y1 in zip(keys, cuts[:-1], cuts[1:]):
                band = A.copy(); band[:y0] = 0; band[y1:] = 0; mm = band[..., 3] > 0; l2, n2 = nd.label(mm, structure=np.ones((3, 3))); keep = np.isin(l2, [i + 1 for i, z in enumerate(nd.sum(mm, l2, range(1, n2 + 1))) if z >= 12]); band[~keep] = 0
                ys, xs = np.where(band[..., 3] > 0); Image.fromarray(band, 'RGBA').crop((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1)).save(os.path.join(b['outdir'], f'{k}_{v}.png'))
            print('sliced variation', v, 'by rows at', cuts); continue
        for k, l in zip(keys, comps):
            ys, xs = np.where(lab == l); Image.fromarray(np.where((lab == l)[..., None], A, 0).astype(np.uint8), 'RGBA').crop((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1)).save(os.path.join(b['outdir'], f'{k}_{v}.png'))
        print('sliced variation', v, [os.path.basename(path)])
def patch_cs(out):
    """pastes the Dense and Grips lines make_gun_x2 printed into HeldWeapon.cs (the Grips row is replaced in place, the Dense entry added or replaced)"""
    import re
    P = os.path.join(HERE, '..', '..', 'Assets', 'Scripts', 'GameCore', 'HeldWeapon.cs'); s = open(P).read()
    for m in re.finditer(r'WeaponArt\.Dense:\s+\["(\w+)"\] = ([0-9.]+f),', out):
        k, d = m.groups(); g = re.search(r'WeaponArt\.Grips:\s+(\["%s"\] = new\[\] \{[^}]*\})' % k, out).group(1)
        rows = re.findall(r'\["%s"\] = new\[\] \{[^}]*\}' % k, s)
        if len(rows) == 1: s = s.replace(rows[0], g)
        else: print('!! grips row for', k, 'found', len(rows), '- add by hand:', g)
        if re.search(r'\["%s"\] = [0-9.]+f,' % k, s): s = re.sub(r'\["%s"\] = [0-9.]+f,' % k, '["%s"] = %s,' % (k, d), s, 1)
        else:
            i = s.index('["VK74"] = '); j = s.index('\n', i); s = s[:j] + '\n            ["%s"] = %s,' % (k, d) + s[j:]
    open(P, 'w').write(s)
def recolor(a_path, b_path, dst):
    """A's pixels get B's colours by brightness rank (histogram matching), done separately for the warm pixels (wood, saturation > 0.25 and red over blue) and the rest (steel, plastic), so the wood palette never leaks into the metal.
    The shape and the shading stay A's, the colours (and their spread) are B's. Yan, 2026-11-15: "B color on the A sprite"."""
    A = np.array(Image.open(a_path).convert('RGBA')); B = np.array(Image.open(b_path).convert('RGBA'))
    def lum(x): return x[..., 0] * 0.3 + x[..., 1] * 0.59 + x[..., 2] * 0.11
    def warm(x): f = x[..., :3].astype(float) / 255; mx = f.max(-1); sat = (mx - f.min(-1)) / np.maximum(mx, 1e-3); return (sat > 0.25) & (f[..., 0] > f[..., 2]) & (mx > 0.2)
    ao = A[..., 3] > 0; bo = B[..., 3] > 0
    for fam in (True, False):
        am = ao & (warm(A) == fam); bm = bo & (warm(B) == fam)
        if am.sum() < 10 or bm.sum() < 10: am = ao if not fam else am; bm = bo if not fam else bm
        if am.sum() < 10 or bm.sum() < 10: continue
        bc = B[bm][:, :3].astype(float); bc = bc[np.argsort(lum(bc))]
        ys, xs = np.where(am); l = lum(A[ys, xs, :3].astype(float)); rank = np.argsort(np.argsort(l)) / max(1, len(l) - 1)
        A[ys, xs, :3] = bc[np.clip((rank * (len(bc) - 1)).astype(int), 0, len(bc) - 1)].astype(np.uint8)
    Image.fromarray(A, 'RGBA').save(dst)
def install(b, picks):
    import shutil
    for (k, _), v in zip(b['refs'], picks):
        dst = os.path.join(b['outdir'], f'{k}_spritecook.png')
        if v == 'AB': recolor(os.path.join(b['outdir'], f'{k}_A.png'), os.path.join(b['outdir'], f'{k}_B.png'), dst)   # A's shape, B's colouring
        else: shutil.copy(os.path.join(b['outdir'], f'{k}_{v}.png'), dst)
        out = subprocess.run([sys.executable, os.path.join(HERE, 'make_gun_x2.py'), k, dst, '--native'], capture_output=True, text=True).stdout; print(out); patch_cs(out)
if __name__ == '__main__':
    cmd, path = sys.argv[1], sys.argv[2]; b = json.load(open(path)); b['_path'] = path
    {'submit': submit, 'fetch': fetch}.get(cmd, lambda b: install(b, sys.argv[3].split(',')))(b)
