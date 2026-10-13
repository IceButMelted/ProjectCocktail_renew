"""Yarn contract check for the Managers restructure (Docs/Bar410_Managers_Restructure_Plan.md).

Registered surface = every active [YarnCommand("x")] / [YarnFunction("x")] in Assets/**/*.cs.
Used commands      = every <<name ...>> in Assets/**/*.yarn (Yarn built-ins excluded).

  python check_yarn_contract.py            write yarn_surface.txt and yarn_used.txt (baseline)
  python check_yarn_contract.py --check    compare the working tree against the baseline, exit 1 on a break

A break is: a baseline name that is no longer registered, or a used command that is not registered.
Adding a new name is reported but is not a break.
"""
import pathlib
import re
import sys

HERE = pathlib.Path(__file__).resolve().parent
ROOT = HERE.parents[2]
ASSETS = ROOT / "Assets"

BUILTIN = {"if", "elseif", "else", "endif", "set", "declare", "jump", "detour", "return",
           "enum", "endenum", "case", "once", "endonce", "stop", "wait"}

REG_RE = re.compile(r'^\s*\[(YarnCommand|YarnFunction)\("([^"]+)"\)\]', re.M)
USE_RE = re.compile(r"<<\s*([A-Za-z_]\w*)")


def rel(p):
    return str(p.relative_to(ROOT)).replace("\\", "/")


def registered():
    out = {}
    for p in ASSETS.rglob("*.cs"):
        text = p.read_text(encoding="utf-8", errors="ignore")
        for m in REG_RE.finditer(text):
            out[m.group(2)] = (m.group(1), rel(p))
    return out


def used():
    out = {}
    for p in ASSETS.rglob("*.yarn"):
        for line in p.read_text(encoding="utf-8", errors="ignore").splitlines():
            if line.lstrip().startswith("//"):
                continue
            for m in USE_RE.finditer(line):
                name = m.group(1)
                if name in BUILTIN:
                    continue
                count, files = out.get(name, (0, set()))
                files.add(rel(p))
                out[name] = (count + 1, files)
    return out


def write_baseline(reg, use):
    lines = [f"{kind}\t{name}\t{src}" for name, (kind, src) in sorted(reg.items())]
    (HERE / "yarn_surface.txt").write_text("\n".join(lines) + "\n", encoding="utf-8")
    lines = [f"{name}\t{count}\t{','.join(sorted(files))}" for name, (count, files) in sorted(use.items())]
    (HERE / "yarn_used.txt").write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"baseline written: {len(reg)} registered, {len(use)} used command names")


def check(reg, use):
    base = set()
    for line in (HERE / "yarn_surface.txt").read_text(encoding="utf-8").splitlines():
        base.add(line.split("\t")[1])
    now = set(reg)
    vanished = sorted(base - now)
    added = sorted(now - base)
    unregistered = sorted(n for n in use if n not in reg)
    print(f"registered now={len(now)} baseline={len(base)}")
    print("vanished from baseline:", vanished or "none")
    print("added since baseline:  ", added or "none")
    print("used in .yarn but not registered:", unregistered or "none")
    return 1 if vanished or unregistered else 0


if __name__ == "__main__":
    reg, use = registered(), used()
    if "--check" in sys.argv:
        sys.exit(check(reg, use))
    write_baseline(reg, use)
