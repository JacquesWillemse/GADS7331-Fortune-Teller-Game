"""Generate carnival fortune-teller UI buttons with true PNG alpha transparency."""

from __future__ import annotations

import math
import os
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets" / "Images" / "UI"
OUT.mkdir(parents=True, exist_ok=True)

# Theme palette
WOOD_DARK = (42, 24, 16, 255)
WOOD_MID = (62, 36, 22, 255)
VELVET = (92, 21, 32, 255)
VELVET_LIGHT = (118, 28, 40, 255)
GOLD = (201, 162, 39, 255)
GOLD_LIGHT = (232, 198, 106, 255)
CREAM = (245, 230, 200, 255)
EMERALD = (46, 204, 113, 255)
EMERALD_GLOW = (46, 204, 113, 90)


def load_font(size: int, bold: bool = False) -> ImageFont.FreeTypeFont | ImageFont.ImageFont:
    size = max(12, int(size))
    candidates = []
    if bold:
        candidates += [
            r"C:\Windows\Fonts\georgiab.ttf",
            r"C:\Windows\Fonts\timesbd.ttf",
            r"C:\Windows\Fonts\arialbd.ttf",
        ]
    else:
        candidates += [
            r"C:\Windows\Fonts\georgia.ttf",
            r"C:\Windows\Fonts\times.ttf",
            r"C:\Windows\Fonts\arial.ttf",
        ]
    for path in candidates:
        if os.path.exists(path):
            return ImageFont.truetype(path, size)
    return ImageFont.load_default()


def box_metrics(box):
    x0, y0, x1, y1 = box
    w, h = x1 - x0, y1 - y0
    cx, cy = (x0 + x1) // 2, (y0 + y1) // 2
    s = min(w, h) / 100.0
    return x0, y0, x1, y1, w, h, cx, cy, s


def draw_button_plaque(base: Image.Image, box, radius: int = 28):
    x0, y0, x1, y1 = box
    layer = Image.new("RGBA", base.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)

    shadow = Image.new("RGBA", base.size, (0, 0, 0, 0))
    sd = ImageDraw.Draw(shadow)
    sd.rounded_rectangle((x0 + 6, y0 + 8, x1 + 6, y1 + 8), radius=radius, fill=(0, 0, 0, 70))
    base.alpha_composite(shadow)

    d.rounded_rectangle(box, radius=radius, fill=GOLD)
    inset = 6
    inner = (x0 + inset, y0 + inset, x1 - inset, y1 - inset)
    d.rounded_rectangle(inner, radius=max(8, radius - 6), fill=WOOD_DARK)

    inset2 = 14
    velvet_box = (x0 + inset2, y0 + inset2, x1 - inset2, y1 - inset2)
    d.rounded_rectangle(velvet_box, radius=max(6, radius - 10), fill=VELVET)

    hx0, hy0, hx1, hy1 = velvet_box
    d.line([(hx0 + 12, hy0 + 10), (hx1 - 12, hy0 + 10)], fill=VELVET_LIGHT, width=2)
    d.rounded_rectangle(velvet_box, radius=max(6, radius - 10), outline=GOLD_LIGHT, width=2)

    dot_r = max(4, int(min(hx1 - hx0, hy1 - hy0) * 0.025))
    for cx, cy in ((hx0 + 18, hy0 + 18), (hx1 - 18, hy0 + 18)):
        d.ellipse((cx - dot_r * 2, cy - dot_r * 2, cx + dot_r * 2, cy + dot_r * 2), fill=EMERALD_GLOW)
        d.ellipse((cx - dot_r, cy - dot_r, cx + dot_r, cy + dot_r), fill=EMERALD)

    base.alpha_composite(layer)
    return velvet_box


def center_text(draw: ImageDraw.ImageDraw, text: str, box, font, fill=CREAM, line_spacing: int = 4):
    x0, y0, x1, y1 = box
    lines = text.split("\n")
    heights, widths = [], []
    for line in lines:
        bbox = draw.textbbox((0, 0), line, font=font)
        widths.append(bbox[2] - bbox[0])
        heights.append(bbox[3] - bbox[1])
    total_h = sum(heights) + line_spacing * (max(0, len(lines) - 1))
    cy = y0 + (y1 - y0 - total_h) // 2
    for i, line in enumerate(lines):
        cx = x0 + (x1 - x0 - widths[i]) // 2
        draw.text((cx, cy), line, font=font, fill=fill)
        cy += heights[i] + line_spacing


