# Генератор иконок кнопок панели: 6 цветных спрайтов 256x256
# (плоский двухтональный стиль + тёмная обводка, суперсэмплинг 4x)
# в src/Lines.Shared/Content/Buttons/.
# Использование: python build/generate-buttons.py

import math
import os

from PIL import Image, ImageDraw

SS = 4            # суперсэмплинг
ICON = 256        # итоговый размер иконки, px
S = ICON * SS     # рабочий размер

OUTLINE = (24, 28, 40, 255)       # обводка

ROOT = os.path.join(os.path.dirname(__file__), "..")
OUT_DIR = os.path.join(ROOT, "src", "Lines.Shared", "Content", "Buttons")


def canvas():
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    return img, ImageDraw.Draw(img)


def finish(img):
    return img.resize((ICON, ICON), Image.LANCZOS)


def glyph(draw_fn, light, dark, split=0.52, bbox_ratio=0.72, outline_w=None):
    """Силуэт через draw_fn, заливка двухтональная: слева light, справа dark."""
    mask = Image.new("L", (S, S), 0)
    md = ImageDraw.Draw(mask)
    m = int(S * (1 - bbox_ratio) / 2)
    box = (m, m, S - m, S - m)
    draw_fn(md, box)
    if outline_w is None:
        outline_w = S // 42
    img, _ = canvas()
    for dx, dy in [(-outline_w, 0), (outline_w, 0), (0, -outline_w),
                   (0, outline_w), (-outline_w, -outline_w),
                   (outline_w, outline_w), (-outline_w, outline_w),
                   (outline_w, -outline_w)]:
        img.paste(Image.new("RGBA", (S, S), OUTLINE), (dx, dy), mask)
    img.paste(Image.new("RGBA", (S, S), light), (0, 0), mask)
    # тёмная правая половина
    right = mask.crop((int(S * split), 0, S, S))
    img.paste(Image.new("RGBA", (S - int(S * split), S), dark),
              (int(S * split), 0), right)
    return finish(img)


# --- глифы (рисуются в маску, белым; fill=0 - вырез) ------------------------

def g_plus(md, b):
    x0, y0, x1, y1 = b
    w = (x1 - x0) * 0.30
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    md.rounded_rectangle((cx - w / 2, y0, cx + w / 2, y1), radius=w / 4, fill=255)
    md.rounded_rectangle((x0, cy - w / 2, x1, cy + w / 2), radius=w / 4, fill=255)


def g_save(md, b):
    x0, y0, x1, y1 = b
    w, h = x1 - x0, y1 - y0
    notch = w * 0.28
    md.polygon([(x0, y0), (x1 - notch, y0), (x1, y0 + notch),
                (x1, y1), (x0, y1)], fill=255)
    sw = w * 0.46
    md.rectangle((x0 + w * 0.12, y0, x0 + w * 0.12 + sw, y0 + h * 0.30), fill=0)
    md.rectangle((x0 + w * 0.34, y0, x0 + w * 0.46, y0 + h * 0.22), fill=255)
    rw = w * 0.52
    md.rectangle(((x0 + x1) / 2 - rw / 2, y1 - h * 0.46,
                  (x0 + x1) / 2 + rw / 2, y1 - h * 0.04), fill=0)


def g_load(md, b):
    x0, y0, x1, y1 = b
    w, h = x1 - x0, y1 - y0
    cx = (x0 + x1) / 2
    sw = w * 0.22
    md.rectangle((cx - sw / 2, y0 + h * 0.28, cx + sw / 2, y0 + h * 0.78), fill=255)
    md.polygon([(cx, y0), (x0 + w * 0.08, y0 + h * 0.42),
                (x1 - w * 0.08, y0 + h * 0.42)], fill=255)
    ty = y0 + h * 0.82
    md.rectangle((x0, ty, x1, y1), fill=255)
    md.rectangle((x0, y0 + h * 0.62, x0 + w * 0.14, y1), fill=255)
    md.rectangle((x1 - w * 0.14, y0 + h * 0.62, x1, y1), fill=255)


def g_gear(md, b):
    x0, y0, x1, y1 = b
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    r_out = (x1 - x0) / 2
    r_hub = r_out * 0.62
    tw = r_out * 0.34
    tl = r_out * 0.30
    for i in range(8):
        a = math.radians(i * 45)
        dx, dy = math.cos(a), math.sin(a)
        px, py = -dy, dx
        r1, r2 = r_hub * 0.88, r_hub * 0.88 + tl
        pts = [(cx + dx * r1 + px * tw / 2, cy + dy * r1 + py * tw / 2),
               (cx + dx * r1 - px * tw / 2, cy + dy * r1 - py * tw / 2),
               (cx + dx * r2 - px * tw / 2, cy + dy * r2 - py * tw / 2),
               (cx + dx * r2 + px * tw / 2, cy + dy * r2 + py * tw / 2)]
        md.polygon(pts, fill=255)
    md.ellipse((cx - r_hub, cy - r_hub, cx + r_hub, cy + r_hub), fill=255)
    hole = r_out * 0.24
    md.ellipse((cx - hole, cy - hole, cx + hole, cy + hole), fill=0)


