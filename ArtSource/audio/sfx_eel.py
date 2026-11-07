# Procedural sounds for the eel boss (Boss 3), made with the same recipes' helpers as sfx.py (synthesized, no samples, no licences).
#   python3 ArtSource/audio/sfx_eel.py Assets/Resources/Audio
# eel_charge: the storm's 4 s charge (a hum climbing with crackle that thickens); eel_burst: the water going electric; eel_mouth: the slow opening groan;
# eel_splash / eel_flood / eel_rush: a water impact, the flood rising, a big body rushing past (the older moves);
# eel_scream: the charge let go; eel_bite: the snap; eel_strain_0..2: a press of the jaw-hold struggle (wet creak and grunt); eel_slam: the jaws shut and the head hits the ground; eel_wake: it shakes its head awake.
import sys, os
import numpy as np
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import sfx as S
from sfx import t, noise, env, lp, hp, bp, fit, sweep, tail, save, SR, rs


def eel_charge():
    d = 4.0; x = t(d); ramp = (x / d) ** 1.6
    f = 70.0 * 14.0 ** (x / d); ph = 2 * np.pi * np.cumsum(f) / SR
    hum = (np.sin(ph) + .5 * np.sin(2 * ph) + .25 * np.sin(3 * ph)) * (.15 + .85 * ramp)
    buzz = np.sign(np.sin(2 * np.pi * 100 * x)) * .25 * ramp
    pops = hp(noise(d), 2200) * (rs.rand(len(x)) < (0.004 + 0.06 * ramp)) * 2.2
    pops = lp(pops, 9000) * (.3 + ramp)
    return lp(hum + buzz, 3500) + pops * .6


def eel_burst():
    zap = sweep(5200, 180, .5, .14) * .9
    body = bp(noise(1.4), 700, 7000) * env(1.4, 0, .28)
    crack = hp(noise(.6), 3500) * env(.6, 0, .05)
    sub = sweep(95, 34, 1.2, .45) * 1.2
    return tail(fit(zap, body * .9, crack * .8, sub), .9, 2200, .3)


def eel_mouth():
    d = 2.8; x = t(d); ramp = np.clip(x / d, 0, 1)
    growl = lp(noise(d), 260) * (1 + .6 * np.sin(2 * np.pi * (7 + 5 * ramp) * x)) * (.2 + .8 * ramp)
    tone = np.sin(2 * np.pi * np.cumsum(55 + 90 * ramp ** 2) / SR) * (.2 + .6 * ramp)
    creak = bp(noise(d), 500, 1400) * (rs.rand(len(x)) < (0.002 + 0.02 * ramp)) * 3
    return growl * 1.1 + tone * .7 + lp(creak, 3000) * .5


def eel_bite():
    snap = hp(noise(.06), 2500) * env(.06, 0, .006)
    thump = sweep(190, 55, .35, .11) * 1.1
    wet = bp(noise(.3), 500, 2600) * env(.3, .004, .07)
    return fit(snap * .9, thump, wet * .7)


def eel_strain(seed):
    r = np.random.RandomState(seed); d = .36; x = t(d)
    creak = bp(r.uniform(-1, 1, len(x)), 380, 1250) * env(d, .012, .13) * (1 + .5 * np.sin(2 * np.pi * (26 + r.rand() * 10) * x))
    grunt = sweep(210 + r.rand() * 40, 140, d, .18) * .6
    wet = lp(r.uniform(-1, 1, len(x)), 900) * env(d, 0, .05) * .4
    return fit(creak, grunt, wet)


def eel_slam():
    thump = sweep(125, 28, 1.0, .32) * 1.2
    crunch = lp(noise(.6), 1700) * env(.6, 0, .12)
    splash = hp(noise(.9), 1400) * env(.9, .006, .22) * .5
    tick = hp(noise(.03), 4500) * env(.03, 0, .004)
    return tail(fit(thump, crunch * .9, splash, tick * .6), .8, 1500, .25)


