#!/usr/bin/env bash
# Run the Firestore security-rules tests against the emulator (issue #31).
#
#   scripts/test-firestore-rules.sh
#
# Contract matches scripts/run-tests.sh: exits non-zero on any failure, so it
# can be a required CI check (planned for #39). Needs:
#   * firebase-tools (npm i -g firebase-tools)
#   * Node 18+ and a JRE (the emulator is Java)
#   * npm install in firebase/tests (first run only)
#
# No credentials are used: demo-hangaroo is a reserved throw-away project id
# and the emulator runs entirely locally.

set -euo pipefail
cd "$(dirname "$0")/.."

if [ ! -d firebase/tests/node_modules ]; then
  echo "Installing rules-test dependencies (first run only)..."
  (cd firebase/tests && npm install --no-audit --no-fund)
fi

# --exit is required: the Firestore client keeps gRPC handles open, which
# otherwise leaves mocha running forever after the last test.
exec firebase emulators:exec --only firestore --project demo-hangaroo \
  "npx mocha firebase/tests/rules.test.js --timeout 15000 --exit"
