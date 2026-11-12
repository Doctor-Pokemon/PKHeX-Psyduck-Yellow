"""Export the expanded game's egg moves for PKHeX legality.
Usage (from the repository root): python -s private/generate-pkhex-expanded-eggmoves.py <rom.nds>
Source: ROM NARC a/2/2/9 (hg-engine egg moves), one file: 24 u16 slots per species (0xFFFF = unused), species 0..1560.
Output: PKHeX.Core/Resources/byte/egg_psyduck_expanded: u16 species count; per species: u8 n, n * u16 move."""
import struct,sys
from pathlib import Path
import ndspy.rom,ndspy.narc
rom=ndspy.rom.NintendoDSRom.fromFile(sys.argv[1])
raw=bytes(ndspy.narc.NARC(rom.getFileByName('a/2/2/9')).files[0])
assert len(raw)%48==0
count=len(raw)//48
out=bytearray(struct.pack('<H',count))
total=0
for sp in range(count):
    moves=[m for m in struct.unpack_from('<24H',raw,sp*48) if m!=0xFFFF]
    total+=len(moves)
    out+=struct.pack('<B',len(moves))+b''.join(struct.pack('<H',m) for m in moves)
dest=Path('private/PKHeX-PY-current/PKHeX.Core/Resources/byte/egg_psyduck_expanded')
dest.write_bytes(out)
print(count,'species',total,'egg moves',len(out),'bytes')
