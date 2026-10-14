#!/usr/bin/env python3
"""Unity-free content checks, runnable on any machine with Python 3.

GitHub can only run the Unity-dependent checks if a licence is configured, so
this script covers the parts of the content pipeline that need no Editor at all.
It is deliberately the *fast* gate; the Editor-side validators remain the source
of truth:

  * `WordListValidator` / `WordValidator`  - the shipped word list policy
  * `UnreferencedScriptValidator`          - dead-MonoBehaviour detection

Keep the constants below in sync with `Assets/Scripts/Words/WordValidator.cs`
(`MinAnswerLength` / `MaxAnswerLength`).

Exits 0 when clean, 1 with a list of problems otherwise.
"""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
WORDS_PATH = ROOT / "Assets/Data/words.json"
SCRIPTS_DIR = ROOT / "Assets/Scripts"

# Mirrors WordValidator.MinAnswerLength / MaxAnswerLength.
MIN_ANSWER_LENGTH = 4
MAX_ANSWER_LENGTH = 12

LETTERS_ONLY = re.compile(r"^[A-Z]+$")


def normalize(value: object) -> str:
    """Trimmed, upper-cased copy of a JSON value; never raises."""
    return value.strip().upper() if isinstance(value, str) else ""


def check_word_list() -> list[str]:
    rel = WORDS_PATH.relative_to(ROOT)

    if not WORDS_PATH.exists():
        return [f"{rel}: not found"]

    try:
        data = json.loads(WORDS_PATH.read_text(encoding="utf-8"))
    except json.JSONDecodeError as error:
        return [f"{rel}: invalid JSON ({error})"]

    if not isinstance(data, dict):
        return [f"{rel}: expected a JSON object with a 'words' array"]

    words = data.get("words")
    if not isinstance(words, list) or not words:
        return [f"{rel}: 'words' must be a non-empty array"]

    problems: list[str] = []
    first_seen: dict[str, int] = {}

    for index, entry in enumerate(words):
        label = f"words[{index}]"

        if not isinstance(entry, dict):
            problems.append(f"{label}: entry is not an object")
            continue

        clue = entry.get("clue")
        answer = entry.get("answer")
        hint = entry.get("hint")

        if not normalize(clue):
            problems.append(f"{label}: clue is empty")

        normalized_answer = normalize(answer)
        if not normalized_answer:
            problems.append(f"{label}: answer is empty")
            continue

        if not LETTERS_ONLY.match(normalized_answer):
            problems.append(
                f"{label}: answer '{answer}' contains characters outside A-Z"
            )
        elif len(normalized_answer) < MIN_ANSWER_LENGTH:
            problems.append(
                f"{label}: answer '{normalized_answer}' is shorter than "
                f"the minimum of {MIN_ANSWER_LENGTH}"
            )
        elif len(normalized_answer) > MAX_ANSWER_LENGTH:
            problems.append(
                f"{label}: answer '{normalized_answer}' is longer than "
                f"the maximum of {MAX_ANSWER_LENGTH}"
            )

        if not normalize(hint):
            problems.append(f"{label}: hint is empty")
        elif normalize(hint) == normalize(clue):
            problems.append(f"{label}: hint duplicates the clue")

        if normalized_answer in first_seen:
            problems.append(
                f"{label}: duplicate answer '{normalized_answer}' "
                f"(first seen at words[{first_seen[normalized_answer]}])"
            )
        else:
            first_seen[normalized_answer] = index

    return problems


def check_asset_metadata() -> list[str]:
    """Every asset has one unique GUID, and every script has a .meta."""
    problems: list[str] = []
    guid_owner: dict[str, Path] = {}

    for meta_path in sorted(ROOT.glob("Assets/**/*.meta")):
        text = meta_path.read_text(encoding="utf-8", errors="replace")
        match = re.search(r"(?m)^guid:\s*([0-9a-fA-F]+)\s*$", text)
        rel = meta_path.relative_to(ROOT)

        if not match:
            problems.append(f"{rel}: missing a 'guid:' line")
            continue

        guid = match.group(1)
        if guid in guid_owner:
            problems.append(f"{rel}: GUID {guid} is also used by {guid_owner[guid]}")
        else:
            guid_owner[guid] = rel

    for script in sorted(SCRIPTS_DIR.rglob("*.cs")):
        if not script.with_name(script.name + ".meta").exists():
            problems.append(f"{script.relative_to(ROOT)}: missing .meta file")

    return problems


def main() -> int:
    problems = check_word_list() + check_asset_metadata()

    if problems:
        print(f"Content checks FAILED with {len(problems)} problem(s):")
        for problem in problems:
            print(f"  - {problem}")
        return 1

    print("Content checks passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
