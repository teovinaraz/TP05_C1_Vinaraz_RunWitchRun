#!/usr/bin/env python3
"""Genera el arte, el audio y el AudioMixer dentro de Assets/. Requiere pillow y numpy."""
import math
import os
import random
import uuid
import wave

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ASSETS = os.path.normpath(os.path.join(HERE, "..", "Assets"))


def apath(*parts):
    p = os.path.join(ASSETS, *parts)
    os.makedirs(os.path.dirname(p), exist_ok=True)
    return p


def hx(h, a=255):
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)


# ----------------------------------------------------------------------------
# Canvas de pixel art
# ----------------------------------------------------------------------------
class Canvas:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        self.px = self.im.load()

    def set(self, x, y, c):
        x, y = int(round(x)), int(round(y))
        if 0 <= x < self.w and 0 <= y < self.h:
            self.px[x, y] = c

    def setw(self, x, y, c):  # con wrap horizontal (para tiles)
        y = int(round(y))
        if 0 <= y < self.h:
            self.px[int(round(x)) % self.w, y] = c

    def get(self, x, y):
        if 0 <= x < self.w and 0 <= y < self.h:
            return self.px[x, y]
        return (0, 0, 0, 0)

    def rect(self, x0, y0, x1, y1, c):  # x1,y1 exclusivos
        for y in range(int(y0), int(y1)):
            for x in range(int(x0), int(x1)):
                self.set(x, y, c)

    def circle(self, cx, cy, r, c):
        for y in range(int(cy - r - 1), int(cy + r + 2)):
            for x in range(int(cx - r - 1), int(cx + r + 2)):
                if (x - cx) ** 2 + (y - cy) ** 2 <= r * r:
                    self.set(x, y, c)

    def ellipse(self, cx, cy, rx, ry, c):
        for y in range(int(cy - ry - 1), int(cy + ry + 2)):
            for x in range(int(cx - rx - 1), int(cx + rx + 2)):
                if ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 <= 1.0:
                    self.set(x, y, c)

    def line(self, x0, y0, x1, y1, c, wrap=False):
        x0, y0, x1, y1 = int(round(x0)), int(round(y0)), int(round(x1)), int(round(y1))
        dx, dy = abs(x1 - x0), -abs(y1 - y0)
        sx = 1 if x0 < x1 else -1
        sy = 1 if y0 < y1 else -1
        err = dx + dy
        while True:
            (self.setw if wrap else self.set)(x0, y0, c)
            if x0 == x1 and y0 == y1:
                break
            e2 = 2 * err
            if e2 >= dy:
                err += dy
                x0 += sx
            if e2 <= dx:
                err += dx
                y0 += sy

    def outline(self, c):
        src = self.im.copy().load()
        for y in range(self.h):
            for x in range(self.w):
                if src[x, y][3] == 0:
                    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                        nx, ny = x + dx, y + dy
                        if 0 <= nx < self.w and 0 <= ny < self.h and src[nx, ny][3] > 0:
                            self.px[x, y] = c
                            break

    def save(self, *parts):
        self.im.save(apath(*parts))


OUTLINE = hx("#0f0820")

# ----------------------------------------------------------------------------
# BRUJA
# ----------------------------------------------------------------------------
HAT = hx("#3a1d66")
HAT_L = hx("#5b32a0")
HAT_D = hx("#27124a")
BAND = hx("#e8b93c")
ROBE = hx("#4a2580")
ROBE_L = hx("#6b3fb0")
ROBE_D = hx("#2b1450")
SKIN = hx("#f2c9a5")
HAIR = hx("#e0602a")
HAIR_D = hx("#a83f1a")
BOOT = hx("#1d1236")
BOOT_D = hx("#120a24")
EYE = hx("#1a1030")


def draw_leg(c, hip_x, hip_y, foot_x, foot_y, col):
    c.line(hip_x, hip_y, foot_x, foot_y, col)
    c.line(hip_x + 1, hip_y, foot_x + 1, foot_y, col)
    # bota
    c.rect(foot_x - 1, foot_y - 1, foot_x + 3, foot_y + 1, BOOT)
    c.set(foot_x + 3, foot_y, BOOT)


