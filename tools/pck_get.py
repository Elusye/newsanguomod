"""Extract one file from the game's Godot 4.5 .pck by path substring.

usage: python pck_get.py <needle> <outfile> [suffix]

Same directory walk as pck_find.py (verified against the pack tail).
"""
import struct
import sys

PCK = r"E:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\SlayTheSpire2.pck"
NEEDLE = sys.argv[1]
OUT = sys.argv[2]
SUFFIX = sys.argv[3] if len(sys.argv) > 3 else ".tscn"
FILE_BASE = 112


def padlen(n):
    return (n + 3) // 4 * 4


data = open(PCK, 'rb').read()

entries = []
pos = len(data)
while pos > FILE_BASE:
    moved = False
    for L in range(1, 1025):
        cand = pos - 40 - L
        if cand < FILE_BASE:
            break
        if struct.unpack_from('<I', data, cand)[0] != L:
            continue
        raw = data[cand + 4:cand + 4 + L]
        path = raw.rstrip(b'\x00')
        if not path or padlen(len(path)) != L:
            continue
        if any(c < 32 or c > 126 for c in path):
            continue
        off, size = struct.unpack_from('<QQ', data, cand + 4 + L)
        if size <= 0 or off + size > len(data):
            continue
        entries.append((path.decode(), off, size))
        pos = cand
        moved = True
        break
    if not moved:
        break

entries.reverse()
hits = [e for e in entries if NEEDLE in e[0] and e[0].endswith(SUFFIX)]
print('hits: %d' % len(hits))
for e in hits[:20]:
    print('  %-70s off=%d size=%d' % e)
if not hits:
    raise SystemExit('not found')

path, off, size = hits[0]
for base in (off + FILE_BASE, off):
    head = data[base:base + 16]
    if head[:5] in (b'[gd_s', b'[gd_r') or head[:4] in (b'GDSC', b'RSRC'):
        blob = data[base:base + size]
        open(OUT, 'wb').write(blob)
        print('wrote %d bytes -> %s (base=%d head=%r)' % (len(blob), OUT, base, head[:12]))
        break
else:
    raise SystemExit('no valid base for %s' % path)
