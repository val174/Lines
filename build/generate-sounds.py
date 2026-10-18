# Генератор звуковых эффектов игры «Шарики».
# Синтезирует 5 коротких WAV (16-бит PCM, 22050 Гц, моно) в Content/Sounds/.
# Использование: python build/generate-sounds.py

import math
import os
import struct
import wave

RATE = 22050
OUT_DIR = os.path.join(os.path.dirname(__file__), "..", "src", "Lines.Shared", "Content", "Sounds")


def write_wav(name, samples):
    path = os.path.join(OUT_DIR, name)
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        frames = b"".join(struct.pack("<h", int(max(-1.0, min(1.0, s)) * 32767)) for s in samples)
        w.writeframes(frames)
    print(f"{name}: {len(samples) / RATE:.2f} s")


def seconds(n):
    return int(RATE * n)


def sine(freq, t):
    return math.sin(2.0 * math.pi * freq * t)


def envelope(i, n, attack=0.005, decay_power=6.0):
    """Нарастание attack сек, затем экспоненциальное затухание."""
    t = i / RATE
    if t < attack:
        return t / attack
    k = (i - int(attack * RATE)) / max(1, n - int(attack * RATE))
    return math.exp(-decay_power * k)


# select - короткий блип (выбор шарика)
def gen_select():
    n = seconds(0.09)
    return [0.5 * sine(880, i / RATE) * envelope(i, n) for i in range(n)]


# move - короткий щелчок (шаг перемещения)
def gen_move():
    n = seconds(0.06)
    samples = []
    for i in range(n):
        t = i / RATE
        f = 520 + 320 * (i / n)  # лёгкий подъём тона
        click = 0.35 * (1.0 - 2.0 * (i % 2)) * math.exp(-120 * t)  # шумовой импульс
        samples.append(0.35 * sine(f, t) * envelope(i, n, 0.002, 9.0) + click)
    return samples


# strip - восходящее «дзынь» со звоном (сжигание линии)
def gen_strip():
    n = seconds(0.45)
    samples = []
    for i in range(n):
        t = i / RATE
        k = i / n
        f = 620 + 700 * k  # глиссандо вверх
        tone = sine(f, t) + 0.5 * sine(2 * f, t) + 0.25 * sine(3 * f, t)
        samples.append(0.35 * tone * envelope(i, n, 0.004, 4.5))
    return samples


# appear - мягкий «поп» (появление новых шариков)
def gen_appear():
    n = seconds(0.14)
    samples = []
    for i in range(n):
        t = i / RATE
        k = i / n
        f = 260 + 160 * k  # быстрое нарастание тона
        samples.append(0.45 * sine(f, t) * envelope(i, n, 0.02, 5.0))
    return samples


# gameover - три ноты вниз (конец игры)
def gameover_note(freq, n, off):
    return [
        0.4 * (sine(freq, (off + i) / RATE) + 0.4 * sine(2 * freq, (off + i) / RATE))
        * envelope(i, n, 0.01, 3.0)
        for i in range(n)
    ]


def gen_gameover():
    n = seconds(0.28)
    samples = [0.0] * seconds(1.0)
    for start, freq in ((0.00, 440.0), (0.25, 349.2), (0.50, 261.6)):
        off = seconds(start)
        note = gameover_note(freq, n, 0)
        for i, s in enumerate(note):
            samples[off + i] += s
    return samples


if __name__ == "__main__":
    os.makedirs(OUT_DIR, exist_ok=True)
    write_wav("select.wav", gen_select())
    write_wav("move.wav", gen_move())
    write_wav("strip.wav", gen_strip())
    write_wav("appear.wav", gen_appear())
    write_wav("gameover.wav", gen_gameover())
