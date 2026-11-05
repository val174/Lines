# Передача короны: из king_leader.png (красный, золотая корона) и
# king_player.png (синий, серебряная корона) готовит спрайты:
#   crown.png            - золотая корона отдельно (летает между пьедесталами)
#   king_leader_base.png - красный без короны
#   king_leader_sad.png  - красный без короны, грустный (бывший лидер)
#   king_player_base.png - синий без короны
# Использование: python build/process-crown.py

import os

from PIL import Image, ImageDraw

ROOT = os.path.join(os.path.dirname(__file__), "..")
CONTENT = os.path.join(ROOT, "src", "Lines.Shared", "Content")

RED = os.path.join(CONTENT, "king_leader.png")
BLUE = os.path.join(CONTENT, "king_player.png")


def is_gold(p):
    r, g, b, a = p
    return a > 0 and r > 170 and g > 120 and b < 110


def is_red(p):
    r, g, b, a = p
    return a > 0 and r > 150 and g < 90 and b < 90


def is_blue(p):
    # строгий синий головы: серебро (с синевой) и фиолет камней
    # исключаем - у головы заметная зелёная составляющая, у камней нет
    r, g, b, a = p
    return a > 0 and b > 140 and b > r + 90 and g > 30


def extract_crown(img, x0, y0, x1, y1):
    """Корона = всё выше нижнего золотого пикселя в каждой колонке."""
    crop = img.crop((x0, y0, x1, y1)).convert("RGBA")
    px = crop.load()
    w, h = crop.size
    for x in range(w):
        bottom = -1
        for y in range(h):
            if is_gold(px[x, y]):
                bottom = y
        for y in range(bottom + 3, h):
            px[x, y] = (0, 0, 0, 0)
    bbox = crop.getbbox()
    return crop.crop(bbox) if bbox else crop


def remove_crown(img, is_head, max_y, min_head_y=0, gold_anchor=False):
    """Стирание короны.
    gold_anchor: в колонках с золотом - всё выше нижнего золотого
    пикселя (+запас): снимает ободок и бархат (по цвету совпадает
    с головой). Иначе: всё выше первого пикселя головы НИЖЕ
    min_head_y (камни короны по цвету похожи на голову, их надо
    исключить порогом); если головы в колонке нет - всё до max_y."""
    out = img.convert("RGBA").copy()
    px = out.load()
    w, h = out.size
    for x in range(w):
        limit = None
        if gold_anchor:
            for y in range(min(max_y, h)):
                if is_gold(px[x, y]):
                    limit = y  # нижний золотой в колонке
            if limit is not None:
                limit += 4
        if limit is None:
            for y in range(min_head_y, min(max_y, h)):
                if is_head(px[x, y]):
                    limit = y
                    break
            if limit is None:
                limit = min(max_y, h)
        for y in range(limit):
            p = px[x, y]
            if p[3] > 0 and not (not gold_anchor and is_head(p)):
                px[x, y] = (0, 0, 0, 0)
    return out


def erase_specks(img, is_head, max_y):
    """Дочистка: остатки бликов/обводки короны выше линии max_y
    (всё, что не цвет головы)."""
    out = img.copy()
    px = out.load()
    w, h = out.size
    for x in range(w):
        for y in range(min(max_y, h)):
            p = px[x, y]
            if p[3] > 0 and not is_head(p):
                px[x, y] = (0, 0, 0, 0)
    return out


def make_sad(base):
    """Грустное лицо: закрываем улыбку цветом головы, рисуем
    грустный рот (дуга вниз) и опущенные к носу брови."""
    out = base.copy()
    d = ImageDraw.Draw(out)
    w, h = out.size
    cx = w // 2
    # координаты подобраны по king_leader.png 479x512; масштабируем
    k = w / 479.0
    # закрасить рот цветом щеки (сэмпл слева ото рта, яркий красный)
    skin = out.getpixel((int(150 * k), int(290 * k)))[:3] + (255,)
    d.ellipse((int(192 * k), int(248 * k), int(288 * k), int(328 * k)), fill=skin)
    # грустный рот: тёмная дуга "∩"
    dark = (60, 12, 12, 255)
    d.arc((int(205 * k), int(262 * k), int(275 * k), int(322 * k)),
          180, 360, fill=dark, width=max(3, int(7 * k)))
    # грустные брови: внутренние концы ПРИПОДНЯТЫ (домик наоборот)
    brow = (90, 15, 15, 255)
    d.line((int(150 * k), int(168 * k), int(198 * k), int(148 * k)),
           fill=brow, width=max(4, int(7 * k)))
    d.line((int(330 * k), int(168 * k), int(282 * k), int(148 * k)),
           fill=brow, width=max(4, int(7 * k)))
    return out


def main():
    red = Image.open(RED).convert("RGBA")
    blue = Image.open(BLUE).convert("RGBA")
    print("red:", red.size, "blue:", blue.size)

    crown = extract_crown(red, 90, 0, 390, 200)
    crown.save(os.path.join(CONTENT, "crown.png"))

    red_base = remove_crown(red, is_red, 200, gold_anchor=True)
    red_base = erase_specks(red_base, is_red, 142)
    red_base.save(os.path.join(CONTENT, "king_leader_base.png"))

    sad = make_sad(red_base)
    sad.save(os.path.join(CONTENT, "king_leader_sad.png"))

    # синий: камни короны похожи на голову - голову ищем ниже y=150
    blue_base = remove_crown(blue, is_blue, 215, min_head_y=150)
    blue_base.save(os.path.join(CONTENT, "king_player_base.png"))

    print("ok: crown.png, king_leader_base.png, king_leader_sad.png, king_player_base.png")


if __name__ == "__main__":
    main()
