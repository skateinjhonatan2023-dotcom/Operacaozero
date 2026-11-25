#!/usr/bin/env bash
set -euo pipefail
if [[ $# != 4 ]]; then
    echo 'Uso: build_costa_android.sh jogo.html pasta-build-tools android.jar saída.apk' >&2
    exit 2
fi
root="$(cd "$(dirname "$0")/.." && pwd)"
html="$(realpath "$1")"
tools="$(realpath "$2")"
platform="$(realpath "$3")"
output="$(realpath -m "$4")"
app="$root/WebGame/android"
work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT
mkdir -p "$work/assets" "$work/compiled" "$work/dex" "$(dirname "$output")"
cp "$html" "$work/assets/index.html"
"$tools/aapt2" compile --dir "$app/res" -o "$work/resources.zip"
"$tools/aapt2" link -I "$platform" --manifest "$app/AndroidManifest.xml" \
    -A "$work/assets" -o "$work/unsigned.apk" "$work/resources.zip"
if [[ -n "${OZ_ECJ_JAR:-}" ]]; then
    java -jar "$OZ_ECJ_JAR" -source 8 -target 8 -proc:none -bootclasspath "$platform" \
        -d "$work/compiled" "$app/GameActivity.java"
else
    javac --release 8 -classpath "$platform" -d "$work/compiled" "$app/GameActivity.java"
fi
mapfile -d '' classes < <(find "$work/compiled" -name '*.class' -print0)
"$tools/d8" --min-api 26 --lib "$platform" --output "$work/dex" "${classes[@]}"
(cd "$work/dex" && zip -q "$work/unsigned.apk" classes.dex)
"$tools/zipalign" -f 4 "$work/unsigned.apk" "$work/aligned.apk"
key="${OZ_ANDROID_DEBUG_KEYSTORE:-$work/debug.keystore}"
if [[ ! -f "$key" ]]; then
    keytool -genkeypair -keystore "$key" -storepass android -keypass android -alias androiddebugkey \
        -dname 'CN=Android Debug,O=Operation Zero,C=BR' -keyalg RSA -keysize 2048 -validity 10000
fi
"$tools/apksigner" sign --ks "$key" --ks-pass pass:android --key-pass pass:android \
    --out "$work/signed.apk" "$work/aligned.apk"
"$tools/apksigner" verify --verbose "$work/signed.apk"
"$tools/zipalign" -c -v 4 "$work/signed.apk"
unzip -tq "$work/signed.apk"
mv "$work/signed.apk" "$output"