def eel_wake():
    d = 1.6; x = t(d)
    groan = np.sin(2 * np.pi * np.cumsum(62 + 55 * (x / d)) / SR) * env(d, .1, .6) * (1 + .4 * np.sin(2 * np.pi * 5 * x))
    shake = bp(noise(d), 180, 900) * (1 + np.sin(2 * np.pi * 13 * x)) * env(d, .2, .5) * .6
    return groan + shake


def eel_splash():
    d = .9; x = t(d)
    wash = hp(noise(d), 1100) * env(d, .006, .2) * .8
    body = lp(noise(d), 700) * env(d, .004, .12)
    bub = sum(sweep(300 + 200 * i, 900 + 300 * i, .09, .05) * (np.arange(int(SR * d)) < 1).mean() for i in range(1))
    chirps = np.zeros(len(x))
    for k in range(6):
        c = sweep(280 + rs.rand() * 250, 900 + rs.rand() * 500, .1, .05); o = int(SR * (.06 + k * .08 + rs.rand() * .03)); chirps[o:o + len(c)] += c[:len(chirps) - o] * .35
    return fit(wash, body * .9, chirps)


def eel_flood():
    d = 3.2; x = t(d); ramp = np.clip(x / 1.6, 0, 1) * np.clip((d - x) / .8, 0, 1)
    rumble = lp(noise(d), 320) * ramp * 1.2
    surge = bp(noise(d), 300, 1800) * (1 + .5 * np.sin(2 * np.pi * 1.7 * x)) * ramp * .5
    chirps = np.zeros(len(x))
    for k in range(14):
        c = sweep(250 + rs.rand() * 250, 800 + rs.rand() * 500, .1, .05); o = int(SR * (.3 + k * .18 + rs.rand() * .08)); chirps[o:o + len(c)] += c[:len(chirps) - o] * .25
    return fit(rumble, surge, chirps)


def eel_rush():
    d = 1.0; x = t(d); ramp = np.sin(np.pi * np.clip(x / d, 0, 1)) ** 1.5
    wind = bp(noise(d), 250, 2400) * ramp
    low = lp(noise(d), 160) * ramp * 1.3
    return fit(wind * .8, low)


def eel_scream():
    d = 1.6; x = t(d); e = env(d, .03, .75)
    f = 620 + 900 * np.sin(np.pi * np.clip(x / 1.0, 0, 1)) ** .8 + 40 * np.sin(2 * np.pi * 11 * x)             # up to a shriek, wobbling, then down
    ph = 2 * np.pi * np.cumsum(f) / SR
    voice = (np.sin(ph) + .55 * np.sin(2 * ph + 1) + .3 * np.sin(3 * ph + 2)) * e
    rasp = hp(noise(d), 1500) * e * .45 * (1 + .6 * np.sin(2 * np.pi * 70 * x))
    buzz = np.sign(np.sin(2 * np.pi * 95 * x)) * .18 * e
    low = np.sin(2 * np.pi * np.cumsum(120 - 50 * (x / d)) / SR) * e * .5
    return tail(voice * .8 + rasp + buzz + low, .6, 2400, .2)


def main(d):
    os.makedirs(d, exist_ok=True)
    save(d, "eel_charge", eel_charge(), .8); save(d, "eel_burst", eel_burst(), .95); save(d, "eel_mouth", eel_mouth(), .7)
    save(d, "eel_bite", eel_bite(), .9)
    for v in range(3): save(d, f"eel_strain_{v}", eel_strain(2100 + v * 11), .75)
    save(d, "eel_slam", eel_slam(), .95); save(d, "eel_wake", eel_wake(), .7)
    save(d, "eel_scream", eel_scream(), .9); save(d, "eel_splash", eel_splash(), .8); save(d, "eel_flood", eel_flood(), .7); save(d, "eel_rush", eel_rush(), .7)
    print("eel sounds written to", d)


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else "Assets/Resources/Audio")
