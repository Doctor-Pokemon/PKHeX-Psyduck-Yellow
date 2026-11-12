"""Export the expanded game's evolution table for PKHeX legality.
Usage (from the repository root): python -s tools/psyduck-yellow/generate-pkhex-expanded-evolutions.py <rom.nds>
Source: ROM NARC a/0/3/4 (hg-engine evolution data): 56 bytes per species = 9 entries of (u16 method, u16 parameter, u16 target species) + 2 zero bytes; method 0 ends the list.
Methods are hg-engine's EVO_* (1 happiness, 2 happiness day, 3 happiness night, 4 level up (parameter = level), 5 trade, 6 trade with item, 7 use item, 8-10 level with stat condition, 11/12 level with PID condition, 13/14 level (new-generation species), ..., 22-35 level with a condition, 36-48 newer methods).
Target: low 11 bits = species, high 5 bits = form (hg-engine packs the form in the target).
Output: PKHeX.Core/Resources/byte/evolution_psyduck_expanded: u16 species count; per species: u8 n, n * (u8 method, u16 parameter, u16 target)."""
import struct,sys
from pathlib import Path
import ndspy.rom,ndspy.narc
rom=ndspy.rom.NintendoDSRom.fromFile(sys.argv[1])
narc=ndspy.narc.NARC(rom.getFileByName('a/0/3/4'))
out=bytearray(struct.pack('<H',len(narc.files)))
total=0
for sp,f in enumerate(narc.files):
    f=bytes(f); assert len(f)==56 and f[54:]==b'\0\0',sp
    rows=[]
    for i in range(9):
        m,p,t=struct.unpack_from('<3H',f,i*6)
        if m==0: break
        assert m<256 and (t&0x7FF)<len(narc.files),(sp,m,t)
        rows.append((m,p,t))
    total+=len(rows)
    out+=struct.pack('<B',len(rows))+b''.join(struct.pack('<BHH',*r) for r in rows)
dest=Path(__file__).resolve().parents[2]/'PKHeX.Core/Resources/byte/evolution_psyduck_expanded'
dest.write_bytes(out)
print(len(narc.files),'species',total,'evolutions',len(out),'bytes ->',dest)
