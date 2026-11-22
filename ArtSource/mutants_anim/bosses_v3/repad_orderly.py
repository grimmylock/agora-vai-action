# 2026-11-22: the Orderly's canvas grew from 224x176 to 276x178 (room for blades reaching out left and up): every older clip is moved inside it so the stand keeps its place (old stand at 18,22 -> new 44,24). Idempotent (only touches 224x176 frames).
import glob, os
from PIL import Image
n = 0
for f in glob.glob(os.path.join(os.path.dirname(os.path.abspath(__file__)), "orderly", "*", "*.png")):
    im = Image.open(f)
    if im.size != (224, 176): continue
    cv = Image.new("RGBA", (276, 178), (0, 0, 0, 0)); cv.paste(im.convert("RGBA"), (26, 2)); cv.save(f); n += 1
print("repadded", n)
