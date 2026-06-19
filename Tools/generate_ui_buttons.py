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


def rounded_rect(draw: ImageDraw.ImageDraw, box, radius: int, fill, outline=None, width: int = 1):
    draw.rounded_rectangle(box, radius=radius, fill=fill, outline=outline, width=width)


def draw_button_plaque(base: Image.Image, box, radius: int = 28):
    x0, y0, x1, y1 = box
    layer = Image.new("RGBA", base.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)

    # Soft drop shadow (only on plaque, not full canvas)
    shadow = Image.new("RGBA", base.size, (0, 0, 0, 0))
    sd = ImageDraw.Draw(shadow)
    sd.rounded_rectangle((x0 + 6, y0 + 8, x1 + 6, y1 + 8), radius=radius, fill=(0, 0, 0, 70))
    base.alpha_composite(shadow)

    # Outer gold frame
    d.rounded_rectangle(box, radius=radius, fill=GOLD)
    inset = 6
    inner = (x0 + inset, y0 + inset, x1 - inset, y1 - inset)
    d.rounded_rectangle(inner, radius=max(8, radius - 6), fill=WOOD_DARK)

    inset2 = 14
    velvet_box = (x0 + inset2, y0 + inset2, x1 - inset2, y1 - inset2)
    d.rounded_rectangle(velvet_box, radius=max(6, radius - 10), fill=VELVET)

    # Subtle inner highlight strip
    hx0, hy0, hx1, hy1 = velvet_box
    d.line([(hx0 + 12, hy0 + 10), (hx1 - 12, hy0 + 10)], fill=VELVET_LIGHT, width=2)
    d.rounded_rectangle(velvet_box, radius=max(6, radius - 10), outline=GOLD_LIGHT, width=2)

    # Corner emerald glow dots
    for cx, cy in ((hx0 + 18, hy0 + 18), (hx1 - 18, hy0 + 18)):
        d.ellipse((cx - 8, cy - 8, cx + 8, cy + 8), fill=EMERALD_GLOW)
        d.ellipse((cx - 4, cy - 4, cx + 4, cy + 4), fill=EMERALD)

    base.alpha_composite(layer)
    return velvet_box


def center_text(draw: ImageDraw.ImageDraw, text: str, box, font, fill=CREAM, line_spacing: int = 4):
    x0, y0, x1, y1 = box
    lines = text.split("\n")
    heights = []
    widths = []
    for line in lines:
        bbox = draw.textbbox((0, 0), line, font=font)
        widths.append(bbox[2] - bbox[0])
        heights.append(bbox[3] - bbox[1])
    total_h = sum(heights) + line_spacing * (len(lines) - 1)
    cy = y0 + (y1 - y0 - total_h) // 2
    for i, line in enumerate(lines):
        w = widths[i]
        h = heights[i]
        cx = x0 + (x1 - x0 - w) // 2
        draw.text((cx, cy), line, font=font, fill=fill)
        cy += h + line_spacing


def icon_tent(draw: ImageDraw.ImageDraw, box):
    x0, y0, x1, y1 = box
    cx = (x0 + x1) // 2
    base_y = y1 - 8
    top_y = y0 + 10
    # Tent body
    draw.polygon([(cx, top_y), (x0 + 20, base_y), (x1 - 20, base_y)], fill=CREAM, outline=GOLD, width=3)
    # Stripes
    for i in range(-2, 3):
        px = cx + i * 22
        draw.line([(px, top_y + 18), (px + (i * 8), base_y - 4)], fill=VELVET, width=4)
    draw.rectangle((cx - 12, base_y - 6, cx + 12, base_y + 8), fill=WOOD_MID, outline=GOLD, width=2)
    draw.polygon([(cx, top_y - 8), (cx - 8, top_y + 4), (cx + 8, top_y + 4)], fill=GOLD)


def icon_book(draw: ImageDraw.ImageDraw, box):
    x0, y0, x1, y1 = box
    pad = 18
    bx0, by0, bx1, by1 = x0 + pad, y0 + pad, x1 - pad, y1 - pad
    draw.rounded_rectangle((bx0, by0, bx1, by1), radius=8, fill=(58, 34, 20, 255), outline=GOLD, width=3)
    draw.rectangle((bx0, by0, bx0 + 16, by1), fill=WOOD_DARK, outline=GOLD, width=2)
    draw.line([(bx0 + 28, by0 + 20), (bx1 - 16, by0 + 20)], fill=GOLD_LIGHT, width=2)
    draw.line([(bx0 + 28, by0 + 38), (bx1 - 16, by0 + 38)], fill=GOLD_LIGHT, width=2)
    draw.ellipse((bx1 - 34, by0 + 48, bx1 - 18, by0 + 64), fill=EMERALD, outline=GOLD, width=2)


