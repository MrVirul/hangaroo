using UnityEngine;

/// <summary>
/// The game's analytics façade (issue #40): one typed <c>Log*</c> method per
/// event, each building its parameter list from <see cref="AnalyticsEvent"/>
/// names and handing it to the injected <see cref="IAnalyticsService"/>.
///
/// Why typed methods instead of a free <c>Log(name, params)</c> escape hatch:
/// free text is how a word, an email or a display name eventually leaks into
/// telemetry. With typed methods the privacy rule ("counts and difficulty
/// only, never content") is enforced by the compiler's signature list, not by
/// reviewer vigilance - and the EditMode contract tests pin both the wire
/// names and the parameter types.
///
/// Delivery is best-effort: the default <see cref="NullAnalyticsService"/>
/// drops everything, and every call is wrapped so a throwing sink can never
/// interrupt gameplay.
/// </summary>
public static class GameAnalytics
{
    public const string LogPrefix = "GameAnalytics:";

    /// <summary>
    /// The active sink; starts as the no-op so the game works with no
    /// Firebase installed. <see cref="FirebaseBootstrap"/> swaps in the
    /// Firebase implementation once - and only once - the SDK is ready.
    /// </summary>
    public static IAnalyticsService Service { get; set; } = NullAnalyticsService.Instance;

    /// <summary>
    /// The run funnel begins. <paramref name="contentVersion"/> is the version
    /// the word list resolved to (0 for bundled), so late-drop funnels can be
    /// sliced by content generation. <paramref name="isSignedIn"/> stays false
    /// until EPIC 3 adds auth.
    /// </summary>
    public static void LogRunStart(string difficulty, long contentVersion, bool isSignedIn)
    {
        Send(
            AnalyticsEvent.RunStart,
            AnalyticsParam.Text(AnalyticsEvent.Difficulty, difficulty),
            AnalyticsParam.Number(AnalyticsEvent.ContentVersion, contentVersion),
            AnalyticsParam.Number(AnalyticsEvent.IsSignedIn, isSignedIn ? 1 : 0));
    }

    /// <summary>
    /// A word was dealt. <paramref name="wordLength"/> (answer letters) is the
    /// length-band balance signal - solved rate per band answers "are 3-4
    /// letter answers too easy?" (issue #40's analytics question one).
    /// </summary>
    public static void LogWordLoaded(string difficulty, int wordLength)
    {
        Send(
            AnalyticsEvent.WordLoaded,
            AnalyticsParam.Text(AnalyticsEvent.Difficulty, difficulty),
            AnalyticsParam.Number(AnalyticsEvent.WordLength, wordLength));
    }

    /// <summary>
    /// The player revealed every letter. <paramref name="livesLeft"/> on solve
    /// is the key balance lever's readout: rising means answers are guessable
    /// from fewer letters (issue #40, analytics question two).
    /// </summary>
    public static void LogWordSolved(string difficulty, int guessesUsed, int hintsUsed, long timeOnWordMs, int livesLeft)
    {
        Send(
            AnalyticsEvent.WordSolved,
            AnalyticsParam.Text(AnalyticsEvent.Difficulty, difficulty),
            AnalyticsParam.Number(AnalyticsEvent.GuessesUsed, guessesUsed),
            AnalyticsParam.Number(AnalyticsEvent.HintsUsed, hintsUsed),
            AnalyticsParam.Number(AnalyticsEvent.TimeOnWord, timeOnWordMs),
            AnalyticsParam.Number(AnalyticsEvent.LivesLeft, livesLeft));
    }

    /// <summary>The player lost the word's last life - the word-to-fail transition.</summary>
    public static void LogWordFailed(string difficulty, int guessesUsed, long timeOnWordMs)
    {
        Send(
            AnalyticsEvent.WordFailed,
            AnalyticsParam.Text(AnalyticsEvent.Difficulty, difficulty),
            AnalyticsParam.Number(AnalyticsEvent.GuessesUsed, guessesUsed),
            AnalyticsParam.Number(AnalyticsEvent.TimeOnWord, timeOnWordMs));
    }

    /// <summary>
    /// The run ended. <paramref name="outcome"/> is one of the
    /// <c>AnalyticsEvent.Outcome*</c> values; wordsSolved distinguishes a
    /// near-miss from a first-word wipe.
    /// </summary>
    public static void LogRunEnd(string difficulty, long score, int wordsSolved, string outcome)
    {
        Send(
            AnalyticsEvent.RunEnd,
            AnalyticsParam.Text(AnalyticsEvent.Difficulty, difficulty),
            AnalyticsParam.Number(AnalyticsEvent.Score, score),
            AnalyticsParam.Number(AnalyticsEvent.WordsSolved, wordsSolved),
            AnalyticsParam.Text(AnalyticsEvent.Outcome, outcome));
    }

    /// <summary>
    /// Content health: which tier served this run's words
    /// (<c>AnalyticsEvent.Source*</c>). Fires on every run so drift between
    /// firestore / cache / bundled is visible at a glance.
    /// </summary>
    public static void LogContentSource(string source, long contentVersion)
    {
        Send(
            AnalyticsEvent.ContentSource,
            AnalyticsParam.Text(AnalyticsEvent.Source, source),
            AnalyticsParam.Number(AnalyticsEvent.ContentVersion, contentVersion));
    }

    /// <summary>
    /// Content health: the remote tier was bypassed. <paramref name="reason"/>
    /// (<c>AnalyticsEvent.Reason*</c>) separates a Firestore outage
    /// (network) from an empty collection (empty) from a bad deploy (invalid);
    /// <paramref name="servedFrom"/> says which tier saved the run.
    /// </summary>
    public static void LogContentFallback(string reason, string servedFrom)
    {
        Send(
            AnalyticsEvent.ContentFallback,
            AnalyticsParam.Text(AnalyticsEvent.Reason, reason),
            AnalyticsParam.Text(AnalyticsEvent.ServedFrom, servedFrom));
    }

    /// <summary>
    /// Hands the event to the sink, guarding the seam: a throwing or missing
    /// service degrades to a dropped event with one warning, never a gameplay
    /// failure.
    /// </summary>
    private static void Send(string eventName, params AnalyticsParam[] parameters)
    {
        try
        {
            Service.LogEvent(eventName, parameters);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning(LogPrefix + " event '" + eventName + "' was dropped: " + e.Message);
        }
    }
}
