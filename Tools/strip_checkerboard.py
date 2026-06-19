"""Remove baked-in transparency checkerboard from PNGs (common AI export bug).

Usage:
  python Tools/strip_checkerboard.py path/to/image.png
  python Tools/strip_checkerboard.py path/to/folder/
"""

from __future__ import annotations

import sys
from pathlib import Path

from PIL import Image


def is_checker(r: int, g: int, b: int) -> bool:
    # Light grey ~ (204,204,204) and white ~ (255,255,255) checker tiles
    if r > 235 and g > 235 and b > 235:
        return True
    if 185 <= r <= 215 and 185 <= g <= 215 and 185 <= b <= 215:
        return True
    return False


def strip_checkerboard(path: Path) -> None:
    img = Image.open(path).convert("RGBA")
    px = img.load()
    w, h = img.size
    changed = 0
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            if is_checker(r, g, b):
                px[x, y] = (0, 0, 0, 0)
                changed += 1
    if changed:
        out = path.with_name(path.stem + "_fixed.png")
        img.save(out, "PNG")
        print(f"{path.name}: made {changed} pixels transparent -> {out.name}")
    else:
        print(f"{path.name}: no checkerboard pixels found")


def main(argv: list[str]) -> int:
    if len(argv) < 2:
        print(__doc__)
        return 1
    target = Path(argv[1])
    if target.is_dir():
        for p in sorted(target.glob("*.png")):
            strip_checkerboard(p)
    else:
        strip_checkerboard(target)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))
