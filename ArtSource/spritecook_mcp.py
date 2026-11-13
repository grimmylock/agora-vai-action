# Tiny SpriteCook MCP caller (2026-11-13): python3 -I ArtSource/spritecook_mcp.py <tool> '<json args>'. The API key is saved on this session's environment (network secret for api.spritecook.ai)
# and injected by the session proxy: never put a key in a file. Long waits (wait_seconds 85) get a 502 from the gateway: submit with wait_seconds 0 and poll check_job_status every 20 s.
import sys, json, urllib.request, os
URL="https://api.spritecook.ai/mcp/"
def call(tool,args,i=2):
    body=json.dumps({"jsonrpc":"2.0","id":i,"method":"tools/call","params":{"name":tool,"arguments":args}}).encode()
    r=urllib.request.Request(URL,data=body,headers={"Content-Type":"application/json","Accept":"application/json, text/event-stream"})
    raw=urllib.request.urlopen(r,timeout=150).read().decode()
    if raw.startswith("event:") or "data:" in raw[:20]:
        raw=[l[5:].strip() for l in raw.splitlines() if l.startswith("data:")][-1]
    return json.loads(raw)
if __name__=="__main__":
    d=call(sys.argv[1], json.loads(sys.argv[2]) if len(sys.argv)>2 else {})
    res=d.get("result",d)
    if isinstance(res,dict) and "content" in res:
        for c in res["content"]:
            print(c.get("text",c) if isinstance(c,dict) else c)
        if res.get("isError"): print("ISERROR")
    else: print(json.dumps(res)[:3000])
