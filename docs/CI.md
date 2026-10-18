# CI

## What GitHub runs

| Workflow | Job | When | What it runs |
|---|---|---|---|
| `content-checks.yml` | `content` | every PR and push to `main` | `scripts/validate-content.py` (no Unity) |
| `unity-tests.yml` | `detect-license` | every PR and push to `main` | whether a Unity licence secret is configured |
| `unity-tests.yml` | `warnings` | every PR/push **when a `.ulf` is present** | `scripts/check-warnings.sh --docker` |
| `unity-tests.yml` | `editmode` | every PR/push **when licensed** | EditMode suite (`game-ci/unity-test-runner@v4`) |
| `unity-tests.yml` | `playmode` | `main`, tags, manual **when licensed** | PlayMode suite |

`content-checks.yml` needs **no Unity Editor** — it runs on a stock
`ubuntu-latest` runner with Python 3.

`scripts/validate-content.py` checks, without opening Unity:

- `Assets/Data/words.json` parses, has a non-empty `words` array, and every entry
  satisfies the shipped content policy — non-empty clue/hint, `A`–`Z` answers,
  answer length 4–12, hint not duplicating the clue, no duplicate answers.
- Every asset under `Assets/` has a `.meta` with a `guid:`, GUIDs are unique, and
  every script under `Assets/Scripts/` has a `.meta`.

It is the **fast** gate. The Editor-side validators below remain the source of
truth, so keep the length constants in `validate-content.py` in sync with
`Assets/Scripts/Words/WordValidator.cs`.

## The Unity job is opt-in

Activating Unity in CI requires a licence secret. Without one the container
fails immediately:

```
License activation strategy could not be determined.
  * UNITY_EMAIL + UNITY_PASSWORD                 - Personal (free) seat
  * UNITY_EMAIL + UNITY_PASSWORD + UNITY_SERIAL  - Pro/Plus seat
  * UNITY_LICENSE or UNITY_LICENSE_FILE          - a .ulf
```

Rather than leaving PRs blocked by an activation we cannot perform, the Unity
jobs in `unity-tests.yml` gate themselves: the `detect-license` job looks for an
activation secret and reports `licensed=true/false`, and `editmode` / `playmode`
carry `if: needs.detect-license.outputs.licensed == 'true'`. With no secret the
jobs are **skipped** (reported as success), so the workflow stays green; with a
secret they run and gate the PR.

To switch them on, add one of the activation secrets under **Settings →
Secrets and variables → Actions**:

| Secret | Purpose |
|---|---|
| `UNITY_LICENSE` | a `.ulf` licence file (Personal or Pro) |
| `UNITY_EMAIL` + `UNITY_PASSWORD` | Personal seat activation |
| `UNITY_EMAIL` + `UNITY_PASSWORD` + `UNITY_SERIAL` | Pro/Plus seat activation |

Both jobs cache `Library/` keyed on `manifest.json`, `packages-lock.json` and
`ProjectSettings/ProjectVersion.txt`, because importing `Library/` is the entire
cost of a Unity CI run.

`unityVersion: 6000.3.10f1` matches `ProjectSettings/ProjectVersion.txt`
exactly — a runner on a different editor version reimports everything and fails
for no good reason.

## Warning-free compile

`scripts/check-warnings.sh` fails the build when the code this repository owns
compiles with warnings. It runs Unity once in batch mode and reads the log:

| Diagnostic | Fails? |
|---|---|
| `error CS…` anywhere, **including** `Library/PackageCache/` and `Packages/` | yes — a package that does not compile is a broken build |
| `warning CS…` under `Assets/` | yes — that is all the code we own |
| `warning CS…` under `Library/PackageCache/` or `Packages/` | **no** — third-party, not ours to fix |
| Unity exiting non-zero | yes |

Third-party packages are excluded on purpose. A noisy third-party warning train
eventually pushes someone to raise the threshold for *everything*, which loses
the signal completely; a path-prefix filter keeps the gate honest instead. The
filter is on the file path, so no per-package allowlist needs maintaining.

The gate catches warnings whenever project code changes, because a source change
forces a recompile — which is exactly when a new warning could appear.

Because the job drives the editor image directly it needs a `.ulf` as
`UNITY_LICENSE`; email/password activation only works through the game-ci
actions, so `detect-license` reports `ulf` separately and the job is skipped
without one.

## Run the checks locally

The Editor **must be closed** while these run — Unity refuses to open a project
that another instance already holds. Override the editor path with
`UNITY=/path/to/Unity` if yours differs.

| Check | Command |
|---|---|
| Content (same as CI, no Unity) | `python3 scripts/validate-content.py` |
| Warning-free compile | `scripts/check-warnings.sh` |
| EditMode tests | `scripts/run-tests.sh EditMode` |
| PlayMode tests | `scripts/run-tests.sh PlayMode` |
| Word list validator (Editor) | `scripts/validate-words.sh` |
| Script reference validator (Editor) | `-executeMethod UnreferencedScriptValidator.ValidateBatchmode` |
| Firestore seed (dry-run, no credentials) | `python3 scripts/seed-firestore.py` |
| Firestore seed unit tests (no network) | `python3 scripts/test-seed-firestore.py` |
| Firestore security rules (emulator) | `scripts/test-firestore-rules.sh` |

`check-warnings.sh` also takes `--log FILE` to re-check a previous run without
starting Unity, which is what the failure message points at.

Each exits non-zero on failure, so they can be chained into a pre-push hook:

```bash
python3 scripts/validate-content.py && scripts/check-warnings.sh && scripts/run-tests.sh EditMode
```

The two Editor validators are also available from the menu, which is handy while
the project is open:

- **Hangaroo → Content → Validate Word List**
- **Hangaroo → Content → Validate Script References**

## Still outstanding (tracked in #39)

The Unity-dependent jobs are **written and statically checked, but have never run
against a live licence** — there is no activation secret in the repository, so
`detect-license` skips them on every run. Specifically, still unverified:

- **Verified failure path in CI.** The content validator and the warning gate are
  both exercised locally (including against a real editor run), but a failing
  check has not been observed blocking a live pull request. Add a licence secret
  and force a failure to confirm.
- **Branch protection.** `main` should require the `Word list & asset metadata`
  check before merge (a repository setting, not a file).
- **`Library/` timings.** The cache is keyed on `manifest.json`,
  `packages-lock.json` and the Unity version, but warm-run timing has not been
  recorded without a licensed run.
- **The Firestore rules job.** `scripts/test-firestore-rules.sh` runs the
  security-rules tests (`firebase/tests/rules.test.js`) under the emulator via
  `firebase emulators:exec` — non-zero on failure, same contract as
  `scripts/run-tests.sh`. It needs no Unity and no credentials, so it can join
  `content-checks.yml` as a second job once `#31` lands; adding a workflow job
  is file work that belongs with the rest of the #39 wiring.

