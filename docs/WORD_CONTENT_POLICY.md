# Word content policy

This document is the decision record for how authored word content is shaped and
validated (issue #18). It exists so the rule is a **decision, not an accident**,
and so the per-difficulty banks have a single contract to satisfy (Story 1.4).

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

Story 1.4 adds the fields and hygiene rules below.

| # | Rule | Failure reason |
|---|------|----------------|
| 9 | `category` is non-empty and one of the documented set | `category is empty` / `category '…' is not one of the documented categories` |
| 10 | `difficulty` is non-empty and one of `easy`/`medium`/`hard` | `difficulty is empty` / `difficulty '…' is not easy/medium/hard` |
| 11 | `answer` length is inside the band the entry **declares** | `answer '…' is outside the <difficulty> band (min-max)` |
| 12 | `clue` does not contain the `answer` | `clue contains the answer (giveaway)` |
| 13 | `hint` does not contain the `answer` | `hint contains the answer (giveaway)` |

The length band `[4, 12]` is the **union** of the Easy (4–6), Medium (6–8) and
Hard (8–12) difficulty bands: the shared list must be usable by at least one
difficulty. `WordPool.Parse` still takes that union band for the whole file, but
each entry is additionally checked against **its own declared band** (rule 11),
so a word cannot be tagged `hard` while being seven letters.

Answers are normalized to upper-case on load, so authors may write them in any
case.

## Categories and difficulty (Story 1.4)

Every entry carries two extra required fields:

```json
{
  "answer": "JUPITER",
  "clue": "Largest Planet",
  "hint": "Starts with J and has 7 letters",
  "category": "space",
  "difficulty": "medium"
}
```

- **`difficulty`** is one of `easy`, `medium`, `hard`, and must match the answer's
  length band. Bands overlap on purpose (6 and 8 letters are valid in two bands),
  so the declared difficulty is authoritative — it is never inferred from length.
- **`category`** is player-visible: the `ShowCategory` setting reveals it as an
  extra nudge. It is a finite, documented set:

  `animals`, `food`, `geography`, `history`, `movies`, `music`, `nature`,
  `science`, `space`, `sports`, `technology`, `general`.

  The set lives in `WordCategory.All`; adding a category means updating that,
  this file, and `KNOWN_CATEGORIES` in `scripts/validate-content.py`.

### Band population

Each difficulty must offer **at least 60** validator-passing words
(`WordListValidator.MinWordsPerBand`). Below that a run can end early or repeat
words, so the count is asserted per band. Hard must additionally contain at least
one word reaching the top of its band (12 letters), so the 8–12 promise is real
and not silently degenerate.

### Runtime selection

The pool is resolved **once per run**: `GameManager.StartRun` filters the parsed
file down to the active difficulty and shuffles that slice. Difficulty is never
read again mid-run, so a mid-run change (or, once EPIC 2 lands, a fetch failure)
cannot swap the words out from under the player.

## Hint copy

Because hints are gated by difficulty (Story 1.3), a hint is an **assist**, not a
second clue: it should give structure (starting/ending letters, length, a
contained letter) rather than restate the clue. Rules 7, 12 and 13 make the worst
cases mechanical to catch; the rest is reviewed by hand.

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

`Assets/Data/words.json` (the bundled, offline word list):

```json
{
  "schemaVersion": 1,
  "words": [
    {
      "clue": "Largest Planet",
      "answer": "JUPITER",
      "hint": "Starts with J and has 7 letters",
      "category": "space",
      "difficulty": "medium"
    }
  ]
}
```

- `schemaVersion` is the file-format version read by
  `WordPool.BundledWordList`; a version newer than the build understands fails
  the parse loudly instead of being silently mis-parsed.
- `weight` (default 1, 0 = never select) and `isActive` (default true) are
  optional and mirror the Firestore schema, so soft-disabling a word behaves
  the same on the bundled and Firestore read paths (issue #33).
- Entries predating Story 1.4 (no `category`/`difficulty`) still load: the
  difficulty is inferred from the answer length and the category defaults to
  `general`, reported as one aggregated warning naming the file. Both paths
  share `WordData.Normalize` + `WordValidator`, so identical content produces
  identical `WordData`.

## Related

- **Run shuffle:** word order is reshuffled at the start of each playthrough of
  the list (see `WordOrderBuilder`), so two playthroughs do not repeat the same
  sequence.
- **Difficulty EPIC:** new Easy/Medium/Hard banks must pass the same validator;
  this is the acceptance gate for that content story.
