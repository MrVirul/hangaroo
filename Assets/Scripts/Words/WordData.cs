using System;

/// <summary>
/// The content model for a single playable word (Story 2.4, issue #33).
///
/// The serialised fields are deliberately lower-case while the model members are
/// PascalCase, because <see cref="UnityEngine.JsonUtility"/> binds JSON keys to
/// fields by exact case (verified empirically: a <c>clue</c> key does not populate
/// a <c>Clue</c> field) and the bundled wire format mandated by issue #4 uses
/// lower-case keys (<c>clue</c>, <c>answer</c>, <c>hint</c>, <c>category</c>,
/// <c>difficulty</c>). Keeping the wire fields as the serialisation shape and
/// exposing <see cref="Answer"/>/<see cref="CategoryId"/> etc. as properties means
/// one type is both what <c>words.json</c> deserialises into and the model every
/// read path hands to the game.
///
/// No Firebase type appears here: the Firestore path reads
/// <c>DocumentSnapshot.Data</c> as a plain dictionary and calls
/// <see cref="FirestoreWordMapper"/> (issue #30 keeps the SDK at the call site), so
/// this model stays <see cref="UnityEngine.JsonUtility"/>-serialisable and unit
/// testable without the SDK.
///
/// Raw instances straight out of JSON are not yet normalised; every read path runs
/// <see cref="Normalize"/> before validation so the bundled and Firestore paths
/// cannot drift apart (issue #33, acceptance criterion 5).
/// </summary>
[Serializable]
public sealed class WordData
{
    /// <summary>Clue shown to the player. Wire key: <c>clue</c>.</summary>
    public string clue;

    /// <summary>The answer to spell, upper-cased after <see cref="Normalize"/>. Wire key: <c>answer</c>.</summary>
    public string answer;

    /// <summary>Hint revealed on demand. Wire key: <c>hint</c>.</summary>
    public string hint;

    /// <summary>
    /// Category key from <see cref="WordCategory"/>. Wire key in <c>words.json</c>:
    /// <c>category</c>; the Firestore document field is <c>categoryId</c> (mapped
    /// by <see cref="FirestoreWordMapper"/>).
    /// </summary>
    public string category;

    /// <summary>
    /// Difficulty band key from <see cref="WordDifficulty"/>. Wire key in
    /// <c>words.json</c>: <c>difficulty</c>; the Firestore document field is
    /// <c>difficultyId</c>.
    /// </summary>
    public string difficulty;

    /// <summary>
    /// Relative selection weight; 0 means never select, mirroring the Firestore
    /// schema so soft-disabling behaves identically on both read paths.
    /// Defaults to 1 when the wire form omits the key.
    /// </summary>
    public int weight = 1;

    /// <summary>
    /// Soft-delete flag: false content never enters the pool, matching the
    /// Firestore query's <c>isActive == true</c> filter. Defaults to true when the
    /// wire form omits the key (including every pre-Story-1.4 entry).
    /// </summary>
    public bool isActive = true;

    // --- Model members (issue #33) ----------------------------------------

    public string Answer => answer;
    public string Clue => clue;
    public string Hint => hint;
    public string CategoryId => category;
    public string DifficultyId => difficulty;
    public int Weight => weight;
    public bool IsActive => isActive;

    /// <summary>Length of the answer; 0 when unset.</summary>
    public int Length => string.IsNullOrEmpty(answer) ? 0 : answer.Length;

    /// <summary>Parameterless constructor exists for <see cref="UnityEngine.JsonUtility"/>.</summary>
    public WordData()
    {
    }

    /// <summary>
    /// Plain constructor storing the given values as-is (no normalisation): raw
    /// wire instances and test fixtures both go through <see cref="Normalize"/>
    /// before validation, in one shared step. <paramref name="category"/> and
    /// <paramref name="difficulty"/> default to empty exactly like a pre-Story-1.4
    /// entry that never declared them, which <see cref="Normalize"/> then infers.
    /// </summary>
    public WordData(string clue, string answer, string hint, string category = "", string difficulty = "",
        int weight = 1, bool isActive = true)
    {
        this.clue = clue;
        this.answer = answer;
        this.hint = hint;
        this.category = category;
        this.difficulty = difficulty;
        this.weight = weight;
        this.isActive = isActive;
    }

    /// <summary>
    /// The single normalisation and legacy-defaults step both read paths run
    /// before validation (issue #33): the answer is trimmed and upper-cased, the
    /// category and difficulty keys are trimmed and lower-cased, an absent
    /// difficulty is inferred from the answer length so a pre-Story-1.4
    /// <c>words.json</c> still loads, and an absent category falls back to
    /// <see cref="WordCategory.General"/>. Declared-but-wrong values are left for
    /// <see cref="WordValidator"/> to reject. Returns a normalised copy;
    /// <paramref name="raw"/> is untouched.
    /// </summary>
    public static WordData Normalize(WordData raw)
    {
        if (raw == null)
            return null;

        string answer = WordValidator.Normalize(raw.answer);

        string difficulty = WordDifficulty.Normalize(raw.difficulty);
        if (difficulty.Length == 0)
            difficulty = WordDifficulty.InferFromLength(answer.Length);

        string category = WordCategory.Normalize(raw.category);
        if (category.Length == 0)
            category = WordCategory.General;

        return new WordData(raw.clue, answer, raw.hint, category, difficulty, raw.weight, raw.isActive);
    }
}
