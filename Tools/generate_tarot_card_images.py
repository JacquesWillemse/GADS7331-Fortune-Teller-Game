"""Generate naive folk-art tarot card illustrations (purple sky / pink ground)."""

from __future__ import annotations

import math
import random
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets" / "Images"

W, H = 800, 1140
SKY = (170, 145, 195)
GROUND = (215, 145, 150)
OUTLINE = (25, 20, 30)
GRASS = (55, 35, 45)


def paper_texture(base: Image.Image):
    px = base.load()
    rng = random.Random(42)
    for y in range(H):
        for x in range(0, W, 2):
            n = rng.randint(-8, 8)
            r, g, b = px[x, y]
            px[x, y] = (max(0, min(255, r + n)), max(0, min(255, g + n)), max(0, min(255, b + n)))


def new_canvas(ground_y: int | None = None) -> tuple[Image.Image, ImageDraw.ImageDraw, int]:
    gy = ground_y if ground_y is not None else int(H * 0.62)
    img = Image.new("RGB", (W, H), SKY)
    d = ImageDraw.Draw(img)
    d.rectangle((0, gy, W, H), fill=GROUND)
    for _ in range(120):
        x = random.randint(20, W - 20)
        y = random.randint(gy + 10, H - 20)
        d.line([(x, y), (x + random.randint(-6, 6), y + random.randint(4, 10))], fill=GRASS, width=2)
    paper_texture(img)
    return img, ImageDraw.Draw(img), gy


def stroke(d: ImageDraw.ImageDraw, coords, width=4, fill=OUTLINE):
    d.line(coords, fill=fill, width=width, joint="curve")


def draw_person(d, cx, cy, scale=1.0, overalls=False, hat=False, bald=False, arms_up=False):
    s = scale
    head_r = int(28 * s)
    if bald:
        d.ellipse((cx - head_r, cy - head_r * 2, cx + head_r, cy), fill=(240, 210, 170), outline=OUTLINE, width=3)
    else:
        d.ellipse((cx - head_r, cy - head_r * 2, cx + head_r, cy), fill=(240, 210, 170), outline=OUTLINE, width=3)
        d.arc((cx - head_r, cy - head_r * 2.4, cx + head_r, cy - head_r * 0.2), 180, 0, fill=(80, 50, 40), width=int(8 * s))
    d.ellipse((cx - 8 * s, cy - head_r * 1.5, cx - 2 * s, cy - head_r * 1.3), fill=OUTLINE)
    d.ellipse((cx + 2 * s, cy - head_r * 1.5, cx + 8 * s, cy - head_r * 1.3), fill=OUTLINE)
    d.arc((cx - 10 * s, cy - head_r * 1.1, cx + 10 * s, cy - head_r * 0.6), 10, 170, fill=OUTLINE, width=2)
    body = (240, 210, 170) if not overalls else (90, 120, 180)
    d.rounded_rectangle((cx - 35 * s, cy, cx + 35 * s, cy + 90 * s), radius=8, fill=body, outline=OUTLINE, width=3)
    if overalls:
        d.rectangle((cx - 35 * s, cy + 20 * s, cx + 35 * s, cy + 90 * s), fill=(70, 100, 160), outline=OUTLINE, width=2)
    if hat:
        d.polygon([(cx - 40 * s, cy - head_r * 2), (cx + 40 * s, cy - head_r * 2), (cx + 30 * s, cy - head_r * 2.8), (cx - 30 * s, cy - head_r * 2.8)], fill=(220, 190, 80), outline=OUTLINE)
    arm_y = cy + 15 * s
    if arms_up:
        stroke(d, [(cx - 35 * s, arm_y), (cx - 55 * s, cy - 20 * s)], 4)
        stroke(d, [(cx + 35 * s, arm_y), (cx + 55 * s, cy - 20 * s)], 4)
    else:
        stroke(d, [(cx - 35 * s, arm_y), (cx - 55 * s, cy + 50 * s)], 4)
        stroke(d, [(cx + 35 * s, arm_y), (cx + 55 * s, cy + 50 * s)], 4)
    stroke(d, [(cx - 15 * s, cy + 90 * s), (cx - 20 * s, cy + 140 * s)], 5)
    stroke(d, [(cx + 15 * s, cy + 90 * s), (cx + 20 * s, cy + 140 * s)], 5)


