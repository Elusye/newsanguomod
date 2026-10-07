"""Dump printable strings from a Godot binary scene (.scn) so node names / property values can be inspected.

usage: python tools/dump_scn_strings.py <file.scn> [minlen]
"""
import re
import sys

path = sys.argv[1]
minlen = int(sys.argv[2]) if len(sys.argv) > 2 else 2
data = open(path, "rb").read()
print(f"size={len(data)}")
for m in re.finditer(rb"[\x20-\x7e]{%d,}" % minlen, data):
    print(f"{m.start():7d}  {m.group().decode('ascii')}")
