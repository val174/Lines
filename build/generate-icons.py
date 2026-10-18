# Генератор иконок приложения: персонаж-лидер (красный шарик в золотой
# короне, build/assets-src/king_leader.png) на тёмном фоне.
# Windows: Icon.ico (16..256) и Icon.bmp в Lines.Windows;
# Android: Resources/Drawable/Icon.png (512, масштабируется Android-ом).
# Использование: python build/generate-icons.py

import os

from PIL import Image

ROOT = os.path.join(os.path.dirname(__file__), "..")
KING = os.path.join(ROOT, "build", "assets-src", "king_leader.png")
BG = (36, 36, 40)

# персонаж занимает эту долю высоты иконки
FILL = 0.92


def make_icon(size):
    img = Image.new("RGBA", (size, size), BG + (255,))
    king = Image.open(KING).convert("RGBA")
    kh = int(size * FILL)
    kw = int(king.width * kh / king.height)
    king = king.resize((kw, kh), Image.LANCZOS)
    img.alpha_composite(king, ((size - kw) // 2, size - kh))
    return img


def main():
    # Windows: иконка exe и окна
    win_dir = os.path.join(ROOT, "src", "Lines.Windows")
    icon = make_icon(256)
    icon.save(os.path.join(win_dir, "Icon.ico"),
              sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)])
    icon.convert("RGB").save(os.path.join(win_dir, "Icon.bmp"))
    print("Windows: Icon.ico, Icon.bmp")

    # Android: лаунчер-иконка
    android_icon = os.path.join(ROOT, "src", "Lines.Android", "Resources", "Drawable", "Icon.png")
    os.makedirs(os.path.dirname(android_icon), exist_ok=True)
    make_icon(512).save(android_icon)
    print("Android: Resources/Drawable/Icon.png")


if __name__ == "__main__":
    main()
