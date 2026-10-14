# CI

`.github/workflows/unity-ci.yml` gates pull requests against `main`.

## Jobs

| Job | When | What it runs |
|---|---|---|
| `editmode-tests` | every PR and push to `main` | EditMode tests, then the word-list validator |
| `playmode-tests` | `main` and tags only | PlayMode tests (slower, and needs a licence) |

Fast checks run on every PR; the slow, licence-heavy PlayMode run is held back
to `main`/tags so PR feedback stays in the minutes range.

## Required repository secrets

Set these in **Settings → Secrets and variables → Actions**. None are committed.

| Secret | Purpose |
|---|---|
| `UNITY_LICENSE` | Unity activation (`.ulf` contents for Personal/Pro) |
| `UNITY_EMAIL` | Editor sign-in |
| `UNITY_PASSWORD` | Editor sign-in |

`UNITY_VERSION` is pinned to `6000.3.10f1` in the workflow, matching
`ProjectSettings/ProjectVersion.txt`.

## Caching

`Library/` is cached on `runner.os` + Unity version + hashes of
`Packages/manifest.json` and `Packages/packages-lock.json`. Without it every run
pays a full reimport.

## Run the same checks locally

```bash
scripts/run-tests.sh EditMode      # same tests as the gate
scripts/validate-words.sh          # same content check as the gate
```

Both exit non-zero on failure, so they can be used as a pre-push hook.

## Still outstanding (tracked in #39)

- **Warning-free compile.** Warnings should be errors for the assemblies we own,
  with third-party packages excluded. The blocker is the deprecated
  `Object.FindObjectsOfType` call fixed in **#21**; the gate lands once the
  project compiles clean.
- **Branch protection.** `main` must require the `EditMode Tests & Word
  Validation` check before merge (repository setting, not a file).
- **Verified failure path.** "A failing test blocks the PR" has to be confirmed
  by opening a deliberately failing PR against the live workflow.
