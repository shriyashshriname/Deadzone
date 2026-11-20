"""Grade Kenney CC0 character skins to the DEADZONE art bible (Docs/ROADMAP_NEXT.md §4).

    python scripts/grade_skins.py

Reads Art_Source/CC0/**/Skins/*.png, writes Assets/_Project/Art/Characters/Textures/T_<name>.png.
Kenney's 'characterMedium' atlas layout (fractions of texture size):
  shirt  = centre-bottom block, trousers = bottom-right block, shoes = right column.
Humans get region recolours (shirt/trousers -> palette fabric, luminance kept so folds and
dirt survive); infected get a desaturate + sickly tint grade over the whole atlas.
"""
from pathlib import Path
from PIL import Image, ImageEnhance

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "Art_Source" / "CC0"
DST = ROOT / "Assets" / "_Project" / "Art" / "Characters" / "Textures"

HEX = lambda h: tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))
OLIVE, COYOTE, OLIVE_DARK, GUNMETAL = HEX("4F5A3C"), HEX("B8A47A"), HEX("363D2A"), HEX("3A3F44")
INFECTED, SHADOW, CRIMSON = HEX("8A9479"), HEX("1D2228"), HEX("5A2328")

# (x0, y0, x1, y1) as fractions of the atlas
SHIRT = (0.150, 0.477, 0.478, 1.0)
PANTS = (0.610, 0.747, 1.0, 1.0)


def lum(p):
    return (0.299 * p[0] + 0.587 * p[1] + 0.114 * p[2]) / 255.0


def recolor_region(img, frac, target, keep_dark=0.22):
    """Map region pixels onto target colour, scaled by their luminance relative to the region median.
    Pixels darker than keep_dark (dirt, straps, outlines) are tinted only lightly."""
    w, h = img.size
    box = tuple(int(f * s) for f, s in zip(frac, (w, h, w, h)))
    px = img.load()
    lums = sorted(lum(px[x, y]) for x in range(box[0], box[2], 4) for y in range(box[1], box[3], 4))
    med = max(lums[len(lums) // 2], 1e-3)
    for x in range(box[0], box[2]):
        for y in range(box[1], box[3]):
            r, g, b, *a = px[x, y]
            l = lum((r, g, b))
            if l < keep_dark:  # dirt / dark detail: nudge toward target, keep it dark
                nr, ng, nb = (int(c * 0.75 + t * 0.25 * l / keep_dark) for c, t in zip((r, g, b), target))
            else:
                k = min(l / med, 1.6)
                nr, ng, nb = (min(255, int(t * k)) for t in target)
            px[x, y] = (nr, ng, nb, *a)


def grade(img, sat, tint, mix, bright=1.0):
    rgb, alpha = img.convert("RGB"), img.getchannel("A") if img.mode == "RGBA" else None
    rgb = ImageEnhance.Color(rgb).enhance(sat)
    rgb = Image.blend(rgb, Image.new("RGB", rgb.size, tint), mix)
    rgb = ImageEnhance.Brightness(rgb).enhance(bright)
    if alpha:
        rgb.putalpha(alpha)
    return rgb


# name -> (source skin, recipe)
SKINS = {
    "Operator":   ("survivors/Skins/survivorMaleB.png",   "operator"),
    "Squadmate":  ("survivors/Skins/survivorFemaleA.png", "squadmate"),
    "Rival":      ("protagonists/Skins/criminalMaleA.png", "rival"),
    "Walker":     ("survivors/Skins/zombieA.png",          "infected"),
    "Runner":     ("survivors/Skins/zombieC.png",          "infected_pale"),
    "Spitter":    ("retro/Skins/zombieFemaleA.png",        "infected_acid"),
    "Brute":      ("retro/Skins/zombieMaleA.png",          "infected_dark"),
}


def build(name, rel, recipe):
    img = Image.open(SRC / rel).convert("RGBA")
    if recipe == "operator":
        recolor_region(img, SHIRT, OLIVE); recolor_region(img, PANTS, OLIVE_DARK)
        img = grade(img, 0.85, SHADOW, 0.06)
    elif recipe == "squadmate":
        recolor_region(img, SHIRT, COYOTE); recolor_region(img, PANTS, OLIVE)
        img = grade(img, 0.85, SHADOW, 0.06)
    elif recipe == "rival":
        recolor_region(img, SHIRT, CRIMSON); recolor_region(img, PANTS, GUNMETAL)
        img = grade(img, 0.8, SHADOW, 0.10)
    elif recipe == "infected":
        img = grade(img, 0.55, INFECTED, 0.22, 0.92)
    elif recipe == "infected_pale":
        img = grade(img, 0.45, INFECTED, 0.15, 1.0)
    elif recipe == "infected_acid":
        img = grade(img, 0.75, HEX("7FA23A"), 0.25, 0.95)
    elif recipe == "infected_dark":
        img = grade(img, 0.5, SHADOW, 0.25, 0.9)
    out = DST / f"T_{name}.png"
    img.save(out)
    print(f"{out.relative_to(ROOT)}  {img.size}")


if __name__ == "__main__":
    DST.mkdir(parents=True, exist_ok=True)
    for n, (rel, recipe) in SKINS.items():
        build(n, rel, recipe)
