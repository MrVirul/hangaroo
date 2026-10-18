#!/usr/bin/env python3
"""Unit tests for the pure parts of scripts/seed-firestore.py (issue #31).

These cover what can be verified without a network or an emulator:
  * deterministic document IDs (the idempotency foundation),
  * field-level diffing (unchanged docs are never rewritten),
  * DifficultyConfig asset parsing (the seeder cannot drift from the build),
  * the content hash (identical content must hash identically).

The rules themselves are covered by firebase/tests/rules.test.js, and the
end-to-end seed is verified against the emulator (see docs/FIRESTORE_SCHEMA.md).

Run:  python3 scripts/test-seed-firestore.py
"""

from __future__ import annotations

import importlib.util
import sys
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent


def load_seeder():
    path = ROOT / "scripts/seed-firestore.py"
    spec = importlib.util.spec_from_file_location("seed_firestore", path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


seeder = load_seeder()


class WordDocIdTests(unittest.TestCase):
    def test_doc_ids_are_deterministic_and_normalized(self):
        self.assertEqual(seeder.word_doc_id("GRAVITY", "Medium"), "gravity-medium")
        self.assertEqual(seeder.word_doc_id("gravity", "medium"), "gravity-medium")
        self.assertEqual(seeder.word_doc_id(" GRAVITY ", " MEDIUM "), "gravity-medium")

    def test_same_answer_in_two_difficulties_makes_two_documents(self):
        self.assertNotEqual(
            seeder.word_doc_id("PLANET", "easy"), seeder.word_doc_id("PLANET", "hard")
        )


class BuildWordDocsTests(unittest.TestCase):
    ENTRY = {
        "clue": "Natural Force",
        "answer": "GRAVITY",
        "hint": "7 letters, starts with G",
        "category": "Science",
        "difficulty": "Medium",
    }

    def test_fields_are_normalized_and_defaults_applied(self):
        docs = seeder.build_word_docs([dict(self.ENTRY)])
        self.assertEqual(len(docs), 1)
        data = docs[0]["data"]
        self.assertEqual(docs[0]["docId"], "gravity-medium")
        self.assertEqual(data["answer"], "GRAVITY")
        self.assertEqual(data["categoryId"], "science")
        self.assertEqual(data["difficultyId"], "medium")
        self.assertEqual(data["weight"], seeder.DEFAULT_WEIGHT)
        self.assertIs(data["isActive"], True)
        self.assertEqual(data["schemaVersion"], seeder.SCHEMA_VERSION)
        self.assertNotIn("updatedAt", data, "updatedAt is written by the server, never the seed")


class DiffTests(unittest.TestCase):
    def make(self, weight=1):
        return {
            "docId": "gravity-medium",
            "data": {
                "answer": "GRAVITY",
                "clue": "Natural Force",
                "hint": "H",
                "categoryId": "science",
                "difficultyId": "medium",
                "weight": weight,
                "isActive": True,
                "schemaVersion": seeder.SCHEMA_VERSION,
            },
        }

    @staticmethod
    def deployed(doc, **overrides):
        data = dict(doc["data"])
        data.update(overrides)
        return {doc["docId"]: data}

    def test_identical_content_is_unchanged(self):
        doc = self.make()
        to_write, unchanged = seeder.diff([doc], self.deployed(doc))
        self.assertEqual(to_write, [])
        self.assertEqual(unchanged, ["gravity-medium"])

    def test_deployed_updatedat_ignores_is_not_a_change(self):
        doc = self.make()
        existing = self.deployed(doc)
        existing["gravity-medium"]["updatedAt"] = 12345
        to_write, unchanged = seeder.diff([doc], existing)
        self.assertEqual(to_write, [])

    def test_a_field_change_triggers_a_write(self):
        doc = self.make(weight=2)
        to_write, unchanged = seeder.diff([doc], self.deployed(self.make()))
        self.assertEqual(len(to_write), 1)
        self.assertEqual(unchanged, [])

    def test_a_missing_document_triggers_a_write(self):
        to_write, _ = seeder.diff([self.make()], {})
        self.assertEqual(len(to_write), 1)


class DifficultyAssetTests(unittest.TestCase):
    def test_shipped_assets_parse_to_expected_values(self):
        parsed = {d["docId"]: d["data"] for d in seeder.build_difficulty_docs()}
        self.assertEqual(set(parsed), {"easy", "medium", "hard"})
        self.assertEqual(parsed["easy"]["livesPerWord"], 5)
        self.assertEqual(parsed["easy"]["hintPolicy"], "FreeImmediate")
        self.assertEqual(parsed["medium"]["scoreMultiplier"], 1.5)
        self.assertEqual(parsed["hard"]["minLength"], 8)
        self.assertEqual(parsed["hard"]["maxLength"], 12)
        self.assertEqual(parsed["hard"]["hintPolicy"], "Penalised")

    def test_unknown_hint_policy_is_rejected(self):
        with tempfile.TemporaryDirectory() as tmp:
            asset = Path(tmp) / "Broken.asset"
            asset.write_text(
                "  displayName: Broken\n  livesPerWord: 3\n  hintPolicy: 9\n"
                "  minWordLength: 4\n  maxWordLength: 6\n"
                "  hintThresholdWrongGuesses: 0\n  hintScorePenalty: 0\n"
                "  scoreMultiplier: 1\n",
                encoding="utf-8",
            )
            with self.assertRaises(SystemExit):
                seeder.parse_difficulty_asset(asset)


class ContentHashTests(unittest.TestCase):
    def test_identical_content_hashes_identically(self):
        words = seeder.build_word_docs([{"clue": "A", "answer": "PLANET", "hint": "H", "category": "space", "difficulty": "easy"}])
        cats = seeder.build_category_docs({"space"})
        diffs = [{"docId": "easy", "data": {"livesPerWord": 5}}]
        a = seeder.content_hash(words, cats, diffs)
        b = seeder.content_hash(words, cats, diffs)
        self.assertEqual(a, b)

    def test_any_content_change_changes_the_hash(self):
        words = seeder.build_word_docs([{"clue": "A", "answer": "PLANET", "hint": "H", "category": "space", "difficulty": "easy"}])
        cats = seeder.build_category_docs({"space"})
        diffs = [{"docId": "easy", "data": {"livesPerWord": 5}}]
        baseline = seeder.content_hash(words, cats, diffs)
        words[0]["data"]["weight"] = 99
        self.assertNotEqual(baseline, seeder.content_hash(words, cats, diffs))


if __name__ == "__main__":
    unittest.main(verbosity=2)