def icon_cards(draw: ImageDraw.ImageDraw, box):
    x0, y0, x1, y1 = box
    cx = (x0 + x1) // 2
    cy = (y0 + y1) // 2
    w, h = 72, 98
    offsets = [(-34, 8, -12), (0, 0, 0), (34, -8, 12)]
    for ox, oy, rot in offsets:
        left = cx - w // 2 + ox
        top = cy - h // 2 + oy
        card = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        cd = ImageDraw.Draw(card)
        cd.rounded_rectangle((4, 4, w - 4, h - 4), radius=8, fill=CREAM, outline=GOLD, width=3)
        cd.ellipse((w // 2 - 10, h // 2 - 10, w // 2 + 10, h // 2 + 10), fill=EMERALD)
        card = card.rotate(rot, expand=True, resample=Image.Resampling.BICUBIC)
        draw._image.paste(card, (left - (card.width - w) // 2, top - (card.height - h) // 2), card)


def icon_judge(draw: ImageDraw.ImageDraw, box):
    x0, y0, x1, y1 = box
    cx = (x0 + x1) // 2
    top = y0 + 16
    draw.rectangle((cx - 54, top, cx + 54, top + 8), fill=GOLD)
    draw.line([(cx, top), (cx, top + 34)], fill=GOLD, width=4)
    for side in (-1, 1):
        px = cx + side * 46
        draw.line([(px, top + 8), (px, top + 52)], fill=GOLD, width=3)
        draw.ellipse((px - 22, top + 52, px + 22, top + 96), fill=GOLD_LIGHT, outline=GOLD, width=3)
    # Eye above scales
    ey = top - 6
    draw.ellipse((cx - 18, ey - 10, cx + 18, ey + 10), fill=CREAM, outline=GOLD, width=2)
    draw.ellipse((cx - 8, ey - 6, cx + 8, ey + 6), fill=EMERALD)


def icon_quill(draw: ImageDraw.ImageDraw, box):
    x0, y0, x1, y1 = box
    cx = (x0 + x1) // 2
    cy = (y0 + y1) // 2
    draw.line([(cx - 30, cy + 28), (cx + 26, cy - 30)], fill=GOLD, width=4)
    draw.polygon([(cx + 26, cy - 30), (cx + 38, cy - 18), (cx + 18, cy - 18)], fill=CREAM, outline=GOLD)
    draw.ellipse((cx - 38, cy + 10, cx - 10, cy + 38), fill=(58, 34, 20, 255), outline=GOLD, width=2)
    draw.ellipse((cx - 8, cy - 42, cx + 8, cy - 26), fill=EMERALD)


def icon_arrow(draw: ImageDraw.ImageDraw, box, direction: str):
    x0, y0, x1, y1 = box
    cy = (y0 + y1) // 2
    if direction == "left":
        points = [(x1 - 12, y0 + 8), (x0 + 24, cy), (x1 - 12, y1 - 8)]
        tail = (x1 - 10, cy, x1 + 8, cy)
    else:
        points = [(x0 + 12, y0 + 8), (x1 - 24, cy), (x0 + 12, y1 - 8)]
        tail = (x0 - 8, cy, x0 + 10, cy)
    draw.polygon(points, fill=GOLD_LIGHT, outline=GOLD, width=2)
    draw.line([tail[0:2], tail[2:4]], fill=GOLD, width=6)


def make_square_button(name: str, label: str, icon_fn):
    size = 512
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    plaque = draw_button_plaque(img, (16, 16, size - 16, size - 16), radius=32)
    d = ImageDraw.Draw(img)
    px0, py0, px1, py1 = plaque
    icon_box = (px0 + 24, py0 + 24, px1 - 24, py1 - 120)
    icon_fn(d, icon_box)
    text_box = (px0 + 12, py1 - 108, px1 - 12, py1 - 20)
    font = load_font(34, bold=True)
    center_text(d, label, text_box, font)
    path = OUT / f"UIBtn_{name}.png"
    img.save(path, "PNG")
    print(f"Wrote {path}")


def make_action_button(name: str, label: str, icon_fn):
    """Wide gameplay button — icon left, label right (Cards panel)."""
    w, h = 640, 220
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    plaque = draw_button_plaque(img, (8, 8, w - 8, h - 8), radius=24)
    d = ImageDraw.Draw(img)
    px0, py0, px1, py1 = plaque
    icon_box = (px0 + 20, py0 + 20, px0 + 130, py1 - 20)
    text_box = (px0 + 140, py0 + 20, px1 - 20, py1 - 20)
    icon_fn(d, icon_box)
    font = load_font(36, bold=True)
    center_text(d, label, text_box, font)
    path = OUT / f"UIBtn_{name}.png"
    img.save(path, "PNG")
    print(f"Wrote {path}")


def make_panel(name: str, w: int, h: int, title: str = None):
    """Decorative panel frame (Cards UI backgrounds)."""
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
    if direction == "left":
        icon_box = (px0 + 20, py0 + 20, px0 + 120, py1 - 20)
        text_box = (px0 + 130, py0 + 20, px1 - 20, py1 - 20)
    else:
        icon_box = (px1 - 120, py0 + 20, px1 - 20, py1 - 20)
        text_box = (px0 + 20, py0 + 20, px1 - 130, py1 - 20)
    icon_arrow(d, icon_box, direction)
    font = load_font(38, bold=True)
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
    print("Done — all PNGs use real alpha transparency (no checkerboard).")


if __name__ == "__main__":
    main()
