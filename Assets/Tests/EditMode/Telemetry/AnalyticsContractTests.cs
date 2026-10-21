using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// EditMode coverage for the analytics contract (issue #40): the wire names
/// are pinned (they are what a dashboard groups on, so a rename is a breaking
/// change), every typed method is asserted to send exactly its documented
/// parameters, and the privacy rule is enforced by reflecting over the public
/// API - no free-text parameter may ever smuggle word content into telemetry.
/// </summary>
public class AnalyticsContractTests
{
    private sealed class FakeAnalyticsService : IAnalyticsService
    {
        public readonly List<(string eventName, AnalyticsParam[] parameters)> events =
            new List<(string, AnalyticsParam[])>();

        public bool throws;

        public void LogEvent(string eventName, params AnalyticsParam[] parameters)
        {
            if (throws)
                throw new InvalidOperationException("sink down");

            events.Add((eventName, parameters));
        }
    }

    private FakeAnalyticsService fake;

    [SetUp]
    public void SetUp()
    {
        fake = new FakeAnalyticsService();
        GameAnalytics.Service = fake;
    }

    [TearDown]
    public void TearDown()
    {
        GameAnalytics.Service = NullAnalyticsService.Instance;
    }

    // --- helpers -------------------------------------------------------------

    private (string eventName, AnalyticsParam[] parameters) Single(string eventName)
    {
        Assert.AreEqual(1, fake.events.Count, "expected exactly one event");
        Assert.AreEqual(eventName, fake.events[0].eventName);
        return fake.events[0];
    }

    private static AnalyticsParam Param((string eventName, AnalyticsParam[] parameters) evt, string name)
    {
        AnalyticsParam match = evt.parameters.FirstOrDefault(p => p.name == name);
        Assert.AreNotEqual(default, match, "missing parameter '" + name + "'");
        return match;
    }

    private static void AssertNames((string eventName, AnalyticsParam[] parameters) evt,
        params string[] expectedNames)
    {
        CollectionAssert.AreEqual(expectedNames,
            evt.parameters.Select(p => p.name).ToArray(),
            "the parameter list is the contract - names AND order");
    }

    // --- the wire contract ---------------------------------------------------

    [Test]
    public void EventNames_MatchTheIssue40Contract()
    {
        Assert.AreEqual("run_start", AnalyticsEvent.RunStart);
        Assert.AreEqual("word_loaded", AnalyticsEvent.WordLoaded);
        Assert.AreEqual("word_solved", AnalyticsEvent.WordSolved);
        Assert.AreEqual("word_failed", AnalyticsEvent.WordFailed);
        Assert.AreEqual("run_end", AnalyticsEvent.RunEnd);
        Assert.AreEqual("content_source", AnalyticsEvent.ContentSource);
        Assert.AreEqual("content_fallback", AnalyticsEvent.ContentFallback);
    }

    [Test]
    public void RunStart_CarriesDifficultyVersionAndSignInFlag()
    {
        GameAnalytics.LogRunStart("medium", 12, isSignedIn: false);

        var evt = Single(AnalyticsEvent.RunStart);
        AssertNames(evt, AnalyticsEvent.Difficulty, AnalyticsEvent.ContentVersion, AnalyticsEvent.IsSignedIn);

        Assert.AreEqual(AnalyticsParamType.Text, Param(evt, AnalyticsEvent.Difficulty).type);
        Assert.AreEqual("medium", Param(evt, AnalyticsEvent.Difficulty).textValue);
        Assert.AreEqual(12, Param(evt, AnalyticsEvent.ContentVersion).numberValue);
        Assert.AreEqual(0, Param(evt, AnalyticsEvent.IsSignedIn).numberValue,
            "bools travel as 1/0; stays 0 until EPIC 3 adds auth");
    }

    [Test]
    public void WordLoaded_CarriesDifficultyAndWordLength()
    {
        GameAnalytics.LogWordLoaded("hard", 6);

        var evt = Single(AnalyticsEvent.WordLoaded);
        AssertNames(evt, AnalyticsEvent.Difficulty, AnalyticsEvent.WordLength);
        Assert.AreEqual(6, Param(evt, AnalyticsEvent.WordLength).numberValue);
    }

    [Test]
    public void WordSolved_CarriesTheBalanceFields()
    {
        GameAnalytics.LogWordSolved("medium", guessesUsed: 4, hintsUsed: 1,
            timeOnWordMs: 25000, livesLeft: 2);

        var evt = Single(AnalyticsEvent.WordSolved);
        AssertNames(evt, AnalyticsEvent.Difficulty, AnalyticsEvent.GuessesUsed,
            AnalyticsEvent.HintsUsed, AnalyticsEvent.TimeOnWord, AnalyticsEvent.LivesLeft);

        Assert.AreEqual(4, Param(evt, AnalyticsEvent.GuessesUsed).numberValue);
        Assert.AreEqual(1, Param(evt, AnalyticsEvent.HintsUsed).numberValue);
        Assert.AreEqual(25000, Param(evt, AnalyticsEvent.TimeOnWord).numberValue);
        Assert.AreEqual(2, Param(evt, AnalyticsEvent.LivesLeft).numberValue,
            "livesLeft is the key balance signal (issue #40 question two)");
    }

