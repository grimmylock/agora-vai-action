"""Real recordings for the game's sounds (Yan, 2026-11-03: "we'll need better sound effects ... start with what you can do by
yourself"). CC0 packs from OpenGameArt (licences checked on each pack's page, see CREDITS_AUDIO.md); the gunshots, reloads,
explosions and creatures stay procedural (ArtSource/audio/sfx.py) until Yan brings the Sonniss bundles.
Each target keeps its file name (Sfx.cs plays by name), converted to the game's format (16-bit mono 44.1 kHz WAV): leading
silence trimmed, one-shots capped, a short fade out, and its loudness matched to the sound it replaces so the mix stays where it
was. Ambience loops get a crossfaded seam.
    python3 ArtSource/audio/import_cc0.py [packs dir]   - with a packs dir: copy the chosen sources into ArtSource/audio/cc0 first
"""
import os, sys, glob, shutil, subprocess, json
HERE = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.join(HERE, "cc0")
CCBY = os.path.join(HERE, "ccby")         # CC-BY sources (credited in CREDITS_AUDIO.md and the in-game Credits screen)
OUT = os.path.join(HERE, "..", "..", "Assets", "Resources", "Audio")

# target -> (source file name, max seconds or "loop"); variations _0.._n of one sound come from different takes
MAP = {
    # footsteps (Sfx.Step: step_<ground>_<n>)
    "step_hard_0": ("Fantozzi-StoneL1.ogg", 0.35), "step_hard_1": ("Fantozzi-StoneR1.ogg", 0.35),
    "step_hard_2": ("Fantozzi-StoneL2.ogg", 0.35), "step_hard_3": ("Fantozzi-StoneR2.ogg", 0.35),
    "step_dirt_0": ("Fantozzi-SandL1.ogg", 0.35), "step_dirt_1": ("Fantozzi-SandR1.ogg", 0.35),
    "step_dirt_2": ("Fantozzi-SandL2.ogg", 0.35), "step_dirt_3": ("Fantozzi-SandR2.ogg", 0.35),
    "step_grass_0": ("footstep00.ogg", 0.35), "step_grass_1": ("footstep01.ogg", 0.35),
    "step_grass_2": ("footstep02.ogg", 0.35), "step_grass_3": ("footstep03.ogg", 0.35),
    "step_wood_0": ("sfx100v2_footstep_wood_01.ogg", 0.35), "step_wood_1": ("sfx100v2_footstep_wood_02.ogg", 0.35),
    "step_wood_2": ("sfx100v2_footstep_wood_03.ogg", 0.35), "step_wood_3": ("sfx100v2_footstep_wood_04.ogg", 0.35),
    "step_water_0": ("sfx100v2_footstep_wet_01.ogg", 0.4), "step_water_1": ("sfx100v2_footstep_wet_02.ogg", 0.4),
    "step_water_2": ("sfx100v2_footstep_wet_03.ogg", 0.4), "step_water_3": ("splash_02.ogg", 0.45),
    # bullet and blade impacts by material (Materials.cs)
    "hit_wood_0": ("impactwood01.mp3.flac", 0.5), "hit_wood_1": ("impactwood02.mp3.flac", 0.5),
    "hit_tin_0": ("bfh1_metal_hit_01.ogg", 0.5), "hit_tin_1": ("bfh1_metal_hit_02.ogg", 0.5),
    "hit_concrete_0": ("bfh1_rock_hit_01.ogg", 0.4), "hit_concrete_1": ("qubodupImpactStone.ogg", 0.4),
    "hit_hard_0": ("sfx100v2_metal_hit_01.ogg", 0.6), "hit_hard_1": ("sfx100v2_metal_hit_02.ogg", 0.6), "hit_hard_2": ("bfh1_metal_hit_03.ogg", 0.6),
    "hit_flesh_0": ("qubodupImpactMeat01.ogg", 0.4), "hit_flesh_1": ("qubodupImpactMeat02.ogg", 0.4), "hit_flesh_2": ("knifeSlice.ogg", 0.4),
    # the world
    "door_open": ("qubodup-DoorOpen01.ogg", 1.2), "door_break": ("bfh1_wood_breaking_01.ogg", 1.0),
    "swing_0": ("swish-1.wav", 0.4), "swing_1": ("swish-3.wav", 0.4),
    "ui_click": ("click_001.ogg", 0.15), "pickup": ("handleSmallLeather.ogg", 0.5),
    "smoke": ("sfx100v2_air_01.ogg", 2.0),
    # CC-BY (Yan, 2026-11-03: a credits screen is fine): the player's pain grunts, a body thud, a real dry-fire click
    "hurt_0": ("pain_jack_01.wav", 0.6), "hurt_1": ("pain_jack_02.wav", 0.6), "hurt_2": ("pain_jack_03.wav", 0.8),
    "hit_flesh_3": ("bodyimpact_jack_01.wav", 0.4), "dryfire": ("Dry Fire Gun-SoundBible.com-2053652037.wav", 0.4),
    # ambience loops (Sfx.Ambient)
    "amb_rain": ("1.ogg", "loop"), "amb_storm": ("rain-thunder.ogg", "loop"),  # amb_wind stays generated: the only CC0 wind here is a 5 s gust (a short loop repeats)
    "amb_cave": ("dungeon_ambient_1_0.ogg", "loop"), "amb_marsh": ("crickets_1.mp3", "loop"),
}
LOOP_MAX, XFADE = 24.0, 0.8

