# Генерация текстур шариков игрового поля (улучшенная процедурная
# отрисовка: сферическое освещение, блик, контровый свет, мягкая кромка).
# Результат: src/Lines.Shared/Content/Balls/ball_1.png .. ball_6.png
# (512x512, RGBA). Рендер - с суперсэмплингом 4x (2048) и даунскейлом
# LANCZOS - гладкая кромка без зубцов при любом масштабе в игре.
# Запуск: python build/generate-balls.py
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
DST = ROOT / "src" / "Lines.Shared" / "Content" / "Balls"

SIZE = 512
SUPERSAMPLE = 4

# палитра как в BallRenderer.Palette (классические «Линии»):
# красный, жёлтый, синий, зелёный, бирюзовый, фиолетовый
PALETTE = [
    (215, 40, 40),
    (225, 205, 40),
    (60, 80, 220),
    (40, 180, 80),
    (50, 190, 200),
    (190, 60, 200),
]

# свет - сверху слева; контровый (отскок от «пола») - снизу справа
LIGHT = np.array([-0.5, -0.6, 0.65])
LIGHT /= np.linalg.norm(LIGHT)
HALF = LIGHT + np.array([0.0, 0.0, 1.0])
HALF /= np.linalg.norm(HALF)
BOUNCE = np.array([0.35, 0.6, 0.5])
BOUNCE /= np.linalg.norm(BOUNCE)

AMBIENT = 0.32
DIFFUSE = 0.95
SPECULAR_POWER = 80.0
SPECULAR_GAIN = 0.9
RIM_GAIN = 0.35
RADIUS = 0.94  # радиус шара в долях от половины текстуры


def render_ball(rgb: tuple) -> np.ndarray:
    s = SIZE * SUPERSAMPLE
    # координаты в единицах радиуса шара
    lin = (np.arange(s) + 0.5) / s * 2.0 - 1.0
    lin /= RADIUS
    nx, ny = np.meshgrid(lin, lin)
    d2 = nx * nx + ny * ny
    inside = d2 <= 1.0

    nz = np.sqrt(np.clip(1.0 - d2, 0.0, 1.0))

    diffuse = np.clip(nx * LIGHT[0] + ny * LIGHT[1] + nz * LIGHT[2], 0.0, None)
    spec_h = np.clip(nx * HALF[0] + ny * HALF[1] + nz * HALF[2], 0.0, None)
    specular = spec_h ** SPECULAR_POWER * SPECULAR_GAIN
    # контровый свет снизу справа, сильнее у кромки
    bounce = np.clip(nx * BOUNCE[0] + ny * BOUNCE[1] + nz * BOUNCE[2], 0.0, None)
    rim = (1.0 - nz) ** 3 * bounce * RIM_GAIN

    shade = AMBIENT + DIFFUSE * diffuse
    base = np.array(rgb, dtype=np.float64)
    color = base[None, None, :] * shade[:, :, None]
    color += 255.0 * specular[:, :, None]
    color += rim[:, :, None] * (255.0 * 0.5 + base * 0.5)[None, None, :]

    # мягкий овальный блик сверху слева (глянец)
    hl = np.exp(-(((nx + 0.34) / 0.20) ** 2 + ((ny + 0.42) / 0.13) ** 2))
    color += 90.0 * hl[:, :, None] * nz[:, :, None]

    color = np.clip(color, 0, 255).astype(np.uint8)
    alpha = np.where(inside, 255, 0).astype(np.uint8)
    return np.dstack([color, alpha[:, :, None]])


def main() -> None:
    DST.mkdir(parents=True, exist_ok=True)
    for i, rgb in enumerate(PALETTE, start=1):
        arr = render_ball(rgb)
        im = Image.fromarray(arr, "RGBA")
        im = im.resize((SIZE, SIZE), Image.LANCZOS)
        out = DST / f"ball_{i}.png"
        im.save(out)
        print(f"ball_{i}: {rgb} -> {out}")


if __name__ == "__main__":
    main()
