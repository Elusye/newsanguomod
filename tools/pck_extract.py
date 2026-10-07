"""Minimal Godot 4 .pck index reader / entry extractor (read-only).

Usage:
  python tools/pck_extract.py list  <pck> <substring>
  python tools/pck_extract.py dump  <pck> <exact-res-path> <outfile>
"""
import struct
import sys


def parse(pck_path):
    f = open(pck_path, 'rb')
    magic = f.read(4)
    if magic != b'GDPC':
        raise SystemExit('not a pck: %r' % magic)
    fmt, vmaj, vmin, vpatch, flags = struct.unpack('<IIIII', f.read(20))
    file_base = struct.unpack('<Q', f.read(8))[0] if fmt >= 2 else 0
    f.seek(16 * 4, 1)
    count = struct.unpack('<I', f.read(4))[0]

    for pad_paths in (False, True):
        f.seek(0)
        f.read(4 + 20)
        if fmt >= 2:
            f.read(8)
        f.seek(16 * 4, 1)
        f.read(4)
        entries = []
        ok = True
        try:
            for _ in range(count):
                plen = struct.unpack('<I', f.read(4))[0]
                if plen == 0 or plen > 4096:
                    ok = False
                    break
                path = f.read(plen).decode('utf-8', 'replace')
                if pad_paths:
                    f.read((4 - (plen % 4)) % 4)
                off, size = struct.unpack('<QQ', f.read(16))
                f.read(16)
                eflags = struct.unpack('<I', f.read(4))[0] if fmt >= 2 else 0
                entries.append((path, off, size, eflags))
                if not path.startswith('res://'):
                    ok = False
                    break
        except Exception:
            ok = False
        if ok and entries:
            return f, fmt, file_base, entries, pad_paths
    raise SystemExit('failed to parse index (tried padded and unpadded paths)')


def main():
    mode = sys.argv[1]
    f, fmt, file_base, entries, pad_paths = parse(sys.argv[2])
    print('format=%d file_base=%d entries=%d path_pad=%s' % (fmt, file_base, len(entries), pad_paths))
    if mode == 'list':
        needle = sys.argv[3].lower()
        for path, off, size, eflags in entries:
            if needle in path.lower():
                print('%-70s off=%d size=%d flags=%s' % (path, off, size, eflags))
        return

    want = sys.argv[3]
    out = sys.argv[4]
    for path, off, size, eflags in entries:
        if path == want:
            for base in (off, off + file_base):
                try:
                    f.seek(base)
                    head = f.read(16)
                except Exception:
                    continue
                if head[:9] in (b'[gd_scene', b'[gd_reso') or head[:4] in (b'GDSC', b'RSRC'):
                    f.seek(base)
                    data = f.read(size)
                    open(out, 'wb').write(data)
                    print('wrote %d bytes from %s (base=%d) head=%r' % (len(data), path, base, head[:12]))
                    return
            print('entry found but data did not look like a scene at off/base')
            return
    raise SystemExit('entry not found: %s' % want)


main()
