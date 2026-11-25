"""Verify the bundled inputs used by the Unity scene generator."""
import hashlib
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "Tools" / "assets.json"
CLIPS = {
    "Idle_Loop", "Walk_Loop", "Sprint_Loop", "Pistol_Aim_Neutral",
    "Pistol_Shoot", "Death01", "Crouch_Idle_Loop", "Crouch_Fwd_Loop",
    "Jump_Loop", "Pistol_Reload", "Hit_Chest",
}


def verify() -> bool:
    good = True
    assets = json.loads(MANIFEST.read_text(encoding="utf-8"))
    for name, source in assets.items():
        path = ROOT / name
        present = path.is_file() and path.stat().st_size == source["bytes"]
        data = path.read_bytes() if present else b""
        valid = present and hashlib.sha256(data).hexdigest() == source["sha256"]
        if valid and path.name == "UAL1_Standard.fbx":
            valid = all(f"|{clip}".encode() + b"\x00" in data or
                        f"|{clip}".encode() + b"\x01" in data for clip in CLIPS)
        print(f"{'OK' if valid else 'FALHA'} {name}")
        good &= valid
    return good


if __name__ == "__main__":
    sys.exit(0 if verify() else 3)
