# Procedural sound effects for agora vai (Batch 30). Everything is synthesized here (no sampled
# audio), so there are no licences to track. Writes 16-bit mono WAVs to Resources/Audio.
#   python3 sfx.py <outdir>
# Recipes: gunshots = transient crack + noise body + low thump + room tail, shaped per weapon class;
# impacts, footsteps, reloads, doors, explosions; ambient loops crossfaded to loop seamlessly.
import sys, os, wave
import numpy as np
from scipy import signal

SR = 44100
rs = np.random.RandomState(7)

def t(sec): return np.arange(int(SR * sec)) / SR
def noise(sec): return rs.uniform(-1, 1, int(SR * sec))
def env(sec, attack, decay):
    x = t(sec); e = np.exp(-x / max(decay, 1e-4))
    if attack > 0: e *= np.clip(x / attack, 0, 1)
    return e
def lp(x, f, o=2): b, a = signal.butter(o, min(f / (SR / 2), .99), "low"); return signal.lfilter(b, a, x)
def hp(x, f, o=2): b, a = signal.butter(o, min(f / (SR / 2), .99), "high"); return signal.lfilter(b, a, x)
def bp(x, lo, hi, o=2): b, a = signal.butter(o, [lo / (SR / 2), min(hi / (SR / 2), .99)], "band"); return signal.lfilter(b, a, x)
def fit(*xs):
    n = max(len(x) for x in xs); out = np.zeros(n)
    for x in xs: out[:len(x)] += x
    return out
def sweep(f0, f1, sec, decay):
    x = t(sec); f = f0 * (f1 / f0) ** (x / sec); ph = 2 * np.pi * np.cumsum(f) / SR
    return np.sin(ph) * np.exp(-x / decay)
def metal(sec, freqs, decay):
    x = t(sec); return sum(np.sin(2 * np.pi * f * x + rs.rand() * 6) * np.exp(-x / (decay * (1 + i * .3))) for i, f in enumerate(freqs)) / len(freqs)
def tail(x, sec, f=1800, amt=.25):
    """cheap room: decaying filtered noise convolved"""
    ir = lp(noise(sec), f) * env(sec, 0, sec / 4)
    y = np.convolve(x, ir)[:len(x) + int(SR * sec)] * amt / 30
    return fit(x, y)
def norm(x, peak=.9):
    x = np.tanh(x / (np.abs(x).max() + 1e-9) * 1.4)          # soft clip = punch
    x = x - np.mean(x)
    return x / (np.abs(x).max() + 1e-9) * peak
def fade_out(x, ms=8):
    n = int(SR * ms / 1000); x = x.copy(); x[-n:] *= np.linspace(1, 0, n); return x

