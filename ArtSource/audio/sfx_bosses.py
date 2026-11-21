# The new creature bosses' own sounds (2026-11-21; BOSS_CONCEPTS_V3.md): procedural, the sfx.py recipes and helpers, so they stay one family with the rest. No samples, no licences.
#   mother_howl     Mother Hollow's howl: a long falling canine wail with a wet rasp under it (calls the brood)
#   acid_spit       a wet hawk and a sizzle (her spit landing)
#   singer_note     the Singer's note: a low organ-like drone that swells, with beating harmonics and a rumble of rock
#   lantern_flare   the Ferryman's flare: a rising glassy shimmer and a bright crack
#   drowned_hands   water that is not there: a slosh, gurgles and a grab
#   bucket_slam     the Excavator's bucket into the ground: a huge thud, rock, a hydraulic hiss
#   diesel          the rig's engine catching: three coughs and a rough idle
#   saw_scrape      the Orderly's saw dragged over tiles: a long metal scream
#   drip_squeeze    the IV bag squeezed: a rubbery creak and a liquid run
# python3 ArtSource/audio/sfx_bosses.py -> Assets/Resources/Audio/<name>.wav (only these are written)
import os, sys
import numpy as np
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from sfx import *

def place(out, x, at):
    i = int(SR * at); n = min(len(x), len(out) - i)
    if n > 0: out[i:i + n] += x[:n]

def mother_howl():
    rs.seed(4101); sec = 1.8; x = t(sec)
    f = 520 * (0.55 + 0.45 * np.exp(-x / 0.9)) * (1 + 0.012 * np.sin(2 * np.pi * 5.5 * x))          # a long wail falling away, with a tremble
    ph = 2 * np.pi * np.cumsum(f) / SR; e = np.clip(x / .12, 0, 1) * np.exp(-np.clip(x - 1.1, 0, None) / .35)
    voice = (np.sin(ph) + .5 * np.sin(2 * ph) + .25 * np.sin(3 * ph + .7)) * e * .5
    rasp = bp(noise(sec), 700, 2600) * e * .35 * (0.6 + 0.4 * np.sin(2 * np.pi * 31 * x))
    wet = lp(noise(sec), 500) * e * .2
    return tail(fit(voice, rasp, wet), .6, 1400, .5)

def acid_spit():
    rs.seed(4102); out = np.zeros(int(SR * .9))
    hawk = bp(noise(.18), 300, 1800) * env(.18, .02, .06) * .8
    splat = fit(lp(noise(.12), 900) * env(.12, 0, .03), sweep(240, 90, .15, .05) * .6)
    sizzle = hp(noise(.6), 3500) * env(.6, .02, .25) * .35 * (0.7 + 0.3 * rs.uniform(-1, 1, int(SR * .6)))
    place(out, hawk, 0); place(out, splat, .2); place(out, sizzle, .22)
    return tail(out, .2, 1600, .2)

def singer_note():
    rs.seed(4103); sec = 2.4; x = t(sec)
    swell = np.sin(np.pi * np.clip(x / sec, 0, 1)) ** 1.2
    f0 = 73.4
    drone = sum(np.sin(2 * np.pi * f0 * k * x + k) * (.9 / k) * (1 + .3 * np.sin(2 * np.pi * (.7 + k * .13) * x)) for k in (1, 2, 3, 4, 6))
    drone += .4 * np.sin(2 * np.pi * (f0 * 1.5 + .8) * x)                                             # a fifth that beats against itself
    rumble = lp(noise(sec), 120) * .8
    return tail((drone * .35 + rumble * .5) * swell, .9, 600, .6)

def lantern_flare():
    rs.seed(4104); out = np.zeros(int(SR * 1.0))
    shimmer = sum(sweep(800 * k, 3200 * k, .5, .4) * (.4 / k) for k in (1, 1.5, 2.25)) * np.linspace(0, 1, int(SR * .5)) ** 2
    crack = fit(hp(noise(.15), 2500) * env(.15, 0, .03) * 1.0, metal(.5, [3100, 4700, 6900], .15) * .6)
    place(out, shimmer, 0); place(out, crack, .48)
    return tail(out, .35, 3000, .35)