def draw_witch(pose):
    c = Canvas(32, 32)
    dead = pose == "dead"
    jump = pose in ("jump", "dead")
    frame = int(pose[3]) if pose.startswith("run") else 0

    # --- piernas ---
    legs = {
        0: ((21, 30), (11, 27)),
        1: ((17, 30), (16, 26)),
        2: ((12, 27), (21, 30)),
        3: ((16, 26), (17, 30)),
    }
    if jump:
        a, b = (19, 27), (14, 28)
    else:
        a, b = legs[frame]
    draw_leg(c, 14, 24, b[0], b[1], BOOT_D)
    draw_leg(c, 16, 24, a[0], a[1], BOOT)

    # --- capa / dobladillo flotando hacia atras ---
    wave_ = (frame % 2) if not jump else 2
    for y in range(20, 25):
        ext = 3 + wave_ + (y - 20) // 2
        c.rect(13 - ext, y, 14, y + 1, ROBE_D)
    c.rect(9 - wave_, 23, 14, 25, ROBE_D)

    # --- torso / tunica ---
    for y in range(17, 25):
        hw = 3 + (y - 17) * 0.6
        x0, x1 = int(round(16 - hw)), int(round(16 + hw))
        for x in range(x0, x1 + 1):
            col = ROBE
            if x <= x0 + 1:
                col = ROBE_D
            elif x >= x1 - 1:
                col = ROBE_L
            c.set(x, y, col)
    # cinturon
    c.rect(12, 21, 21, 22, BAND)
    c.rect(15, 21, 17, 23, hx("#fff2b0"))

    # --- brazo ---
    if jump:
        hand = (23, 16)
    else:
        hand = [(22, 21), (22, 23), (22, 21), (22, 19)][frame]
    c.line(18, 19, hand[0], hand[1], ROBE_L)
    c.line(18, 20, hand[0], hand[1] + 1, ROBE)
    c.set(hand[0] + 1, hand[1], SKIN)
    c.set(hand[0] + 1, hand[1] + 1, SKIN)

    # --- pelo trasero ---
    c.rect(11, 12, 15, 20, HAIR)
    c.rect(10, 14, 12, 19, HAIR_D)
    if jump:
        c.rect(8, 13, 11, 18, HAIR)  # pelo al viento

    # --- cabeza ---
    c.circle(16, 14, 3.4, SKIN)
    c.rect(14, 10, 20, 12, HAIR)  # flequillo
    c.set(20, 12, HAIR)
    if dead:
        c.set(18, 13, EYE)
        c.set(19, 14, EYE)
        c.set(19, 13, EYE)
        c.set(18, 14, EYE)
    else:
        c.set(18, 14, EYE)
        c.set(19, 14, EYE)
        c.set(19, 13, hx("#ffffff"))
    c.set(20, 15, SKIN)
    c.set(18, 16, hx("#d98c7a"))

    # --- sombrero ---
    c.rect(8, 10, 25, 11, HAT_D)
    c.rect(10, 9, 23, 10, HAT)
    for y in range(2, 9):
        cx = 16 + (8 - y) * 0.7
        hw = 1.2 + (y - 2) * 0.55
        for x in range(int(round(cx - hw)), int(round(cx + hw)) + 1):
            col = HAT
            if x >= cx + hw - 1:
                col = HAT_D
            elif x <= cx - hw + 1:
                col = HAT_L
            c.set(x, y, col)
    c.set(21, 1, HAT)
    c.set(22, 1, HAT)
    c.set(23, 2, HAT_D)
    c.rect(12, 8, 21, 9, BAND)  # cinta
    c.set(16, 8, hx("#fff2b0"))

    c.outline(OUTLINE)
    if dead:
        im = c.im.rotate(38, resample=Image.NEAREST, center=(16, 20))
        # apoyar sobre el borde inferior
        bbox = im.getbbox()
        if bbox:
            shift = 31 - (bbox[3] - 1)
            im2 = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
            im2.paste(im, (0, shift))
            im = im2
        c.im = im
    return c


# ----------------------------------------------------------------------------
# OBSTACULOS
# ----------------------------------------------------------------------------
def make_tombstone():
    c = Canvas(16, 24)
    st, sh, hi, dk = hx("#7d7f9a"), hx("#5b5d78"), hx("#a3a5c0"), hx("#3c3e58")
    c.rect(3, 7, 13, 21, st)
    c.rect(5, 3, 11, 4, st)
    c.rect(4, 4, 12, 5, st)
    c.rect(3, 5, 13, 7, st)
    c.rect(3, 5, 4, 21, hi)
    c.rect(12, 5, 13, 21, sh)
    c.rect(2, 21, 14, 24, sh)
    c.rect(2, 21, 14, 22, st)
    c.rect(7, 8, 9, 17, dk)  # cruz
    c.rect(5, 10, 11, 12, dk)
    for x, y in ((4, 19), (5, 19), (6, 20), (10, 20), (11, 19), (3, 15), (4, 16)):
        c.set(x, y, hx("#3f7a4a"))
    c.set(9, 6, hx("#3f7a4a"))
    c.line(10, 14, 11, 17, dk)
    c.outline(OUTLINE)
    return c


def make_pumpkin():
    c = Canvas(20, 18)
    o, od, ol = hx("#e8791c"), hx("#b5520f"), hx("#ffa040")
    c.ellipse(10, 11, 8.5, 6.3, o)
    for x in (6, 10, 14):
        for y in range(6, 17):
            if c.get(x, y)[3]:
                c.set(x, y, od)
    c.ellipse(6, 8, 2, 1.2, ol)
    c.rect(9, 3, 11, 6, hx("#3d7a2e"))
    c.set(11, 3, hx("#3d7a2e"))
    c.set(12, 2, hx("#3d7a2e"))
    glow = hx("#ffe066")
    for x0 in (5, 12):  # ojos triangulares
        c.rect(x0, 9, x0 + 3, 10, glow)
        c.rect(x0 + 1, 8, x0 + 2, 9, glow)
    for i, x in enumerate(range(6, 15)):  # sonrisa zigzag
        c.set(x, 13 + (i % 2), glow)
    c.outline(OUTLINE)
    return c


def make_rock():
    c = Canvas(20, 12)
    widths = [6, 10, 13, 15, 16, 17, 17, 18, 18, 18]
    base, hi, sh = hx("#6a6c85"), hx("#8b8da8"), hx("#4a4c63")
    for i, w in enumerate(widths):
        y = 1 + i
        x0 = 10 - w // 2
        for x in range(x0, x0 + w):
            col = base
            if i < 4 and x < x0 + w // 2:
                col = hi
            if x >= x0 + w - 3 or i >= 8:
                col = sh
            c.set(x, y, col)
    c.line(8, 4, 10, 7, sh)
    c.line(10, 7, 9, 9, sh)
    c.set(14, 6, hx("#3f7a4a"))
    c.set(15, 6, hx("#3f7a4a"))
    c.set(4, 9, hx("#3f7a4a"))
    c.outline(OUTLINE)
    return c


