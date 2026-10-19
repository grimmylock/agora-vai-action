# Batch 93: reference photos for redrawing world props (Yan: "use real objects as references"). Photos are references only:
# every sprite is still drawn by hand in Python/PIL. Pulls openly licensed photos from Openverse (Flickr, Wikimedia...) into
# ArtSource/refs/<prop>/ with credits.txt, and a contact sheet <prop>/sheet.jpg.
#   python3 ArtSource/refs/fetch.py jersey "concrete jersey barrier road" ["second query" ...]   adds photos (keeps the ones there)
#   python3 ArtSource/refs/fetch.py jersey --drop 2,5,7                                           removes photos by sheet number
#   python3 ArtSource/refs/fetch.py jersey --sheet                                                just redraws the sheet
# Batch 104: adding keeps what's there (curated libraries grow), --drop prunes off-topic shots, ids already dropped never come back.
import json, os, sys, time, urllib.error, urllib.parse, urllib.request
from PIL import Image, ImageDraw

UA = "AgoraVaiRefs/1.0 (game art references; yanbuschle@gmail.com)"
HERE = os.path.dirname(os.path.abspath(__file__))

def get(url):
    for k in range(6):   # Openverse rate-limits anonymous use: back off on 429 instead of losing the run
        try:
            req = urllib.request.Request(url, headers={"User-Agent": UA})
            with urllib.request.urlopen(req, timeout=30) as r: return r.read()
        except urllib.error.HTTPError as e:
            if e.code != 429 or k == 5: raise
            time.sleep(15 * (k + 1))

def read_credits(out):
    p = os.path.join(out, "credits.txt")
    return [l for l in open(p).read().splitlines() if l.strip()] if os.path.exists(p) else []

def write(out, credits):
    open(os.path.join(out, "credits.txt"), "w").write("\n".join(credits) + ("\n" if credits else ""))
    files = [(os.path.join(out, c.split("\t")[0]), c.split("\t")[1][:60]) for c in credits]
    # contact sheet: 4 across, 300 px wide each, numbered title under each
    if not files: return
    C, W, H = 4, 300, 230
    S = Image.new("RGB", (C * W, ((len(files) + C - 1) // C) * H), (18, 18, 20)); d = ImageDraw.Draw(S)
    for i, (fn, t) in enumerate(files):
        im = Image.open(fn).convert("RGB"); im.thumbnail((W - 6, H - 26))
        x, y = (i % C) * W, (i // C) * H
        S.paste(im, (x + 3, y + 3)); d.text((x + 4, y + H - 20), f"{i + 1}. {t[:44]}", fill=(220, 215, 190))
    S.save(os.path.join(out, "sheet.jpg"), quality=85)

def fetch(name, queries, per=4):
    out = os.path.join(HERE, name); os.makedirs(out, exist_ok=True)
    credits = read_credits(out)
    banned = set(open(os.path.join(out, "dropped.txt")).read().split()) if os.path.exists(os.path.join(out, "dropped.txt")) else set()
    have = {c.split("\t")[0] for c in credits}
    for q in queries:
        d = json.loads(get("https://api.openverse.org/v1/images/?" + urllib.parse.urlencode({"q": q, "page_size": per})))
        for r in d["results"]:
            base = r["id"][:8] + ".jpg"
            if base in have or base in banned: continue
            fn = os.path.join(out, base)
            try:
                open(fn, "wb").write(get(f"https://api.openverse.org/v1/images/{r['id']}/thumb/"))
                Image.open(fn).verify()
            except Exception as e:
                print("skip", r["id"], e); continue
            have.add(base)
            credits.append(f"{base}\t{r['title']}\t{r.get('creator')}\t{r.get('license')} {r.get('license_version')}\t{r.get('foreign_landing_url')}")
            time.sleep(0.5)
    write(out, credits)
    print(name, len(credits), "photos")

def drop(name, nums):
    out = os.path.join(HERE, name); credits = read_credits(out)
    gone = [credits[n - 1] for n in nums if 0 < n <= len(credits)]
    for c in gone:
        f = os.path.join(out, c.split("\t")[0])
        if os.path.exists(f): os.remove(f)
    with open(os.path.join(out, "dropped.txt"), "a") as fh:
        for c in gone: fh.write(c.split("\t")[0] + "\n")
    write(out, [c for c in credits if c not in gone])
    print(name, len(credits) - len(gone), "photos")

if __name__ == "__main__":
    name, rest = sys.argv[1], sys.argv[2:]
    if rest[:1] == ["--drop"]: drop(name, [int(n) for n in rest[1].split(",") if n])
    elif rest[:1] == ["--sheet"]: write(os.path.join(HERE, name), read_credits(os.path.join(HERE, name)))
    else: fetch(name, rest)
