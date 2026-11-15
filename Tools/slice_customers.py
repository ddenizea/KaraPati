"""Cut ChatGPT 4x2 customer sheets (1536x1024, #00FF00) into 256x256 transparent frames.

Usage: python3 Tools/slice_customers.py OUT_DIR id=sheet.webp[@c0,c1,..,c7] ...
(@ maps output frames to sheet cells 0-7, row-major, when the sheet strays from the layout)
Frames: <id>_walk_0..3, <id>_idle_0..1, <id>_happy_0..1.
"""
import sys, os
import numpy as np
from PIL import Image
from collections import deque


def label(mask):
    """4-connected components: (labels, count)."""
    h, w = mask.shape
    lab = np.zeros((h, w), np.int32)
    n = 0
    for y0, x0 in zip(*np.nonzero(mask)):
        if lab[y0, x0]:
            continue
        n += 1
        lab[y0, x0] = n
        q = deque([(y0, x0)])
        while q:
            y, x = q.popleft()
            for yy, xx in ((y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)):
                if 0 <= yy < h and 0 <= xx < w and mask[yy, xx] and not lab[yy, xx]:
                    lab[yy, xx] = n
                    q.append((yy, xx))
    return lab, n


def sizes_of(lab, n):
    return np.bincount(lab.ravel(), minlength=n + 1)[1:]


def dilate(m, it):
    for _ in range(it):
        d = m.copy()
        d[1:] |= m[:-1]; d[:-1] |= m[1:]; d[:, 1:] |= m[:, :-1]; d[:, :-1] |= m[:, 1:]
        m = d
    return m

COLS, ROWS, CW, CH = 4, 2, 384, 512
OUT = 256
PAD = 8
NAMES = ["walk_0", "walk_1", "walk_2", "walk_3", "idle_0", "idle_1", "happy_0", "happy_1"]


def key_green(rgb):
    r, g, b = [rgb[..., i].astype(np.float32) for i in range(3)]
    greenness = g - np.maximum(r, b)
    # fully background above 90, fully opaque below 35, soft in between
    a = np.clip((90.0 - greenness) / (90.0 - 35.0), 0, 1)
    # despill: never let green exceed the stronger of red/blue on edge pixels
    spill = a < 1
    g2 = np.where(spill, np.minimum(g, np.maximum(r, b)), g)
    out = np.dstack([r, g2, b, a * 255]).clip(0, 255).astype(np.uint8)
    return out


def clean(rgba, keep_extras):
    """Drop stray marks (sparkles) unless keep_extras; also drop tiny specks."""
    mask = rgba[..., 3] > 40
    lab, n = label(mask)
    if n == 0:
        return rgba
    sizes = sizes_of(lab, n)
    big = sizes.max()
    keep = np.zeros(n + 1, bool)
    for i, s in enumerate(sizes, 1):
        keep[i] = s >= (0.004 * big if keep_extras else 0.05 * big)
    # include soft edge pixels next to kept parts
    kept = dilate(keep[lab], 2)
    rgba = rgba.copy()
    rgba[..., 3] = np.where(kept, rgba[..., 3], 0)
    return rgba


def body_center_x(rgba):
    mask = rgba[..., 3] > 40
    lab, n = label(mask)
    sizes = sizes_of(lab, n)
    body = lab == (int(np.argmax(sizes)) + 1)
    xs = np.where(body.any(0))[0]
    return (xs[0] + xs[-1]) / 2


def process(sheet_path, cid, out_dir, order=range(8)):
    img = np.array(Image.open(sheet_path).convert("RGB"))
    assert img.shape[:2] == (CH * ROWS, CW * COLS), img.shape
    rgba = key_green(img)
    cells = []
    for k, src in enumerate(order):
        r, c = divmod(src, COLS)
        cell = rgba[r * CH:(r + 1) * CH, c * CW:(c + 1) * CW]
        cells.append(clean(cell, keep_extras=k >= 6))
    # line every frame up on its body's horizontal centre; keep the vertical motion as drawn
    shift = [CW / 2 - body_center_x(cl) for cl in cells]
    x0 = y0 = 10 ** 9
    x1 = y1 = -1
    for cl, dx in zip(cells, shift):
        ys, xs = np.where(cl[..., 3] > 8)
        x0, x1 = min(x0, xs.min() + dx), max(x1, xs.max() + dx)
        y0, y1 = min(y0, ys.min()), max(y1, ys.max())
    w, h = x1 - x0 + 1, y1 - y0 + 1
    # centred horizontally around the cell centre so the pivot is the body centre
    half_w = max(CW / 2 - x0, x1 - CW / 2)
    side = max(2 * half_w, h)
    scale = (OUT - 2 * PAD) / side
    os.makedirs(out_dir, exist_ok=True)
    for cl, dx, name in zip(cells, shift, NAMES):
        im = Image.fromarray(cl)
        canvas = Image.new("RGBA", (OUT, OUT), (0, 0, 0, 0))
        sw, sh = round(CW * scale), round(CH * scale)
        small = im.resize((sw, sh), Image.LANCZOS)
        # cell centre x -> canvas centre; bottom of the union box -> PAD above the canvas bottom
        ox = round(OUT / 2 - (CW / 2 - dx) * scale)
        oy = round(OUT - PAD - (y1 + 1) * scale)
        canvas.paste(small, (ox, oy), small)
        canvas.save(os.path.join(out_dir, f"cust_{cid}_{name}.png"))
    print(cid, "scale %.3f" % scale, "union %dx%d" % (w, h))


if __name__ == "__main__":
    out_dir = sys.argv[1]
    for arg in sys.argv[2:]:
        cid, rest = arg.split("=", 1)
        path, _, order = rest.partition("@")
        process(path, cid, out_dir, [int(x) for x in order.split(",")] if order else range(8))
