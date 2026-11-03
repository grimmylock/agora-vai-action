"""Real gunfire, reloads, explosions and the parry clang (Yan, 2026-11-03: "search opengameart for gunshots"). CC0 only:
"The Free Firearm Sound Library" (Ben Jaszczak et al.: 22 real guns at 96 kHz, two to six takes each, every take a string of
shots) and small CC0 packs (licences checked on each page; CREDITS_AUDIO.md). The game's file names and gun names stay its own.
A gun take holds several shots: Shot() finds where a shot starts (the loudness jumps out of the quiet) and keeps the bang and its
tail, up to the next shot or the type's length. Then the same treatment as import_cc0.py: 16-bit mono 44.1 kHz, a short fade,
loudness matched to the generated sound it replaces. The cut is kept in ArtSource/audio/cc0/guns/ so this reruns without the
library.
    python3 ArtSource/audio/import_guns.py <library dir> <small packs dir>   (or no args: rebuild from ArtSource/audio/cc0/guns)"""
import os, sys, glob, subprocess, shutil
import numpy as np
HERE = os.path.dirname(os.path.abspath(__file__))
KEEP = os.path.join(HERE, "cc0", "guns")
OUT = os.path.join(HERE, "..", "..", "Assets", "Resources", "Audio")
sys.path.insert(0, HERE)
from import_cc0 import level_gain

# target -> (library folder, take index, shot index in the take, seconds kept); an SMG's shots are 60-90 ms apart (full auto)
# Yan: "the sounds cut off before the echoes are done": every shot (99) is the one in its take with the most room after it - no
# shot right behind it, so its echo runs out on its own - kept until the echo dies away (DECAY_DB) with a generous cap per type.
SHOTS = {
    "shot_pistol_0": ("Walther PPQ", 0, 99, 1.8), "shot_pistol_1": ("1911", 0, 99, 1.8),
    "shot_smg_0": ("PPSh", 0, 99, 1.2), "shot_smg_1": ("PPSh", 1, 99, 1.2),
    "shot_rifle_0": ("AK-47", 0, 99, 2.0), "shot_rifle_1": ("AK-47", 1, 99, 2.0),
    "shot_shotgun_0": ("Mossberg", 0, 99, 2.4), "shot_shotgun_1": ("Nova", 0, 99, 2.4),
    "shot_sniper_0": ("Mosin Nagant", 0, 99, 3.0), "shot_sniper_1": ("Tikka", 0, 99, 3.0),
    "shot_heavy_0": ("Marlin 336", 0, 99, 2.6), "shot_heavy_1": ("Savage 10 .300 Blackout", 0, 99, 2.6),
}
DECAY_DB = -60.0          # the echo is over once it stays this far under the shot's peak
# target -> (file name in the small packs, seconds kept)
OTHERS = {
    "reload_mag": ("assaultriflereload1_0.wav", 2.0), "reload_shell": ("Subsequent Shells.mp3", 0.7), "reload_cylinder": ("singlebullet1.wav", 0.6),
    "explosion_0": ("explosion.wav", 2.5), "explosion_1": ("cannon_01.ogg", 2.5),
    "parry_0": ("sword_clash.1.ogg", 0.8), "parry_1": ("sword_clash.2.ogg", 0.8), "parry_perfect": ("sword_clash.3.ogg", 1.2),
}
SR = 44100

def load(path):
    raw = subprocess.run(["ffmpeg", "-v", "error", "-i", path, "-ac", "1", "-ar", str(SR), "-f", "f32le", "-"], capture_output=True, check=True).stdout
    return np.frombuffer(raw, dtype=np.float32)

def save(x, path):
    x = np.clip(x, -1, 1); os.makedirs(os.path.dirname(path), exist_ok=True)
    subprocess.run(["ffmpeg", "-v", "error", "-y", "-f", "f32le", "-ar", str(SR), "-ac", "1", "-i", "-", "-c:a", "pcm_s16le", path],
                   input=x.astype(np.float32).tobytes(), check=True)

