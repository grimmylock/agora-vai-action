#!/usr/bin/env python3
"""The cave ambience (Yan, 2026-11-17: "and this to the cave ambience"): a long ambience recording cut into one SEAMLESS loop for Assets/Resources/Audio/amb_mine.ogg.
Source: "AMBIENCE | Scary Haunted Mine | For study, chill, sleep" by Max "stallfish" Denton (10:26, 320 kbps mp3; Yan supplied the file; see CREDITS_AUDIO.md).
The loop: take [START, START+LOOP+XFADE]; the last XFADE seconds (the natural continuation of the loop's end) are cross-faded into its first XFADE seconds (equal power), so the end flows into the start
with nothing audible at the seam. Levelled to TARGET_LUFS (the old cave bed measured -27.6 LUFS, and the game's ambient volume was tuned for it), encoded as Ogg Vorbis (gapless in Unity; mp3 is not).
   python3 ArtSource/audio/make_mine_ambience.py <source.mp3>"""
import os, re, subprocess, sys, tempfile, wave
import numpy as np
START, LOOP, XFADE, TARGET_LUFS = 20.0, 330.0, 8.0, -27.0
SR = 44100
HERE = os.path.dirname(os.path.abspath(__file__)); OUT = os.path.join(HERE, '..', '..', 'Assets', 'Resources', 'Audio', 'amb_mine.ogg')
def run(*a): return subprocess.run(a, capture_output=True, text=True)
def main(src):
    tmp = tempfile.mkdtemp(); full = os.path.join(tmp, 'full.wav'); loop = os.path.join(tmp, 'loop.wav')
    run('ffmpeg', '-v', 'error', '-y', '-ss', str(START), '-t', str(LOOP + XFADE), '-i', src, '-ac', '2', '-ar', str(SR), '-c:a', 'pcm_s16le', full)
    w = wave.open(full); n = w.getnframes(); a = np.frombuffer(w.readframes(n), dtype=np.int16).astype(np.float64).reshape(-1, 2) / 32768.0
    L, X = int(LOOP * SR), int(XFADE * SR); assert len(a) >= L + X, 'source too short'
    head, tail = a[:X], a[L:L + X]; t = np.linspace(0.0, np.pi / 2, X)[:, None]
    out = a[:L].copy(); out[:X] = tail * np.cos(t) + head * np.sin(t)                   # equal power: cos^2 + sin^2 = 1
    pcm = (np.clip(out, -1, 1) * 32767).astype(np.int16)
    with wave.open(loop, 'wb') as o: o.setnchannels(2); o.setsampwidth(2); o.setframerate(SR); o.writeframes(pcm.tobytes())
    m = re.findall(r'I:\s+(-?[\d.]+) LUFS', run('ffmpeg', '-hide_banner', '-i', loop, '-af', 'ebur128=peak=true', '-f', 'null', '-').stderr)
    gain = TARGET_LUFS - float(m[-1]); print('loop %.1f s, measured %.1f LUFS, gain %+.1f dB' % (L / SR, float(m[-1]), gain))
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    r = run('ffmpeg', '-v', 'error', '-y', '-i', loop, '-af', 'volume=%.2fdB' % gain, '-c:a', 'libvorbis', '-q:a', '3', '-metadata', 'title=amb_mine (loop from "AMBIENCE | Scary Haunted Mine" by Max "stallfish" Denton)', OUT)
    print(r.stderr.strip() or 'ok', os.path.getsize(OUT), 'bytes ->', os.path.normpath(OUT))
    # the seam: the jump from the last sample to the first, next to a typical sample-to-sample step
    b = out; seam = np.abs(b[0] - b[-1]).max(); typ = np.median(np.abs(np.diff(b, axis=0)).max(axis=1)); print('seam step %.5f vs typical step %.5f' % (seam, typ))
if __name__ == '__main__': main(sys.argv[1])
