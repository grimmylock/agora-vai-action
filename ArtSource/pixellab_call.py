# generic Pixel Lab v2 caller (2026-11-21): python3 pixellab_call.py <endpoint> <body.json> <out.json>   (posts, polls the background job, writes last_response)
import sys, json, time, urllib.request
def call(url, data=None):
    req = urllib.request.Request("https://api.pixellab.ai" + url, data=json.dumps(data).encode() if data is not None else None, headers={"Content-Type": "application/json"})
    try: return json.load(urllib.request.urlopen(req, timeout=180))
    except urllib.error.HTTPError as e: print(e.code, e.read()[:1500].decode("utf8", "replace")); raise
ep, bodyp, outp = sys.argv[1:4]; r = call(ep, json.load(open(bodyp))); jid = r.get("background_job_id"); print({k: v for k, v in r.items() if len(str(v)) < 200})
if jid:
    for _ in range(180):
        time.sleep(6); j = call("/v2/background-jobs/" + jid)
        if j.get("status") in ("completed", "failed"): break
    print("status", j.get("status")); r = j.get("last_response") or j
json.dump(r, open(outp, "w"))
