# Procedural sounds for the eel boss (Boss 3), made with the same recipes' helpers as sfx.py (synthesized, no samples, no licences).
#   python3 ArtSource/audio/sfx_eel.py Assets/Resources/Audio
# eel_charge: the storm's 4 s charge (a hum climbing with crackle that thickens); eel_burst: the water going electric; eel_mouth: the slow opening groan;
# eel_bite: the snap; eel_strain_0..2: a press of the jaw-hold struggle (wet creak and grunt); eel_slam: the jaws shut and the head hits the ground; eel_wake: it shakes its head awake.
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


def eel_roar(seed):
    """The start of a move (combo, arch, dash, snare): a wet low roar rising out of the water."""
    r = np.random.RandomState(seed); d = 1.1 + .2 * r.rand(); x = t(d); ramp = np.clip(x / (d * .45), 0, 1)
    f = (58 + 14 * r.rand()) * (1 + .8 * ramp ** 1.5); ph = 2 * np.pi * np.cumsum(f) / SR
    voice = (np.sin(ph) + .6 * np.sin(2 * ph) + .3 * np.sin(3 * ph)) * env(d, .08, .45) * (1 + .35 * np.sin(2 * np.pi * (9 + 4 * r.rand()) * x))
    rasp = bp(r.uniform(-1, 1, len(x)), 300, 1800) * env(d, .1, .3) * .5
    gurgle = lp(r.uniform(-1, 1, len(x)), 700) * (r.rand(len(x)) < .01) * 6 * env(d, 0, .5)
    return fit(voice, rasp, lp(gurgle, 1200) * .5)


def eel_splash(seed, big=False):
    """Water thrown out when it lands in a pool; `big` is the arch's slam: a deep boom and a long wash."""
    r = np.random.RandomState(seed); d = 1.5 if big else .7; x = t(d)
    wash = bp(r.uniform(-1, 1, len(x)), 400, 6500) * env(d, .004, .32 if big else .15)
    slap = hp(r.uniform(-1, 1, len(x)), 2500) * env(d, 0, .03) * .8
    boom = sweep(110 if big else 150, 38 if big else 60, d, .3 if big else .12) * (1.3 if big else .7)
    drops = hp(r.uniform(-1, 1, len(x)), 3000) * (r.rand(len(x)) < (.004 if big else .002)) * 2 * env(d, .15, d / 3)
    return fit(wash, slap, boom, lp(drops, 9000) * .5)


def eel_thud(seed):
    """Part of the body or the head hitting the ground (the combo's head strike, the snare's catch)."""
    r = np.random.RandomState(seed); d = .55; x = t(d)
    thump = sweep(140 + 20 * r.rand(), 42, d, .13) * 1.2
    slap = lp(r.uniform(-1, 1, len(x)), 1800) * env(d, 0, .05) * .8
    wet = bp(r.uniform(-1, 1, len(x)), 600, 3000) * env(d, .003, .09) * .5
    return fit(thump, slap, wet)


def eel_flood():
    """The flood surge: the pools swelling and the floor turning to water, a rumbling swell that drains out (about 6 s)."""
    d = 6.0; x = t(d); swell = np.clip(x / 1.8, 0, 1) * np.clip((d - x) / 1.6, 0, 1)
    rumble = lp(noise(d), 220) * (.3 + .9 * swell) * (1 + .3 * np.sin(2 * np.pi * 3 * x))
    wash = bp(noise(d), 300, 3500) * swell ** 1.5 * .55
    bubbles = hp(noise(d), 1800) * (rs.rand(len(x)) < (.001 + .004 * swell)) * 2.5 * swell
    return fit(rumble * 1.2, wash, lp(bubbles, 8000) * .5)


def main(d):
    os.makedirs(d, exist_ok=True)
    save(d, "eel_charge", eel_charge(), .8); save(d, "eel_burst", eel_burst(), .95); save(d, "eel_mouth", eel_mouth(), .7)
    save(d, "eel_bite", eel_bite(), .9)
    for v in range(3): save(d, f"eel_strain_{v}", eel_strain(2100 + v * 11), .75)
    save(d, "eel_slam", eel_slam(), .95); save(d, "eel_wake", eel_wake(), .7)
    # the older moves' sounds (added after the first set, and last: eel_flood draws on the shared random stream, so the sounds above stay byte-identical)
    for v in range(2): save(d, f"eel_roar_{v}", eel_roar(3100 + v * 7), .85)
    for v in range(2): save(d, f"eel_splash_{v}", eel_splash(3200 + v * 5), .8)
    save(d, "eel_splash_big", eel_splash(3300, True), .95)
    for v in range(2): save(d, f"eel_thud_{v}", eel_thud(3400 + v * 3), .85)
    save(d, "eel_flood", eel_flood(), .8)
    print("eel sounds written to", d)


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else "Assets/Resources/Audio")
