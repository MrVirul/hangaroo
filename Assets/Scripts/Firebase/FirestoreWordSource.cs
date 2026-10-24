using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;

/// <summary>
/// The Firestore implementation of <see cref="IWordSource"/> (Story 2.3, issue #32).
///
/// Queries Cloud Firestore according to the schema of record (docs/FIRESTORE_SCHEMA.md):
///   - Collection: words
///   - Filters: difficultyId == {difficulty}, isActive == true
///   - Order: weight desc
///   - Limit: 200
///
/// Also reads contentVersion from contentMeta/singleton so WordRepository can
/// detect content changes and invalidate stale local caches.
///
/// Safety rules:
///   - Never throws into the caller: any network/SDK/permission exception logs one
///     warning and returns WordSourceResult.Failed(), prompting WordRepository
///     to seamlessly fall back to local cache or bundled words.json.
///   - Awaits FirebaseBootstrap.WhenReady before touching FirebaseFirestore.
/// </summary>
public sealed class FirestoreWordSource : IWordSource
{
    public const string LogPrefix = "FirestoreWordSource:";

    public const string CollectionWords = "words";
    public const string CollectionContentMeta = "contentMeta";
    public const string DocContentMetaSingleton = "singleton";
    public const int QueryLimit = 200;

    private readonly Func<FirebaseFirestore> firestoreProvider;
    private readonly Func<bool> readinessChecker;
    private readonly Func<Task> whenReadyProvider;

    /// <summary>Default constructor using FirebaseFirestore.DefaultInstance and FirebaseBootstrap.</summary>
    public FirestoreWordSource() : this(
        () => FirebaseFirestore.DefaultInstance,
        () => FirebaseBootstrap.IsReady,
        () => FirebaseBootstrap.WhenReady)
    {
    }

    /// <summary>Constructor for unit testing and dependency injection.</summary>
    public FirestoreWordSource(
        Func<FirebaseFirestore> firestoreProvider,
        Func<bool> readinessChecker = null,
        Func<Task> whenReadyProvider = null)
    {
        this.firestoreProvider = firestoreProvider ?? throw new ArgumentNullException(nameof(firestoreProvider));
        this.readinessChecker = readinessChecker ?? (() => FirebaseBootstrap.IsReady);
        this.whenReadyProvider = whenReadyProvider ?? (() => FirebaseBootstrap.WhenReady);
    }

    /// <inheritdoc/>
    public async Task<WordSourceResult> FetchAsync(string difficultyId)
    {
        if (string.IsNullOrEmpty(difficultyId))
        {
            return WordSourceResult.Failed();
        }

        try
        {
            // Gate on initialization without hanging: WhenReady completes once bootstrap finishes.
            if (!readinessChecker())
            {
                Task waitTask = whenReadyProvider?.Invoke();
                if (waitTask != null && !waitTask.IsCompleted)
                {
                    await waitTask;
                }

                if (!readinessChecker())
                {
                    Debug.LogWarning(LogPrefix + " Firebase is unavailable; falling back.");
                    return WordSourceResult.Failed();
                }
            }

            FirebaseFirestore firestore = firestoreProvider();
            if (firestore == null)
            {
                Debug.LogWarning(LogPrefix + " FirebaseFirestore instance is null; falling back.");
                return WordSourceResult.Failed();
            }

            int contentVersion = await FetchContentVersionAsync(firestore);

            Query query = firestore
                .Collection(CollectionWords)
                .WhereEqualTo("difficultyId", difficultyId)
                .WhereEqualTo("isActive", true)
                .OrderByDescending("weight")
                .Limit(QueryLimit);

            QuerySnapshot querySnapshot = await query.GetSnapshotAsync();
            if (querySnapshot == null)
            {
                return WordSourceResult.Failed();
            }

            List<IReadOnlyDictionary<string, object>> documents =
                new List<IReadOnlyDictionary<string, object>>();

            foreach (DocumentSnapshot doc in querySnapshot.Documents)
            {
                if (doc != null && doc.Exists)
                {
                    Dictionary<string, object> dict = doc.ToDictionary();
                    if (dict != null)
                    {
                        documents.Add(dict);
                    }
                }
            }

            return WordSourceResult.From(contentVersion, documents);
        }
        catch (Exception e)
        {
            Debug.LogWarning(LogPrefix + " fetch failed (" + e.Message + "); falling back.");
            return WordSourceResult.Failed();
        }
    }

    private static async Task<int> FetchContentVersionAsync(FirebaseFirestore firestore)
    {
        try
        {
            DocumentReference metaRef = firestore
                .Collection(CollectionContentMeta)
                .Document(DocContentMetaSingleton);

            DocumentSnapshot metaSnapshot = await metaRef.GetSnapshotAsync();
            if (metaSnapshot != null && metaSnapshot.Exists)
            {
                Dictionary<string, object> data = metaSnapshot.ToDictionary();
                if (data != null && data.TryGetValue("contentVersion", out object verObj) && verObj != null)
                {
                    return Convert.ToInt32(verObj);
                }
            }
        }
        catch (Exception e)
        {
            // Missing contentMeta doc is non-fatal: treat version as 0.
            Debug.LogWarning(LogPrefix + " could not read contentVersion (" + e.Message + "); defaulting to 0.");
        }

        return 0;
    }
}
