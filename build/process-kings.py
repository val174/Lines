# Обработка изображений персонажей для пьедесталов (лидер / претендент).
# Источники: build/assets-src/king_player.png, king_leader.png (1024x1024, RGBA).
# Результат: src/Lines.Shared/Content/king_player.png, king_leader.png
#   - обрезаны по альфа-каналу (убраны прозрачные поля),
#   - уменьшены до 512px по длинной стороне.
# Запуск: python build/process-kings.py
import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
SRC = ROOT / "build" / "assets-src"
DST = ROOT / "src" / "Lines.Shared" / "Content"
MAX_SIZE = 512
MARGIN = 4  # px отступ вокруг непрозрачной области


def process(name: str) -> None:
    src_path = SRC / f"{name}.png"
    dst_path = DST / f"{name}.png"
    im = Image.open(src_path).convert("RGBA")

    bbox = im.getchannel("A").getbbox()
    if bbox is None:
        sys.exit(f"{src_path}: нет непрозрачных пикселей")
    l, t, r, b = bbox
    l = max(0, l - MARGIN)
    t = max(0, t - MARGIN)
    r = min(im.width, r + MARGIN)
    b = min(im.height, b + MARGIN)
    im = im.crop((l, t, r, b))

    scale = MAX_SIZE / max(im.width, im.height)
    if scale < 1.0:
        im = im.resize((round(im.width * scale), round(im.height * scale)),
                       Image.LANCZOS)

    dst_path.parent.mkdir(parents=True, exist_ok=True)
    im.save(dst_path)
    print(f"{name}: {src_path.name} -> {dst_path} {im.size}")


if __name__ == "__main__":
    process("king_player")
    process("king_leader")
