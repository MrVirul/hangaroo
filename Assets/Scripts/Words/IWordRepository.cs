using System;
using System.Threading.Tasks;

/// <summary>
/// The game's word-content read path (Story 2.3, issue #32).
///
/// <see cref="GetWordsAsync"/> is the single entry point: it resolves the three
/// tiers (remote source, local cache, bundled <c>words.json</c>) and is
/// guaranteed never to return an empty list and never to throw - a network
/// failure, an empty collection or a malformed document all degrade to
/// something playable. <see cref="GameManager"/> consumes this interface, with
/// a test double behind it in EditMode and the Firestore tier behind it once
/// the Firebase SDK lands (issue #30), so the game is testable without
/// Firebase (issue #32, acceptance criterion 11).
///
/// The resolved list is fetched once at run start and held in memory for the
/// whole run: difficulty cannot change underneath the player, a mid-run
/// network drop cannot change the word set, and Firestore reads are billed
/// (issue #32, "One list per run").
/// </summary>
public interface IWordRepository
{
    /// <summary>
    /// Resolves the word list for one difficulty band. Never empty, never
    /// throws - failures degrade through local cache, then bundled content,
    /// then the built-in fallback pool.
    /// </summary>
    Task<WordListResult> GetWordsAsync(string difficultyId);

    /// <summary>
    /// Raised when a successful remote fetch replaces the cached content with a
    /// different <c>contentVersion</c> (the version is the change marker, per
    /// <c>docs/FIRESTORE_SCHEMA.md</c>).
    /// </summary>
    event Action OnContentChanged;
}
