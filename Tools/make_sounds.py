"""
Kara Pati sound generator.
Synthesises every sound effect and the background music from scratch (sine waves, noise, envelopes)
and writes 16-bit mono WAV files into Assets/Resources/Audio.  Run:  python Tools/make_sounds.py
Replace any WAV with a real recording of the same name later if you like.
"""
import os
import numpy as np
from scipy.io import wavfile
from scipy.signal import butter, lfilter

SR = 44100
OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "Resources", "Audio")
rng = np.random.default_rng(7)


def t_axis(dur):
    return np.arange(int(SR * dur)) / SR


def env(n, attack=0.005, release=0.1, hold=0.0, curve=3.0):
    """Attack-hold-exponential release envelope, n samples long."""
    t = np.arange(n) / SR
    e = np.ones(n)
    a = max(1, int(attack * SR))
    e[:a] = np.linspace(0, 1, a)
    start = a + int(hold * SR)
    if start < n:
        rel_t = t[start:] - t[start]
        e[start:] = np.exp(-curve * rel_t / max(release, 1e-4))
    return e


def tone(freq, dur, kind="sine", phase=0.0):
    """freq may be a number or an array (for glides)."""
    t = t_axis(dur)
    f = np.broadcast_to(np.asarray(freq, dtype=float), t.shape)
    ph = 2 * np.pi * np.cumsum(f) / SR + phase
    if kind == "sine":
        return np.sin(ph)
    if kind == "tri":
        return 2 / np.pi * np.arcsin(np.sin(ph))
    if kind == "square":
        return np.tanh(np.sin(ph) * 4)
    raise ValueError(kind)


def bell(freq, dur, bright=0.5):
    """Music-box / glockenspiel tone: fundamental + inharmonic partials, fast decay."""
    n = int(SR * dur)
    s = tone(freq, dur) * env(n, 0.002, dur * 0.6)
    s += bright * 0.5 * tone(freq * 2.76, dur) * env(n, 0.001, dur * 0.25)
    s += bright * 0.25 * tone(freq * 5.4, dur) * env(n, 0.001, dur * 0.12)
    return s


def noise(dur):
    return rng.uniform(-1, 1, int(SR * dur))


def lowpass(x, cutoff, order=2):
    b, a = butter(order, cutoff / (SR / 2), btype="low")
    return lfilter(b, a, x)


def bandpass(x, lo, hi, order=2):
    b, a = butter(order, [lo / (SR / 2), hi / (SR / 2)], btype="band")
    return lfilter(b, a, x)


def mix(*parts):
    n = max(len(p) for p in parts)
    out = np.zeros(n)
    for p in parts:
        out[: len(p)] += p
    return out


def at(sig, start, total):
    """Place sig at `start` seconds inside a buffer of `total` seconds."""
    out = np.zeros(int(SR * total))
    i = int(SR * start)
    j = min(len(out), i + len(sig))
    out[i:j] += sig[: j - i]
    return out


def note(name):
    names = {"C": -9, "C#": -8, "D": -7, "D#": -6, "E": -5, "F": -4, "F#": -3, "G": -2, "G#": -1, "A": 0, "A#": 1, "B": 2}
    pitch, octave = name[:-1], int(name[-1])
    return 440.0 * 2 ** ((names[pitch] + (octave - 4) * 12) / 12)


