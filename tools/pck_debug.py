import struct

PCK = r"E:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\SlayTheSpire2.pck"
data = open(PCK, 'rb').read()
L = len(data)
print('len', L)
anchor = L - 128
print('anchor', anchor, struct.unpack_from('<I', data, anchor)[0], data[anchor + 4:anchor + 28])

# hexdump with absolute offsets around the anchor
start = anchor - 120
for off in range(start, anchor + 132, 16):
    chunk = data[off:off + 16]
    asc = ''.join(chr(c) if 32 <= c < 127 else '.' for c in chunk)
    print('%d  %-47s  %s' % (off, ' '.join('%02X' % c for c in chunk), asc))

# try candidate previous-entry starts under both layouts
pos = anchor
for Lp in range(1, 200):
    cand = pos - 40 - Lp
    a = struct.unpack_from('<I', data, cand)[0]
    b = struct.unpack_from('<I', data, cand + 4)[0]
    if a == Lp or b == Lp:
        path = data[cand + (0 if a == Lp else 4) + 4:][:Lp]
        print('cand L=%d cand=%d A=%d B=%d path=%r' % (Lp, cand, a, b, path))
