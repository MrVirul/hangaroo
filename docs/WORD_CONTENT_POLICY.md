# Word content policy

This document is the decision record for how authored word content is shaped and
validated (issue #18). It exists so the rule is a **decision, not an accident**,
and so the future per-difficulty banks have a single contract to satisfy.

## Decision

**Option (a): answers are single words of `A`–`Z` only.** Spaces, apostrophes,
hyphens and accented characters are rejected by design. Richer vocabulary (for
example `NEW YORK`, `RÉSUMÉ`, `CO-OP`) is explicitly **out of scope** for now.

Rationale: the game renders exactly one tappable slot per character, the keyboard
has 26 `A`–`Z` buttons, and the scoring path derives a slot's value from its
alphabetical index. Any character outside `A`–`Z` would create a slot no key can
fill — an uncompletable word that can also subtract from the score.

Option (b) — treating non-letters as auto-revealed "given" slots that are
excluded from the score sum — remains a possible future direction, but it is a
gameplay feature and must be adopted deliberately or not at all.

## Rules

A word entry (`clue`, `answer`, `hint`) is valid only when **all** of the
following hold. Checks run in this order, and the first failure is reported:

| # | Rule | Failure reason |
|---|------|----------------|
| 1 | `clue` is non-empty | `clue is empty` |
| 2 | `answer` is non-empty | `answer is empty` |
| 3 | `answer` normalizes to `A`–`Z` only | `answer '…' contains characters outside A-Z` |
| 4 | `answer` length ≥ 4 | `answer '…' is shorter than the minimum of 4` |
| 5 | `answer` length ≤ 12 | `answer '…' is longer than the maximum of 12` |
| 6 | `hint` is non-empty | `hint is empty` |
| 7 | `hint` does not duplicate the `clue` (case-insensitive) | `hint duplicates the clue` |
| 8 | `answer` is unique within the list (case-insensitive) | `duplicate answer '…'` |

The length band `[4, 12]` is the **union** of the Easy (4–6), Medium (6–8) and
Hard (8–12) difficulty bands: the shared list must be usable by at least one
difficulty. When content is split per difficulty, each bank should be validated
against its own band by passing that band to `WordValidator.Validate`.

Answers are normalized to upper-case on load, so authors may write them in any
case.

## Enforcement

The same rules (`WordValidator`) are applied everywhere, so there is one source
of truth:

1. **Runtime** — `WordPool.Parse` (`Assets/Scripts/Words/WordPool.cs`) skips
   invalid entries, logs one aggregated `Debug.LogError` naming each rejection,
   and falls back to `WordPool.CreateFallback()` (a small always-valid pool) if
   no valid words remain. A malformed entry can therefore never produce an
   uncompletable word, a negative score, or a blank board.
2. **Editor** — `WordListValidator` (`Assets/Editor/WordListValidator.cs`):
   - `Hangaroo ▸ Content ▸ Validate Word List` runs it on demand;
   - it re-runs automatically whenever `Assets/Data/words.json` changes;
   - it runs before every build and **fails the build** on any problem.
   Problems are reported with `Debug.LogError`, never a warning.
3. **Tests** — `Assets/Tests/EditMode/WordValidatorTests.cs`,
   `WordPoolTests.cs` and `WordOrderBuilderTests.cs`, including a guard that the
   shipped `words.json` passes validation.

## Content location and schema

`Assets/Data/words.json`:

```json
{
  "words": [
    { "clue": "Largest Planet", "answer": "JUPITER", "hint": "Starts with J and has 7 letters" }
  ]
}
```

## Related

- **Run shuffle:** word order is reshuffled at the start of each playthrough of
  the list (see `WordOrderBuilder`), so two playthroughs do not repeat the same
  sequence.
- **Difficulty EPIC:** new Easy/Medium/Hard banks must pass the same validator;
  this is the acceptance gate for that content story.