    [Test]
    public void WordFailed_CarriesDifficultyGuessesAndTime()
    {
        GameAnalytics.LogWordFailed("easy", guessesUsed: 6, timeOnWordMs: 9000);

        var evt = Single(AnalyticsEvent.WordFailed);
        AssertNames(evt, AnalyticsEvent.Difficulty, AnalyticsEvent.GuessesUsed, AnalyticsEvent.TimeOnWord);
        Assert.AreEqual(6, Param(evt, AnalyticsEvent.GuessesUsed).numberValue);
        Assert.AreEqual(9000, Param(evt, AnalyticsEvent.TimeOnWord).numberValue);
    }

    [Test]
    public void RunEnd_CarriesScoreSolvedCountAndOutcome()
    {
        GameAnalytics.LogRunEnd("hard", score: 1450, wordsSolved: 5,
            outcome: AnalyticsEvent.OutcomeCompleted);

        var evt = Single(AnalyticsEvent.RunEnd);
        AssertNames(evt, AnalyticsEvent.Difficulty, AnalyticsEvent.Score,
            AnalyticsEvent.WordsSolved, AnalyticsEvent.Outcome);

        Assert.AreEqual(1450, Param(evt, AnalyticsEvent.Score).numberValue);
        Assert.AreEqual(5, Param(evt, AnalyticsEvent.WordsSolved).numberValue);
        Assert.AreEqual("completed", Param(evt, AnalyticsEvent.Outcome).textValue);
    }

    [Test]
    public void ContentSource_CarriesSourceAndVersion()
    {
        GameAnalytics.LogContentSource(AnalyticsEvent.SourceCache, 7);

        var evt = Single(AnalyticsEvent.ContentSource);
        AssertNames(evt, AnalyticsEvent.Source, AnalyticsEvent.ContentVersion);
        Assert.AreEqual("cache", Param(evt, AnalyticsEvent.Source).textValue);
        Assert.AreEqual(7, Param(evt, AnalyticsEvent.ContentVersion).numberValue);
    }

    [Test]
    public void ContentFallback_CarriesReasonAndServedFrom()
    {
        GameAnalytics.LogContentFallback(
            AnalyticsEvent.ReasonNetwork, AnalyticsEvent.SourceBundled);

        var evt = Single(AnalyticsEvent.ContentFallback);
        AssertNames(evt, AnalyticsEvent.Reason, AnalyticsEvent.ServedFrom);
        Assert.AreEqual("network", Param(evt, AnalyticsEvent.Reason).textValue);
        Assert.AreEqual("bundled", Param(evt, AnalyticsEvent.ServedFrom).textValue);
    }

    // --- delivery is best-effort --------------------------------------------

    [Test]
    public void NullService_DropsEveryEventWithoutThrowing()
    {
        GameAnalytics.Service = NullAnalyticsService.Instance;

        Assert.DoesNotThrow(() =>
        {
            GameAnalytics.LogRunStart("easy", 1, false);
            GameAnalytics.LogWordLoaded("easy", 5);
            GameAnalytics.LogWordSolved("easy", 2, 0, 1000, 4);
            GameAnalytics.LogWordFailed("easy", 6, 2000);
            GameAnalytics.LogRunEnd("easy", 100, 1, AnalyticsEvent.OutcomeGameOver);
            GameAnalytics.LogContentSource(AnalyticsEvent.SourceBundled, 0);
            GameAnalytics.LogContentFallback(AnalyticsEvent.ReasonEmpty, AnalyticsEvent.SourceBundled);
        }, "the game ships playable with no analytics - dropping is the whole job");
    }

    [Test]
    public void ThrowingService_IsContainedWithOneWarningAndNeverPropagates()
    {
        fake.throws = true;
        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("GameAnalytics:"));

        Assert.DoesNotThrow(() => GameAnalytics.LogWordLoaded("easy", 5),
            "a broken sink must never reach gameplay");
    }

    // --- privacy by construction (issue #40) ---------------------------------

    /// <summary>
    /// The only strings the public API accepts are these identifier slots -
    /// difficulty keys, outcome/source/reason values. No parameter exists that
    /// could carry a word, a clue, a hint, an email or a display name.
    /// </summary>
    [Test]
    public void PublicApi_OnlyAcceptsIdentifierStrings_NoContentSlots()
    {
        string[] allowedStringParameters = { "difficulty", "outcome", "source", "reason", "servedFrom" };

        MethodInfo[] publicMethods = typeof(GameAnalytics)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.DeclaringType == typeof(GameAnalytics))
            .ToArray();

        Assert.IsNotEmpty(publicMethods, "the façade must keep its typed methods");

        foreach (MethodInfo method in publicMethods)
        {
            foreach (ParameterInfo parameter in method.GetParameters())
            {
                if (parameter.ParameterType != typeof(string))
                    continue;

                Assert.Contains(parameter.Name, allowedStringParameters,
                    method.Name + " takes a string parameter '" + parameter.Name +
                    "' outside the identifier whitelist - issue #40 forbids content in telemetry");
            }
        }
    }
}
