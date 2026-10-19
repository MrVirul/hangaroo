# Testing HANGARoo

This document is the rules of record for the automated test suite. It supersedes
the old "there is no test suite, linter or CI" guidance: as of **Story 4.1
(#38)** the project ships EditMode and PlayMode suites that run headlessly.

## Layout

```
Assets/Tests/
  EditMode/                     # fast, pure-logic suites (no play mode needed)
    Difficulty/  Layout/  Run/  Scoring/  Words/
  PlayMode/                     # scene-flow regressions against the real GamePlay scene
    GameplayFlowTests.cs        # #10, #12, #13, #19
    HeartManagerTests.cs        # #19 heart row
```

Each folder has its own `.asmdef` (e.g. `Hangaroo.Scoring.Tests`) that references
only the assemblies under test plus `UnityEngine.TestRunner` /
`UnityEditor.TestRunner`, and defines `UNITY_INCLUDE_TESTS`. Test assemblies are
`autoReferenced: false`, so they never ship in a build.

### `Hangaroo.Game`

`Assets/Scripts/Hangaroo.Game.asmdef` puts the core scene scripts (`GameManager`,
`HeartManager`, `KeyboardManager`, `UIManager`, ...) in a named assembly. This is
what lets a test assembly reference them: an asmdef **cannot** reference the
predefined `Assembly-CSharp`. The subsystem assemblies (`Hangaroo.Difficulty`,
`Hangaroo.Layout`, `Hangaroo.Run`, `Hangaroo.Scoring`, `Hangaroo.Words`) stay as
they were and `Hangaroo.Game` references them.

## Running the suites

### In the editor

`Window > General > Test Runner`, then **Run All** in the EditMode and PlayMode
tabs. PlayMode tests load the real `Assets/Scenes/GamePlay.unity`, so the editor
must keep ticking while they run (focus the editor, or enable
*Run In Background* in `Edit > Preferences > General`).

### Headless (CI)

```bash
scripts/run-tests.sh                 # EditMode (default)
scripts/run-tests.sh EditMode PlayMode
UNITY=/path/to/Unity scripts/run-tests.sh   # non-default editor install
```

The script runs Unity in `-batchmode -nographics -runTests`, writes JUnit XML to
`Logs/test-results-<Platform>.xml`, and **exits non-zero if any test fails**, so
it can be a required check. The editor must be closed while it runs.

## Current size and runtime

| Platform | Tests | Warm runtime |
|---|---|---|
| EditMode | 147 | ~3 s |
| PlayMode | 24 | ~2 s |

EditMode is well under the one-minute budget. PlayMode is deliberately small
(issue #38: "fewest, only what broke") and loads the scene per test.

## Regression coverage

The Wave 0 bug sweep is covered as follows:

| Filed bug | Test |
|---|---|
| #10 keys never re-enable | `GameplayFlowTests.Keyboard_Reenables_Every_Key_For_The_Next_Word` |
| #11 cumulative `revealedCount` | `ScoreCalculatorTests` |
| #12 `Restart()` leaves `timeScale == 0` | `GameplayFlowTests.Restart_After_Pause_Resets_Time_Scale` |
| #13 `CheckLetter` not idempotent | `GameplayFlowTests.Repeated_*_Guess_*` |
| #18 word order never reshuffles | `WordOrderBuilderTests` |
| #18 invalid answers accepted | `WordValidatorTests` |
| #16 `FitSlotsToWidth` no-op | `SlotStripLayoutTests`, `LetterSlotStripLayoutTests` |
| #19 run lifecycle / heart refill | `RunLifecycleTests`, `GameplayFlowTests`, `HeartManagerTests` |

Tests assert the **intended** rules, not the current implementation, so they stay
valid across the upcoming refactors.

## Conventions

- **Seeded randomness.** Anything that shuffles takes an explicit
  `System.Random`; tests pass a fixed seed. Never assert on `Random.Range`.
- **Real time only.** PlayMode tests wait with `yield return null` or
  `WaitForSecondsRealtime`, never `WaitForSeconds`, and reset
  `Time.timeScale = 1` in teardown so a paused frame cannot hang the next test.
- **Difficulty independence.** Tests read `HeartManager.MaxLives` after the scene
  loads instead of assuming an Easy/Medium/Hard value.
- **No test-only production API.** Where a value is private (the current word),
  tests read it from scene objects (`LetterSlot.GetLetter`) rather than adding
  accessors to `GameManager`.
- **Known gap.** `GameManager.Awake` writes the `ShowCategory` / `Hints`
  `PlayerPrefs` keys, so the PlayMode suite touches the editor's preferences.
  The assertions are written to tolerate any difficulty, but full PlayerPrefs
  isolation is still outstanding.

## Verification limit

The project still cannot be **built** from the CLI: `Assembly-CSharp.csproj`
hard-references the editor install under
`/Applications/Unity/Hub/Editor/6000.3.10f1/`. Running tests headlessly (above)
works; producing a player build from the CLI does not yet.
