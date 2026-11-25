#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
mkdir -p Logs
UNITY="${UNITY_EDITOR:-}"
if [ -z "$UNITY" ]; then UNITY=$(command -v Unity || command -v unity-editor || true); fi
if [ -z "$UNITY" ] || [ ! -x "$UNITY" ]; then echo 'BLOQUEADO: Unity Editor ausente. Defina UNITY_EDITOR para o executável do Unity 6000.0.38f1.' >&2; exit 2; fi
python3 Tools/preflight.py
rm -f Builds/OperationZero-debug.apk
"$UNITY" -batchmode -quit -projectPath "$PWD" -buildTarget Android -executeMethod OZBuild.Android -logFile "$PWD/Logs/android-build.log"
test -s Builds/OperationZero-debug.apk
python3 Tools/verify_apk.py Builds/OperationZero-debug.apk
echo 'APK produzido; instalação e teste em aparelho ainda obrigatórios.'
