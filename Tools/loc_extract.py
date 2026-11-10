#!/usr/bin/env python3
"""
Collect every text the game shows into Assets/Resources/Lang/strings.csv.

The code writes texts in Turkish inside L.T("..."), L.F("...", args) or L.N("..."). This script finds them all
and updates the table: new texts get a row with an empty English column, translations already in the table are
kept, rows whose Turkish text no longer exists in the code are dropped (and listed, in case one was only reworded).

Run from the project folder:  python3 Tools/loc_extract.py
"""
import csv, glob, io, os, re, sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CSV_PATH = os.path.join(ROOT, "Assets", "Resources", "Lang", "strings.csv")
LANGS = ["tr", "en"]

CALL = re.compile(r'\bL\.(?:T|F|N)\(\s*"((?:[^"\\]|\\.)*)"')
ESC = {"n": "\n", "t": "\t", '"': '"', "\\": "\\", "'": "'", "0": "\0"}


def unescape(s):
    out, i = [], 0
    while i < len(s):
        c = s[i]
        if c == "\\" and i + 1 < len(s):
            n = s[i + 1]
            if n == "u":
                out.append(chr(int(s[i + 2:i + 6], 16))); i += 6; continue
            out.append(ESC.get(n, n)); i += 2; continue
        out.append(c); i += 1
    return "".join(out)


def to_cell(s):   # line breaks are written as \n so each text stays on one line of the table
    return s.replace("\n", "\\n")


def from_cell(s):
    return s.replace("\\n", "\n")


def main():
    found, where = [], {}
    for path in sorted(glob.glob(os.path.join(ROOT, "Assets", "Scripts", "**", "*.cs"), recursive=True)):
        if os.sep + "Editor" + os.sep in path:
            continue
        text = open(path, encoding="utf-8").read()
        for m in CALL.finditer(text):
            s = unescape(m.group(1))
            if not s.strip():
                continue
            if s not in where:
                found.append(s)
                where[s] = os.path.relpath(path, ROOT)

    old = {}
    if os.path.exists(CSV_PATH):
        with open(CSV_PATH, encoding="utf-8-sig", newline="") as f:
            rows = list(csv.reader(f))
        if rows:
            head = rows[0]
            for r in rows[1:]:
                if not r or not r[0]:
                    continue
                old[from_cell(r[0])] = {head[i]: r[i] for i in range(1, min(len(head), len(r)))}

    os.makedirs(os.path.dirname(CSV_PATH), exist_ok=True)
    buf = io.StringIO()
    w = csv.writer(buf, lineterminator="\n")
    w.writerow(LANGS)
    missing = 0
    for s in found:
        prev = old.get(s, {})
        cells = [to_cell(s)] + [prev.get(l, "") for l in LANGS[1:]]
        if not cells[1]:
            missing += 1
        w.writerow(cells)
    with open(CSV_PATH, "w", encoding="utf-8-sig", newline="") as f:
        f.write(buf.getvalue())

    gone = [k for k in old if k not in where]
    print(f"{len(found)} metin, {missing} tanesi henüz çevrilmedi -> {os.path.relpath(CSV_PATH, ROOT)}")
    if gone:
        print(f"Koddan kalkan {len(gone)} satır silindi:")
        for k in gone:
            print("  -", k.replace("\n", "\\n")[:90])
    if "--missing" in sys.argv:
        for s in found:
            if not old.get(s, {}).get("en"):
                print(f"  [{where[s]}] {to_cell(s)}")


if __name__ == "__main__":
    main()
