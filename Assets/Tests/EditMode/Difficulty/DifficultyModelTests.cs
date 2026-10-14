using NUnit.Framework;
using UnityEngine;

/// <summary>
/// EditMode coverage for the difficulty model contract (Story 1.1).
/// </summary>
public class DifficultyModelTests
{
    [Test]
    public void FromPlayerPrefs_ReturnsEachKnownDifficulty()
    {
        Assert.AreEqual(DifficultyId.Easy, DifficultyIdExtensions.FromPlayerPrefs((int)DifficultyId.Easy));
        Assert.AreEqual(DifficultyId.Medium, DifficultyIdExtensions.FromPlayerPrefs((int)DifficultyId.Medium));
        Assert.AreEqual(DifficultyId.Hard, DifficultyIdExtensions.FromPlayerPrefs((int)DifficultyId.Hard));
    }

    [Test]
    public void FromPlayerPrefs_FallsBackToMediumForUnknownValues()
    {
        Assert.AreEqual(DifficultyId.Medium, DifficultyIdExtensions.FromPlayerPrefs(-1));
        Assert.AreEqual(DifficultyId.Medium, DifficultyIdExtensions.FromPlayerPrefs(99));
        Assert.AreEqual(DifficultyId.Medium, DifficultyIdExtensions.Default);
    }

    [Test]
    public void ToPlayerPrefs_RoundTripsThroughFromPlayerPrefs()
    {
        foreach (DifficultyId id in System.Enum.GetValues(typeof(DifficultyId)))
        {
            Assert.AreEqual(id, DifficultyIdExtensions.FromPlayerPrefs(id.ToPlayerPrefs()));
        }
    }

    [Test]
    public void Library_AssetIsResolvableFromResources()
    {
        DifficultyLibrary library = Resources.Load<DifficultyLibrary>("Difficulty/DifficultyLibrary");

        Assert.IsNotNull(library, "DifficultyLibrary.asset must live under Assets/Resources/Difficulty/.");
    }

    [Test]
    public void Library_ReferencesAllThreeConfigs()
    {
        DifficultyLibrary library = Resources.Load<DifficultyLibrary>("Difficulty/DifficultyLibrary");
        Assert.IsNotNull(library);

        Assert.IsNotNull(library.Get(DifficultyId.Easy), "Missing Easy config.");
        Assert.IsNotNull(library.Get(DifficultyId.Medium), "Missing Medium config.");
        Assert.IsNotNull(library.Get(DifficultyId.Hard), "Missing Hard config.");
    }

    [Test]
    public void Current_IsAvailableWithoutSceneStart()
    {
        // Accessing Current forces lazy resolution and must yield a defined id
        // before any scene Start() has run.
        Assert.IsTrue(System.Enum.IsDefined(typeof(DifficultyId), DifficultyLibrary.Current));
    }
}
