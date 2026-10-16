using System;

/// <summary>
/// A single authored word record: the clue shown to the player, the answer they
/// must spell, the hint revealed on demand, plus the category (player-visible
/// via the ShowCategory setting) and the difficulty band it belongs to
/// (Story 1.4).
///
/// Plain serializable data (no MonoBehaviour) so it can be produced by
/// <see cref="WordPool"/>, authored in JSON, and asserted on in unit tests.
/// Field names are deliberately lower-case to match the JSON schema consumed by
/// <see cref="UnityEngine.JsonUtility"/>.
/// </summary>
[Serializable]
public sealed class WordEntry
{
    public string clue;
    public string answer;
    public string hint;
    public string category;
    public string difficulty;

    public WordEntry(string clue, string answer, string hint)
        : this(clue, answer, hint, string.Empty, string.Empty)
    {
    }

    public WordEntry(string clue, string answer, string hint, string category, string difficulty)
    {
        this.clue = clue;
        this.answer = answer;
        this.hint = hint;
        this.category = category;
        this.difficulty = difficulty;
    }
}
