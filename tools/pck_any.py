"""List / extract entries of an arbitrary Godot 4.x .pck (same backward directory walk as pck_get.py).

usage:
  python pck_any.py <pck>                      # list every path
  python pck_any.py <pck> <needle> <outfile>   # extract the first path containing <needle>
"""
import struct
import sys

PCK = sys.argv[1]
NEEDLE = sys.argv[2] if len(sys.argv) > 2 else None
OUT = sys.argv[3] if len(sys.argv) > 3 else None
FILE_BASE = 112


def padlen(n):
    return (n + 3) // 4 * 4


data = open(PCK, 'rb').read()
print('pack %s size=%d head=%r' % (PCK, len(data), data[:8]))

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
print('entries: %d' % len(entries))

if NEEDLE is None:
    for path, off, size in entries:
        print('  %-80s size=%d' % (path, size))
    raise SystemExit(0)

hits = [e for e in entries if NEEDLE in e[0]]
print('hits: %d' % len(hits))
for e in hits[:20]:
    print('  %-80s off=%d size=%d' % e)
if not hits:
    raise SystemExit('not found')
if not OUT:
    raise SystemExit(0)

path, off, size = hits[0]
for base in (off + FILE_BASE, off):
    head = data[base:base + 16]
    if head[:5] in (b'[gd_s', b'[gd_r') or head[:4] in (b'GDSC', b'RSRC'):
        open(OUT, 'wb').write(data[base:base + size])
        print('wrote %d bytes -> %s (base=%d head=%r)' % (size, OUT, base, head[:12]))
        break
else:
    raise SystemExit('no valid base for %s' % path)
