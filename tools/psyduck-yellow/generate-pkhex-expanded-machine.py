"""Export the expanded game's TM/HM and move tutor compatibility for PKHeX legality.
Usage (from the repository root): python -s tools/psyduck-yellow/generate-pkhex-expanded-machine.py <rom.nds>
Sources (hg-engine, ROM verified 2026-11-12, ROM 0255): the machine list sMachineMoves is a u16 array in overlay 129 at 0x023DD750 (340 engine entries, then the seven Yellow TMs: Bide, Water Gun,
Mega Punch, Whirlwind, BubbleBeam, Thunder Wave, Seismic Toss; 347 in all); per-species compatibility is a bit field of 11 u32 words in NARC a/0/2/8 member 14 (bit i = sMachineMoves[i]);
the tutor table is (u16 move, u8 cost, u8 tutor) rows at overlay 1 offset 0x23AE0; per-species tutor compatibility is 2 u32 words in a/0/2/8 member 15 (bit i = tutor row i).
Output: PKHeX.Core/Resources/byte/machine_psyduck_expanded: u16 species count; per species: u16 n, n * u16 machine move, u16 m, m * u16 tutor move."""
import struct,sys
from pathlib import Path
import ndspy.rom,ndspy.narc
rom=ndspy.rom.NintendoDSRom.fromFile(sys.argv[1])
ov=rom.loadArm9Overlays()
perm=ov[129]; base=perm.ramAddress
MACHINE_AT,MACHINE_COUNT=0x23DD750,347
machines=list(struct.unpack_from(f'<{MACHINE_COUNT}H',bytes(perm.data),MACHINE_AT-base))
assert machines[:6]==[264,337,352,347,46,92] and machines[340:]==[117,55,5,18,61,86,69] and all(0<m<2000 for m in machines)
o1=bytes(ov[1].data); tutors=[]
at=0x23AE0
while True:
    move,cost,tutor=struct.unpack_from('<HBB',o1,at+4*len(tutors))
    if move==0 or move>2000 or tutor>3 or len(tutors)>=64: break
    tutors.append(move)
assert tutors[:3]==[291,189,146-0] or tutors[0]==291,tutors[:5]
narc=ndspy.narc.NARC(rom.getFileByName('a/0/2/8'))
mach=bytes(narc.files[14]); tut=bytes(narc.files[15])
count=len(mach)//44; assert count*44==len(mach) and len(tut)==count*8
out=bytearray(struct.pack('<H',count))
total=0
for sp in range(count):
    bits=struct.unpack_from('<11I',mach,sp*44)
    mm=[machines[i] for i in range(MACHINE_COUNT) if bits[i//32]>>(i%32)&1]
    tb=struct.unpack_from('<2I',tut,sp*8)
    tm=[tutors[i] for i in range(len(tutors)) if tb[i//32]>>(i%32)&1]
    total+=len(mm)+len(tm)
    out+=struct.pack('<H',len(mm))+b''.join(struct.pack('<H',m) for m in mm)+struct.pack('<H',len(tm))+b''.join(struct.pack('<H',m) for m in tm)
dest=Path(__file__).resolve().parents[2]/'PKHeX.Core/Resources/byte/machine_psyduck_expanded'
dest.write_bytes(out)
print(count,'species',len(tutors),'tutor moves',total,'entries',len(out),'bytes ->',dest)
