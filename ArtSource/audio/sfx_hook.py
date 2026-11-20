# The Verger's Hook (2026-11-20; Boss.Verger "Hook", Boss.DragIn): three procedural sounds written next to the ones sfx.py makes (same recipes and helpers, so the set stays one family):
#   hook_throw  the chain pays out in a fast ratchet and the hook rings at the end of it
#   hook_catch  the beard bites (a thud, a cloth-and-flesh tear, steel on steel) and the chain goes taut with a twang
#   chain_drag  the haul: a ratchet of links over stone with a scrape under it
# python3 ArtSource/audio/sfx_hook.py -> Assets/Resources/Audio/hook_throw.wav, hook_catch.wav, chain_drag.wav (only these three are written)
import os, sys
import numpy as np
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import sfx
from sfx import *

def place(out, x, at):
    i = int(SR * at); n = min(len(x), len(out) - i)
    if n > 0: out[i:i + n] += x[:n]

def link_tick(seed, bright=1.0):
    rs.seed(seed); f = rs.uniform(1800, 4200) * bright
    return fit(metal(.05, [f, f * 1.51, f * 2.3], .012) * .8, hp(noise(.02), 3000) * env(.02, 0, .004) * .6)

def hook_throw():
    rs.seed(3001); sec = .62; out = np.zeros(int(SR * sec))
    whoosh = bp(noise(sec), 400, 2600) * np.sin(np.linspace(0, np.pi, len(out))) ** 1.5 * .55
    out += whoosh
    at = 0.02; gap = .045
    for k in range(11):                                           # the chain runs out: ticks speeding up
        place(out, link_tick(3100 + k, 1.0 + k * .03) * (.5 + k * .04), at); at += gap; gap = max(.012, gap * .84)
    place(out, metal(.5, [2900, 4350, 6100], .09) * .9, .36)        # the hook rings as it reaches the end
    place(out, hp(noise(.12), 1800) * env(.12, 0, .02) * .5, .36)
    return tail(out, .25, 1400, .3)

def hook_catch():
    rs.seed(3002); sec = .9; out = np.zeros(int(SR * sec))
    thud = fit(sweep(170, 55, .35, .09) * 1.4, lp(noise(.3), 300) * env(.3, 0, .05) * .9)
    tear = bp(noise(.22), 300, 2600) * env(.22, .004, .06) * .8                                   # cloth and flesh
    clank = metal(.5, [1700, 2650, 3900, 5200], .12) * .9
    place(out, thud, 0); place(out, tear, .005); place(out, clank, .0)
    x = t(.6); twang = np.sin(2 * np.pi * (520 - 60 * (1 - np.exp(-x * 7))) * x) * np.exp(-x / .22) * .5 + np.sin(2 * np.pi * 1040 * x) * np.exp(-x / .1) * .15
    place(out, twang, .05)                                                                         # the chain goes taut
    for k, tt in enumerate((.09, .15, .2)): place(out, link_tick(3200 + k) * .45, tt)              # links settling
    return tail(out, .3, 1200, .4)

def chain_drag():
    rs.seed(3003); sec = .75; out = np.zeros(int(SR * sec))
    scrape = bp(noise(sec), 500, 2200) * env(sec, .03, .45) * .35 + lp(noise(sec), 420) * env(sec, .02, .5) * .5       # a body over stone
    out += scrape
    at = 0.0; gap = .075
    for k in range(13):                                                                            # the chain hauled in: a ratchet of links, quickening
        place(out, link_tick(3300 + k, .8) * (.9 - k * .03), at); at += gap; gap = max(.03, gap * .93)
    place(out, sweep(120, 60, .3, .12) * .5, .45)                                                  # the last pull: a heavy settle
    return tail(out, .2, 1200, .25)

def main(d):
    os.makedirs(d, exist_ok=True)
    save(d, "hook_throw", hook_throw(), .8); save(d, "hook_catch", hook_catch(), .9); save(d, "chain_drag", chain_drag(), .75)
    print("wrote hook_throw, hook_catch, chain_drag to", d)

if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "Resources", "Audio"))