def card_burning_boat():
    img, d, gy = new_canvas()
    # burning yacht
    d.polygon([(480, gy - 20), (720, gy - 10), (700, gy - 80), (500, gy - 90)], fill=(200, 200, 210), outline=OUTLINE, width=3)
    d.rectangle((560, gy - 130, 640, gy - 90), fill=(180, 180, 190), outline=OUTLINE, width=2)
    for i, col in enumerate([(255, 120, 40), (255, 180, 60), (255, 80, 30)]):
        d.polygon([(520 + i * 30, gy - 100), (560 + i * 25, gy - 180 - i * 20), (600 + i * 20, gy - 95)], fill=col, outline=OUTLINE)
    d.ellipse((530, gy - 200, 590, gy - 140), fill=(200, 200, 210), outline=OUTLINE, width=2)
    # marshmallow roaster on shore
    draw_person(d, 220, gy - 140, scale=0.9)
    d.line([(260, gy - 100), (320, gy - 130)], fill=(100, 60, 30), width=4)
    d.ellipse((315, gy - 140, 335, gy - 120), fill=(250, 250, 240), outline=OUTLINE, width=2)
    img.save(OUT / "BurningBillionairesBoat.png")


def card_molotov():
    img, d, gy = new_canvas()
    # mob silhouettes
    for i, x in enumerate([120, 200, 280, 360, 620, 680]):
        draw_person(d, x, gy - 160, scale=0.75, arms_up=True)
        bx, by = x + 40, gy - 120
        d.rectangle((bx, by, bx + 22, by + 50), fill=(80, 120, 60), outline=OUTLINE, width=2)
        d.rectangle((bx + 4, by - 18, bx + 18, by), fill=(200, 60, 50), outline=OUTLINE, width=2)
    # drinker center
    draw_person(d, 480, gy - 150, scale=1.0)
    for i, xo in enumerate([-30, 0, 30]):
        bx = 480 + xo + 50
        d.rectangle((bx, by - 130, bx + 18, by - 70), fill=(80, 120, 60), outline=OUTLINE, width=2)
        d.line([(bx + 9, gy - 130), (bx + 25, gy - 150)], fill=(200, 200, 200), width=3)
    d.arc((460, gy - 200, 500, gy - 170), 200, 340, fill=OUTLINE, width=3)
    img.save(OUT / "DrinkingMolotovAlcohol.png")


