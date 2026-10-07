"""Extract one file from the game's Godot 4.5 .pck (pack format v3).

Entry layout (verified against the pack tail):
  u32 path_len   -- padded length, multiple of 4, includes a trailing NUL block
  path bytes     -- path_len bytes, NUL padded, no "res://" prefix
  u64 offset     -- relative to the pack header's file_base when PACK_REL_FILEBASE
  u64 size
  u8  md5[16]
  u32 flags
The directory is the trailing block of the pack; it is walked backwards from the
last entry (whose end is exactly EOF).
"""
import struct
import sys

PCK = r"E:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\SlayTheSpire2.pck"
NEEDLE = (sys.argv[1] if len(sys.argv) > 1 else "scenes/screens/card_library/card_library")
OUT = sys.argv[2] if len(sys.argv) > 2 else r"E:\games\杀戮尖塔2\newsanguo\.decompile\card_library.tscn"
FILE_BASE = 112


def padlen(n):
    # the writer pads the path to a 4-byte multiple with NULs (none when already aligned)
    return (n + 3) // 4 * 4


data = open(PCK, 'rb').read()
print('pck size=%d' % len(data))

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
        entries.append((path.decode(), off, size, cand))
        pos = cand
        moved = True
        break
    if not moved:
        break

entries.reverse()
print('walked %d entries back to offset %d' % (len(entries), pos))
print('first: %s | last: %s' % (entries[0][0], entries[-1][0]))

hits = [e for e in entries if NEEDLE in e[0]]
for e in hits[:20]:
    print('hit %-70s off=%d size=%d at=%d' % e)

target = None
for e in hits:
    if e[0].endswith('card_library.tscn') or e[0].endswith('card_library.scn'):
        target = e
        break
if target is None:
    raise SystemExit('card_library scene entry not found')

path, off, size, at = target
for base in (off, off + FILE_BASE):
    head = data[base:base + 16]
    print('try base=%d head=%r' % (base, head[:16]))
    if head[:9] in (b'[gd_scene', b'[gd_resou') or head[:4] in (b'GDSC', b'RSRC'):
        blob = data[base:base + size]
        open(OUT, 'wb').write(blob)
        print('wrote %d bytes -> %s' % (len(blob), OUT))
        break