def fit_font(draw: ImageDraw.ImageDraw, text: str, box, start_size: int, min_size: int = 18, bold: bool = True):
    x0, y0, x1, y1 = box
    lines = text.split("\n")
    for size in range(start_size, min_size - 1, -2):
        font = load_font(size, bold=bold)
        ok = True
        max_w = 0
        total_h = 0
        for line in lines:
            bbox = draw.textbbox((0, 0), line, font=font)
            w, h = bbox[2] - bbox[0], bbox[3] - bbox[1]
            max_w = max(max_w, w)
            total_h += h + 4
        if max_w <= (x1 - x0) and total_h <= (y1 - y0):
            return font
    return load_font(min_size, bold=bold)


def icon_tent(draw: ImageDraw.ImageDraw, box):
    x0, y0, x1, y1, w, h, cx, cy, s = box_metrics(box)
    base_y = y1 - int(8 * s)
    top_y = y0 + int(6 * s)
    half_w = int(w * 0.38)
    draw.polygon([(cx, top_y), (cx - half_w, base_y), (cx + half_w, base_y)], fill=CREAM, outline=GOLD, width=max(2, int(3 * s)))
    for i in range(-2, 3):
        px = cx + int(i * 22 * s)
        draw.line([(px, top_y + int(18 * s)), (px + int(i * 8 * s), base_y - int(4 * s))], fill=VELVET, width=max(2, int(4 * s)))
    door_w = int(24 * s)
    draw.rectangle((cx - door_w // 2, base_y - int(6 * s), cx + door_w // 2, base_y + int(8 * s)), fill=WOOD_MID, outline=GOLD, width=max(1, int(2 * s)))
    draw.polygon([(cx, top_y - int(8 * s)), (cx - int(8 * s), top_y + int(4 * s)), (cx + int(8 * s), top_y + int(4 * s))], fill=GOLD)


def icon_book(draw: ImageDraw.ImageDraw, box):
    x0, y0, x1, y1, w, h, cx, cy, s = box_metrics(box)
    pad = int(8 * s)
    bx0, by0, bx1, by1 = x0 + pad, y0 + pad, x1 - pad, y1 - pad
    draw.rounded_rectangle((bx0, by0, bx1, by1), radius=max(4, int(8 * s)), fill=(58, 34, 20, 255), outline=GOLD, width=max(2, int(3 * s)))
    spine = int(16 * s)
    draw.rectangle((bx0, by0, bx0 + spine, by1), fill=WOOD_DARK, outline=GOLD, width=max(1, int(2 * s)))
    line_y1 = by0 + int(h * 0.22)
    line_y2 = by0 + int(h * 0.38)
    draw.line([(bx0 + spine + int(12 * s), line_y1), (bx1 - int(16 * s), line_y1)], fill=GOLD_LIGHT, width=max(1, int(2 * s)))
    draw.line([(bx0 + spine + int(12 * s), line_y2), (bx1 - int(16 * s), line_y2)], fill=GOLD_LIGHT, width=max(1, int(2 * s)))
    r = int(16 * s)
    draw.ellipse((bx1 - r * 2 - int(8 * s), by0 + int(h * 0.48), bx1 - int(8 * s), by0 + int(h * 0.48) + r * 2), fill=EMERALD, outline=GOLD, width=max(1, int(2 * s)))


def icon_cards(draw: ImageDraw.ImageDraw, box):
    x0, y0, x1, y1, w, h, cx, cy, s = box_metrics(box)
    card_w = int(w * 0.28)
    card_h = int(h * 0.82)
    offsets = [(-int(w * 0.16), int(h * 0.04), -12), (0, 0, 0), (int(w * 0.16), -int(h * 0.04), 12)]
    for ox, oy, rot in offsets:
        card = Image.new("RGBA", (card_w, card_h), (0, 0, 0, 0))
        cd = ImageDraw.Draw(card)
        cd.rounded_rectangle((4, 4, card_w - 4, card_h - 4), radius=max(4, int(8 * s)), fill=CREAM, outline=GOLD, width=max(2, int(3 * s)))
        er = int(min(card_w, card_h) * 0.12)
        cd.ellipse((card_w // 2 - er, card_h // 2 - er, card_w // 2 + er, card_h // 2 + er), fill=EMERALD)
        card = card.rotate(rot, expand=True, resample=Image.Resampling.BICUBIC)
        left = cx - card_w // 2 + ox - (card.width - card_w) // 2
        top = cy - card_h // 2 + oy - (card.height - card_h) // 2
        draw._image.paste(card, (left, top), card)


def icon_judge(draw: ImageDraw.ImageDraw, box):
    x0, y0, x1, y1, w, h, cx, cy, s = box_metrics(box)
    top = y0 + int(h * 0.08)
    bar_w = int(w * 0.72)
    bar_h = max(4, int(8 * s))
    draw.rectangle((cx - bar_w // 2, top, cx + bar_w // 2, top + bar_h), fill=GOLD)
    draw.line([(cx, top), (cx, top + int(h * 0.38))], fill=GOLD, width=max(3, int(5 * s)))
    pan_r = int(w * 0.18)
    arm = int(w * 0.32)
    for side in (-1, 1):
        px = cx + side * arm
        draw.line([(px, top + bar_h), (px, top + int(h * 0.52))], fill=GOLD, width=max(2, int(4 * s)))
        py = top + int(h * 0.52)
        draw.ellipse((px - pan_r, py, px + pan_r, py + pan_r * 2), fill=GOLD_LIGHT, outline=GOLD, width=max(2, int(3 * s)))


def icon_energy(draw: ImageDraw.ImageDraw, box):
    x0, y0, x1, y1, w, h, cx, cy, s = box_metrics(box)
    r = int(min(w, h) * 0.34)
    glow = Image.new("RGBA", draw._image.size, (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)
    gd.ellipse((cx - r - int(10 * s), cy - r - int(10 * s), cx + r + int(10 * s), cy + r + int(10 * s)), fill=EMERALD_GLOW)
    draw._image.alpha_composite(glow)
    draw.ellipse((cx - r, cy - r, cx + r, cy + r), fill=(58, 34, 20, 255), outline=GOLD, width=max(2, int(4 * s)))
    inner = int(r * 0.62)
    draw.ellipse((cx - inner, cy - inner, cx + inner, cy + inner), fill=EMERALD, outline=GOLD_LIGHT, width=max(1, int(2 * s)))
    for angle in (0, 72, 144, 216, 288):
        rad = math.radians(angle - 90)
        sx = cx + int(math.cos(rad) * r * 0.55)
        sy = cy + int(math.sin(rad) * r * 0.55)
        ex = cx + int(math.cos(rad) * (r + int(16 * s)))
        ey = cy + int(math.sin(rad) * (r + int(16 * s)))
        draw.line([(sx, sy), (ex, ey)], fill=GOLD_LIGHT, width=max(2, int(3 * s)))
    draw.polygon(
        [(cx, cy - int(r * 0.75)), (cx + int(r * 0.22), cy), (cx, cy + int(r * 0.75)), (cx - int(r * 0.22), cy)],
        fill=CREAM,
        outline=GOLD,
    )


def icon_clients(draw: ImageDraw.ImageDraw, box):
    x0, y0, x1, y1, w, h, cx, cy, s = box_metrics(box)
    head_r = int(min(w, h) * 0.11)
    body_w = int(min(w, h) * 0.18)
    body_h = int(min(w, h) * 0.22)
    base_y = y1 - int(8 * s)
    offsets = [(-int(w * 0.2), 0), (0, -int(h * 0.06)), (int(w * 0.2), 0)]
    for ox, oy in offsets:
        px = cx + ox
        py = base_y + oy
        draw.ellipse((px - head_r, py - body_h - head_r * 2, px + head_r, py - body_h), fill=CREAM, outline=GOLD, width=max(1, int(2 * s)))
        draw.rounded_rectangle(
            (px - body_w // 2, py - body_h, px + body_w // 2, py),
            radius=max(3, int(6 * s)),
            fill=VELVET,
            outline=GOLD,
            width=max(1, int(2 * s)),
        )


def icon_door_client(draw: ImageDraw.ImageDraw, box):
    x0, y0, x1, y1, w, h, cx, cy, s = box_metrics(box)
    fw = int(w * 0.62)
    fh = int(h * 0.55)
    fy = cy + int(h * 0.06)
    draw.rectangle((cx - fw // 2, fy - fh // 2, cx + fw // 2, fy + fh // 2), fill=(58, 34, 20, 255), outline=GOLD, width=max(2, int(4 * s)))
    dw = int(fw * 0.28)
    draw.rectangle((cx - dw // 2, fy - int(fh * 0.35), cx + dw // 2, fy + fh // 2), fill=VELVET, outline=GOLD, width=max(2, int(3 * s)))
    knob = max(4, int(8 * s))
    draw.ellipse((cx + dw // 2 - knob * 2, fy + int(fh * 0.05), cx + dw // 2 - knob, fy + int(fh * 0.05) + knob), fill=GOLD)
    draw.line([(cx - fw // 2 - int(w * 0.08), fy + fh // 2), (cx + fw // 2 + int(w * 0.08), fy + fh // 2)], fill=GOLD, width=max(2, int(4 * s)))
    aw = int(fw * 0.35)
    ah = int(h * 0.14)
    draw.polygon([(cx - aw // 2, fy - fh // 2 - ah), (cx, fy - fh // 2 - ah - int(h * 0.08)), (cx + aw // 2, fy - fh // 2 - ah)], fill=CREAM, outline=GOLD)


def icon_accept(draw: ImageDraw.ImageDraw, box):
    x0, y0, x1, y1, w, h, cx, cy, s = box_metrics(box)
    r = int(min(w, h) * 0.38)
    draw.ellipse((cx - r, cy - r, cx + r, cy + r), fill=(58, 34, 20, 255), outline=GOLD, width=max(2, int(4 * s)))
    draw.line([(cx - int(r * 0.45), cy + int(r * 0.05)), (cx - int(r * 0.1), cy + int(r * 0.42)), (cx + int(r * 0.55), cy - int(r * 0.35))], fill=EMERALD, width=max(4, int(8 * s)))


def icon_quill(draw: ImageDraw.ImageDraw, box):
    x0, y0, x1, y1, w, h, cx, cy, s = box_metrics(box)
    draw.line([(cx - int(w * 0.28), cy + int(h * 0.32)), (cx + int(w * 0.26), cy - int(h * 0.32))], fill=GOLD, width=max(3, int(5 * s)))
    tip = int(12 * s)
    draw.polygon([(cx + int(w * 0.26), cy - int(h * 0.32)), (cx + int(w * 0.26) + tip, cy - int(h * 0.32) + tip), (cx + int(w * 0.26) - tip // 2, cy - int(h * 0.32) + tip)], fill=CREAM, outline=GOLD)
    ink_r = int(min(w, h) * 0.22)
    draw.ellipse((cx - int(w * 0.35), cy + int(h * 0.05), cx - int(w * 0.35) + ink_r * 2, cy + int(h * 0.05) + ink_r * 2), fill=(58, 34, 20, 255), outline=GOLD, width=max(1, int(2 * s)))
    draw.ellipse((cx - int(w * 0.08), cy - int(h * 0.42), cx + int(w * 0.08), cy - int(h * 0.26)), fill=EMERALD)


def icon_arrow(draw: ImageDraw.ImageDraw, box, direction: str):
    x0, y0, x1, y1, w, h, cx, cy, s = box_metrics(box)
    ah = int(h * 0.55)
    aw = int(w * 0.55)
    if direction == "left":
        points = [(x1 - int(w * 0.08), cy - ah // 2), (x0 + int(w * 0.22), cy), (x1 - int(w * 0.08), cy + ah // 2)]
        tail = (x1 - int(w * 0.06), cy, x1 + int(w * 0.06), cy)
    else:
        points = [(x0 + int(w * 0.08), cy - ah // 2), (x1 - int(w * 0.22), cy), (x0 + int(w * 0.08), cy + ah // 2)]
        tail = (x0 - int(w * 0.06), cy, x0 + int(w * 0.06), cy)
    draw.polygon(points, fill=GOLD_LIGHT, outline=GOLD, width=max(2, int(3 * s)))
    draw.line([tail[0:2], tail[2:4]], fill=GOLD, width=max(4, int(7 * s)))


def split_square_layout(plaque, icon_frac: float = 0.62, text_frac: float = 0.28):
    px0, py0, px1, py1 = plaque
    ph = py1 - py0
    ih = int(ph * icon_frac)
    th = int(ph * text_frac)
    icon_box = (px0 + 10, py0 + 10, px1 - 10, py0 + 10 + ih)
    text_box = (px0 + 6, py1 - th - 8, px1 - 6, py1 - 8)
    return icon_box, text_box


def make_square_button(name: str, label: str, icon_fn, icon_frac: float = 0.62):
    size = 512
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    plaque = draw_button_plaque(img, (10, 10, size - 10, size - 10), radius=32)
    d = ImageDraw.Draw(img)
    icon_box, text_box = split_square_layout(plaque, icon_frac=icon_frac)
    icon_fn(d, icon_box)
    _, _, _, _, _, th, _, _, _ = box_metrics(text_box)
    font = fit_font(d, label, text_box, start_size=max(36, int(th * 0.42)), min_size=22)
    center_text(d, label, text_box, font)
    path = OUT / f"UIBtn_{name}.png"
    img.save(path, "PNG")
    print(f"Wrote {path}")


def make_action_button(name: str, label: str, icon_fn):
    w, h = 640, 220
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    plaque = draw_button_plaque(img, (8, 8, w - 8, h - 8), radius=24)
    d = ImageDraw.Draw(img)
    px0, py0, px1, py1 = plaque
    icon_w = int((px1 - px0) * 0.34)
    icon_box = (px0 + 12, py0 + 12, px0 + 12 + icon_w, py1 - 12)
    text_box = (px0 + 18 + icon_w, py0 + 12, px1 - 12, py1 - 12)
    icon_fn(d, icon_box)
    font = fit_font(d, label, text_box, start_size=44, min_size=26)
    center_text(d, label, text_box, font)
    path = OUT / f"UIBtn_{name}.png"
    img.save(path, "PNG")
    print(f"Wrote {path}")


def make_hud_bar(name: str = "Bar"):
    w, h = 280, 140
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw_button_plaque(img, (4, 4, w - 4, h - 4), radius=18)
    path = OUT / f"UIHud_{name}.png"
    img.save(path, "PNG")
    print(f"Wrote {path}")


def make_icon_sprite(name: str, icon_fn, size: int = 256):
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    pad = int(size * 0.08)
    icon_fn(d, (pad, pad, size - pad, size - pad))
    path = OUT / f"UIIcon_{name}.png"
    img.save(path, "PNG")
    print(f"Wrote {path}")


def make_panel(name: str, w: int, h: int, title: str = None):
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw_button_plaque(img, (6, 6, w - 6, h - 6), radius=20)
    if title:
        d = ImageDraw.Draw(img)
        font = load_font(24, bold=True)
        center_text(d, title, (20, 14, w - 20, 52), font, fill=GOLD_LIGHT)
    path = OUT / f"UIPanel_{name}.png"
    img.save(path, "PNG")
    print(f"Wrote {path}")


def make_page_button(name: str, label: str, direction: str):
    w, h = 640, 220
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    plaque = draw_button_plaque(img, (8, 8, w - 8, h - 8), radius=24)
    d = ImageDraw.Draw(img)
    px0, py0, px1, py1 = plaque
    icon_w = int((px1 - px0) * 0.22)
    if direction == "left":
        icon_box = (px0 + 12, py0 + 12, px0 + 12 + icon_w, py1 - 12)
        text_box = (px0 + 20 + icon_w, py0 + 12, px1 - 12, py1 - 12)
    else:
        icon_box = (px1 - 12 - icon_w, py0 + 12, px1 - 12, py1 - 12)
        text_box = (px0 + 12, py0 + 12, px1 - 20 - icon_w, py1 - 12)
    icon_arrow(d, icon_box, direction)
    font = fit_font(d, label, text_box, start_size=42, min_size=24)
    center_text(d, label, text_box, font)
    path = OUT / f"UIBtn_{name}.png"
    img.save(path, "PNG")
    print(f"Wrote {path}")


def main():
    make_square_button("Tent", "TENT", icon_tent)
    make_square_button("BookOfWisdom", "BOOK OF\nWISDOM", icon_book)
    make_square_button("Cards", "CARDS", icon_cards)
    make_square_button("Judge", "JUDGE", icon_judge)
    make_page_button("PreviousPage", "PREVIOUS PAGE", "left")
    make_page_button("NextPage", "NEXT PAGE", "right")
    make_action_button("DrawCards", "DRAW CARDS", icon_cards)
    make_action_button("ReadFortune", "READ FORTUNE", icon_quill)
    make_panel("FortuneInput", 620, 260, "YOUR READING")
    make_panel("Energy", 320, 260, "MAGICAL ENERGY")
    make_action_button("MakeJudgement", "MAKE\nJUDGEMENT", icon_judge)
    make_action_button("AcceptJudgement", "ACCEPT\nJUDGEMENT", icon_accept)
    # Call-client is tiny in world space — give icon even more room
    make_square_button("CallClient", "CALL\nCLIENT IN", icon_door_client, icon_frac=0.68)
    make_hud_bar("Bar")
    make_icon_sprite("Energy", icon_energy)
    make_icon_sprite("Clients", icon_clients)
    print("Done — all PNGs use real alpha transparency (no checkerboard).")


if __name__ == "__main__":
    main()