def g_trophy(md, b):
    """Кубок: чаша с плоским широким верхом, сужающаяся к округлому
    дну у ножки; ручки-дуги прилегают к боковинам, оба конца уходят
    в тело чаши; ножка и двухступенчатое основание."""
    x0, y0, x1, y1 = b
    w, h = x1 - x0, y1 - y0
    cx = (x0 + x1) / 2
    cw = w * 0.66
    top = y0 + h * 0.02
    bot = y0 + h * 0.58
    # ручки: широкие дуги, опущены чуть ниже верха чаши и прилегают
    # к сужающимся боковинам; концы дуги уходят в тело чаши
    ht = int(w * 0.095)
    rh_x, rh_y = w * 0.22, h * 0.28
    hcy = top + rh_y * 1.02
    for sgn in (-1, 1):
        hcx = cx + sgn * cw * 0.36
        box_e = (hcx - rh_x, hcy - rh_y, hcx + rh_x, hcy + rh_y)
        if sgn > 0:
            md.arc(box_e, start=-105, end=110, fill=255, width=ht)
        else:
            md.arc(box_e, start=70, end=285, fill=255, width=ht)
    # чаша: широкий плоский верх, боковины сходятся к узкому округлому дну
    ch = bot - top
    cbw = cw * 0.42            # ширина дна у ножки
    pts = []
    steps = 24
    for i in range(steps + 1):  # правая боковина сверху вниз
        t = i / steps
        hw = cw / 2 - (t ** 1.6) * (cw / 2 - cbw / 2)
        pts.append((cx + hw, top + t * (ch - cbw / 2)))
    for i in range(steps + 1):  # округлое дно справа налево
        a = math.radians(i * 180 / steps)
        pts.append((cx + math.cos(a) * cbw / 2,
                    top + ch - cbw / 2 + math.sin(a) * cbw / 2))
    for i in range(steps + 1):  # левая боковина снизу вверх
        t = 1 - i / steps
        hw = cw / 2 - (t ** 1.6) * (cw / 2 - cbw / 2)
        pts.append((cx - hw, top + t * (ch - cbw / 2)))
    md.polygon(pts, fill=255)
    # ножка
    md.rectangle((cx - w * 0.09, bot - h * 0.06, cx + w * 0.09, y0 + h * 0.78),
                 fill=255)
    # основание: верхняя ступень и нижняя плита
    md.rectangle((cx - w * 0.24, y0 + h * 0.76, cx + w * 0.24, y0 + h * 0.88),
                 fill=255)
    md.rounded_rectangle((cx - w * 0.34, y0 + h * 0.86, cx + w * 0.34, y1),
                         radius=w * 0.05, fill=255)


def g_exit(md, b):
    x0, y0, x1, y1 = b
    w, h = x1 - x0, y1 - y0
    dw = w * 0.46
    md.rectangle((x0, y0, x0 + dw, y1), fill=255)
    md.rectangle((x0 + dw * 0.18, y0 + h * 0.10, x0 + dw * 0.82, y0 + h * 0.90), fill=0)
    ax0 = x0 + w * 0.34
    ax1 = x1 - w * 0.02
    cy = (y0 + y1) / 2
    sw = h * 0.13
    md.rectangle((ax0, cy - sw / 2, ax1 - w * 0.26, cy + sw / 2), fill=255)
    md.polygon([(ax1 - w * 0.34, cy - h * 0.20),
                (ax1 - w * 0.34, cy + h * 0.20),
                (ax1, cy)], fill=255)


# --- палитры (светлый / тёмный тон) -----------------------------------------

GREEN = ((122, 199, 79, 255), (76, 158, 52, 255))
BLUE = ((96, 165, 250, 255), (37, 99, 235, 255))
AMBER = ((251, 191, 36, 255), (217, 119, 6, 255))
STEEL = ((203, 213, 225, 255), (100, 116, 139, 255))
GOLD = ((250, 204, 21, 255), (202, 138, 4, 255))
RED = ((248, 113, 113, 255), (185, 28, 28, 255))

ICONS = [
    ("btn_new_game", g_plus, GREEN),
    ("btn_save", g_save, BLUE),
    ("btn_load", g_load, AMBER),
    ("btn_settings", g_gear, STEEL),
    ("btn_records", g_trophy, GOLD),
    ("btn_exit", g_exit, RED),
]


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    for name, fn, colors in ICONS:
        icon = glyph(fn, colors[0], colors[1])
        out = os.path.join(OUT_DIR, "%s.png" % name)
        icon.save(out)
        print(out)


if __name__ == "__main__":
    main()
