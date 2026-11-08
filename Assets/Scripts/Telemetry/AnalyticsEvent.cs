/// <summary>
/// The analytics contract (issue #40): event names, parameter names and
/// parameter values, as one testable list of constants.
///
/// The names are the API of the feature - they are what a dashboard groups
/// and filters on - so they live here rather than scattered as string literals
/// through <see cref="GameAnalytics"/> callers, and EditMode tests pin them.
///
/// Privacy (issue #40): no constant here may carry word content, an email or
/// a display name - counts, difficulty keys and versions only.
/// </summary>
public static class AnalyticsEvent
{
    // --- events: the run funnel ---------------------------------------------

    /// <summary>A run began; the word list is resolved.</summary>
    public const string RunStart = "run_start";

    /// <summary>A word was dealt to the player.</summary>
    public const string WordLoaded = "word_loaded";

    /// <summary>The player revealed every letter of a word.</summary>
    public const string WordSolved = "word_solved";

    /// <summary>The player lost the word's last life.</summary>
    public const string WordFailed = "word_failed";

    /// <summary>The run ended, completed or not.</summary>
    public const string RunEnd = "run_end";

    // --- events: content health ---------------------------------------------

    /// <summary>Which tier served the run's words (firestore / cache / bundled).</summary>
    public const string ContentSource = "content_source";

    /// <summary>The remote tier was bypassed; the reason says why.</summary>
    public const string ContentFallback = "content_fallback";

    // --- parameter names -----------------------------------------------------

    /// <summary>easy | medium | hard (WordDifficulty-normalised); on every event.</summary>
    public const string Difficulty = "difficulty";

    /// <summary>The contentVersion the run's words carry; 0 for bundled content.</summary>
    public const string ContentVersion = "contentVersion";

    /// <summary>0/1; stays 0 until EPIC 3 lands auth.</summary>
    public const string IsSignedIn = "isSignedIn";

    /// <summary>Answer length in letters - the balance question for the length bands.</summary>
    public const string WordLength = "wordLength";

    /// <summary>Distinct letters guessed on the word.</summary>
    public const string GuessesUsed = "guessesUsed";

    /// <summary>Earned/paid hints consumed on the word; Easy's free hint is not a use.</summary>
    public const string HintsUsed = "hintsUsed";

    /// <summary>Milliseconds from word dealt to solve/fail.</summary>
    public const string TimeOnWord = "timeOnWord";

    /// <summary>Lives left when the word was solved (the key balance signal).</summary>
    public const string LivesLeft = "livesLeft";

    /// <summary>Final run score.</summary>
    public const string Score = "score";

    /// <summary>Words solved before the run ended.</summary>
    public const string WordsSolved = "wordsSolved";

    /// <summary>completed | game_over.</summary>
    public const string Outcome = "outcome";

    /// <summary>firestore | cache | bundled - what served the words.</summary>
    public const string Source = "source";

    /// <summary>Why the remote tier was bypassed (see the Reason* values).</summary>
    public const string Reason = "reason";

    /// <summary>The tier that served the words after a fallback (cache | bundled).</summary>
    public const string ServedFrom = "servedFrom";

    // --- parameter values ----------------------------------------------------

    /// <summary>Every word of the run was solved.</summary>
    public const string OutcomeCompleted = "completed";

    /// <summary>The run ended on a failed word.</summary>
    public const string OutcomeGameOver = "game_over";

    /// <summary>The remote Firestore query served the words.</summary>
    public const string SourceFirestore = "firestore";

    /// <summary>The versioned local cache served the words.</summary>
    public const string SourceCache = "cache";

    /// <summary>The bundled words.json (or built-in pool) served the words.</summary>
    public const string SourceBundled = "bundled";

    /// <summary>Transport failure: network, permission or timeout (a Firestore outage).</summary>
    public const string ReasonNetwork = "network";

    /// <summary>The query succeeded but returned no documents.</summary>
    public const string ReasonEmpty = "empty";

    /// <summary>Documents existed but none passed validation - a bad deploy.</summary>
    public const string ReasonInvalid = "invalid";
}
