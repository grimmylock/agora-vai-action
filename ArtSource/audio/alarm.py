# The Hatch Tunnels' alarm siren (Tier 2 "Taken", 2026-11-16): a two-tone wail with a corridor echo, a seamless 4 s loop -> Assets/Resources/Audio/amb_alarm.wav (played as the ambient by Tier2Story, "amb_*" like the other loops).
# Standalone so it never rewrites the rest of the sounds: python3 ArtSource/audio/alarm.py
import numpy as np, wave, os
SR = 44100; dur = 4.0; t = np.arange(int(SR * dur)) / SR
lfo = np.abs(((t / 2.0) % 2) - 1); f = 620 + 520 * lfo; ph = 2 * np.pi * np.cumsum(f) / SR
x = np.tanh((np.sin(ph) + 0.5 * np.sin(2 * ph) + 0.25 * np.sin(3 * ph) + 0.12 * np.sin(5 * ph)) * 1.6)
d = int(SR * 0.18); y = x.copy(); y[d:] += 0.35 * x[:-d]; d2 = int(SR * 0.37); y[d2:] += 0.18 * x[:-d2]; y = y / np.abs(y).max() * 0.7
n = int(SR * 0.05); y[:n] = y[:n] * np.linspace(0, 1, n) + y[-n:] * np.linspace(1, 0, n); y = y[:-n]
out = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'Assets', 'Resources', 'Audio', 'amb_alarm.wav')
with wave.open(out, 'wb') as w: w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes((y * 32767).astype('<i2').tobytes())
