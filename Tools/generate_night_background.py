#!/usr/bin/env python3
"""Genera el cielo y las capas de ciudad de Assets/Art/Environment/Night (solo biblioteca estándar)."""
import os, random, struct, zlib

OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "Art", "Environment", "Night")
PPU = 96
W, H = 32 * PPU, 12 * PPU            # capas de ciudad: 32 x 12 unidades, base en y = -2
HORIZON = H - 2 * PPU                # fila de y = 0


def lerp(a, b, t): return tuple(a[i] + (b[i] - a[i]) * t for i in range(3))
def px(c, a=255): return bytes((int(c[0]), int(c[1]), int(c[2]), a))


def save(name, w, h, rows):
    raw = b"".join(b"\0" + bytes(r) for r in rows)
    chunk = lambda t, d: struct.pack(">I", len(d)) + t + d + struct.pack(">I", zlib.crc32(t + d))
    with open(os.path.join(OUT, name), "wb") as f:
        f.write(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0))
                + chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b""))


def sky():
    rnd = random.Random(7)
    w, h = 1024, 640
    top, mid, low = (17, 15, 32), (45, 37, 66), (112, 94, 112)
    rows = []
    for y in range(h):
        t = y / (h - 1)
        c = lerp(top, mid, t / .6) if t < .6 else lerp(mid, low, (t - .6) / .4)
        rows.append(bytearray(b"".join(px(tuple(v + rnd.random() for v in c)) for _ in range(w))))
    for _ in range(150):                                   # estrellas apagadas
        x, y = rnd.randrange(w), int(rnd.random() ** 1.6 * h * .55)
        k = rnd.uniform(.15, .55)
        base = rows[y][x * 4:x * 4 + 3]
        rows[y][x * 4:x * 4 + 3] = bytes(int(v) for v in lerp(tuple(base), (235, 222, 210), k))
    cx, cy, r = 655, 225, 22                               # luna cálida con halo
    for y in range(cy - 150, cy + 150):
        for x in range(cx - 150, cx + 150):
            d = ((x - cx) ** 2 + (y - cy) ** 2) ** .5
            k = max(0., min(1., r - d + .5)) * .85 + max(0., 1 - d / 150) ** 2 * .22
            i = x * 4
            rows[y][i:i + 3] = bytes(int(v) for v in lerp(tuple(rows[y][i:i + 3]), (238, 222, 196), min(1, k)))
    save("night_sky.png", w, h, rows)


def city(name, seed, wall, haze, hmin, hmax, wmin, wmax, gap, lit, win):
    rnd = random.Random(seed)
    rows = [bytearray(px(wall, 0) * W) for _ in range(H)]
    wall_at = [lerp(wall, haze, max(0., min(1., 1 - (HORIZON - y) / (4.2 * PPU))) ** 1.4 * .85) for y in range(H)]

    def rect(x0, y0, x1, y1, shade=0.):
        x0, x1, y0, y1 = max(0, int(x0)), min(W, int(x1)), max(0, int(y0)), min(H, int(y1))
        for y in range(y0, y1):
            c = tuple(max(0, min(255, v + shade)) for v in wall_at[y])
            rows[y][x0 * 4:x1 * 4] = px(c) * (x1 - x0)

    def window(x, y, w, h, k):
        for yy in range(int(y), min(H, int(y + h))):
            rows[yy][int(x) * 4:int(x + w) * 4] = px(lerp(wall_at[yy], win, k)) * int(w)

    x = 0.
    while x < W - wmin * PPU:
        bw = min(rnd.uniform(wmin, wmax) * PPU, W - x)
        top = HORIZON - rnd.uniform(hmin, hmax) * PPU
        rect(x, top, x + bw, H)
        rect(x, top, x + 6, H, 9); rect(x + bw - 10, top, x + bw, H, -9)     # canto de luz y de sombra
        rect(x - 5, top - 9, x + bw + 5, top, -6)                            # cornisa
        kind = rnd.random()
        if kind < .35:                                                        # antena
            ax = x + bw * rnd.uniform(.25, .75)
            rect(ax, top - 95, ax + 4, top); rect(ax - 16, top - 72, ax + 20, top - 69); rect(ax - 10, top - 52, ax + 14, top - 49)
        elif kind < .6:                                                       # depósito
            tx = x + bw * .3
            rect(tx, top - 70, tx + 58, top - 22); rect(tx - 4, top - 76, tx + 62, top - 70, -8)
            for lx in (4, 48): rect(tx + lx, top - 22, tx + lx + 6, top)
        elif kind < .85:                                                      # casetón
            rect(x + bw * .15, top - 40, x + bw * .5, top, -5)
        ww, wh, sx, sy = 20, 28, 48, 62
        n = int((bw - 40) // sx)
        ox = x + (bw - (n * sx - (sx - ww))) / 2
        y = top + 34
        while y < H - 40:
            for i in range(n):
                on = rnd.random() < lit
                if on: window(ox + i * sx, y, ww, wh, rnd.uniform(.45, .8))
                else: rect(ox + i * sx, y, ox + i * sx + ww, y + wh, -10)
            y += sy
        x += bw + rnd.uniform(0, gap) * PPU
    save(name, W, H, rows)


os.makedirs(OUT, exist_ok=True)
sky()
city("city_far.png", 11, (66, 57, 82), (116, 100, 116), 3.4, 8.2, 1.4, 2.8, .5, .05, (214, 170, 112))
city("city_mid.png", 23, (74, 64, 84), (124, 108, 120), 1.6, 5.4, 2.0, 3.6, 2.4, .10, (232, 178, 106))
