"""Build a self-contained Costa de Ferro HTML using local esbuild and Three.js."""
import argparse
import base64
import html
import json
import os
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WEB = ROOT / "WebGame"
CONTENT = ROOT / "Assets" / "Content"
FRONT = WEB / "assets/front"
ASSETS = {
    "soldier": (WEB / "assets/soldier.glb", "model/gltf-binary"),
    "brasaHead": (WEB / "assets/operators/brasa.glb", "model/gltf-binary"),
    "irisHead": (WEB / "assets/operators/iris.glb", "model/gltf-binary"),
    "weapon": (WEB / "assets/rifle.glb", "model/gltf-binary"),
    "distantWeapon": (WEB / "assets/rifle-distant.glb", "model/gltf-binary"),
    "outcrop": (WEB / "assets/outcrop.glb", "model/gltf-binary"),
    "tree": (WEB / "assets/tree.glb", "model/gltf-binary"),
    "distantTree": (WEB / "assets/tree-distant.glb", "model/gltf-binary"),
    "coastalSky": (WEB / "assets/mobile/coastal-sky.webp", "image/webp"),
    **{key: (WEB / f"assets/mobile/{key}.jpg", "image/jpeg")
       for key in ("concrete", "asphalt", "grass", "stone")},
    **{key + "Normal": (WEB / f"assets/mobile/{key}-normal.jpg", "image/jpeg")
       for key in ("concrete", "asphalt", "grass", "stone")},
    **{key + "HD": (WEB / f"assets/hd/{key}.webp", "image/webp")
       for key in ("concrete", "asphalt", "grass", "stone")},
    **{key + "NormalHD": (WEB / f"assets/hd/{key}-normal.webp", "image/webp")
       for key in ("concrete", "asphalt", "grass", "stone")},
    "shot": (CONTENT / "Shot.wav", "audio/wav"),
}


def build(output, modules, firebase=None):
    process = subprocess.run(
        [str(modules / ".bin/esbuild"), str(WEB / "game.js"),
         "--bundle", "--format=iife", "--minify", "--legal-comments=inline"],
        env={**os.environ, "NODE_PATH": str(modules)},
        capture_output=True, text=True, check=True,
    )
    if "</script" in process.stdout.lower():
        raise ValueError("Bundle contém fechamento de script HTML.")
    payload = {}
    for key, (path, mime) in ASSETS.items():
        payload[key] = f"data:{mime};base64," + base64.b64encode(path.read_bytes()).decode("ascii")
    settings = json.dumps(firebase).replace("<", r"\u003c")
    js = f"<script>window.ZERO_FIREBASE={settings};window.ZERO_ASSETS={json.dumps(payload)};</script>\n<script>{process.stdout}</script>"
    credits = (WEB / "CREDITOS.txt").read_text(encoding="utf-8")
    three_license = (modules / "three/LICENSE").read_text(encoding="utf-8")
    firebase_license = (WEB / "licenses/Apache-2.0.txt").read_text(encoding="utf-8")
    page = (WEB / "index.html").read_text(encoding="utf-8")
    art = base64.b64encode((FRONT / "coast-entry.webp").read_bytes()).decode("ascii")
    font = base64.b64encode((FRONT / "barlow-condensed-bold.woff2").read_bytes()).decode("ascii")
    front_assets = (f':root {{ --coast-art: url("data:image/webp;base64,{art}"); }}\n'
                    f'@font-face {{ font-family: "Barlow Condensed"; font-weight: 700; font-display: swap; '
                    f'src: url("data:font/woff2;base64,{font}") format("woff2"); }}')
    page = page.replace("/* FRONT_ASSETS */", front_assets)
    page = page.replace("/* GAME_CSS */", "\n".join((WEB / name).read_text(encoding="utf-8")
                                                  for name in ("style.css", "front.css")))
    barlow_license = (WEB / "licenses/OFL-Barlow.txt").read_text(encoding="utf-8")
    page = page.replace("<!-- GAME_CREDITS -->", html.escape("\n".join((credits, three_license, firebase_license, barlow_license))))
    page = page.replace("<!-- GAME_JS -->", js)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(page, encoding="utf-8")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("output", type=Path)
    parser.add_argument("--node-modules", type=Path, required=True)
    parser.add_argument("--firebase-config", type=Path)
    args = parser.parse_args()
    firebase = None
    if args.firebase_config:
        raw = json.loads(args.firebase_config.read_text(encoding="utf-8"))
        allowed = ("apiKey", "authDomain", "projectId", "appId", "messagingSenderId", "storageBucket")
        if not isinstance(raw, dict) or any(not isinstance(raw.get(key), str) or not raw[key].strip()
                                            for key in ("apiKey", "authDomain", "projectId")):
            parser.error("--firebase-config requer apiKey, authDomain e projectId da configuração Web pública.")
        firebase = {key: raw[key] for key in allowed if isinstance(raw.get(key), str)}
    build(args.output, args.node_modules.resolve(), firebase)