def card_salon_hair():
    img, d, gy = new_canvas()
    # mirror and chair
    d.rectangle((520, gy - 320, 680, gy - 120), fill=(200, 220, 230), outline=OUTLINE, width=3)
    d.rectangle((540, gy - 300, 660, gy - 140), fill=(170, 190, 210), outline=OUTLINE, width=2)
    d.rectangle((500, gy - 100, 580, gy - 20), fill=(120, 80, 60), outline=OUTLINE, width=3)
    # hair piles
    for x, y, r in [(300, gy - 40, 35), (360, gy - 20, 28), (420, gy - 50, 40), (250, gy - 60, 25)]:
        d.ellipse((x - r, y - r // 2, x + r, y + r // 2), fill=(60, 40, 30), outline=OUTLINE, width=2)
    draw_person(d, 320, gy - 170, scale=0.95, bald=True)
    d.polygon([(280, gy - 80), (340, gy - 110), (360, gy - 70), (300, gy - 40)], fill=(60, 40, 30), outline=OUTLINE, width=2)
    img.save(OUT / "StealingSalonHair.png")


def card_abattoir_date():
    img, d, gy = new_canvas()
    d.rectangle((250, gy - 60, 550, gy - 20), fill=(120, 80, 50), outline=OUTLINE, width=3)
    d.rectangle((370, gy - 110, 390, gy - 60), fill=(240, 200, 100), outline=OUTLINE, width=2)
    # butcher
    draw_person(d, 300, gy - 200, scale=0.9)
    d.rectangle((265, gy - 150, 335, gy - 90), fill=(240, 240, 240), outline=OUTLINE, width=2)
    d.polygon([(330, gy - 140), (380, gy - 120), (350, gy - 100)], fill=(180, 180, 190), outline=OUTLINE, width=2)
    # date partner staring at meat
    draw_person(d, 500, gy - 200, scale=0.9)
    d.ellipse((480, gy - 250, 500, gy - 230), fill=(200, 60, 80))
    d.ellipse((520, gy - 250, 540, gy - 230), fill=(200, 60, 80))
    d.ellipse((490, gy - 120, 540, gy - 90), fill=(180, 60, 60), outline=OUTLINE, width=2)
    d.ellipse((460, gy - 100, 500, gy - 70), fill=(200, 80, 80), outline=OUTLINE, width=2)
    img.save(OUT / "DatingAbattoirWorker.png")


def card_chinese_meals():
    img, d, gy = new_canvas()
    draw_person(d, 400, gy - 120, scale=0.7)
    boxes = [(280, gy - 280), (360, gy - 300), (440, gy - 320), (520, gy - 290), (600, gy - 270),
             (320, gy - 220), (400, gy - 240), (480, gy - 260), (560, gy - 230),
             (300, gy - 160), (380, gy - 180), (460, gy - 200), (540, gy - 170), (620, gy - 190)]
    for i, (x, y) in enumerate(boxes):
        col = (200, 40, 40) if i % 2 == 0 else (240, 240, 230)
        d.rectangle((x, y, x + 70, y + 55), fill=col, outline=OUTLINE, width=2)
        if col[0] > 200:
            d.arc((x + 10, y + 10, x + 60, y + 45), 0, 180, fill=(220, 180, 60), width=3)
    img.save(OUT / "TwentySucculentChineseMeals.png")


def card_cold_snap():
    img, d, gy = new_canvas(int(H * 0.58))
    d.rectangle((100, gy - 280, 700, gy - 20), fill=(210, 190, 170), outline=OUTLINE, width=4)
    for x in range(140, 660, 90):
        draw_person(d, x, gy - 180, scale=0.55)
        d.rectangle((x - 30, gy - 130, x + 30, gy - 70), fill=(180, 200, 220), outline=OUTLINE, width=2)
    draw_person(d, 650, gy - 220, scale=0.8, arms_up=True)
    for i in range(8):
        sx = 120 + i * 70
        d.line([(sx, 80), (sx + 15, 140), (sx, 200)], fill=(180, 210, 240), width=3)
        d.ellipse((sx - 8, 200, sx + 8, 216), fill=(180, 210, 240), outline=OUTLINE)
    d.rectangle((600, gy - 300, 690, gy - 240), fill=(200, 220, 240), outline=OUTLINE, width=2)
    d.text((610, gy - 290), "32F", fill=OUTLINE)
    img.save(OUT / "ColdSnapRetirementHome.png")


def write_meta(filename: str, guid: str):
    template = (ROOT / "Assets" / "Images" / "Mohawk.png.meta").read_text()
    meta = template.replace("47f419058b51c1b4d9ad2373b2f54a1d", guid)
    (OUT / f"{filename}.meta").write_text(meta)


def main():
    random.seed(7)
    card_burning_boat()
    card_molotov()
    card_salon_hair()
    card_abattoir_date()
    card_chinese_meals()
    card_cold_snap()
    guids = {
        "TwentySucculentChineseMeals.png": "6719316ae5494704a1226b2dbe2623c3",
        "ColdSnapRetirementHome.png": "84b75c4024e045efa4be33e6d5181fc9",
        "BurningBillionairesBoat.png": "c6c997ad5b564c31befad805e99e1aad",
        "DrinkingMolotovAlcohol.png": "6b3b0023e4d54695a643b894b1d1eaf6",
        "StealingSalonHair.png": "d284f0993033414cba4022a1e5672339",
        "DatingAbattoirWorker.png": "6808ddb886704bd8a7670f6d4038a12b",
    }
    for name, guid in guids.items():
        write_meta(name, guid)
        print(f"Wrote {OUT / name}")
    print("Done.")


if __name__ == "__main__":
    main()