def find(root, name):
    hits = [p for p in glob.glob(os.path.join(root, "**", name), recursive=True) if "__MACOSX" not in p]
    return hits[0] if hits else None

def _vol(path, key):
    out = subprocess.run(["ffmpeg", "-v", "info", "-i", path, "-af", "volumedetect", "-f", "null", "-"], capture_output=True, text=True).stderr
    for line in out.splitlines():
        if key in line: return float(line.split(":")[1].replace("dB", ""))
    return None
def rms_db(path): return _vol(path, "mean_volume")
def peak_db(path): return _vol(path, "max_volume")
def level_gain(src, old):
    """dB to bring src toward the loudness (mean) of the sound it replaces - but only as far as its peak can go without touching
    the ceiling: a long real recording matched on its average would be squashed flat by the limiter (a gunshot loses its crack)."""
    g = rms_db(old) - rms_db(src)
    return min(g, -0.5 - peak_db(src))

def duration(path):
    return float(subprocess.run(["ffprobe", "-v", "error", "-show_entries", "format=duration", "-of", "csv=p=0", path], capture_output=True, text=True).stdout.strip() or 0)

def convert(src, dst, cap, old=None):
    tmp = dst + ".tmp.wav"
    L = min(LOOP_MAX, duration(src) - XFADE - 0.1)            # a loop: its own length (minus the seam), at most LOOP_MAX
    af = ["aformat=sample_fmts=s16:sample_rates=44100:channel_layouts=mono",
          "silenceremove=start_periods=1:start_threshold=-45dB"]
    if cap != "loop": af += [f"atrim=0:{cap}", f"afade=t=out:st={max(0.0, cap - 0.04)}:d=0.04"]
    else: af += [f"atrim=0:{L + XFADE}"]
    subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", src, "-af", ",".join(af), "-ac", "1", "-ar", "44100", tmp], check=True)
    if cap == "loop":                       # fold the tail over the head so the end runs into the start
        a = tmp + ".a.wav"
        subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", tmp, "-i", tmp, "-filter_complex",
                        f"[0]atrim=0:{XFADE},asetpts=PTS-STARTPTS[h];[1]atrim={L}:{L + XFADE},asetpts=PTS-STARTPTS[t];"
                        f"[t][h]acrossfade=d={XFADE}:c1=tri:c2=tri[x];[0]atrim={XFADE}:{L},asetpts=PTS-STARTPTS[m];[m][x]concat=n=2:v=0:a=1",
                        a], check=True)
        os.replace(a, tmp)
    if old is not None:
        g = max(-20.0, min(20.0, level_gain(tmp, old)))
        subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", tmp, "-af", f"volume={g}dB", "-ac", "1", "-ar", "44100", "-c:a", "pcm_s16le", dst], check=True)
        os.remove(tmp); return
    subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", tmp, "-c:a", "pcm_s16le", dst], check=True); os.remove(tmp)

if __name__ == "__main__":
    os.makedirs(SRC, exist_ok=True)
    if len(sys.argv) > 1:                   # pick the sources out of the downloaded packs
        for t, (name, _) in MAP.items():
            p = find(sys.argv[1], name)
            if p is None: print("not found:", name); continue
            shutil.copy(p, os.path.join(SRC, name))
    done = {}
    for t, (name, cap) in MAP.items():
        src = os.path.join(SRC, name)
        if not os.path.exists(src): src = os.path.join(CCBY, name)
        if not os.path.exists(src): print("skip (no source):", t); continue
        dst = os.path.join(OUT, t + ".wav")
        old = dst + ".old.wav"
        ref = dst if os.path.exists(dst) else os.path.join(OUT, t.rsplit("_", 1)[0] + "_0.wav")   # a new variant levels like its first
        if os.path.exists(ref): shutil.copy(ref, old)
        convert(src, dst, cap, old if os.path.exists(old) else None)
        if os.path.exists(old): os.remove(old)
        done[t] = name
    print(f"{len(done)} sounds replaced")
