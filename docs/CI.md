# CI

## What GitHub runs

`.github/workflows/content-checks.yml` gates pull requests against `main` with
checks that need **no Unity Editor** — they run on a stock `ubuntu-latest`
runner with Python 3, so they work without a Unity licence.

| Job | When | What it runs |
|---|---|---|
| `content` | every PR and push to `main` | `scripts/validate-content.py` |

`scripts/validate-content.py` checks, without opening Unity:

- `Assets/Data/words.json` parses, has a non-empty `words` array, and every entry
  satisfies the shipped content policy — non-empty clue/hint, `A`–`Z` answers,
  answer length 4–12, hint not duplicating the clue, no duplicate answers.
- Every asset under `Assets/` has a `.meta` with a `guid:`, GUIDs are unique, and
  every script under `Assets/Scripts/` has a `.meta`.

It is the **fast** gate. The Editor-side validators below remain the source of
truth, so keep the length constants in `validate-content.py` in sync with
`Assets/Scripts/Words/WordValidator.cs`.

## Why there is no Unity job

The Unity tests used to run on GitHub via GameCI, but activating Unity in CI
requires a licence secret. Without one, the container fails immediately:

```
License activation strategy could not be determined.
  * UNITY_EMAIL + UNITY_PASSWORD                 - Personal (free) seat
  * UNITY_EMAIL + UNITY_PASSWORD + UNITY_SERIAL  - Pro/Plus seat
  * UNITY_LICENSE or UNITY_LICENSE_FILE          - a .ulf
```

That job was removed so PRs are not blocked by an activation we cannot perform.
The Unity-dependent checks are run **locally** instead (below). To restore a
Unity job later, add one of the activation secrets in **Settings → Secrets and
variables → Actions** and re-add a `game-ci/unity-test-runner@v4` job with
`unityVersion: 6000.3.10f1`.

## Run the checks locally

The Editor **must be closed** while these run — Unity refuses to open a project
that another instance already holds. Override the editor path with
`UNITY=/path/to/Unity` if yours differs.

| Check | Command |
|---|---|
| Content (same as CI, no Unity) | `python3 scripts/validate-content.py` |
| EditMode tests | `scripts/run-tests.sh EditMode` |
| PlayMode tests | `scripts/run-tests.sh PlayMode` |
| Word list validator (Editor) | `scripts/validate-words.sh` |
| Script reference validator (Editor) | `-executeMethod UnreferencedScriptValidator.ValidateBatchmode` |

Each exits non-zero on failure, so they can be chained into a pre-push hook:

```bash
python3 scripts/validate-content.py && scripts/run-tests.sh EditMode && scripts/validate-words.sh
```

The two Editor validators are also available from the menu, which is handy while
the project is open:

- **Hangaroo → Content → Validate Word List**
- **Hangaroo → Content → Validate Script References**

## Still outstanding (tracked in #39)

- **Verified failure path in CI.** "A failing check blocks the PR" is exercised
  locally, but has not been confirmed against the live workflow with a
  deliberately broken commit.
- **Branch protection.** `main` should require the `Word list & asset metadata`
  check before merge (a repository setting, not a file).
