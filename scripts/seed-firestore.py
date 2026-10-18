#!/usr/bin/env python3
"""Seed Firestore with HANGARoo content, idempotently (issue #31, Story 2.2).

Pipeline:  words.json  ->  validate  ->  diff against Firestore  ->  write.

Design notes (the schema of record is docs/FIRESTORE_SCHEMA.md):

* **Validation is shared, not mirrored.** This script imports
  scripts/validate-content.py - the same module CI runs - and refuses to
  write anything if it reports a problem. The seed path cannot drift from
  the local build.
* **Idempotent by construction.** Document IDs are deterministic
  ('{answer lowercase}-{difficultyId}', e.g. 'gravity-medium'), and every
  document is compared field-by-field against the deployed copy before
  writing. Re-running against unchanged content performs zero writes and
  zero contentVersion bumps.
* **Dry-run by default.** Nothing is written without --push.
* **Credentials never enter the repository.** A service account is supplied
  via GOOGLE_APPLICATION_CREDENTIALS (the standard env var). The only
  exception is the Firestore emulator (FIRESTORE_EMULATOR_HOST), which
  needs no credentials at all.

There is no Firebase Admin SDK for Unity/C# (Admin SDKs are Node, Python,
Java and Go), which is why this is a Python script rather than the Editor
menu item sketched in #31; it is also what lets the seed run unattended in CI.
"""

from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
import os
import re
import sys
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
WORDS_PATH = ROOT / "Assets/Data/words.json"
DIFFICULTY_ASSET_DIR = ROOT / "Assets/Settings/Difficulty"

# Schema constants - docs/FIRESTORE_SCHEMA.md is the contract.
SCHEMA_VERSION = 1
DEFAULT_WEIGHT = 1  # relative selection weight; 0 = never select
COLLECTION_WORDS = "words"
COLLECTION_CATEGORIES = "categories"
COLLECTION_DIFFICULTIES = "difficulties"
COLLECTION_CONTENT_META = "contentMeta"
CONTENT_META_DOC_ID = "singleton"

# HintPolicy enum mirror (Assets/Scripts/Difficulty/HintPolicy.cs).
HINT_POLICY_NAMES = {
    0: "FreeImmediate",
    1: "FreeAfterWrongGuesses",
    2: "Penalised",
    3: "Disabled",
}


def fail(message: str) -> "SystemExit":
    return SystemExit(f"seed-firestore: {message}")


def load_validator_module():
    """Import scripts/validate-content.py despite the hyphen in its name."""
    path = ROOT / "scripts/validate-content.py"
    spec = importlib.util.spec_from_file_location("hangaroo_content_checks", path)
    if spec is None or spec.loader is None:
        raise fail(f"cannot load {path}")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def word_doc_id(answer: str, difficulty: str) -> str:
    """Deterministic document ID: re-seeding updates in place, never duplicates."""
    return f"{answer.strip().lower()}-{difficulty.strip().lower()}"


def build_word_docs(words: list[dict]) -> list[dict]:
    docs = []
    for entry in words:
        answer = str(entry["answer"]).strip().upper()
        difficulty = str(entry["difficulty"]).strip().lower()
        docs.append(
            {
                "docId": word_doc_id(answer, difficulty),
                "data": {
                    "answer": answer,
                    "clue": str(entry["clue"]).strip(),
                    "hint": str(entry["hint"]).strip(),
                    "categoryId": str(entry["category"]).strip().lower(),
                    "difficultyId": difficulty,
                    "weight": int(entry.get("weight", DEFAULT_WEIGHT)),
                    "isActive": bool(entry.get("isActive", True)),
                    "schemaVersion": SCHEMA_VERSION,
                },
            }
        )
    return docs


def build_category_docs(known_categories) -> list:
    """One document per known category; icon/colorHex are display metadata
    authored later in the Firebase Console (they are not shipped yet)."""
    docs = []
    for sort_order, key in enumerate(sorted(known_categories)):
        docs.append(
            {
                "docId": key,
                "data": {
                    "name": key.title(),
                    "icon": "",
                    "colorHex": "",
                    "sortOrder": sort_order,
                },
            }
        )
    return docs


