# The Gaunt's scream (2026-11-16): a wendigo's shriek, synthesised (no samples, nothing to license): a harmonic source gliding up from a low moan to a tearing shriek and falling back, with a ragged
# vibrato, vowel formants that open as it climbs, a breath-noise layer, a grit stage and a corridor tail. Two variants -> Assets/Resources/Audio/gaunt_scream_0.wav, _1.wav (Sfx.Play picks one).
# Standalone so the other sounds are untouched:  python3 ArtSource/audio/gaunt_scream.py
import numpy as np, wave, os
from scipy import signal
SR = 44100
def bp(x, lo, hi, o=2): b, a = signal.butter(o, [lo / (SR / 2), min(hi / (SR / 2), .99)], "band"); return signal.lfilter(b, a, x)
def lp(x, f, o=2): b, a = signal.butter(o, min(f / (SR / 2), .99), "low"); return signal.lfilter(b, a, x)
def scream(seed, dur=1.9):
    rs = np.random.RandomState(seed); n = int(SR * dur); t = np.arange(n) / SR; u = t / dur
    glide = np.where(u < 0.35, 150 + 520 * (u / 0.35) ** 1.5, np.where(u < 0.75, 670 + 40 * np.sin(u * 30), 670 - 420 * ((u - 0.75) / 0.25) ** 1.2))
    vib = 1 + 0.035 * np.sin(2 * np.pi * (5.5 + 2 * u) * t) + 0.02 * lp(rs.randn(n), 40, 1) * 8                # a ragged wobble
    f = glide * vib; ph = 2 * np.pi * np.cumsum(f) / SR
    src = sum(np.sin(k * ph) / k ** 0.8 for k in range(1, 18))                                              # a bright, buzzy harmonic stack
    breath = rs.randn(n) * (0.35 + 0.4 * np.sin(np.pi * np.clip(u, 0, 1)))
    vowel_a = bp(src, 600, 1100) + 0.6 * bp(src, 1800, 3000) + 0.35 * bp(src, 3200, 5200)               # "aah" formants...
    vowel_e = bp(src, 350, 700) + 0.9 * bp(src, 2200, 3200) + 0.5 * bp(src, 3600, 5600)                 # ...opening to "eee" as it shrieks
    mix = np.clip((u - 0.2) / 0.4, 0, 1); y = vowel_a * (1 - mix) + vowel_e * mix + 0.25 * bp(breath, 1500, 7000)
    env = np.clip(u / 0.04, 0, 1) * np.where(u < 0.8, 1.0, np.clip((1 - u) / 0.2, 0, 1) ** 1.3) * (0.82 + 0.18 * np.sin(2 * np.pi * 9 * t))
    y = np.tanh(y / (np.abs(y).max() + 1e-9) * 2.6 * env)                                                    # grit
    ir = lp(rs.randn(int(SR * 0.8)), 2200) * np.exp(-np.arange(int(SR * 0.8)) / (SR * 0.16)); y = np.convolve(y, ir)[: n + int(SR * 0.6)] * 0.02 + np.pad(y, (0, int(SR * 0.6)))
    return y / np.abs(y).max() * 0.85
out = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'Assets', 'Resources', 'Audio')
for i, sd in enumerate((11, 23)):
    y = scream(sd)
    with wave.open(os.path.join(out, f'gaunt_scream_{i}.wav'), 'wb') as w: w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes((y * 32767).astype('<i2').tobytes())
    print(f'gaunt_scream_{i}.wav', round(len(y) / SR, 2), 's')
