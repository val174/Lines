# Макет: полупрозрачная надпись «АВТО» по центру превью, без плашки.
from PIL import Image, ImageDraw, ImageFont

BORDER = (96, 88, 64)
BACK = (48, 46, 40)
CELL = (189, 178, 127)
WHITE = (255, 255, 255)
BG = (36, 36, 40)
PALETTE = [(215, 40, 40), (225, 205, 40), (60, 80, 220),
           (40, 180, 80), (50, 190, 200), (190, 60, 200)]
BALLS = {(0, 0): 1, (4, 4): 3, (8, 8): 4, (2, 6): 5, (6, 2): 2, (7, 7): 6}

FONT = "C:/Windows/Fonts/arial.ttf"
FONT_B = "C:/Windows/Fonts/arialbd.ttf"

CELL_W, CELL_H = 210, 260
LABEL_H = 42


def draw_ball(dr, x, y, r, color):
    dr.ellipse([x - r, y - r, x + r, y + r], fill=color)
    hl = tuple(min(255, c + 90) for c in color)
    dr.ellipse([x - r * 0.55, y - r * 0.6, x - r * 0.05, y - r * 0.15], fill=hl)


def draw_cell(dr, ox, oy):
    dr.rectangle([ox, oy, ox + CELL_W, oy + CELL_H], fill=BORDER)
    dr.rectangle([ox + 2, oy + 2, ox + CELL_W - 2, oy + CELL_H - 2], fill=BACK)
    side = CELL_W - 24
    bx, by = ox + 12, oy + 10
    dr.rectangle([bx, by, bx + side, by + side], fill=BORDER)
    cw = side // 9
    for gx in range(9):
        for gy in range(9):
            dr.rectangle([bx + gx * cw, by + gy * cw,
                          bx + gx * cw + cw - 1, by + gy * cw + cw - 1], fill=CELL)
    for (gx, gy), c in BALLS.items():
        draw_ball(dr, bx + gx * cw + cw // 2, by + gy * cw + cw // 2,
                  cw // 2 - 2, PALETTE[c - 1])
    return bx, by, side


def score(dr, ox, oy, font):
    t = "Счёт 214 - 08.10.26 22:15"
    f = font
    bb = dr.textbbox((0, 0), t, font=f)
    while bb[2] - bb[0] > CELL_W - 14 and f.size > 9:
        f = ImageFont.truetype(FONT, f.size - 1)
        bb = dr.textbbox((0, 0), t, font=f)
    dr.text((ox + (CELL_W - (bb[2] - bb[0])) / 2 - bb[0],
             oy + CELL_H - LABEL_H - bb[1]), t, font=f, fill=WHITE)


# две ячейки: прозрачность 40% и 55%
img = Image.new("RGB", (2 * (CELL_W + 40) + 40, CELL_H + 80), BG)
f_score = ImageFont.truetype(FONT, 13)
f_cap = ImageFont.truetype(FONT, 15)

for i, alpha in enumerate((100, 140)):
    ox = 40 + i * (CELL_W + 40)
    oy = 20
    cell_img = Image.new("RGB", (CELL_W + 4, CELL_H + 4), BG)
    drc = ImageDraw.Draw(cell_img)
    bx, by, side = draw_cell(drc, 0, 0)
    score(drc, 0, 0, f_score)

    # полупрозрачная надпись на отдельном слое
    overlay = Image.new("RGBA", cell_img.size, (0, 0, 0, 0))
    dro = ImageDraw.Draw(overlay)
    f_big = ImageFont.truetype(FONT_B, 44)
    t = "АВТО"
    bb = dro.textbbox((0, 0), t, font=f_big)
    w, h = bb[2] - bb[0], bb[3] - bb[1]
    cx, cy = bx + side / 2, by + side / 2
    dro.text((cx - w / 2 - bb[0], cy - h / 2 - bb[1]), t, font=f_big,
             fill=(220, 220, 220, alpha))
    cell_img = Image.alpha_composite(
        cell_img.convert("RGBA"), overlay).convert("RGB")

    img.paste(cell_img, (ox, oy))
    cap = f"прозрачность {round(alpha / 2.55)}%"
    drcap = ImageDraw.Draw(img)
    bb = drcap.textbbox((0, 0), cap, font=f_cap)
    drcap.text((ox + (CELL_W - (bb[2] - bb[0])) / 2 - bb[0],
                oy + CELL_H + 20), cap, font=f_cap, fill=WHITE)

out = "build/out/badge-watermark.png"
img.save(out)
print(out)
