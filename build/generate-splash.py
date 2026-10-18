# Генератор картинки заставки (splash.png): игровое поле 9x9
# с декоративными шариками (текстуры Content/Balls, те же, что в игре);
# в центре - персонаж-лидер (build/assets-src/king_leader.png).
# Использование: python build/generate-splash.py

import os

from PIL import Image, ImageDraw

CELL = 96          # размер клетки, px
MAP = 9            # поле 9x9
MARGIN = 14        # рамка вокруг поля
SIZE = CELL * MAP + MARGIN * 2  # итоговый квадрат

FRAME = (96, 88, 64)
CELL_BG = (189, 178, 127)

ROOT = os.path.join(os.path.dirname(__file__), "..")
BALLS = os.path.join(ROOT, "src", "Lines.Shared", "Content", "Balls")
KING = os.path.join(ROOT, "build", "assets-src", "king_leader.png")
OUT = os.path.join(ROOT, "src", "Lines.Shared", "Content", "splash.png")

# декоративные шарики вокруг центра: x, y, цвет (1..6)
DECOR = [
    (1, 1, 2), (2, 6, 3), (6, 1, 4), (7, 4, 5),
    (5, 7, 6), (1, 5, 3), (7, 7, 2), (3, 2, 6),
]

# персонаж в центре: высота в клетках
KING_H_CELLS = 3.2


def paste_ball(img, ball, cx, cy, diameter):
    """Шарик из готовой текстуры + мягкая тень под ним."""
    r = diameter / 2
    sh = Image.new("RGBA", img.size, (0, 0, 0, 0))
    sd = ImageDraw.Draw(sh)
    sd.ellipse(
        (cx - r * 1.05, cy - r * 0.65, cx + r * 1.05, cy + r * 1.15),
        fill=(0, 0, 0, 110),
    )
    img.alpha_composite(sh)

    b = ball.resize((int(diameter), int(diameter)), Image.LANCZOS)
    img.alpha_composite(b, (int(cx - r), int(cy - r)))


def main():
    img = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    # рамка и клетки
    d.rectangle((0, 0, SIZE - 1, SIZE - 1), fill=FRAME)
    for x in range(MAP):
        for y in range(MAP):
            d.rectangle(
                (MARGIN + x * CELL + 1, MARGIN + y * CELL + 1,
                 MARGIN + (x + 1) * CELL - 2, MARGIN + (y + 1) * CELL - 2),
                fill=CELL_BG,
            )

    # декоративные шарики - те же текстуры, что в игре
    balls = {i: Image.open(os.path.join(BALLS, f"ball_{i}.png")).convert("RGBA")
             for i in range(1, 7)}
    for x, y, color in DECOR:
        paste_ball(img, balls[color],
                   MARGIN + (x + 0.5) * CELL, MARGIN + (y + 0.5) * CELL,
                   CELL * 0.84)

    # персонаж-лидер в центре поля
    king = Image.open(KING).convert("RGBA")
    kh = int(CELL * KING_H_CELLS)
    kw = int(king.width * kh / king.height)
    king = king.resize((kw, kh), Image.LANCZOS)

    center = MAP // 2
    cx = MARGIN + (center + 0.5) * CELL
    bottom = MARGIN + (center + 1.5) * CELL  # ноги на нижней границе клетки

    sh = Image.new("RGBA", img.size, (0, 0, 0, 0))
    sd = ImageDraw.Draw(sh)
    sd.ellipse((cx - kw * 0.4, bottom - kh * 0.06,
                cx + kw * 0.4, bottom + kh * 0.04), fill=(0, 0, 0, 120))
    img.alpha_composite(sh)
    img.alpha_composite(king, (int(cx - kw / 2), int(bottom - kh)))

    img.save(OUT)
    print(f"saved {OUT} ({SIZE}x{SIZE})")


if __name__ == "__main__":
    main()
