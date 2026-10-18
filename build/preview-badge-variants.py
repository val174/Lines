# Макеты вариантов размещения бейджа «Авто» в ячейке сохранения.
# Рисует приближение ячейки SavesScreen (рамка, превью 9x9 с шариками,
# строка счёта) и варианты A-E - для выбора пользователем.
from PIL import Image, ImageDraw, ImageFont

BORDER = (96, 88, 64)
BACK = (48, 46, 40)
CELL = (189, 178, 127)
YELLOW = (240, 205, 80)
WHITE = (255, 255, 255)
GREY = (36, 34, 30)
BG = (36, 36, 40)
PALETTE = [(215, 40, 40), (225, 205, 40), (60, 80, 220),
           (40, 180, 80), (50, 190, 200), (190, 60, 200)]
BALLS = {(0, 0): 1, (4, 4): 3, (8, 8): 4, (2, 6): 5, (6, 2): 2, (7, 7): 6}

FONT = "C:/Windows/Fonts/arial.ttf"

CELL_W, CELL_H = 210, 260
LABEL_H = 42


def draw_ball(dr, x, y, r, color):
    dr.ellipse([x - r, y - r, x + r, y + r], fill=color)
    hl = tuple(min(255, c + 90) for c in color)
    dr.ellipse([x - r * 0.55, y - r * 0.6, x - r * 0.05, y - r * 0.15], fill=hl)


def draw_cell(dr, ox, oy):
    dr.rectangle([ox, oy, ox + CELL_W, oy + CELL_H], fill=BORDER)
    dr.rectangle([ox + 2, oy + 2, ox + CELL_W - 2, oy + CELL_H - 2], fill=BACK)
    # превью 9x9 с сеткой
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


def chip(dr, cx, cy, font, scale=1.0):
    t = "Авто"
    bb = dr.textbbox((0, 0), t, font=font)
    w, h = bb[2] - bb[0] + 12, bb[3] - bb[1] + 6
    x0, y0 = int(cx - w / 2), int(cy - h / 2)
    dr.rectangle([x0, y0, x0 + w, y0 + h], fill=GREY)
    dr.line([x0, y0 + h, x0 + w, y0 + h], fill=YELLOW)
    dr.text((x0 + 6 - bb[0], y0 + 3 - bb[1]), t, font=font, fill=YELLOW)


def score(dr, ox, oy, font, prefix=""):
    t = f"{prefix}Счёт 214 - 08.10.26 22:15"
    f = font
    bb = dr.textbbox((0, 0), t, font=f)
    while bb[2] - bb[0] > CELL_W - 14 and f.size > 9:
        f = ImageFont.truetype(FONT, f.size - 1)
        bb = dr.textbbox((0, 0), t, font=f)
    dr.text((ox + (CELL_W - (bb[2] - bb[0])) / 2 - bb[0],
             oy + CELL_H - LABEL_H - bb[1]), t, font=f, fill=WHITE)


img = Image.new("RGB", (5 * (CELL_W + 30) + 30, CELL_H + 100), BG)
dr = ImageDraw.Draw(img)
f_chip = ImageFont.truetype(FONT, 13)
f_score = ImageFont.truetype(FONT, 13)
f_cap = ImageFont.truetype(FONT, 15)
f_cap_b = ImageFont.truetype(FONT, 17)

captions = [
    ("A", "по центру под счётом", "(текущий вариант)"),
    ("B", "лента над превью", "по центру вверху"),
    ("C", "правый нижний угол", "под счётом"),
    ("D", "без плашки: «Авто»", "в строке счёта"),
    ("E", "плашка по центру", "поверх превью"),
]

for i, (letter, line1, line2) in enumerate(captions):
    ox = 30 + i * (CELL_W + 30)
    oy = 20
    bx, by, side = draw_cell(dr, ox, oy)

    if letter == "A":
        score(dr, ox, oy, f_score)
        chip(dr, ox + CELL_W / 2, oy + CELL_H - 11, f_chip)
    elif letter == "B":
        score(dr, ox, oy, f_score)
        dr.rectangle([ox + 2, oy + 2, ox + CELL_W - 2, oy + 24], fill=GREY)
        dr.line([ox + 2, oy + 24, ox + CELL_W - 2, oy + 24], fill=YELLOW)
        bb = dr.textbbox((0, 0), "Авто", font=f_chip)
        dr.text((ox + (CELL_W - (bb[2] - bb[0])) / 2 - bb[0], oy + 5 - bb[1]),
                "Авто", font=f_chip, fill=YELLOW)
    elif letter == "C":
        score(dr, ox, oy, f_score)
        chip(dr, ox + CELL_W - 34, oy + CELL_H - 11, f_chip)
    elif letter == "D":
        score(dr, ox, oy, f_score, prefix="Авто. ")
    elif letter == "E":
        score(dr, ox, oy, f_score)
        chip(dr, ox + CELL_W / 2, oy + 10 + side / 2, f_chip)

    dr.text((ox + CELL_W / 2 - 6, oy + CELL_H + 14), letter, font=f_cap_b, fill=YELLOW)
    for j, ln in enumerate((line1, line2)):
        bb = dr.textbbox((0, 0), ln, font=f_cap)
        dr.text((ox + (CELL_W - (bb[2] - bb[0])) / 2 - bb[0],
                 oy + CELL_H + 40 + j * 20), ln, font=f_cap, fill=WHITE)

out = "build/out/badge-variants.png"
img.save(out)
print(out)