def parse_difficulty_asset(path: Path) -> dict:
    """Minimal reader for a DifficultyConfig .asset (flat YAML scalars).

    The seeder sources difficulty values from the very assets the game ships,
    so the deployed collection cannot drift from the build.
    """
    text = path.read_text(encoding="utf-8")

    def scalar(key: str, cast):
        match = re.search(rf"^\s*{re.escape(key)}:\s*(.+?)\s*$", text, re.MULTILINE)
        if not match:
            raise fail(f"{path.name}: field '{key}' not found")
        raw = match.group(1).strip().strip("'\"")
        try:
            return cast(raw)
        except ValueError:
            raise fail(f"{path.name}: field '{key}' has unparsable value {raw!r}")

    policy = scalar("hintPolicy", int)
    if policy not in HINT_POLICY_NAMES:
        raise fail(f"{path.name}: unknown hintPolicy {policy}")

    return {
        "key": path.stem.lower(),
        "displayName": scalar("displayName", str),
        "livesPerWord": scalar("livesPerWord", int),
        "minLength": scalar("minWordLength", int),
        "maxLength": scalar("maxWordLength", int),
        "hintThreshold": scalar("hintThresholdWrongGuesses", int),
        "hintPolicy": HINT_POLICY_NAMES[policy],
        "scoreMultiplier": scalar("scoreMultiplier", float),
    }


def build_difficulty_docs() -> list:
    assets = sorted(DIFFICULTY_ASSET_DIR.glob("*.asset"))
    if not assets:
        raise fail(f"no DifficultyConfig assets found under {DIFFICULTY_ASSET_DIR}")
    return [{"docId": path.stem.lower(), "data": parse_difficulty_asset(path)} for path in assets]


def content_hash(word_docs: list[dict], category_docs: list[dict], difficulty_docs: list[dict]) -> str:
    """SHA-256 over the canonical content, so identical content seeds as a no-op."""
    canonical = json.dumps(
        {
            "words": sorted((d["docId"], d["data"]) for d in word_docs),
            "categories": sorted((d["docId"], d["data"]) for d in category_docs),
            "difficulties": sorted((d["docId"], d["data"]) for d in difficulty_docs),
            "schemaVersion": SCHEMA_VERSION,
        },
        sort_keys=True,
    )
    return hashlib.sha256(canonical.encode("utf-8")).hexdigest()


def init_firestore(project: str):
    """Connect to Firestore: emulator if configured, else the real project."""
    try:
        from firebase_admin import credentials, firestore, initialize_app
    except ImportError:
        raise fail(
            "the 'firebase_admin' package is not installed "
            "(pip install firebase-admin)"
        )

    emulator_host = os.environ.get("FIRESTORE_EMULATOR_HOST")
    if emulator_host:
        print(f"Using Firestore emulator at {emulator_host}")
        # The Admin SDK still resolves Application Default Credentials when
        # building the client even against the emulator; anonymous
        # credentials keep this path credential-free, as documented.
        from google.auth.credentials import AnonymousCredentials

        initialize_app(
            AnonymousCredentials(),
            options={"projectId": project},
        )
    else:
        cred_path = os.environ.get("GOOGLE_APPLICATION_CREDENTIALS")
        if not cred_path or not Path(cred_path).exists():
            raise fail(
                "no credentials. Set GOOGLE_APPLICATION_CREDENTIALS to a service "
                "account JSON kept OUTSIDE the repository, or run against the "
                "emulator with FIRESTORE_EMULATOR_HOST."
            )
        initialize_app(credentials.Certificate(cred_path))

    return firestore.client()


def fetch_existing(client, collection: str) -> dict:
    return {doc.id: doc.to_dict() for doc in client.collection(collection).stream()}


def diff(collection_docs: list[dict], existing: dict) -> tuple[list, list]:
    """Split into (to_write, unchanged_ids) by field-level comparison.

    Compares every field the builder produces; fields the schema does not
    define here (server-written updatedAt, or Console-only metadata such as a
    category's icon) are deliberately ignored, so an untouched deploy is
    recognized rather than rewritten.
    """
    to_write = []
    unchanged = []
    for doc in collection_docs:
        deployed = existing.get(doc["docId"])
        if deployed is not None and all(
            deployed.get(field) == value for field, value in doc["data"].items()
        ):
            unchanged.append(doc["docId"])
        else:
            to_write.append(doc)
    return to_write, unchanged


