"""Package the REV07 web client and its compatible renderer in a single HTML file."""
import argparse
import re
from pathlib import Path

legacy = Path(__file__).resolve().parents[1] / "LegacyServer"

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("output", type=Path)
output = parser.parse_args().output

page = (legacy / "index.html").read_text(encoding="utf-8")
css = (legacy / "lobby.css").read_text(encoding="utf-8")
page, styles = re.subn(r'<link rel="stylesheet" href="lobby.css"\s*/?>',
                       lambda _: f"<style>\n{css}\n</style>", page)
if styles != 1:
    raise ValueError("Folha de estilos do lobby ausente.")
page = re.sub(r'<link rel="manifest"[^>]*>', "", page)
page = re.sub(r'<option value="online(?:br)?">[^<]*</option>', "", page)
page = re.sub(r'<button data-tab="online">[^<]*</button>', "", page)
page = re.sub(r'<script type="importmap">.*?</script>', "", page)
page = page.replace('<script src="graphics.js"></script>', "")
page = re.sub(r'<script>if\(location.protocol.startsWith.*?</script>', "", page)
page = page.replace("BUILD 0.5 · PRÉ-BETA", "VERSÃO WEB OFFLINE")
for name in ("engine.js", "visuals.js", "game.js", "lobby.js", "online.js"):
    tag = f'<script src="{name}"></script>'
    if page.count(tag) != 1:
        raise ValueError(f"Script ausente: {name}")
    script = (legacy / name).read_text(encoding="utf-8")
    if "</script" in script.lower():
        raise ValueError(f"Script contém fechamento de HTML: {name}")
    page = page.replace(tag, f"<script>\n{script}\n</script>")
output.parent.mkdir(parents=True, exist_ok=True)
output.write_text(page, encoding="utf-8")
