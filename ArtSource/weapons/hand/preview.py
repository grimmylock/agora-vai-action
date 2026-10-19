# python3 preview.py vk74 VK74 -> /tmp/claude-0/hp_<mod>.png : trace | hand A | old A | hand B | old B
import sys, importlib; sys.path.insert(0, '/home/claude/art/weapons/hand')
from PIL import Image
from common import render
mod, key = sys.argv[1], sys.argv[2]
m = importlib.import_module(mod)
def big(im, s, bg=(42, 38, 32, 255)):
    c = Image.new('RGBA', im.size, bg); c.alpha_composite(im); return c.resize((im.width * s, im.height * s), Image.NEAREST)
a, b = big(render(m.A), 10), big(render(m.B), 20)
oldB = big(Image.open(f'/tmp/claude-0/weapons_out/weapon_{key}.png').convert('RGBA'), 20)
oldA = big(Image.open(f'/tmp/claude-0/weapons_out/weapon_{key}_hd.png').convert('RGBA'), 10)
tr = Image.open(f'/tmp/claude-0/trace_{key}.png').convert('RGBA')
out = Image.new('RGBA', (1300, tr.height + a.height * 2 + 30), (27, 29, 31, 255))
out.alpha_composite(tr, (0, 0)); out.alpha_composite(a, (0, tr.height + 10)); out.alpha_composite(oldA, (660, tr.height + 10))
out.alpha_composite(b, (0, tr.height + a.height + 20)); out.alpha_composite(oldB, (660, tr.height + a.height + 20))
out.convert('RGB').save(f'/tmp/claude-0/hp_{mod}.png'); print('ok')
