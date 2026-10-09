#!/usr/bin/env python3
"""Remove project-local Godot caches from an exported mod PCK."""

from __future__ import annotations

import hashlib
import struct
import sys
from pathlib import Path

PACK_MAGIC = b"GDPC"
PACK_FORMAT_V3 = 3
PACK_REL_FILEBASE = 0x2
PCK_PADDING = 16
HEADER_SIZE = 104
EXCLUDED_ENTRIES = {
    ".godot/global_script_class_cache.cfg",
    ".godot/uid_cache.bin",
}


def _pad(alignment: int, n: int) -> int:
    rest = n % alignment
    return 0 if rest == 0 else alignment - rest


def read_pck(path: Path) -> tuple[int, int, int, int, int, list[tuple[str, bytes]]]:
    blob = path.read_bytes()
    if blob[:4] != PACK_MAGIC:
        raise ValueError(f"{path} is not a Godot PCK")

    version, major, minor, patch, flags = struct.unpack_from("<IIIII", blob, 4)
    if version != PACK_FORMAT_V3:
        raise ValueError(f"unsupported pack version {version}")

    file_base, directory_offset = struct.unpack_from("<QQ", blob, 24)
    offset = directory_offset
    file_count = struct.unpack_from("<I", blob, offset)[0]
    offset += 4

    files: list[tuple[str, bytes]] = []
    for _ in range(file_count):
        path_length = struct.unpack_from("<I", blob, offset)[0]
        offset += 4
        raw_name = blob[offset : offset + path_length]
        name = raw_name.split(b"\x00", 1)[0].decode("utf-8")
        offset += path_length
        file_offset, size = struct.unpack_from("<QQ", blob, offset)
        offset += 16
        offset += 16  # md5
        entry_flags = struct.unpack_from("<I", blob, offset)[0]
        offset += 4
        if entry_flags:
            raise ValueError(f"encrypted/removal PCK entry not supported: {name}")
        files.append((name, blob[file_base + file_offset : file_base + file_offset + size]))

    return version, major, minor, patch, flags, files


def write_pck(path: Path, major: int, minor: int, patch: int, files: list[tuple[str, bytes]]) -> None:
    files = sorted(files, key=lambda item: item[0])
    file_base = HEADER_SIZE + _pad(PCK_PADDING, HEADER_SIZE)

    payload = bytearray()
    payload.extend(PACK_MAGIC)
    payload.extend(struct.pack("<IIIII", PACK_FORMAT_V3, major, minor, patch, PACK_REL_FILEBASE))
    file_base_offset = len(payload)
    payload.extend(struct.pack("<Q", 0))
    directory_offset_position = len(payload)
    payload.extend(struct.pack("<Q", 0))
    payload.extend(b"\x00" * 64)
    payload.extend(b"\x00" * _pad(PCK_PADDING, len(payload)))
    struct.pack_into("<Q", payload, file_base_offset, file_base)

    directory: list[tuple[str, int, int, bytes]] = []
    for name, data in files:
        file_offset = len(payload) - file_base
        payload.extend(data)
        payload.extend(b"\x00" * _pad(PCK_PADDING, len(payload)))
        directory.append((name, file_offset, len(data), hashlib.md5(data).digest()))

    payload.extend(b"\x00" * _pad(PCK_PADDING, len(payload)))
    directory_offset = len(payload)
    struct.pack_into("<Q", payload, directory_offset_position, directory_offset)

    payload.extend(struct.pack("<I", len(directory)))
    for name, file_offset, size, md5 in directory:
        encoded = name.encode("utf-8")
        name_padding = _pad(4, len(encoded))
        payload.extend(struct.pack("<I", len(encoded) + name_padding))
        payload.extend(encoded)
        payload.extend(b"\x00" * name_padding)
        payload.extend(struct.pack("<QQ", file_offset, size))
        payload.extend(md5)
        payload.extend(struct.pack("<I", 0))

    path.write_bytes(payload)


def sanitize_pck(path: Path) -> int:
    _version, major, minor, patch, flags, entries = read_pck(path)
    if flags & ~PACK_REL_FILEBASE:
        raise ValueError(f"unsupported pack flags {flags:#x}")

    retained = [(name, data) for name, data in entries if name not in EXCLUDED_ENTRIES]
    removed = len(entries) - len(retained)
    write_pck(path, major, minor, patch, retained)
    return removed


def main() -> int:
    if len(sys.argv) != 2:
        print("usage: sanitize_pck.py <pack.pck>", file=sys.stderr)
        return 2

    path = Path(sys.argv[1])
    removed = sanitize_pck(path)
    print(f"Wrote {path.name}: removed {removed} conflicting Godot cache files")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