def drowned_hands():
    rs.seed(4105); out = np.zeros(int(SR * 1.1))
    slosh = lp(noise(.5), 700) * env(.5, .05, .18) * .8
    place(out, slosh, 0)
    for k in range(6):                                                                                 # gurgles
        g = sweep(rs.uniform(180, 420), rs.uniform(500, 900), .08, .03) * .45; place(out, g, .1 + k * .1 + rs.uniform(0, .04))
    grab = fit(sweep(150, 60, .2, .06) * .9, bp(noise(.1), 400, 2000) * env(.1, 0, .03) * .6)
    place(out, grab, .7)
    return tail(out, .3, 1100, .35)

def bucket_slam():
    rs.seed(4106); out = np.zeros(int(SR * 1.4))
    thud = fit(sweep(90, 32, .6, .2) * 1.6, lp(noise(.5), 220) * env(.5, 0, .12) * 1.2)
    rock = bp(noise(.7), 300, 3000) * env(.7, .005, .2) * .6
    for k in range(10): place(out, bp(noise(.04), 800, 4000) * env(.04, 0, .01) * .4, .08 + k * .05 + rs.uniform(0, .03))
    hiss = hp(noise(.6), 2500) * env(.6, .05, .25) * .3
    place(out, thud, 0); place(out, rock, .01); place(out, hiss, .5)
    return tail(out, .5, 900, .5)

def diesel():
    rs.seed(4107); out = np.zeros(int(SR * 2.2))
    for k, at in enumerate((0, .45, .85)):                                                             # three coughs
        place(out, fit(lp(noise(.25), 300) * env(.25, .01, .08) * (1 + k * .2), sweep(70, 45, .25, .08) * .8), at)
    x = t(1.1); idle = np.sin(2 * np.pi * 28 * x) ** 9 * .7 + lp(noise(1.1), 180) * .5 * (0.5 + 0.5 * np.sin(2 * np.pi * 14 * x))
    place(out, idle * np.clip(x / .2, 0, 1), 1.05)
    return tail(out, .3, 700, .3)

def saw_scrape():
    rs.seed(4108); sec = 1.2; x = t(sec)
    f = 1900 + 300 * np.sin(2 * np.pi * 2.3 * x) + 120 * rs.uniform(-1, 1, len(x))
    ph = 2 * np.pi * np.cumsum(f) / SR
    scream = (np.sin(ph) + .6 * np.sin(1.97 * ph) + .3 * np.sin(3.1 * ph)) * .3
    grit = bp(noise(sec), 1500, 6000) * .4 * (0.5 + 0.5 * np.abs(np.sin(2 * np.pi * 9 * x)))
    e = np.clip(x / .05, 0, 1) * np.exp(-np.clip(x - .8, 0, None) / .15)
    return tail((scream + grit) * e, .3, 2400, .3)

def drip_squeeze():
    rs.seed(4109); out = np.zeros(int(SR * 1.0))
    creak = fit(*[sweep(rs.uniform(250, 380), rs.uniform(180, 240), .12, .06) * .5 for _ in range(3)])
    run = lp(noise(.6), 900) * env(.6, .08, .25) * .35
    for k in range(5): place(out, sweep(900, 1400, .05, .02) * .3, .3 + k * .09)                       # drops
    place(out, creak, 0); place(out, run, .2)
    return tail(out, .25, 1500, .3)

SOUNDS = {"mother_howl": (mother_howl, .85), "acid_spit": (acid_spit, .7), "singer_note": (singer_note, .9), "lantern_flare": (lantern_flare, .75), "drowned_hands": (drowned_hands, .8),
          "bucket_slam": (bucket_slam, .95), "diesel": (diesel, .7), "saw_scrape": (saw_scrape, .6), "drip_squeeze": (drip_squeeze, .6)}

def main(d):
    os.makedirs(d, exist_ok=True)
    for n, (fn, peak) in SOUNDS.items(): save(d, n, fn(), peak)
    print("wrote", ", ".join(SOUNDS), "to", d)

if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "Resources", "Audio"))
