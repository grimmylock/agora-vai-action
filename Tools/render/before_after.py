# Side-by-side before/after of a rendered map: python3 Tools/render/before_after.py <before.png> <after.png> <out.png>
import sys
from PIL import Image, ImageDraw
a, b = Image.open(sys.argv[1]).convert("RGB"), Image.open(sys.argv[2]).convert("RGB")
h = max(a.height, b.height); pad = 16
o = Image.new("RGB", (a.width + b.width + pad * 3, h + pad * 2 + 14), (24, 26, 28)); d = ImageDraw.Draw(o)
o.paste(a, (pad, pad + 14)); o.paste(b, (a.width + pad * 2, pad + 14))
d.text((pad, 6), "BEFORE", fill=(230, 230, 230)); d.text((a.width + pad * 2, 6), "AFTER", fill=(230, 230, 230))
o.save(sys.argv[3]); print(sys.argv[3], o.size)
