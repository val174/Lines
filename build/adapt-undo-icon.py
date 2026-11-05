# Адаптация SVG-иконки «Отменить» (Material Symbols, build/assets-src/btn_undo.svg)
# под стиль кнопок панели: силуэт растрируется из SVG (svglib), к нему
# добавляется тёмная обводка и двухтональная фиолетовая заливка
# (светлая слева / тёмная справа), как у остальных btn_* (см.
# generate-buttons.py). Результат: src/Lines.Shared/Content/Buttons/btn_undo.png
# + превью на фоне кнопки build/out/btn_undo_preview.png.
# Требует: pip install svglib reportlab pillow
# Использование: python build/adapt-undo-icon.py

import os

from PIL import Image, ImageDraw
from reportlab.graphics import renderPM
from svglib.svglib import svg2rlg

SS = 4            # суперсэмплинг
ICON = 256        # итоговый размер иконки, px
S = ICON * SS     # рабочий размер

OUTLINE = (24, 28, 40, 255)
VIOLET = ((192, 132, 252, 255), (126, 58, 242, 255))
SPLIT = 0.52      # граница светлый/тёмный (как у остальных кнопок)
BBOX_RATIO = 0.72 # глиф занимает 72% иконки (как у остальных кнопок)

ROOT = os.path.join(os.path.dirname(__file__), "..")
SRC = os.path.join(ROOT, "build", "assets-src", "btn_undo.svg")
OUT_ICON = os.path.join(ROOT, "src", "Lines.Shared", "Content", "Buttons", "btn_undo.png")
OUT_PREVIEW = os.path.join(ROOT, "build", "out", "btn_undo_preview.png")

# цвет заливки в исходном SVG (#4B77D1): по отклонению от белого фона
# восстанавливаем альфа-канал (края сглажены белым)
SVG_FILL_MIN = 75  # min(r,g,b) заливки


def render_mask():
    """SVG -> маска силуэта (L), растр S*2 -> сглаживание до S."""
    drawing = svg2rlg(SRC)
    scale = S * 2 / 48  # viewBox 48x48
    drawing.width *= scale
    drawing.height *= scale
    drawing.scale(scale, scale)
    img = renderPM.drawToPIL(drawing, bg=0xFFFFFF).convert("RGB")

    mask = Image.new("L", img.size, 0)
    src = img.load()
    dst = mask.load()
    denom = 255 - SVG_FILL_MIN
    for y in range(img.size[1]):
        for x in range(img.size[0]):
            r, g, b = src[x, y]
            a = (255 - min(r, g, b)) * 255 // denom
            dst[x, y] = max(0, min(255, a))
    return mask.resize((S, S), Image.LANCZOS)


def fit_into_box(mask):
    """Вписать силуэт в квадрат BBOX_RATIO по центру канваса S x S."""
    bbox = mask.getbbox()
    cropped = mask.crop(bbox)
    box = int(S * BBOX_RATIO)
    ratio = min(box / cropped.size[0], box / cropped.size[1])
    resized = cropped.resize(
        (int(cropped.size[0] * ratio), int(cropped.size[1] * ratio)),
        Image.LANCZOS)
    canvas = Image.new("L", (S, S), 0)
    canvas.paste(resized, ((S - resized.size[0]) // 2,
                           (S - resized.size[1]) // 2))
    return canvas


def stylize(mask, light, dark):
    """Обводка + двухтональная заливка по маске (как glyph() в generate-buttons)."""
    outline_w = S // 42
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    for dx, dy in [(-outline_w, 0), (outline_w, 0), (0, -outline_w),
                   (0, outline_w), (-outline_w, -outline_w),
                   (outline_w, outline_w), (-outline_w, outline_w),
                   (outline_w, -outline_w)]:
        img.paste(Image.new("RGBA", (S, S), OUTLINE), (dx, dy), mask)
    img.paste(Image.new("RGBA", (S, S), light), (0, 0), mask)
    right = mask.crop((int(S * SPLIT), 0, S, S))
    img.paste(Image.new("RGBA", (S - int(S * SPLIT), S), dark),
              (int(S * SPLIT), 0), right)
    return img.resize((ICON, ICON), Image.LANCZOS)


def main():
    mask = fit_into_box(render_mask())
    icon = stylize(mask, VIOLET[0], VIOLET[1])
    icon.save(OUT_ICON)
    print(OUT_ICON)

    # превью: на фоне кнопки + в ряду с остальными иконками панели
    names = ["btn_new_game", "btn_undo", "btn_save", "btn_load",
             "btn_settings", "btn_records", "btn_exit"]
    tile = ICON + 24
    sheet = Image.new("RGB", (tile * len(names), ICON + 48), (30, 26, 22))
    buttons_dir = os.path.dirname(OUT_ICON)
    for i, name in enumerate(names):
        btn = Image.new("RGB", (ICON, ICON), (64, 58, 48))
        path = os.path.join(buttons_dir, name + ".png")
        ic = Image.open(path) if name != "btn_undo" else icon
        btn.paste(ic, (0, 0), ic)
        sheet.paste(btn, (i * tile + 12, 24))
    os.makedirs(os.path.dirname(OUT_PREVIEW), exist_ok=True)
    sheet.save(OUT_PREVIEW)
    print(OUT_PREVIEW)


if __name__ == "__main__":
    main()
