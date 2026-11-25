"""Check the archive and the native libraries expected of an ARM64 IL2CPP APK."""
import sys
from zipfile import ZipFile

APK_CONTENT = {
    "AndroidManifest.xml", "classes.dex",
    "lib/arm64-v8a/libunity.so", "lib/arm64-v8a/libil2cpp.so",
}

with ZipFile(sys.argv[1]) as apk:
    missing = APK_CONTENT.difference(apk.namelist())
    missing.update(entry for entry in APK_CONTENT.intersection(apk.namelist())
                   if not apk.getinfo(entry).file_size)
    if missing:
        raise SystemExit("Entradas ausentes ou vazias: " + ", ".join(sorted(missing)))
    corrupt = apk.testzip()
    if corrupt:
        raise SystemExit("Entrada corrompida: " + corrupt)
print("Estrutura do APK ARM64 IL2CPP verificada.")