def make_bush():
    c = Canvas(24, 16)
    dk, md, lt = hx("#1b4433"), hx("#256349"), hx("#3a9a6a")
    for cx, cy, r in ((7, 10, 5), (12, 8, 6.2), (17, 10, 5)):
        c.circle(cx, cy, r, dk)
    for cx, cy, r in ((7, 9, 4), (12, 7, 5.2), (17, 9, 4)):
        c.circle(cx, cy, r, md)
    for cx, cy, r in ((6, 8, 2), (11, 5, 2.5), (16, 8, 2)):
        c.circle(cx, cy, r, lt)
    c.rect(2, 12, 22, 14, dk)
    for x, y in ((5, 11), (9, 9), (14, 10), (18, 12), (12, 12)):
        c.set(x, y, hx("#a34dff"))
        c.set(x + 1, y, hx("#d29bff"))
    for x in (4, 9, 15, 20):  # espinas
        c.line(x, 8, x - 1, 5, hx("#0f2a20"))
    c.outline(OUTLINE)
    return c


def make_dead_tree():
    c = Canvas(24, 30)
    tr, sh, hi = hx("#4a3347"), hx("#2f1f2f"), hx("#6b4a63")
    for y in range(10, 30):
        w = 3 + max(0, (y - 24)) // 2
        x0 = 12 - w // 2 - (1 if y > 24 else 0)
        for x in range(x0, x0 + w + 1):
            col = tr
            if x == x0:
                col = hi
            elif x >= x0 + w:
                col = sh
            c.set(x, y, col)
    for (x0, y0, x1, y1) in ((11, 14, 4, 8), (12, 12, 20, 5), (11, 18, 6, 15), (13, 17, 19, 13), (4, 8, 2, 4), (20, 5, 22, 2)):
        c.line(x0, y0, x1, y1, tr)
        c.line(x0 + 1, y0, x1 + 1, y1, sh)
    c.set(11, 19, hx("#ffe066"))
    c.set(13, 19, hx("#ffe066"))
    c.set(12, 21, SKIN)
    c.outline(OUTLINE)
    return c


def make_cauldron():
    c = Canvas(24, 18)
    b, bl, bd = hx("#2a2540"), hx("#453e66"), hx("#15122a")
    c.ellipse(12, 11, 9.5, 6.5, b)
    c.rect(2, 6, 22, 8, bd)
    c.rect(3, 5, 21, 6, bl)
    c.ellipse(12, 6, 8, 1.6, hx("#5dff7a"))
    c.ellipse(12, 6, 5, 0.8, hx("#b6ffc0"))
    c.rect(4, 17, 7, 18, bd)
    c.rect(17, 17, 20, 18, bd)
    c.rect(5, 9, 8, 13, bl)
    for x, y in ((9, 3), (14, 2), (12, 4), (16, 4)):
        c.set(x, y, hx("#b6ffc0"))
    c.outline(OUTLINE)
    return c


# ----------------------------------------------------------------------------
# COLLECTIBLE / POWER-UP
# ----------------------------------------------------------------------------
def make_gem():
    c = Canvas(14, 14)
    cx, cy = 6.5, 6.5
    for y in range(14):
        for x in range(14):
            d1 = math.hypot(x - cx, y - cy)
            d2 = math.hypot(x - (cx + 2.6), y - (cy - 1.4))
            if d1 <= 5.6 and d2 > 4.6:
                t = (x + y) / 26.0
                if t < 0.38:
                    col = hx("#e8ffff")
                elif t < 0.55:
                    col = hx("#7cf0ff")
                else:
                    col = hx("#2fa8f0")
                c.set(x + 0, y, col)
    for x, y in ((11, 2), (11, 1), (11, 3), (10, 2), (12, 2)):
        c.set(x, y, hx("#ffffff"))
    c.outline(hx("#0b2a5a"))
    return c


def make_broom():
    c = Canvas(32, 16)
    wood, wl, wd = hx("#8a5a2b"), hx("#b57c3e"), hx("#5a3818")
    c.line(9, 9, 30, 6, wood)
    c.line(9, 8, 30, 5, wl)
    c.line(9, 10, 30, 7, wd)
    # cerdas
    straw, sl, sd = hx("#e6c35c"), hx("#fff0a0"), hx("#b8902f")
    for i, y in enumerate(range(3, 15)):
        length = 7 - abs(y - 9) // 2
        for x in range(9 - length, 10):
            col = straw
            if (x + y) % 3 == 0:
                col = sd
            if y < 7:
                col = sl if (x + y) % 2 == 0 else straw
            c.set(x, y, col)
    c.rect(9, 5, 12, 13, hx("#8f3fd0"))
    c.rect(9, 5, 10, 13, hx("#c48bff"))
    # estrellita magica en la punta
    for x, y in ((29, 2), (29, 1), (29, 3), (28, 2), (30, 2)):
        c.set(x, y, hx("#fff2b0"))
    c.outline(OUTLINE)
    return c


def make_heart():
    rows = [
        "..##....##..",
        ".####..####.",
        "############",
        "############",
        "############",
        ".##########.",
        "..########..",
        "...######...",
        "....####....",
        ".....##.....",
    ]
    c = Canvas(len(rows[0]), len(rows))
    base, light, dark = hx("#e8355a"), hx("#ff7a95"), hx("#a01e3d")
    for y, row in enumerate(rows):
        for x, ch in enumerate(row):
            if ch != "#":
                continue
            col = base
            if y < 2 and x < 6:
                col = light
            elif x > 8 and y > 4:
                col = dark
            c.set(x, y, col)
    c.outline(OUTLINE)
    return c


