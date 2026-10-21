using System;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// EditMode tests for <see cref="FirestoreWordSource"/> (Story 2.3, issue #32).
/// Verifies initialization guards, contract constants, null tolerance, and exception safety.
/// </summary>
public class FirestoreWordSourceTests
{
    [Test]
    public void Constructor_NullProvider_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new FirestoreWordSource(null));
    }

    [Test]
    public void SchemaConstants_MatchSchemaOfRecord()
    {
        Assert.AreEqual("words", FirestoreWordSource.CollectionWords, "collection name matches FIRESTORE_SCHEMA.md");
        Assert.AreEqual("contentMeta", FirestoreWordSource.CollectionContentMeta, "singleton collection matches FIRESTORE_SCHEMA.md");
        Assert.AreEqual("singleton", FirestoreWordSource.DocContentMetaSingleton, "singleton document id matches FIRESTORE_SCHEMA.md");
        Assert.AreEqual(200, FirestoreWordSource.QueryLimit, "client query limit is 200");
    }

    [Test]
    public async Task FetchAsync_NullOrEmptyDifficulty_ReturnsFailed()
    {
        FirestoreWordSource source = new FirestoreWordSource(
            () => null,
            readinessChecker: () => true);

        WordSourceResult nullResult = await source.FetchAsync(null);
        WordSourceResult emptyResult = await source.FetchAsync(string.Empty);

        Assert.IsFalse(nullResult.succeeded, "null difficulty returns failed result");
        Assert.IsFalse(emptyResult.succeeded, "empty difficulty returns failed result");
    }

    [Test]
    public async Task FetchAsync_WhenFirebaseUnavailable_LogsWarningAndReturnsFailed()
    {
        LogAssert.Expect(LogType.Warning, "FirestoreWordSource: Firebase is unavailable; falling back.");

        FirestoreWordSource source = new FirestoreWordSource(
            () => null,
            readinessChecker: () => false,
            whenReadyProvider: () => Task.CompletedTask);

        WordSourceResult result = await source.FetchAsync("easy");

        Assert.IsFalse(result.succeeded, "unavailable Firebase degrades to failed result");
        Assert.AreEqual(0, result.contentVersion);
        Assert.AreEqual(0, result.documents.Count);
    }

    [Test]
    public async Task FetchAsync_WhenProviderReturnsNull_LogsWarningAndReturnsFailed()
    {
        LogAssert.Expect(LogType.Warning, "FirestoreWordSource: FirebaseFirestore instance is null; falling back.");

        FirestoreWordSource source = new FirestoreWordSource(
            () => null,
            readinessChecker: () => true);

        WordSourceResult result = await source.FetchAsync("easy");

        Assert.IsFalse(result.succeeded, "null provider returns failed result");
        Assert.AreEqual(0, result.contentVersion);
        Assert.AreEqual(0, result.documents.Count);
    }

    [Test]
    public async Task FetchAsync_WhenProviderThrows_LogsWarningAndReturnsFailedWithoutThrowing()
    {
        LogAssert.Expect(LogType.Warning, "FirestoreWordSource: fetch failed (simulated provider crash); falling back.");

        FirestoreWordSource source = new FirestoreWordSource(
            () => throw new InvalidOperationException("simulated provider crash"),
            readinessChecker: () => true);

        WordSourceResult result = await source.FetchAsync("medium");

        Assert.IsFalse(result.succeeded, "exceptions are trapped and degrade to Failed()");
        Assert.AreEqual(0, result.contentVersion);
        Assert.AreEqual(0, result.documents.Count);
    }
}
