# Pixel Lab auth (2026-11-23). In the cloud container a proxy added the token; on a normal PC set it yourself:
#   PowerShell: $env:PIXELLAB_TOKEN = "<token from pixellab.ai account page>"   (or put it in ArtSource/.pixellab_token, git-ignored)
import os
def _tok():
    t = os.environ.get("PIXELLAB_TOKEN", "").strip()
    if not t:
        p = os.path.join(os.path.dirname(os.path.abspath(__file__)), ".pixellab_token")
        if os.path.exists(p): t = open(p).read().strip()
    return t
HEADERS = {"Content-Type": "application/json"}
if _tok(): HEADERS["Authorization"] = "Bearer " + _tok()