def save(d, name, x, peak=.9):
    x = fade_out(norm(x, peak))
    with wave.open(os.path.join(d, name + ".wav"), "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes((x * 32767).astype(np.int16).tobytes())

# ------------------------------------------------------------------ guns
def gunshot(crack, body_f, body_dec, thump_f, thump_amt, tail_sec, tail_amt, sec, seed):
    rs.seed(seed)
    c = hp(noise(sec), 1500) * env(sec, 0, crack)                        # supersonic crack / muzzle blast transient
    b = lp(noise(sec), body_f) * env(sec, .0005, body_dec)                # powder blast body
    th = sweep(thump_f * 1.8, thump_f, sec, body_dec * 1.4) * thump_amt  # chest thump
    x = c * .8 + b * 1.0 + th
    return tail(x, tail_sec, 1500, tail_amt)

GUNS = {
    #           crack  body_f body_dec thump thumpA tail  tailA  sec
    "pistol":  (.004, 3200, .035, 140, .5, .5, .35, .35),
    "smg":     (.003, 3800, .025, 160, .35, .35, .3, .25),
    "rifle":   (.003, 2600, .05, 110, .7, .9, .45, .5),
    "shotgun": (.006, 1500, .09, 80, 1.0, 1.1, .5, .7),
    "sniper":  (.002, 2200, .07, 70, 1.0, 1.6, .6, .8),
    "heavy":   (.003, 2300, .055, 90, .9, 1.0, .5, .55),
    "rail":    None,
}

def railgun(seed):
    rs.seed(seed)
    z = sweep(3200, 380, .6, .25) * .7 + hp(noise(.6), 4000) * env(.6, 0, .05) * .6
    return tail(z + sweep(90, 40, .6, .2), 1.0, 2500, .4)

def dryfire():
    return fit(metal(.08, [2400, 3900, 6100], .012), hp(noise(.08), 3000) * env(.08, 0, .004))

# ------------------------------------------------------------------ hits & melee
def hit_flesh(seed):
    rs.seed(seed)
    thud = lp(noise(.18), 500) * env(.18, .001, .03) * 1.2
    sq = bp(noise(.18), 700, 2200) * env(.18, .003, .025) * .5
    return thud + sq + sweep(160, 70, .18, .04) * .5

def hit_hard(seed):
    rs.seed(seed)
    click = hp(noise(.5), 2500) * env(.5, 0, .004)
    ric = sweep(rs.uniform(2600, 3800), rs.uniform(1100, 1700), .5, .12) * .35
    return click + ric + metal(.5, [1900, 2700], .03) * .3

# Batch 105: impacts by material (Materials.Sound): wood splinters, tin rings, concrete chips, earth thuds
def hit_wood(seed):
    rs.seed(seed); sec = .3
    crack = hp(noise(sec), 1800) * env(sec, 0, .006)
    knock = lp(noise(sec), 900) * env(sec, .001, .03) * .9 + metal(sec, [220, 520, 980], .04) * .45
    splin = bp(noise(sec), 2500, 7000) * env(sec, .01, .03) * .3 * (rs.rand(int(SR * sec)) > .85)
    return crack + knock + splin

def hit_tin(seed):
    rs.seed(seed); sec = .5
    return hp(noise(sec), 2000) * env(sec, 0, .005) + metal(sec, [rs.uniform(700, 900), rs.uniform(1300, 1600), 2350, 3100], .08) * .7

def hit_concrete(seed):
    rs.seed(seed); sec = .35
    chip = bp(noise(sec), 1500, 6000) * env(sec, 0, .01)
    crumble = bp(noise(sec), 400, 2500) * env(sec, .01, .06) * .5 * (rs.rand(int(SR * sec)) > .7)
    return chip + crumble + lp(noise(sec), 250) * env(sec, .001, .02) * .6

def hit_dirt(seed):
    rs.seed(seed); sec = .25
    return lp(noise(sec), 700) * env(sec, .002, .04) + bp(noise(sec), 1200, 4000) * env(sec, .01, .05) * .3 * (rs.rand(int(SR * sec)) > .8)

def swing(seed):
    rs.seed(seed); x = t(.3)
    sh = np.sin(np.pi * np.clip(x / .3, 0, 1)) ** 2
    return bp(noise(.3), 500, 2500) * sh * .6

def claw(seed):
    rs.seed(seed)
    growl = lp(noise(.45) * (1 + np.sin(2 * np.pi * 34 * t(.45))), 600) * env(.45, .02, .15)
    return growl + bp(noise(.45), 1500, 4000) * env(.45, .1, .05) * .5

def psi():
    x = t(.7); f = 420 + 160 * np.sin(2 * np.pi * 7 * x)
    return np.sin(2 * np.pi * np.cumsum(f) / SR) * env(.7, .05, .3) + np.sin(2 * np.pi * np.cumsum(f * 1.5) / SR) * env(.7, .05, .2) * .5

def explosion(seed):
    rs.seed(seed)
    boom = lp(noise(2.2), 700) * env(2.2, .002, .35)
    sub = sweep(70, 28, 2.2, .5) * 1.4
    crack = hp(noise(2.2), 1200) * env(2.2, 0, .03)
    debris = bp(noise(2.2), 2000, 6000) * env(2.2, .2, .4) * .15 * (rs.rand(int(SR * 2.2)) > .995)
    return tail(boom + sub + crack + debris * 20, 1.5, 900, .6)

# ------------------------------------------------------------------ reloads, doors, steps, ui
def click(f=(2400, 3900, 6100), dec=.01, sec=.07):
    return metal(sec, list(f), dec) * .8 + hp(noise(sec), 2500) * env(sec, 0, .003)

def reload_mag():
    out = np.zeros(int(SR * .75))
    a = click((1800, 2900, 4400), .015); b = click((1500, 2600, 5200), .02, .1)
    out[:len(a)] += a; out[int(SR * .45):int(SR * .45) + len(b)] += b * 1.2
    return out

def reload_shell():
    out = np.zeros(int(SR * .3)); a = click((2100, 3300), .02); s = lp(noise(.06), 2500) * env(.06, 0, .01)
    out[:len(s)] += s * .6; out[int(SR * .08):int(SR * .08) + len(a)] += a
    return out

def reload_cyl():
    out = np.zeros(int(SR * .9))
    for k, at in enumerate((0, .12, .5, .62, .74)):
        c = click((2600 + k * 90, 4100), .012, .06); i = int(SR * at); out[i:i + len(c)] += c * (1.3 if k == 4 else .7)
    return out

def step(kind, seed):
    rs.seed(seed); sec = .16
    if kind == "dirt":
        x = lp(noise(sec), 900) * env(sec, .004, .035) + bp(noise(sec), 1500, 4000) * env(sec, .01, .02) * .25 * (rs.rand(int(SR * sec)) > .9)
    elif kind == "hard":
        x = bp(noise(sec), 800, 3500) * env(sec, 0, .012) + lp(noise(sec), 300) * env(sec, .002, .03) * .8
    elif kind == "grass":
        x = bp(noise(sec), 1200, 5000) * env(sec, .01, .04) * .5 + lp(noise(sec), 500) * env(sec, .003, .03) * .6
    elif kind == "wood":
        x = lp(noise(sec), 700) * env(sec, .001, .025) + metal(sec, [180, 410], .03) * .5
    else:   # water
        x = bp(noise(.3), 300, 2500) * env(.3, .01, .07) + sweep(900, 300, .3, .05) * .2
    return x

def door(seed):
    rs.seed(seed)
    creak = sum(np.sin(2 * np.pi * np.cumsum(np.full(int(SR * .7), f) * (1 + .02 * np.sin(2 * np.pi * 9 * t(.7)))) / SR) for f in (310, 470)) * env(.7, .05, .3) * .25
    latch = click((1300, 2200), .02, .1)
    out = np.zeros(int(SR * .8)); out[:len(latch)] += latch; out[:len(creak)] += creak * (1 + .5 * rs.rand(len(creak)))
    return lp(out, 3000)

def door_break(seed):
    rs.seed(seed)
    wood = lp(noise(.6), 1800) * env(.6, .001, .08)
    splinters = bp(noise(.6), 2000, 6000) * env(.6, .01, .12) * (rs.rand(int(SR * .6)) > .97) * 3
    return wood * 1.2 + splinters + sweep(120, 60, .6, .1)

def pickup(): return fit(bp(noise(.18), 600, 3000) * env(.18, .005, .03), click((2000, 3100), .01, .1) * .4)
def ui_click(): return metal(.05, [1600, 2500], .006) + hp(noise(.05), 2000) * env(.05, 0, .002) * .5
def hurt(seed):
    rs.seed(seed); return fit(hit_flesh(seed), lp(noise(.3), 300) * env(.3, .01, .06) * .6)

# ------------------------------------------------------------------ ambience (seamless loops)
def loopify(x, xf=1.5):
    n = int(SR * xf); a = x[:n]; b = x[-n:]; w = np.linspace(0, 1, n)
    body = x[n:-n].copy() if len(x) > 2 * n else x
    return np.concatenate([b * (1 - w) + a * w, body])

def wind(sec=14):
    rs.seed(11); x = t(sec)
    brown = np.cumsum(rs.uniform(-1, 1, len(x))); brown = hp(brown, 20); brown /= np.abs(brown).max()
    gust = .55 + .45 * np.sin(2 * np.pi * x / 6.3) * np.sin(2 * np.pi * x / 3.1 + 1)
    w = lp(brown, 500) * gust + bp(noise(sec), 400, 1200) * (gust ** 3) * .2
    birds = np.zeros(len(x))
    for at in (2.1, 2.35, 7.8, 8.0, 8.2, 11.5):
        c = np.sin(2 * np.pi * np.cumsum(3100 + 900 * np.sin(2 * np.pi * 14 * t(.12))) / SR) * env(.12, .01, .04) * .05
        i = int(SR * at); birds[i:i + len(c)] += c
    return loopify(w + birds)

def hum(sec=10):
    x = t(sec); h = sum(np.sin(2 * np.pi * 50 * k * x) / k for k in (1, 2, 3, 5)) * .5
    return loopify(h * (.85 + .15 * np.sin(2 * np.pi * x / 5)) + lp(noise(sec), 2000) * .08)

def cave(sec=12):
    rs.seed(5); x = t(sec); base = lp(noise(sec), 250) * .35
    for at in rs.uniform(.5, sec - 1, 9):
        d = sweep(rs.uniform(1100, 1900), rs.uniform(500, 800), .25, .05) * .25; i = int(SR * at); base[i:i + len(d)] += d
    return loopify(tail(base, 1.2, 1500, .5)[:len(x)])

def marsh(sec=14):
    rs.seed(9); x = t(sec); base = lp(noise(sec), 600) * .25
    for at in rs.uniform(.5, sec - 1, 10):                            # frogs / bubbles
        f = rs.uniform(180, 320); c = np.sin(2 * np.pi * f * t(.15)) * env(.15, .005, .04) * (1 + np.sign(np.sin(2 * np.pi * 30 * t(.15)))) * .15
        i = int(SR * at); base[i:i + len(c)] += c
    return loopify(base)

def rain(sec=12, heavy=False):
    rs.seed(21 if heavy else 20); x = t(sec)
    hiss = bp(noise(sec), 500, 7000) * (0.55 if heavy else 0.35)
    drops = np.zeros(len(x))
    for at in rs.uniform(0, sec - .05, 2600 if heavy else 1200):            # individual drops ticking on leaves and tin
        d = bp(noise(.02), 1500, 6000) * env(.02, 0, .004) * rs.uniform(.1, .5); i = int(SR * at); drops[i:i + len(d)] += d
    out = hiss + drops
    if heavy: out = out + lp(noise(sec), 180) * (0.6 + 0.4 * np.sin(2 * np.pi * x / 5.3)) * 0.8   # wind roar
    return loopify(out)

def thunder(seed):
    rs.seed(seed); sec = 4.5
    crack = hp(noise(sec), 900) * env(sec, 0, .08) * .8
    roll = lp(noise(sec), 160) * env(sec, .05, 1.3) * (1 + .6 * np.sin(2 * np.pi * 1.7 * t(sec)) * np.sin(2 * np.pi * .6 * t(sec)))
    return tail(crack + roll * 2, 1.5, 400, .5)

def main(d):
    os.makedirs(d, exist_ok=True)
    for name, p in GUNS.items():
        for v in range(2):
            save(d, f"shot_{name}_{v}", railgun(50 + v) if p is None else gunshot(*p, seed=100 + v * 13 + len(name)))
    save(d, "dryfire", dryfire(), .5)
    for v in range(3): save(d, f"hit_flesh_{v}", hit_flesh(200 + v), .7); save(d, f"hit_hard_{v}", hit_hard(300 + v), .5)
    for v in range(2): save(d, f"swing_{v}", swing(400 + v), .5); save(d, f"claw_{v}", claw(410 + v), .7)
    for v in range(2):
        save(d, f"hit_wood_{v}", hit_wood(320 + v), .55); save(d, f"hit_tin_{v}", hit_tin(330 + v), .45)
        save(d, f"hit_concrete_{v}", hit_concrete(340 + v), .5); save(d, f"hit_dirt_{v}", hit_dirt(350 + v), .5)
    save(d, "psi", psi(), .5)
    for v in range(2): save(d, f"explosion_{v}", explosion(500 + v), .95)
    save(d, "reload_mag", reload_mag(), .55); save(d, "reload_shell", reload_shell(), .55); save(d, "reload_cylinder", reload_cyl(), .55)
    for kind in ("dirt", "hard", "grass", "wood", "water"):
        for v in range(4): save(d, f"step_{kind}_{v}", step(kind, 600 + v * 7 + len(kind)), .35)
    save(d, "door_open", door(700), .5); save(d, "door_break", door_break(701), .8)
    save(d, "pickup", pickup(), .5); save(d, "ui_click", ui_click(), .35)
    for v in range(2): save(d, f"hurt_{v}", hurt(800 + v), .75)
    save(d, "amb_rain", rain(), .35); save(d, "amb_storm", rain(heavy=True), .45)
    for v in range(2): save(d, f"thunder_{v}", thunder(900 + v), .9)
    save(d, "amb_wind", wind(), .35); save(d, "amb_hum", hum(), .25); save(d, "amb_cave", cave(), .3); save(d, "amb_marsh", marsh(), .3)
    print(len(os.listdir(d)), "sounds in", d)

if __name__ == "__main__": main(sys.argv[1] if len(sys.argv) > 1 else "/tmp/claude-0/audio_out")
