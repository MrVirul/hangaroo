using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>
/// PlayMode regressions for the defects that only reproduce against real scene
/// state (issue #38). Kept deliberately small: these cover the four scene-flow
/// bugs that reached <c>main</c>, nothing more.
///
/// Every test loads the real <c>GamePlay</c> scene, so it exercises the same
/// wiring a player gets. Difficulties come from <c>PlayerPrefs</c>; the tests
/// read the resulting heart count instead of assuming one, so they stay green
/// whichever difficulty is active (issue #38 - "tests from the intended rules").
/// </summary>
public class GameplayFlowTests
{
    private const string GameplayScene = "GamePlay";

    [UnitySetUp]
    public IEnumerator LoadGameplayScene()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(GameplayScene);

        // The scene load is deferred to the end of the frame it is requested in.
        float deadline = Time.realtimeSinceStartup + 15f;
        while (GameManager.Instance == null)
        {
            Assert.Less(Time.realtimeSinceStartup, deadline, "GamePlay scene did not finish loading.");
            yield return null;
        }

        // One more frame so Awake/Start (StartRun + LoadWord) have fully run.
        yield return null;
    }

    [TearDown]
    public void ResetTimeScale()
    {
        // A paused frame would hang any WaitForSeconds-based test that follows.
        Time.timeScale = 1f;
    }

    // ---------------------------------------------------------------------
    // #10 - Keyboard keys are never re-enabled, making the game unwinnable
    //       from word 2 onwards.
    // ---------------------------------------------------------------------

    [UnityTest]
    public IEnumerator Keyboard_Reenables_Every_Key_For_The_Next_Word()
    {
        GameManager game = GameManager.Instance;
        KeyboardManager keyboard = KeyboardManager.Instance;

        foreach (char letter in UniqueLetters(CurrentAnswer(game)))
        {
            keyboard.PressLetter(letter.ToString());
        }

        yield return null;

        Assert.IsTrue(ShowVictory(), "Solving every slot must show the Victory panel.");
        Assert.IsFalse(AllLetterKeysInteractable(keyboard),
            "Guessed letters should still be disabled while the Victory panel is up.");

        game.ContinueGame();

        yield return null;

        Assert.IsTrue(AllLetterKeysInteractable(keyboard),
            "After continuing, every letter key must be interactable again (issue #10).");
    }

    // ---------------------------------------------------------------------
    // #12 - Restart() does not reset Time.timeScale, so pausing then
    //       restarting yields a frozen GamePlay scene.
    // ---------------------------------------------------------------------

    [UnityTest]
    public IEnumerator Restart_After_Pause_Resets_TimeScale()
    {
        UIManager.Instance.Pause();
        Assert.AreEqual(0f, Time.timeScale, "Pause must freeze time.");

        GameManager.Instance.Restart();

        // Restart's fix sets the scale synchronously; the scene reload is deferred,
        // so this asserts the fix itself rather than the freshly Awoken scene.
        Assert.AreEqual(1f, Time.timeScale, "Restart must reset timeScale (issue #12).");

        // Let the deferred reload settle so it cannot bleed into the next test.
        yield return null;
        yield return null;
    }

    // ---------------------------------------------------------------------
    // #13 - CheckLetter has no idempotency guard; correctness depended on the
    //       UI greying keys out.
    // ---------------------------------------------------------------------

    [UnityTest]
    public IEnumerator Repeated_Correct_Guess_Scores_Only_Once()
    {
        GameManager game = GameManager.Instance;
        char letter = UniqueLetters(CurrentAnswer(game))[0];

        int before = game.CurrentScore;
        game.CheckLetter(letter);
        int afterFirst = game.CurrentScore;

        game.CheckLetter(letter);
        int afterSecond = game.CurrentScore;

        yield return null;

        Assert.Greater(afterFirst, before, "The first correct guess must score.");
        Assert.AreEqual(afterFirst, afterSecond,
            "Repeating a used guess must not score again (issue #13).");
    }

    [UnityTest]
    public IEnumerator Repeated_Wrong_Guess_Costs_Only_One_Heart()
    {
        GameManager game = GameManager.Instance;
        char wrong = FirstLetterNotIn(CurrentAnswer(game));

        int before = HeartManager.Instance.Lives;
        game.CheckLetter(wrong);
        int afterFirst = HeartManager.Instance.Lives;

        game.CheckLetter(wrong);
        int afterSecond = HeartManager.Instance.Lives;

        yield return null;

        Assert.AreEqual(before - 1, afterFirst, "The first wrong guess must cost one heart.");
        Assert.AreEqual(afterFirst, afterSecond,
            "Repeating a used guess must not cost another heart (issue #13).");
    }

    // ---------------------------------------------------------------------
    // #19 - Run lifecycle: hearts refill per word, and the run ends (rather
    //       than looping or dead-ending) when the last heart is spent.
    // ---------------------------------------------------------------------

    [UnityTest]
    public IEnumerator Hearts_Refill_At_The_Start_Of_Each_Word()
    {
        GameManager game = GameManager.Instance;
        int maxLives = HeartManager.Instance.MaxLives;

        game.CheckLetter(FirstLetterNotIn(CurrentAnswer(game)));
        Assert.AreEqual(maxLives - 1, HeartManager.Instance.Lives, "A wrong guess costs a heart.");

        foreach (char letter in UniqueLetters(CurrentAnswer(game)))
        {
            game.CheckLetter(letter);
        }

        game.ContinueGame();
        yield return null;

        Assert.AreEqual(maxLives, HeartManager.Instance.Lives,
            "Each new word must start with a full set of hearts (issue #19).");
    }

    [UnityTest]
    public IEnumerator Run_Ends_When_The_Last_Heart_Is_Spent()
    {
        GameManager game = GameManager.Instance;
        string answer = CurrentAnswer(game);

        // Guess distinct letters that are not in the word until the run ends.
        foreach (char letter in "ABCDEFGHIJKLMNOPQRSTUVWXYZ")
        {
            if (ShowGameOver())
            {
                break;
            }

            if (answer.IndexOf(letter) >= 0)
            {
                continue;
            }

            game.CheckLetter(letter);
        }

        yield return null;

        Assert.IsTrue(ShowGameOver(),
            "Exhausting the word's hearts must end the run and show Game Over (issue #19).");
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------

    /// <summary>
    /// Reads the answer from the letter slots rather than the private field, so
    /// the tests do not require test-only API on <see cref="GameManager"/>.
    /// </summary>
    private static string CurrentAnswer(GameManager game)
    {
        LetterSlot[] slots = game.letterSlotParent.GetComponentsInChildren<LetterSlot>();
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        foreach (LetterSlot slot in slots)
        {
            builder.Append(slot.GetLetter());
        }

        return builder.ToString().ToUpperInvariant();
    }

    private static List<char> UniqueLetters(string word)
    {
        List<char> letters = new List<char>();
        foreach (char c in word)
        {
            char upper = char.ToUpperInvariant(c);
            if (!letters.Contains(upper))
            {
                letters.Add(upper);
            }
        }

        return letters;
    }

    private static char FirstLetterNotIn(string word)
    {
        string upper = word.ToUpperInvariant();
        for (char c = 'A'; c <= 'Z'; c++)
        {
            if (upper.IndexOf(c) < 0)
            {
                return c;
            }
        }

        return 'Z';
    }

    private static bool AllLetterKeysInteractable(KeyboardManager keyboard)
    {
        foreach (Button button in keyboard.GetComponentsInChildren<Button>(true))
        {
            if (IsLetterKey(button.name) && !button.interactable)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsLetterKey(string buttonName)
    {
        return !string.IsNullOrEmpty(buttonName) && char.IsLetter(buttonName[0]);
    }

    private static bool ShowVictory()
    {
        return UIManager.Instance.victoryPanel.activeSelf;
    }

    private static bool ShowGameOver()
    {
        return UIManager.Instance.gameOverPanel.activeSelf;
    }
}
