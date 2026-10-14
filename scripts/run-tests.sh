#!/usr/bin/env bash
#
# Runs the HANGARoo test suites from the command line (no editor UI).
#
# Usage:
#   scripts/run-tests.sh              # EditMode, the default
#   scripts/run-tests.sh EditMode
#   scripts/run-tests.sh PlayMode
#   scripts/run-tests.sh EditMode PlayMode
#
# Exits non-zero if any test fails, or if Unity itself exits non-zero, so it is
# safe to use as a required CI check (issue #39).
#
# The editor must be closed: Unity cannot open a project that another instance
# already holds. Override the editor path with UNITY=/path/to/Unity if needed.

set -euo pipefail

UNITY="${UNITY:-/Applications/Unity/Hub/Editor/6000.3.10f1/Unity.app/Contents/MacOS/Unity}"
PROJECT="$(cd "$(dirname "$0")/.." && pwd)"

if [[ ! -x "$UNITY" ]]; then
    echo "Unity executable not found at: $UNITY" >&2
    echo "Set UNITY to your Unity binary and retry." >&2
    exit 127
fi

if [[ "$#" -eq 0 ]]; then
    set -- EditMode
fi

status=0
for platform in "$@"; do
    results="$PROJECT/Logs/test-results-$platform.xml"
    echo "==> Running $platform tests"
    if ! "$UNITY" \
        -batchmode \
        -nographics \
        -projectPath "$PROJECT" \
        -runTests \
        -testPlatform "$platform" \
        -testResults "$results" \
        -logFile -; then
        echo "==> $platform tests FAILED (results: $results)" >&2
        status=1
    else
        echo "==> $platform tests passed (results: $results)"
    fi
done

exit "$status"