def save(name, x, peak=0.8, fade=True):
    x = np.asarray(x, dtype=float)
    # tiny fade in/out so files never click
    f = min(len(x) // 4, int(0.004 * SR)) if fade else 0
    if f > 0:
        x[:f] *= np.linspace(0, 1, f)
        x[-f:] *= np.linspace(1, 0, f)
    m = np.max(np.abs(x)) or 1.0
    x = x / m * peak
    os.makedirs(OUT, exist_ok=True)
    wavfile.write(os.path.join(OUT, name + ".wav"), SR, (x * 32767).astype(np.int16))
    print(f"{name:10s} {len(x) / SR:5.2f}s")


# ------------------------------------------------------------------ effects
def sfx_pop():
    # bubbly "blup": fast upward pitch glide with a soft body
    d = 0.16
    n = int(SR * d)
    f = np.linspace(380, 900, n) ** 1.0
    body = tone(f, d) * env(n, 0.002, 0.08)
    click = lowpass(noise(0.02), 3000) * env(int(SR * 0.02), 0.0005, 0.01)
    return mix(body, 0.3 * click)


def sfx_spawn():
    # soft low "plop" when the cauldron makes an ingredient
    d = 0.18
    n = int(SR * d)
    f = np.linspace(520, 220, n)
    return tone(f, d) * env(n, 0.002, 0.07) + 0.15 * tone(f * 2, d) * env(n, 0.001, 0.04)


def sfx_coin():
    # two-note chime B5 -> E6
    a = bell(note("B5"), 0.35, 0.8)
    b = bell(note("E6"), 0.6, 0.8)
    return mix(at(a, 0, 0.8), at(b, 0.07, 0.8))


def sfx_stamp():
    # paw-stamp: muffled thump + little sparkle
    d = 0.25
    n = int(SR * d)
    thump = lowpass(noise(d), 300) * env(n, 0.001, 0.06) * 3
    thump += tone(np.linspace(160, 70, n), d) * env(n, 0.001, 0.08)
    spark = bell(note("A6"), 0.3, 0.6) * 0.35
    return mix(thump, at(spark, 0.05, 0.4))


def sfx_brew():
    # cauldron starts bubbling: a burst of random water blips over a low rumble
    total = 1.4
    out = lowpass(noise(total), 180) * env(int(SR * total), 0.2, 0.9, hold=0.2) * 1.5
    for _ in range(22):
        s = rng.uniform(0, total - 0.15)
        f0 = rng.uniform(250, 600)
        d = rng.uniform(0.04, 0.09)
        n = int(SR * d)
        blip = tone(np.linspace(f0, f0 * 2.2, n), d) * env(n, 0.002, d * 0.5) * rng.uniform(0.3, 0.8)
        out += at(blip, s, total)
    return out


def sfx_ready():
    # magic sparkle: quick rising arpeggio of bells
    notes = ["E5", "G#5", "B5", "E6", "G#6"]
    total = 1.0
    out = np.zeros(int(SR * total))
    for i, nm in enumerate(notes):
        out += at(bell(note(nm), 0.5, 0.9) * (0.7 + 0.06 * i), i * 0.06, total)
    shimmer = bandpass(noise(total), 5000, 12000) * env(int(SR * total), 0.05, 0.4, hold=0.1) * 0.15
    return out + shimmer


def sfx_gift():
    # chest opening fanfare: C major arpeggio + chord
    total = 1.3
    out = np.zeros(int(SR * total))
    for i, nm in enumerate(["C5", "E5", "G5", "C6"]):
        out += at(bell(note(nm), 0.45, 0.7), i * 0.08, total)
    chord = sum(tone(note(nm), 0.9, "tri") for nm in ["C5", "E5", "G5"]) * env(int(SR * 0.9), 0.01, 0.5, hold=0.15) * 0.25
    return out + at(chord, 0.32, total)


def sfx_levelup():
    total = 1.8
    out = np.zeros(int(SR * total))
    seq = ["G4", "C5", "E5", "G5", "C6", "E6"]
    for i, nm in enumerate(seq):
        out += at(bell(note(nm), 0.5, 0.8), i * 0.07, total)
    chord = sum(tone(note(nm), 1.2, "tri") for nm in ["C5", "G5", "C6", "E6"]) * env(int(SR * 1.2), 0.01, 0.7, hold=0.2) * 0.22
    return out + at(chord, 0.45, total)


def sfx_paw():
    # cartoon paw tap: a tiny rising "pit" followed by a springy "boing" with a bell sparkle
    total = 0.45
    d1 = 0.06
    n1 = int(SR * d1)
    pit = tone(np.linspace(700, 1300, n1), d1) * env(n1, 0.002, 0.03)

    d2 = 0.35
    n2 = int(SR * d2)
    tt = t_axis(d2)
    wobble = 1 + 0.18 * np.exp(-tt * 9) * np.sin(2 * np.pi * 16 * tt)   # spring vibrato that settles
    f = 330 * wobble * (1 + 0.25 * np.exp(-tt * 20))                      # starts a bit higher, drops into place
    boing = tone(f, d2, "tri") * env(n2, 0.003, 0.18)
    sparkle = bell(2093, 0.25, 0.5) * 0.15

    return mix(at(pit, 0, total), at(boing, 0.05, total), at(sparkle, 0.06, total))


def sfx_bubble():
    # ghost bubble "bloop"
    d = 0.3
    n = int(SR * d)
    f = 300 + 500 * (1 - np.exp(-t_axis(d) * 25))
    s = tone(f, d) * env(n, 0.005, 0.15)
    s += 0.3 * tone(f * 1.5, d) * env(n, 0.005, 0.08)
    return s


def sfx_click():
    d = 0.05
    n = int(SR * d)
    return tone(1400, d) * env(n, 0.0005, 0.015) + 0.4 * lowpass(noise(d), 4000) * env(n, 0.0005, 0.006)


def sfx_error():
    # gentle "nuh-uh": two low muted notes
    a = tone(note("D#4"), 0.14, "tri") * env(int(SR * 0.14), 0.005, 0.06)
    b = tone(note("A#3"), 0.22, "tri") * env(int(SR * 0.22), 0.005, 0.1)
    return lowpass(mix(at(a, 0, 0.4), at(b, 0.13, 0.4)), 1800)


def sfx_whoosh():
    # air swish: band-passed noise that swells and fades
    d = 0.45
    n = int(SR * d)
    lo = bandpass(noise(d), 300, 1200)
    hi = bandpass(noise(d), 1500, 5000)
    w = np.sin(np.pi * np.linspace(0, 1, n))
    return (lo * (1 - w) * 0.6 + hi * w) * w ** 2


def sfx_page():
    # paper swish for panels
    d = 0.22
    n = int(SR * d)
    return bandpass(noise(d), 1500, 6000) * env(n, 0.03, 0.08)


# ------------------------------------------------------------------ music
def music():
    """A 16-bar music-box waltz in A minor, 100 bpm 3/4, loops seamlessly."""
    bpm = 100
    beat = 60 / bpm
    bars = 16
    total = bars * 3 * beat
    N = int(SR * total)
    out = np.zeros(N + int(SR * 2))   # 2 s of extra room for ringing tails
    total_buf = len(out) / SR

    chords = ["Am", "Am", "F", "E", "Am", "Dm", "E", "E",
              "F", "G", "C", "Am", "Dm", "E", "Am", "E"]
    chord_notes = {
        "Am": ["A2", "E3", "A3", "C4"], "F": ["F2", "C3", "F3", "A3"], "E": ["E2", "B2", "E3", "G#3"],
        "Dm": ["D2", "A2", "D3", "F3"], "G": ["G2", "D3", "G3", "B3"], "C": ["C3", "G3", "C4", "E4"],
    }
    # melody: (bar, beat, note, length in beats)
    mel = [
        (0, 0, "E5", 1), (0, 1, "A5", 1), (0, 2, "C6", 1),
        (1, 0, "B5", 2), (1, 2, "A5", 1),
        (2, 0, "A5", 1), (2, 1, "C6", 1), (2, 2, "F6", 1),
        (3, 0, "E6", 2), (3, 2, "D6", 1),
        (4, 0, "C6", 1), (4, 1, "B5", 1), (4, 2, "A5", 1),
        (5, 0, "D6", 1), (5, 1, "F6", 1), (5, 2, "A5", 1),
        (6, 0, "G#5", 2), (6, 2, "B5", 1),
        (7, 0, "E5", 3),
        (8, 0, "F5", 1), (8, 1, "A5", 1), (8, 2, "C6", 1),
        (9, 0, "D6", 1), (9, 1, "B5", 1), (9, 2, "G5", 1),
        (10, 0, "E6", 1), (10, 1, "C6", 1), (10, 2, "G5", 1),
        (11, 0, "A5", 2), (11, 2, "C6", 1),
        (12, 0, "F6", 1), (12, 1, "D6", 1), (12, 2, "A5", 1),
        (13, 0, "G#5", 1), (13, 1, "B5", 1), (13, 2, "E6", 1),
        (14, 0, "A5", 3),
        (15, 0, "B5", 1), (15, 1, "G#5", 1), (15, 2, "E5", 1),
    ]
    for bar, b, nm, ln in mel:
        s = bell(note(nm), max(0.6, ln * beat * 1.2), 0.6) * 0.55
        out += at(s, (bar * 3 + b) * beat, total_buf)[: len(out)]

    # waltz accompaniment: bass on 1, soft chord on 2 and 3
    for bar, ch in enumerate(chords):
        ns = chord_notes[ch]
        base = bar * 3 * beat
        bass = tone(note(ns[0]), beat * 1.5, "tri")
        bass = bass * env(len(bass), 0.01, beat * 0.8) * 0.35
        out += at(bass, base, total_buf)[: len(out)]
        for k in (1, 2):
            c = sum(tone(note(x), beat * 0.9) for x in ns[1:])
            c = c * env(len(c), 0.02, beat * 0.4) * 0.09
            out += at(c, base + k * beat, total_buf)[: len(out)]

    # soft airy pad underneath for a spooky mood
    pad = lowpass(noise(total), 600) * 0.03
    out[:len(pad)] += pad

    # let tails that run past the end wrap around to the start so the loop is seamless
    tail = out[N:]
    loop = out[:N].copy()
    loop[:len(tail)] += tail
    return loop


if __name__ == "__main__":
    save("pop", sfx_pop())
    save("spawn", sfx_spawn())
    save("coin", sfx_coin())
    save("stamp", sfx_stamp())
    save("brew", sfx_brew(), 0.6)
    save("ready", sfx_ready())
    save("gift", sfx_gift())
    save("levelup", sfx_levelup())
    save("paw", sfx_paw(), 0.75)
    save("bubble", sfx_bubble())
    save("click", sfx_click(), 0.5)
    save("error", sfx_error(), 0.6)
    save("whoosh", sfx_whoosh(), 0.5)
    save("page", sfx_page(), 0.4)
    save("music", music(), 0.55, fade=False)
