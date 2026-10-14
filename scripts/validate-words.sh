#!/usr/bin/env bash
#
# Runs the word list validator in Unity batchmode. Exits non-zero on failure.
# Usage:
#   scripts/validate-words.sh
# Override UNITY if your editor path differs.

set -euo pipefail

UNITY="${UNITY:-/Applications/Unity/Hub/Editor/6000.3.10f1/Unity.app/Contents/MacOS/Unity}"
PROJECT="$(cd "$(dirname "$0")/.." && pwd)"
LOG="$PROJECT/Logs/word-validator.log"

if [[ ! -x "$UNITY" ]]; then
    echo "Unity executable not found at: $UNITY" >&2
    echo "Set UNITY to your Unity binary and retry." >&2
    exit 127
fi

mkdir -p "$PROJECT/Logs"

"$UNITY" \
    -batchmode \
    -nographics \
    -projectPath "$PROJECT" \
    -executeMethod WordListValidator.ValidateWordListBatchmode \
    -logFile "$LOG" \
    -quit \
    || { echo "Word list validation failed. Log: $LOG" >&2; exit 1; }

echo "Word list validation passed. Log: $LOG"
