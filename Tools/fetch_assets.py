"""Restore the redistributable Poly Haven textures from their original URLs."""
import argparse
import hashlib
import json
import urllib.request

from preflight import MANIFEST, ROOT, verify


def restore_textures():
    assets = json.loads(MANIFEST.read_text(encoding="utf-8"))
    for name, entry in assets.items():
        url = entry.get("download")
        if not url:
            continue
        path = ROOT / name
        if path.is_file() and hashlib.sha256(path.read_bytes()).hexdigest() == entry["sha256"]:
            continue
        with urllib.request.urlopen(url, timeout=30) as response:
            data = response.read(entry["bytes"] + 1)
        if hashlib.sha256(data).hexdigest() != entry["sha256"]:
            raise ValueError(f"Checksum inválido: {name}")
        path.parent.mkdir(parents=True, exist_ok=True)
        temp = path.with_suffix(path.suffix + ".part")
        temp.write_bytes(data)
        temp.replace(path)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true", help="Verifica sem baixar.")
    if not parser.parse_args().check:
        restore_textures()
    raise SystemExit(0 if verify() else 3)
