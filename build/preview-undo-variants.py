# Варианты иконки «Отменить» (попытка 2) - лист превью в
# build/out/undo-icon-variants.png. Не часть игры.
# Использование: python build/preview-undo-variants.py

import math
import os

from PIL import Image, ImageDraw

SS = 4
ICON = 256
S = ICON * SS
OUTLINE = (24, 28, 40, 255)

ROOT = os.path.join(os.path.dirname(__file__), "..")
OUT = os.path.join(ROOT, "build", "out", "undo-icon-variants.png")

VIOLET = ((192, 132, 252, 255), (126, 58, 242, 255))


def canvas():
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    return img, ImageDraw.Draw(img)


def finish(img):
    return img.resize((ICON, ICON), Image.LANCZOS)


def glyph(draw_fn, light, dark, split=0.52, bbox_ratio=0.72):
    mask = Image.new("L", (S, S), 0)
    md = ImageDraw.Draw(mask)
    m = int(S * (1 - bbox_ratio) / 2)
    box = (m, m, S - m, S - m)
    draw_fn(md, box)
    outline_w = S // 42
    img, _ = canvas()
    for dx, dy in [(-outline_w, 0), (outline_w, 0), (0, -outline_w),
                   (0, outline_w), (-outline_w, -outline_w),
                   (outline_w, outline_w), (-outline_w, outline_w),
                   (outline_w, -outline_w)]:
        img.paste(Image.new("RGBA", (S, S), OUTLINE), (dx, dy), mask)
    img.paste(Image.new("RGBA", (S, S), light), (0, 0), mask)
    right = mask.crop((int(S * split), 0, S, S))
    img.paste(Image.new("RGBA", (S - int(S * split), S), dark),
              (int(S * split), 0), right)
    return finish(img)


# E: классическая «rotate-left»: дуга 270° (верх -> право -> низ -> лево),
# наконечник наверху указывает строго влево (Font Awesome style)
def g_rotate(md, b):
    x0, y0, x1, y1 = b
    w, h = x1 - x0, y1 - y0
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    r = w * 0.36
    lw = int(w * 0.16)
    md.arc((cx - r, cy - r, cx + r, cy + r), start=270, end=540,
           fill=255, width=lw)
    tx, ty = cx, cy - r
    s = w * 0.26
    md.polygon([(tx - s, ty),
                (tx + s * 0.55, ty - s * 0.62),
                (tx + s * 0.55, ty + s * 0.62)], fill=255)


# F: простая жирная стрелка влево («назад», как в браузере),
# скруглённые концы
def g_back(md, b):
    x0, y0, x1, y1 = b
    w, h = x1 - x0, y1 - y0
    cy = (y0 + y1) / 2
    lw = int(w * 0.20)
    x_tip = x0 + w * 0.06
    x_head = x0 + w * 0.42
    md.line((x_head, cy, x1 - w * 0.06, cy), fill=255, width=lw)
    s = w * 0.34
    md.polygon([(x_tip, cy),
                (x_head + lw * 0.4, cy - s * 0.62),
                (x_head + lw * 0.4, cy + s * 0.62)], fill=255)
    # скругление конца хвоста
    dot = lw / 2
    md.ellipse((x1 - w * 0.06 - dot, cy - dot, x1 - w * 0.06 + dot, cy + dot),
               fill=255)


# G: «история»: стрелка rotate-left + стрелки часов внутри (на 12 и 4)
def g_history(md, b):
    x0, y0, x1, y1 = b
    w, h = x1 - x0, y1 - y0
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    r = w * 0.38
    lw = int(w * 0.14)
    md.arc((cx - r, cy - r, cx + r, cy + r), start=270, end=540,
           fill=255, width=lw)
    # стрелки часов: короткая вниз-вправо (на 5), длинная вверх (на 12),
    # короче дуги, чтобы не задевать наконечник
    lw2 = int(w * 0.10)
    md.line((cx, cy, cx, cy - r * 0.38), fill=255, width=lw2)
    md.line((cx, cy, cx + r * 0.34, cy + r * 0.24), fill=255, width=lw2)
    dot = lw2 * 0.9
    md.ellipse((cx - dot, cy - dot, cx + dot, cy + dot), fill=255)
    # наконечник наверху влево
    tx, ty = cx, cy - r
    s = w * 0.24
    md.polygon([(tx - s, ty),
                (tx + s * 0.55, ty - s * 0.60),
                (tx + s * 0.55, ty + s * 0.60)], fill=255)


# H: «предыдущий трек»: вертикальная планка слева + треугольник,
# наконечником ВЛЕВО (media previous)
def g_prev(md, b):
    x0, y0, x1, y1 = b
    w, h = x1 - x0, y1 - y0
    bw = w * 0.16
    cy = (y0 + y1) / 2
    md.rounded_rectangle((x0, y0 + h * 0.12, x0 + bw, y1 - h * 0.12),
                         radius=bw / 2, fill=255)
    tx_base = x1                    # основание треугольника справа
    tx_tip = x0 + bw * 1.8          # остриё слева
    th = h * 0.76
    md.polygon([(tx_base, cy - th / 2), (tx_base, cy + th / 2),
                (tx_tip, cy)], fill=255)


VARIANTS = [
    ("E rotate-left", g_rotate),
    ("F back-arrow", g_back),
    ("G history", g_history),
    ("H prev-track", g_prev),
]


def main():
    tile = ICON + 90
    sheet = Image.new("RGB", (tile * len(VARIANTS), tile + 60), (30, 26, 22))
    for i, (name, fn) in enumerate(VARIANTS):
        icon = glyph(fn, VIOLET[0], VIOLET[1])
        btn = Image.new("RGB", (ICON, ICON), (64, 58, 48))
        btn.paste(icon, (0, 0), icon)
        x = i * tile + 45
        sheet.paste(btn, (x, 30))
        d = ImageDraw.Draw(sheet)
        d.text((x + ICON / 2 - len(name) * 4, ICON + 42), name,
               fill=(240, 234, 217))
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    sheet.save(OUT)
    print(OUT)


if __name__ == "__main__":
    main()
