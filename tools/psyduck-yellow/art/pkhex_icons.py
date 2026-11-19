"""Box / artwork sprites for the custom species in the Psyduck Yellow PKHeX fork (owner order 2026-11-02).
Ten species have no national sprite: Brawleon, Venomeon, Duneon, Zephyreon, Chryseon, Strateon, Wraitheon, Wyrmeon, Ferreon and Stellareon. Each gets 68x56 RGBA pictures
b_py<i>.png (Big Pokemon Sprites), b_py<i>s.png (Big Shiny Sprites) and a_py<i>.png (Artwork Pokemon Sprites), i = index in NAMES, derived from the species' front battle sprite
(tools/eeveelution_art.py output; Stellareon's is decoded from the ROM with nitrogfx). The shiny picture is the same picture with its hue turned like the game's shiny palette.
Fossil species (indices 10-13: Raptozolt, Dracosaur, Dunklevish, Arctosaur; added 2026-11-19) are read straight from the ROM, no art dir needed: pass `new` as a fifth argument to write only those.
Usage (from yellow-heartgold): pkhex_icons.py <rom> <art dir> <work dir> <PKHeX.Drawing.PokeSprite folder> [new]"""
import colorsys
import struct
import shutil
import subprocess
import sys
from pathlib import Path
import ndspy.narc
import ndspy.rom
from PIL import Image
from eeveelution_art import SPECIES as ART

NAMES = ['Brawleon', 'Venomeon', 'Duneon', 'Zephyreon', 'Chryseon', 'Strateon', 'Wraitheon', 'Wyrmeon', 'Ferreon', 'Stellareon', 'Raptozolt', 'Dracosaur', 'Dunklevish', 'Arctosaur']
FOSSIL_SLOTS = {'Raptozolt': (1587, 150), 'Dracosaur': (1588, 100), 'Dunklevish': (1589, 140), 'Arctosaur': (1590, 120)}   # ROM species id, shiny hue (the game's own shiny palettes are hue turns of the same amount)
STELLAREON_SPECIES, STELLAREON_HUE = 1581, 150
GFX = Path(__file__).resolve().parents[1] / 'build/tools-nitrogfx/nitrogfx'
SIZE = (68, 56)
KEYS = {'front': bytes.fromhex('110a4bba')}


def unscramble(data):
    """The game's pokepic cipher (UnscanPokepic_PtHGSS): 3200 u16, seed = first word."""
    a = list(struct.unpack('<3200H', data))
    seed = a[0]
    for i in range(3200):
        a[i] ^= seed & 0xFFFF
        seed = (seed * 1103515245 + 24691) & 0xFFFFFFFF
    return struct.pack('<3200H', *a)


def rom_front(rom, species):
    """80 x 80 RGBA of a species' front battle picture read straight from the ROM (pokegra a/0/0/4: file 6*species+2 = front, +4 = palette), index 0 transparent."""
    g = ndspy.narc.NARC(rom.getFileByName('a/0/0/4'))
    raw = unscramble(bytes(g.files[6 * species + 2])[48:])
    cols = struct.unpack_from('<16H', bytes(g.files[6 * species + 4]), 0x28)
    pal = [((v & 31) * 255 // 31, ((v >> 5) & 31) * 255 // 31, ((v >> 10) & 31) * 255 // 31) for v in cols]
    im = Image.new('RGBA', (80, 80), (0, 0, 0, 0))
    px = im.load()
    for y in range(80):
        for x in range(80):
            b = raw[y * 80 + x // 2]
            i = (b >> 4) if x & 1 else (b & 15)
            if i:
                px[x, y] = pal[i] + (255,)
    return im


def front_frame(png):
    im = Image.open(png).convert('RGBA')
    return im.crop((0, 0, 80, 80))


def stellareon_front(rom, work):
    g = ndspy.narc.NARC(rom.getFileByName('a/0/0/4'))
    (work / 'st.NCGR').write_bytes(bytes(g.files[STELLAREON_SPECIES * 6 + 3]))
    (work / 'st.NCLR').write_bytes(bytes(g.files[STELLAREON_SPECIES * 6 + 4]))
    subprocess.run([str(GFX), str(work / 'st.NCGR'), str(work / 'st.png'), '-scanfronttoback', '-palette', str(work / 'st.NCLR'), '-object'], check=True)
    sheet = Image.open(work / 'st.png').convert('RGBA')
    frame = sheet.crop((0, 0, 80, 80))
    idx = Image.open(work / 'st.png')
    px = frame.load()
    ip = idx.load()
    for y in range(80):
        for x in range(80):
            if ip[x, y] == 0:
                px[x, y] = (0, 0, 0, 0)
    return frame


def fit(frame):
    box = frame.getchannel('A').getbbox()
    sprite = frame.crop(box)
    scale = min((SIZE[0] - 2) / sprite.width, (SIZE[1] - 2) / sprite.height)
    small = sprite.resize((max(1, round(sprite.width * scale)), max(1, round(sprite.height * scale))), Image.LANCZOS)
    out = Image.new('RGBA', SIZE, (0, 0, 0, 0))
    out.paste(small, ((SIZE[0] - small.width) // 2, SIZE[1] - small.height - 1), small)
    return out


def hue_shift(img, degrees):
    out = img.copy()
    px = out.load()
    for y in range(img.height):
        for x in range(img.width):
            r, g, b, a = px[x, y]
            if a:
                h, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
                rr, gg, bb = colorsys.hsv_to_rgb((h + degrees / 360) % 1.0, s, v)
                px[x, y] = (round(rr * 255), round(gg * 255), round(bb * 255), a)
    return out


def main(rom_path, art, work, target, only='all'):
    work, target, art = Path(work), Path(target), Path(art)
    work.mkdir(parents=True, exist_ok=True)
    rom = ndspy.rom.NintendoDSRom.fromFile(rom_path)
    for i, name in enumerate(NAMES):
        if only == 'new' and name not in FOSSIL_SLOTS:
            continue
        if name in FOSSIL_SLOTS:
            frame, hue = rom_front(rom, FOSSIL_SLOTS[name][0]), FOSSIL_SLOTS[name][1]
        elif name == 'Stellareon':
            frame, hue = stellareon_front(rom, work), STELLAREON_HUE
        else:
            frame, hue = front_frame(art / f'{name}_front.png'), ART[name][3]
            sheet = Image.open(art / f'{name}_front.png')
            px, sp = frame.load(), sheet.load()
            for y in range(80):
                for x in range(80):
                    if sp[x, y] == 0:
                        px[x, y] = (0, 0, 0, 0)
        pic = fit(frame)
        pic.save(target / 'Resources/img/Big Pokemon Sprites' / f'b_py{i}.png')
        pic.save(target / 'Resources/img/Artwork Pokemon Sprites' / f'a_py{i}.png')
        hue_shift(pic, hue).save(target / 'Resources/img/Big Shiny Sprites' / f'b_py{i}s.png')
    print('wrote pictures for', [n for n in NAMES if only == 'all' or n in FOSSIL_SLOTS])


if __name__ == '__main__':
    main(*sys.argv[1:6])