def onsets(x, gap):
    """Where each shot starts: the 5 ms envelope jumping to half the take's peak out of a quieter 30 ms before it, at least `gap`
    s after the last one (a bang's own echoes and the action's clatter aren't new shots)."""
    hop = int(SR * 0.005); env = np.array([np.abs(x[i:i + hop]).max() for i in range(0, len(x) - hop, hop)])
    peak = env.max(); found, last = [], -10**9
    for i in range(6, len(env)):
        if env[i] > 0.5 * peak and env[max(0, i - 6):i].mean() < 0.35 * env[i] and (i - last) * hop > gap * SR:
            found.append(i * hop); last = i
    return found

def shot(x, idx, keep, gap):
    on = onsets(x, gap)
    if not on: return x[:int(keep * SR)]
    if idx == 99:                                   # the shot with the most room after it - its echo is the longest one left alone
        gaps = [((on[i + 1] if i + 1 < len(on) else len(x)) - on[i], i) for i in range(len(on))]
        idx = max(gaps)[1]
    s = max(0, on[min(idx, len(on) - 1)] - int(0.004 * SR))
    nxt = [o for o in on if o > s + int(gap * SR)]
    e = min(s + int(keep * SR), (nxt[0] - int(0.004 * SR)) if nxt else len(x))
    y = x[s:e].copy()
    loud = np.nonzero(np.abs(y) > np.abs(y).max() * 10 ** (DECAY_DB / 20))[0]        # where the echo is still above the floor
    if len(loud): y = y[:min(len(y), loud[-1] + int(0.05 * SR))]
    f = min(len(y), int(0.12 * SR)); y[-f:] *= np.linspace(1, 0, f) ** 2              # a gentle fade over the last of it
    return y

def finish(src_wav, target, cap=None):
    dst = os.path.join(OUT, target + ".wav"); old = dst + ".old.wav"
    if os.path.exists(dst): shutil.copy(dst, old)
    x = load(src_wav)
    if cap: x = x[:int(cap * SR)]
    nz = np.nonzero(np.abs(x) > 0.02 * (np.abs(x).max() + 1e-9))[0]
    if len(nz): x = x[max(0, nz[0] - int(0.003 * SR)):]                                      # trim the lead-in
    x = x.copy()
    if not target.startswith("shot_"): f = min(len(x), int(0.04 * SR)); x[-f:] *= np.linspace(1, 0, f)   # (shots already fade out with their echo)
    tmp = dst + ".tmp.wav"; save(x, tmp)
    if os.path.exists(old):
        g = max(-20.0, min(20.0, level_gain(tmp, old)))
        subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", tmp, "-af", f"volume={g}dB", "-ac", "1", "-ar", str(SR), "-c:a", "pcm_s16le", dst], check=True)
        os.remove(tmp); os.remove(old)
    else: os.replace(tmp, dst)

if __name__ == "__main__":
    os.makedirs(KEEP, exist_ok=True)
    if len(sys.argv) > 2:
        lib, packs = sys.argv[1], sys.argv[2]
        for t, (folder, take, idx, keep) in SHOTS.items():
            takes = sorted(glob.glob(os.path.join(lib, folder, "*.wav")))
            if not takes: print("missing", folder); continue
            save(shot(load(takes[min(take, len(takes) - 1)]), idx, keep, 0.05 if t.startswith("shot_smg") else 0.3), os.path.join(KEEP, t + ".wav"))
        for t, (name, keep) in OTHERS.items():
            hits = [p for p in glob.glob(os.path.join(packs, "**", name), recursive=True) if "__MACOSX" not in p]
            if not hits: print("missing", name); continue
            save(load(hits[0])[:int(keep * SR)], os.path.join(KEEP, t + ".wav"))
    n = 0
    for t in list(SHOTS) + list(OTHERS):
        src = os.path.join(KEEP, t + ".wav")
        if os.path.exists(src): finish(src, t); n += 1
    print(n, "sounds replaced")
