#!/usr/bin/env bash
#
# Fail if the code this repository owns (Assets/) compiles with warnings or
# errors. Issue #39 (Story 4.2): "Warnings are errors for project code, with
# third-party packages excluded".
#
# Usage:
#   scripts/check-warnings.sh               # compile with the local editor, then check
#   scripts/check-warnings.sh --log FILE    # check an existing Unity editor log
#   scripts/check-warnings.sh --docker      # compile inside the game-ci editor image
#
# What fails the check:
#   * any "<file>(line,col): error CS<n>" line anywhere in the log, including
#     third-party packages - a package that does not compile is a broken build;
#   * any "<file>(line,col): warning CS<n>" line whose file is under Assets/,
#     which is all the code this repository owns;
#   * a non-zero exit code from Unity itself.
#
# Third-party packages are deliberately excluded from the *warning* gate. They
# live under Library/PackageCache/ and Packages/, are not ours to fix, and a
# noisy third-party warning train would only push someone to disable the gate
# and lose the signal entirely. The exclusion is documented in docs/CI.md.
#
# Environment:
#   UNITY          editor binary for local mode
#   PROJECT        Unity project root (default: this repository)
#   UNITY_IMAGE    editor image for --docker mode
#   UNITY_LICENSE  a .ulf licence, required by --docker mode
#
# Compiling locally needs the editor closed: Unity refuses to open a project
# that another instance already holds.

set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PROJECT="${PROJECT:-$ROOT}"
UNITY="${UNITY:-/Applications/Unity/Hub/Editor/6000.3.10f1/Unity.app/Contents/MacOS/Unity}"
UNITY_IMAGE="${UNITY_IMAGE:-unityci/editor:ubuntu-6000.3.10f1-base-3}"
LOG_DIR="$PROJECT/Logs"

usage() {
    # Print every leading comment line except the shebang, so the help text
    # tracks the header instead of a hard-coded line range.
    awk '/^#/ { sub(/^# ?/, ""); print; next } { exit }' "$0" | tail -n +2
}

MODE="local"
LOG=""
case "${1:-}" in
    --log)
        [[ $# -ge 2 ]] || { echo "usage: $0 --log FILE" >&2; exit 2; }
        MODE="log"
        LOG="$2"
        ;;
    --docker) MODE="docker" ;;
    -h|--help) usage; exit 0 ;;
    "") ;;
    *) echo "unknown argument: $1" >&2; usage >&2; exit 2 ;;
esac

UNITY_FAILED=0

compile_local() {
    if [[ ! -x "$UNITY" ]]; then
        echo "Unity executable not found at: $UNITY" >&2
        echo "Set UNITY to your Unity binary and retry." >&2
        exit 127
    fi
    mkdir -p "$LOG_DIR"
    LOG="$LOG_DIR/compile-warnings.log"
    echo "==> Compiling $PROJECT"
    if ! "$UNITY" \
        -batchmode \
        -nographics \
        -quit \
        -projectPath "$PROJECT" \
        -logFile "$LOG"; then
        echo "Unity exited non-zero while compiling." >&2
        UNITY_FAILED=1
    fi
}

compile_docker() {
    if [[ -z "${UNITY_LICENSE:-}" ]]; then
        echo "UNITY_LICENSE (a .ulf) is required for --docker mode." >&2
        echo "See docs/CI.md for how to obtain one." >&2
        exit 64
    fi
    if ! command -v docker >/dev/null 2>&1; then
        echo "docker not found on PATH." >&2
        exit 127
    fi

    # Unity on Linux looks here for an activation file.
    local licence_dir="$HOME/.local/share/unity3d/Unity"
    mkdir -p "$licence_dir"
    printf '%s\n' "$UNITY_LICENSE" > "$licence_dir/Unity_lic.ulf"

    mkdir -p "$LOG_DIR"
    LOG="$LOG_DIR/compile-warnings.log"
    echo "==> Compiling $PROJECT in $UNITY_IMAGE"
    if ! docker run --rm \
        -v "$PROJECT:/project" \
        -v "$licence_dir:/root/.local/share/unity3d/Unity:ro" \
        -w /project \
        "$UNITY_IMAGE" \
        -batchmode -nographics -quit \
        -projectPath /project \
        -logFile /project/Logs/compile-warnings.log; then
        echo "Unity exited non-zero while compiling." >&2
        UNITY_FAILED=1
    fi
}

case "$MODE" in
    local) compile_local ;;
    docker) compile_docker ;;
esac

if [[ ! -f "$LOG" ]]; then
    echo "No Unity log found at: $LOG" >&2
    echo "A missing log means the editor never started; treating that as a failure." >&2
    exit 1
fi

# Unity prints diagnostics as "path(line,col): warning CSnnnn: message". Package
# diagnostics use the same shape but a different path, so filtering on the path
# prefix is what keeps third-party code out of the warning gate. Unity logs each
# diagnostic twice (a compiler-output block and a console message), so awk drops
# the repeats while keeping the order. The separator accepts both / and \ so a
# Windows-generated log still matches.
ERRORS="$(grep -E ': error CS[0-9]+' "$LOG" | awk '!seen[$0]++' || true)"
WARNINGS="$(grep -E 'Assets[\\/].*: warning CS[0-9]+' "$LOG" | awk '!seen[$0]++' || true)"

status=0

if [[ -n "$ERRORS" ]]; then
    status=1
    echo "==> Compiler error(s):"
    printf '%s\n' "$ERRORS" | sed 's/^/    /'
fi

if [[ -n "$WARNINGS" ]]; then
    status=1
    echo "==> Warning(s) in Assets/ (warnings are errors for our code):"
    printf '%s\n' "$WARNINGS" | sed 's/^/    /'
fi

if [[ $UNITY_FAILED -ne 0 ]]; then
    status=1
fi

if [[ $status -eq 0 ]]; then
    echo "==> Warning-free compile: OK ($LOG)"
else
    echo "==> Warning-free compile: FAILED ($LOG)" >&2
    if [[ -z "$ERRORS" && -z "$WARNINGS" ]]; then
        echo "==> Unity did not exit cleanly; see the log for the reason." >&2
        tail -n 40 "$LOG" >&2 || true
    fi
fi

exit $status