def make_ward():
    c = Canvas(16, 16)
    cx, cy = 7.5, 7.5
    ring, ring_l = hx("#3fe0a8"), hx("#bdfff0")
    for y in range(16):
        for x in range(16):
            d = math.hypot(x - cx, y - cy)
            if 5.6 <= d <= 7.0:
                c.set(x, y, ring_l if (x + y) % 3 == 0 else ring)
    dim, spark = hx("#a8fce6"), hx("#eafff8")
    c.line(7, 3, 7, 12, dim)
    c.line(3, 7, 12, 7, dim)
    for x, y in ((7, 2), (7, 13), (2, 7), (13, 7)):
        c.set(x, y, spark)
    c.circle(cx, cy, 1.6, spark)
    c.outline(OUTLINE)
    return c


# ----------------------------------------------------------------------------
# ENTORNO
# ----------------------------------------------------------------------------
BAYER = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]


def lerp_col(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


def make_sky(rng):
    W, H = 256, 144
    c = Canvas(W, H)
    stops = [(0.0, hx("#070818")), (0.5, hx("#141042")), (0.8, hx("#2b1a5c")), (1.0, hx("#5a3080"))]
    bands = 12

    def color_at(t):
        for i in range(len(stops) - 1):
            if stops[i][0] <= t <= stops[i + 1][0]:
                k = (t - stops[i][0]) / (stops[i + 1][0] - stops[i][0])
                return lerp_col(stops[i][1], stops[i + 1][1], k)
        return stops[-1][1][:3]

    for y in range(H):
        t = y / (H - 1)
        v = t * bands
        base = int(v)
        frac = v - base
        for x in range(W):
            b = base + (1 if frac * 16 > BAYER[x % 4][y % 4] else 0)
            col = color_at(min(1.0, b / bands))
            c.px[x, y] = col + (255,)
    for _ in range(110):
        x, y = rng.randrange(W), rng.randrange(0, 95)
        col = rng.choice([hx("#ffffff"), hx("#cfd8ff"), hx("#ffe9a8")])
        c.set(x, y, col)
        if rng.random() < 0.18:
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                c.set(x + dx, y + dy, col[:3] + (120,))
    return c


def make_moon():
    c = Canvas(48, 48)
    cx = cy = 24
    c.circle(cx, cy, 23, hx("#f4efc0", 28))
    c.circle(cx, cy, 21, hx("#f4efc0", 50))
    c.circle(cx, cy, 18, hx("#f4efc0"))
    for x in range(48):  # sombreado a la derecha
        for y in range(48):
            if (x - cx) ** 2 + (y - cy) ** 2 <= 18 * 18 and x > cx + 8 + (y % 3 == 0):
                c.set(x, y, hx("#e0d99e"))
    for (x, y, r) in ((17, 18, 3), (27, 27, 4), (20, 30, 2), (29, 16, 2)):
        c.circle(x, y, r, hx("#d3cc92"))
        c.circle(x - 1, y - 1, max(1, r - 2), hx("#e6e0a8"))
    return c


def draw_pine(c, x, base, h, w, col, dark):
    tiers = max(3, h // 11)
    for r in range(h):
        frac = r / float(h)
        tier = (r * tiers / float(h)) % 1.0
        half = w * (0.10 + 0.9 * frac) * (0.62 + 0.38 * tier) / 2.0
        y = base - h + r
        for dx in range(-int(half), int(half) + 1):
            col_ = col if dx > -int(half) // 2 else dark
            c.setw(x + dx, y, col_)
    for y in range(base - 4, base + 1):
        c.setw(x, y, dark)
        c.setw(x + 1, y, dark)


def draw_bare_tree(c, x, base, h, tw, col, dark, rng):
    for y in range(base - h, base + 1):
        w = tw + (2 if y > base - 8 else 0) - (1 if y < base - h * 0.7 and tw > 3 else 0)
        for dx in range(-w // 2, w // 2 + 1):
            c.setw(x + dx, y, dark if dx > 0 else col)
    for k in range(6):
        y0 = base - h + int(h * (0.05 + 0.75 * rng.random()))
        side = rng.choice([-1, 1])
        length = rng.randint(8, 22)
        x1 = x + side * length
        y1 = y0 - rng.randint(4, 12)
        c.line(x, y0, x1, y1, col, wrap=True)
        c.line(x, y0 + 1, x1, y1 + 1, dark, wrap=True)
        # ramita
        mx, my = (x + x1) // 2, (y0 + y1) // 2
        c.line(mx, my, mx + side * rng.randint(3, 8), my - rng.randint(4, 9), col, wrap=True)


def make_far_layer(rng):
    c = Canvas(256, 144)
    col, dark = hx("#2b2260"), hx("#231b52")
    n = 15
    for i in range(n):
        x = int(i * 256 / n + rng.randint(-6, 6))
        h = rng.randint(60, 105)
        draw_pine(c, x, 143, h, rng.randint(22, 34), col, dark)
    return c


def make_mid_layer(rng):
    c = Canvas(256, 144)
    col, dark = hx("#1b1546"), hx("#150f38")
    for i in range(7):
        x = int(i * 256 / 7 + rng.randint(-8, 8))
        if i % 2 == 0:
            draw_bare_tree(c, x, 143, rng.randint(75, 105), 5, col, dark, rng)
        else:
            draw_pine(c, x, 143, rng.randint(70, 100), rng.randint(26, 36), col, dark)
    return c


def make_near_layer(rng):
    c = Canvas(256, 144)
    col, dark = hx("#0f0b2a"), hx("#0a0720")
    for i in range(4):
        x = int(i * 256 / 4 + rng.randint(-12, 12))
        draw_bare_tree(c, x, 143, rng.randint(95, 128), 8, col, dark, rng)
    # arbustos oscuros entre medio
    for i in range(9):
        x = int(i * 256 / 9 + rng.randint(-8, 8))
        for k in range(3):
            c.circle(x + k * 4, 140 - k % 2, 5 - k, col)
    return c


def make_fog(rng, seed_phase, density):
    W, H = 256, 144
    c = Canvas(W, H)
    ph = [rng.random() * 6.28 for _ in range(4)]
    for y in range(50, H):
        profile = math.sin((y - 50) / (H - 50) * math.pi * 0.85 + 0.25)
        for x in range(W):
            a = 2 * math.pi * x / W
            n = 0.5 + 0.22 * math.sin(2 * a + ph[0] + y * 0.045 + seed_phase)
            n += 0.16 * math.sin(3 * a + ph[1] - y * 0.06)
            n += 0.12 * math.sin(7 * a + ph[2] + y * 0.11)
            v = (n * profile * density - 0.42) * 2.6
            v = max(0.0, min(1.0, v))
            levels = 4
            q = int(v * levels + (BAYER[x % 4][y % 4] / 16.0 - 0.5) * 0.9)
            q = max(0, min(levels, q))
            if q > 0:
                c.px[x, y] = hx("#b7a4ff", int(q * 9))
    return c


def make_ground(rng):
    c = Canvas(256, 32)
    g_hi, g_md, g_dk = hx("#4fb08c"), hx("#2f7d62"), hx("#1f5a48")
    d1, d2, d3, st = hx("#2a1b3d"), hx("#33224a"), hx("#1f1330"), hx("#4a3868")
    for y in range(32):
        for x in range(256):
            if y == 0:
                col = g_hi
            elif y == 1:
                col = g_md if (x + rng.randint(0, 1)) % 5 else g_hi
            elif y == 2:
                col = g_md
            elif y == 3:
                col = g_dk if (x + y) % 2 == 0 else g_md
            elif y == 4:
                col = g_dk if rng.random() < 0.6 else d1
            else:
                r = rng.random()
                col = d1
                if r < 0.18:
                    col = d2
                elif r < 0.26:
                    col = d3
                elif r < 0.30:
                    col = st
                if y > 26:
                    col = d3 if r < 0.7 else d1
            c.px[x, y] = col
    for _ in range(26):  # piedritas
        x, y = rng.randrange(256), rng.randrange(8, 28)
        for dx, dy in ((0, 0), (1, 0), (0, 1), (1, 1)):
            c.setw(x + dx, y + dy, st)
        c.setw(x, y, hx("#6a5490"))
    return c


def make_plants(rng):
    c = Canvas(256, 32)
    base = 31
    greens = [hx("#2f7d62"), hx("#3fa17e"), hx("#1f5a48")]
    for _ in range(46):  # matas de pasto
        x = rng.randrange(256)
        for b in range(rng.randint(2, 4)):
            h = rng.randint(3, 9)
            lean = rng.choice([-1, 0, 1])
            col = rng.choice(greens)
            for i in range(h):
                c.setw(x + b + (lean * i) // 3, base - i, col)
    for _ in range(11):  # hongos luminosos
        x = rng.randrange(256)
        h = rng.randint(3, 5)
        c.rect(x, base - h, x + 2, base + 1, hx("#d9cfe8"))
        capw = rng.randint(3, 4)
        for dx in range(-capw, capw + 2):
            for dy in (0, 1):
                if dy == 1 and abs(dx) > capw - 1:
                    continue
                c.setw(x + dx, base - h - 1 - dy + 1, hx("#8b3fd0"))
        c.setw(x - 1, base - h - 1, hx("#7cf0ff"))
        c.setw(x + 2, base - h, hx("#7cf0ff"))
    for _ in range(9):  # flores magicas
        x = rng.randrange(256)
        h = rng.randint(6, 11)
        for i in range(h):
            c.setw(x, base - i, greens[0])
        c.setw(x, base - h, hx("#ff7ad9"))
        c.setw(x - 1, base - h, hx("#ffb0ea"))
        c.setw(x + 1, base - h, hx("#ffb0ea"))
        c.setw(x, base - h - 1, hx("#ffb0ea"))
    for _ in range(6):  # helechos
        x = rng.randrange(256)
        for side in (-1, 1):
            for i in range(1, 7):
                c.setw(x + side * i, base - 2 - i // 2 - (1 if i > 4 else 0), greens[1])
    return c


# ----------------------------------------------------------------------------
# UI + particulas
# ----------------------------------------------------------------------------
def make_button():
    c = Canvas(32, 16)
    c.rect(0, 0, 32, 16, hx("#0e0820"))
    c.rect(1, 1, 31, 15, hx("#8f68e0"))
    c.rect(2, 2, 30, 14, hx("#4a2f8a"))
    c.rect(3, 3, 29, 13, hx("#2b1b52"))
    c.rect(3, 3, 29, 4, hx("#3d2870"))
    for x, y in ((0, 0), (31, 0), (0, 15), (31, 15)):
        c.px[x, y] = (0, 0, 0, 0)
    return c


def make_panel():
    c = Canvas(32, 32)
    c.rect(0, 0, 32, 32, hx("#0a0616"))
    c.rect(1, 1, 31, 31, hx("#7a55c8"))
    c.rect(2, 2, 30, 30, hx("#3a2470"))
    c.rect(3, 3, 29, 29, hx("#150d2e", 240))
    for x, y in ((0, 0), (31, 0), (0, 31), (31, 31), (1, 1), (30, 1), (1, 30), (30, 30)):
        c.px[x, y] = (0, 0, 0, 0)
    return c


def make_pause_icon():
    c = Canvas(16, 16)
    c.rect(3, 2, 7, 14, hx("#f2e9ff"))
    c.rect(9, 2, 13, 14, hx("#f2e9ff"))
    c.rect(3, 12, 7, 14, hx("#b9a4e8"))
    c.rect(9, 12, 13, 14, hx("#b9a4e8"))
    return c


def make_spark():
    c = Canvas(5, 5)
    w = hx("#ffffff")
    for x, y in ((2, 0), (2, 1), (0, 2), (1, 2), (2, 2), (3, 2), (4, 2), (2, 3), (2, 4)):
        c.set(x, y, w)
    return c


def make_dot():
    c = Canvas(3, 3)
    c.rect(0, 0, 3, 3, hx("#ffffff"))
    return c


# ----------------------------------------------------------------------------
# AUDIO
# ----------------------------------------------------------------------------
SR = 22050


def midi(n):
    return 440.0 * 2 ** ((n - 69) / 12.0)


def osc(kind, f, n, phase=0.0):
    t = np.arange(n) / SR
    ph = (f * t + phase) % 1.0
    if kind == "square":
        return np.where(ph < 0.5, 1.0, -1.0) * 0.6
    if kind == "pulse":
        return np.where(ph < 0.25, 1.0, -1.0) * 0.6
    if kind == "tri":
        return 2 * np.abs(2 * ph - 1) - 1
    if kind == "saw":
        return 2 * ph - 1
    return np.sin(2 * np.pi * ph)


def adsr(n, a, d, s, r):
    a, d, r = int(a * SR), int(d * SR), int(r * SR)
    e = np.ones(n) * s
    a = min(a, n)
    if a > 0:
        e[:a] = np.linspace(0, 1, a)
    d = min(d, n - a)
    if d > 0:
        e[a:a + d] = np.linspace(1, s, d)
    r = min(r, n)
    if r > 0:
        e[n - r:] *= np.linspace(1, 0, r)
    return e


def add_wrap(buf, start, sig):
    n = len(buf)
    idx = (np.arange(len(sig)) + start) % n
    np.add.at(buf, idx, sig)


def noise(n, rng):
    return np.array([rng.uniform(-1, 1) for _ in range(n)])


def kick(n=int(0.25 * SR)):
    t = np.arange(n) / SR
    f = 120 * np.exp(-t * 22) + 42
    return np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * 11)


def snare(rng, n=int(0.18 * SR)):
    t = np.arange(n) / SR
    return (noise(n, rng) * 0.8 + np.sin(2 * np.pi * 190 * t) * 0.3) * np.exp(-t * 20)


def hat(rng, n=int(0.05 * SR)):
    t = np.arange(n) / SR
    return noise(n, rng) * np.exp(-t * 70)


def save_wav(name_parts, data, gain=0.9):
    data = np.asarray(data, dtype=np.float64)
    peak = np.max(np.abs(data)) or 1.0
    data = data / peak * gain
    pcm = (np.clip(data, -1, 1) * 32767).astype("<i2")
    with wave.open(apath(*name_parts), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(pcm.tobytes())


CHORDS = {  # notas midi (raiz, tercera, quinta)
    "Am": (57, 60, 64), "F": (53, 57, 60), "Dm": (50, 53, 57),
    "E": (52, 56, 59), "C": (48, 52, 55), "G": (55, 59, 62),
}


def make_menu_music(rng):
    bpm = 78
    beat = 60.0 / bpm
    bars = 8
    prog = ["Am", "F", "Dm", "E", "Am", "F", "Dm", "E"]
    n = int(bars * 4 * beat * SR)
    buf = np.zeros(n)
    bar_n = int(4 * beat * SR)
    melody = [69, None, 72, 71, 69, None, 64, None, 65, 67, 69, None, 68, None, 71, None]
    for b, ch in enumerate(prog):
        st = b * bar_n
        r, t3, f5 = CHORDS[ch]
        # pad
        for note in (r + 12, t3 + 12, f5 + 12):
            ln = bar_n + int(0.5 * SR)
            sig = osc("tri", midi(note), ln) * adsr(ln, 0.5, 0.3, 0.7, 0.5) * 0.16
            add_wrap(buf, st, sig)
        # bajo
        ln = int(3.6 * beat * SR)
        add_wrap(buf, st, osc("sine", midi(r - 12), ln) * adsr(ln, 0.05, 0.4, 0.6, 0.3) * 0.5)
        # arpegio suave
        for i in range(8):
            note = (r, t3, f5, t3 + 12)[i % 4] + 12
            ln = int(beat * 0.55 * SR)
            sig = osc("pulse", midi(note), ln) * adsr(ln, 0.01, 0.1, 0.3, 0.15) * 0.09
            add_wrap(buf, st + int(i * beat * 0.5 * SR), sig)
        # melodia esporadica (2 compases de cada 4)
        if b % 2 == 0:
            for i, m in enumerate(melody[:8] if b % 4 == 0 else melody[8:]):
                if m is None:
                    continue
                ln = int(beat * 0.9 * SR)
                sig = osc("sine", midi(m), ln) * adsr(ln, 0.02, 0.2, 0.5, 0.4) * 0.22
                sig += osc("sine", midi(m + 12), ln) * adsr(ln, 0.01, 0.15, 0.2, 0.3) * 0.06
                add_wrap(buf, st + int(i * beat * 0.5 * SR), sig)
    # campanita de arranque
    for i, m in enumerate((81, 76)):
        ln = int(2 * beat * SR)
        add_wrap(buf, i * int(beat * 2 * SR), osc("sine", midi(m), ln) * adsr(ln, 0.005, 1.5, 0.0, 0.2) * 0.10)
    return buf


def make_gameplay_music(rng):
    bpm = 138
    beat = 60.0 / bpm
    step = beat / 4
    bars = 16
    prog = ["Am", "Am", "F", "G", "Am", "Am", "Dm", "E"] * 2
    n = int(bars * 4 * beat * SR)
    buf = np.zeros(n)
    bar_n = int(4 * beat * SR)
    lead_a = [69, 72, 76, 72, 69, 72, 77, 76, 74, 72, 71, 72, 69, 68, 64, 68]
    lead_b = [76, 77, 76, 72, 69, 72, 76, 81, 79, 77, 76, 74, 72, 71, 69, 68]
    for b, ch in enumerate(prog):
        st = b * bar_n
        r, t3, f5 = CHORDS[ch]
        # bajo en corcheas
        for i in range(8):
            ln = int(beat * 0.45 * SR)
            root = r - 12 if i % 4 != 3 else r
            sig = osc("saw", midi(root), ln) * adsr(ln, 0.005, 0.1, 0.5, 0.05) * 0.26
            add_wrap(buf, st + int(i * beat * 0.5 * SR), sig)
        # arpegio rapido
        for i in range(16):
            note = (r, t3, f5, f5 + 12)[[0, 1, 2, 3, 2, 1, 2, 1][i % 8]] + 12
            ln = int(step * 0.9 * SR)
            sig = osc("square", midi(note), ln) * adsr(ln, 0.003, 0.05, 0.4, 0.03) * 0.07
            add_wrap(buf, st + int(i * step * SR), sig)
        # melodia lead
        if b % 4 in (2, 3) or b >= 8:
            seq = lead_a if b % 2 == 0 else lead_b
            for i in range(16):
                if b % 4 in (0, 1) and i % 2:
                    continue
                m = seq[i % len(seq)] + (0 if b < 12 else 0)
                ln = int(step * 1.6 * SR)
                sig = osc("pulse", midi(m), ln) * adsr(ln, 0.005, 0.08, 0.5, 0.08) * 0.12
                add_wrap(buf, st + int(i * step * SR), sig)
        # bateria
        for beat_i in range(4):
            pos = st + int(beat_i * beat * SR)
            add_wrap(buf, pos, kick() * (0.9 if beat_i % 2 == 0 else 0.0))
            if beat_i % 2 == 1:
                add_wrap(buf, pos, snare(rng) * 0.45)
            add_wrap(buf, pos, hat(rng) * 0.18)
            add_wrap(buf, pos + int(beat * 0.5 * SR), hat(rng) * 0.12)
    return buf


# ---- SFX ----
def sfx_jump():
    n = int(0.22 * SR)
    t = np.arange(n) / SR
    f = 320 + 700 * (t / t[-1]) ** 0.8
    sig = np.sign(np.sin(2 * np.pi * np.cumsum(f) / SR)) * 0.5
    sig += np.sin(2 * np.pi * np.cumsum(f * 2) / SR) * 0.15
    return sig * adsr(n, 0.005, 0.05, 0.6, 0.08)


def sfx_land(rng):
    n = int(0.14 * SR)
    t = np.arange(n) / SR
    sig = np.sin(2 * np.pi * np.cumsum(140 * np.exp(-t * 18) + 50) / SR) * 0.9
    sig += noise(n, rng) * 0.25 * np.exp(-t * 40)
    return sig * np.exp(-t * 16)


def sfx_gem():
    parts = []
    for f, ln in ((988, 0.07), (1319, 0.07), (1760, 0.22)):
        n = int(ln * SR)
        parts.append((osc("square", f, n) * 0.45 + osc("sine", f * 2, n) * 0.2) * adsr(n, 0.003, 0.05, 0.5, ln * 0.5))
    return np.concatenate(parts)


def sfx_powerup():
    notes = [523, 659, 784, 1047, 1319, 1568, 2093]
    parts = []
    for f in notes:
        n = int(0.075 * SR)
        parts.append((osc("pulse", f, n) * 0.5 + osc("sine", f * 2, n) * 0.15) * adsr(n, 0.004, 0.03, 0.6, 0.03))
    n = int(0.5 * SR)
    tail = (osc("sine", 2093, n) * 0.4 + osc("sine", 3136, n) * 0.15) * adsr(n, 0.005, 0.3, 0.2, 0.3)
    # whoosh
    rng = random.Random(9)
    w = noise(int(0.35 * SR), rng) * np.linspace(0, 1, int(0.35 * SR)) * 0.12
    seq = np.concatenate(parts + [tail])
    seq[:len(w)] += w
    return seq


def sfx_gameover():
    parts = []
    for f, ln in ((440, 0.18), (392, 0.18), (349, 0.18), (294, 0.55)):
        n = int(ln * SR)
        t = np.arange(n) / SR
        vib = 1 + 0.01 * np.sin(2 * np.pi * 6 * t)
        parts.append((osc("tri", f, n) * 0.7 + osc("square", f / 2, n) * 0.25) * adsr(n, 0.01, 0.1, 0.7, 0.12) * vib)
    return np.concatenate(parts)


def sfx_button():
    n = int(0.07 * SR)
    return (osc("square", 660, n) * 0.4 + osc("square", 990, n) * 0.2) * adsr(n, 0.002, 0.02, 0.5, 0.03)


# ----------------------------------------------------------------------------
# AUDIOMIXER (YAML de Unity, escrito a mano)
# ----------------------------------------------------------------------------
def gid():
    return uuid.uuid4().hex


def write_mixer():
    master_gid, music_gid, sfx_gid = gid(), gid(), gid()
    m_vol, m_pit = gid(), gid()
    mu_vol, mu_pit = gid(), gid()
    sf_vol, sf_pit = gid(), gid()
    e_master, e_music, e_sfx = gid(), gid(), gid()
    mix_master, mix_music, mix_sfx = gid(), gid(), gid()
    snap_id = gid()

    def group(fid, name, g, children, vol, pit, effect):
        ch = "\n".join("  - {fileID: %d}" % c for c in children) if children else "  []"
        head = "  m_Children:\n" + ch if children else "  m_Children: []"
        return f"""--- !u!243 &{fid}
AudioMixerGroupController:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: {name}
  m_AudioMixer: {{fileID: 24100000}}
  m_GroupID: {g}
{head}
  m_Volume: {vol}
  m_Pitch: {pit}
  m_Send: 00000000000000000000000000000000
  m_Effects:
  - {{fileID: {effect}}}
  m_UserColorIndex: 0
  m_Mute: 0
  m_Solo: 0
  m_BypassEffects: 0
"""

    def effect(fid, eid, mix):
        return f"""--- !u!244 &{fid}
AudioMixerEffectController:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name:
  m_EffectID: {eid}
  m_EffectName: Attenuation
  m_MixLevel: {mix}
  m_Parameters: []
  m_SendTarget: {{fileID: 0}}
  m_EnableWetMix: 0
  m_Bypass: 0
"""

    txt = f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!241 &24100000
AudioMixerController:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: MainMixer
  m_OutputGroup: {{fileID: 0}}
  m_MasterGroup: {{fileID: 24300002}}
  m_Snapshots:
  - {{fileID: 24500006}}
  m_StartSnapshot: {{fileID: 24500006}}
  m_SuspendThreshold: -80
  m_EnableSuspend: 1
  m_UpdateMode: 0
  m_ExposedParameters:
  - guid: {mu_vol}
    name: MusicVolume
  - guid: {sf_vol}
    name: SFXVolume
  m_AudioMixerGroupViews:
  - guids:
    - {master_gid}
    - {music_gid}
    - {sfx_gid}
    name: View
  m_CurrentViewIndex: 0
  m_TargetSnapshot: {{fileID: 24500006}}
"""
    txt += group(24300002, "Master", master_gid, [24300004, 24300006], m_vol, m_pit, 24400002)
    txt += effect(24400002, e_master, mix_master)
    txt += group(24300004, "Music", music_gid, [], mu_vol, mu_pit, 24400004)
    txt += effect(24400004, e_music, mix_music)
    txt += group(24300006, "SFX", sfx_gid, [], sf_vol, sf_pit, 24400006)
    txt += effect(24400006, e_sfx, mix_sfx)
    txt += f"""--- !u!245 &24500006
AudioMixerSnapshotController:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: Snapshot
  m_AudioMixer: {{fileID: 24100000}}
  m_SnapshotID: {snap_id}
  m_FloatValues:
    {mu_vol}: 0
    {sf_vol}: 0
  m_TransitionOverrides: {{}}
"""
    with open(apath("Audio", "Mixers", "MainMixer.mixer"), "w", newline="\n") as f:
        f.write(txt)


# ----------------------------------------------------------------------------
def main():
    rng = random.Random(1337)

    make_witch = {
        "witch_run_0": "run0", "witch_run_1": "run1", "witch_run_2": "run2", "witch_run_3": "run3",
        "witch_jump": "jump", "witch_dead": "dead",
    }
    for name, pose in make_witch.items():
        draw_witch(pose).save("Art", "Characters", name + ".png")

    make_tombstone().save("Art", "Obstacles", "obstacle_tombstone.png")
    make_pumpkin().save("Art", "Obstacles", "obstacle_pumpkin.png")
    make_rock().save("Art", "Obstacles", "obstacle_rock.png")
    make_bush().save("Art", "Obstacles", "obstacle_bush.png")
    make_dead_tree().save("Art", "Obstacles", "obstacle_tree.png")
    make_cauldron().save("Art", "Obstacles", "obstacle_cauldron.png")

    make_gem().save("Art", "Collectibles", "moon_gem.png")
    make_broom().save("Art", "PowerUps", "magic_broom.png")
    make_heart().save("Art", "PowerUps", "extra_life.png")
    make_ward().save("Art", "PowerUps", "invincibility.png")

    make_sky(rng).save("Art", "Environment", "bg_sky.png")
    make_moon().save("Art", "Environment", "bg_moon.png")
    make_far_layer(rng).save("Art", "Environment", "bg_trees_far.png")
    make_mid_layer(rng).save("Art", "Environment", "bg_trees_mid.png")
    make_near_layer(rng).save("Art", "Environment", "bg_trees_near.png")
    make_fog(rng, 0.0, 1.0).save("Art", "Environment", "bg_fog_back.png")
    make_fog(rng, 2.1, 1.15).save("Art", "Environment", "bg_fog_front.png")
    make_ground(rng).save("Art", "Environment", "ground.png")
    make_plants(rng).save("Art", "Environment", "ground_plants.png")

    make_button().save("Art", "UI", "ui_button.png")
    make_panel().save("Art", "UI", "ui_panel.png")
    make_pause_icon().save("Art", "UI", "ui_pause_icon.png")
    make_spark().save("Art", "Particles", "particle_spark.png")
    make_dot().save("Art", "Particles", "particle_dot.png")

    save_wav(("Audio", "Music", "menu_music.wav"), make_menu_music(rng), 0.8)
    save_wav(("Audio", "Music", "gameplay_music.wav"), make_gameplay_music(rng), 0.85)
    save_wav(("Audio", "SFX", "sfx_jump.wav"), sfx_jump())
    save_wav(("Audio", "SFX", "sfx_land.wav"), sfx_land(rng), 0.7)
    save_wav(("Audio", "SFX", "sfx_gem.wav"), sfx_gem())
    save_wav(("Audio", "SFX", "sfx_powerup.wav"), sfx_powerup())
    save_wav(("Audio", "SFX", "sfx_gameover.wav"), sfx_gameover())
    save_wav(("Audio", "SFX", "sfx_button.wav"), sfx_button(), 0.8)

    write_mixer()
    print("Assets generados en", ASSETS)


if __name__ == "__main__":
    main()
