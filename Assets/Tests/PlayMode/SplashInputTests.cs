using NUnit.Framework;
using UnityEngine.InputSystem;

/// <summary>
/// Regression tests for #60. The project runs with Active Input Handling =
/// "Input System Package (New)" (ProjectSettings.asset, activeInputHandler: 1),
/// where the legacy UnityEngine.Input API throws InvalidOperationException on
/// first use. These tests drive real Input System devices through
/// <see cref="InputTestFixture"/>, so they fail loudly if the splash query ever
/// goes back to the legacy API.
/// </summary>
public class SplashInputTests : InputTestFixture
{
    [Test]
    public void SkipRequested_WithNoDevices_IsFalse()
    {
        Assert.IsFalse(SplashScreenManager.SkipRequested(null, null));
    }

    [Test]
    public void SkipRequested_WithAnIdleKeyboard_IsFalse()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        Assert.IsFalse(SplashScreenManager.SkipRequested(keyboard, null));
    }

    [Test]
    public void SkipRequested_AfterAKeyPress_IsTrue()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();
        Press(keyboard[Key.Space]);

        Assert.IsTrue(SplashScreenManager.SkipRequested(keyboard, null));
    }

    [Test]
    public void SkipRequested_AfterAPointerPress_IsTrue()
    {
        // The keyboard is present but idle: the skip must come from the pointer.
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var mouse = InputSystem.AddDevice<Mouse>();
        Press(mouse.leftButton);

        Assert.IsTrue(SplashScreenManager.SkipRequested(keyboard, mouse));
    }
}