def publish(client, to_write: list, new_hash: str, notes: str) -> None:
    from firebase_admin import firestore as fs

    batch_size = 400
    for start in range(0, len(to_write), batch_size):
        batch = client.batch()
        for doc in to_write[start : start + batch_size]:
            ref = client.collection(doc["collection"]).document(doc["docId"])
            # merge=True so untouched fields (e.g. a future Console edit) survive;
            # updatedAt is a server timestamp and moves only because we write.
            batch.set(ref, {**doc["data"], "updatedAt": fs.SERVER_TIMESTAMP}, merge=True)
        batch.commit()

    meta_ref = client.collection(COLLECTION_CONTENT_META).document(CONTENT_META_DOC_ID)
    meta = meta_ref.get().to_dict() or {}
    meta_ref.set(
        {
            "contentVersion": int(meta.get("contentVersion", 0)) + 1,
            "contentHash": new_hash,
            "publishedAt": fs.SERVER_TIMESTAMP,
            "notes": notes,
        }
    )
    print(f"contentMeta.contentVersion -> {int(meta.get('contentVersion', 0)) + 1}")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--push", action="store_true", help="actually write (default: dry-run)")
    parser.add_argument("--project", default=os.environ.get("GCLOUD_PROJECT", "demo-hangaroo"))
    parser.add_argument("--notes", default="", help="note stored on contentMeta/singleton")
    args = parser.parse_args()

    # 1. Validation - the same checks CI runs, imported, not re-implemented.
    validator = load_validator_module()
    problems = validator.check_word_list()
    if problems:
        print(f"Refusing to seed: {len(problems)} content problem(s):")
        for problem in problems:
            print(f"  - {problem}")
        return 1

    # 2. Build documents.
    payload = json.loads(WORDS_PATH.read_text(encoding="utf-8"))
    word_docs = build_word_docs(payload["words"])
    category_docs = build_category_docs(validator.KNOWN_CATEGORIES)
    difficulty_docs = build_difficulty_docs()

    by_answer = {d["data"]["answer"] for d in word_docs}
    if len(by_answer) != len(word_docs):
        raise fail("duplicate answers survived validation - this is a bug, not content")

    new_hash = content_hash(word_docs, category_docs, difficulty_docs)
    print(
        f"Validated {len(word_docs)} words, {len(category_docs)} categories, "
        f"{len(difficulty_docs)} difficulties. contentHash={new_hash[:12]}..."
    )

    if not args.push:
        print("Dry-run: no writes performed. Re-run with --push to publish.")
        return 0

    # 3. Diff against what is already deployed.
    client = init_firestore(args.project)
    to_write: list[dict] = []
    for collection, docs in (
        (COLLECTION_WORDS, word_docs),
        (COLLECTION_CATEGORIES, category_docs),
        (COLLECTION_DIFFICULTIES, difficulty_docs),
    ):
        existing = fetch_existing(client, collection)
        writes, unchanged = diff(docs, existing)
        for doc in writes:
            doc["collection"] = collection
        to_write.extend(writes)
        print(f"{collection}: {len(writes)} to write, {len(unchanged)} unchanged")

    meta = (
        client.collection(COLLECTION_CONTENT_META)
        .document(CONTENT_META_DOC_ID)
        .get()
        .to_dict()
        or {}
    )
    if not to_write and meta.get("contentHash") == new_hash:
        print("Content is identical to the deployed copy - nothing to do.")
        return 0

    # 4. Publish: changed docs only, then bump the version exactly once.
    notes = args.notes or f"seeded {time.strftime('%Y-%m-%d %H:%M:%S UTC', time.gmtime())}"
    publish(client, to_write, new_hash, notes)
    print(f"Published {len(to_write)} document(s).")
    return 0


if __name__ == "__main__":
    sys.exit(main())
