using System.Threading.Tasks;
using Firebase;
using UnityEngine;

/// <summary>
/// The single Firebase entry point for the game (issue #30, extended by #40).
///
/// Initialisation runs once per process, as early as Unity allows
/// (<c>BeforeSceneLoad</c>), off the main load path but resuming on the Unity
/// main thread (Unity's SynchronizationContext), so installing the analytics
/// sink is safe here. The contract that makes this usable from anywhere:
///
///   * <see cref="WhenReady"/> ALWAYS completes - after success, after a
///     dependency failure, after an exception. Callers gate, never hang.
///   * When dependencies are unavailable Firebase features stay off for the
///     whole session: the analytics sink stays <c>NullAnalyticsService</c>,
///     and the word repository receives a null Firestore source.
///   * Never throws into the player. Failures log and degrade.
///
/// This assembly is the ONLY place Firebase types appear - the game assembly
/// talks to <c>IAnalyticsService</c> and awaits <see cref="WhenReady"/>.
/// </summary>
public static class FirebaseBootstrap
{
    public const string LogPrefix = "FirebaseBootstrap:";

    /// <summary>
    /// PlayerPrefs key for the analytics opt-out gate (issue #40). Reads as
    /// enabled when unset (1 = enabled); a future settings toggle writes it
    /// and calls <c>FirebaseAnalyticsService.SetCollectionEnabled</c>.
    /// </summary>
    public const string AnalyticsEnabledKey = "Hangaroo.Analytics.Enabled";

    private static readonly TaskCompletionSource<bool> readyCompletion =
        new TaskCompletionSource<bool>();

    private static bool initializeStarted;

    /// <summary>True once dependencies resolved <c>Available</c> and the sink was installed.</summary>
    public static bool IsReady { get; private set; }

    /// <summary>Completes when the one-time init attempt has finished, whatever the outcome. Never null, never pending forever.</summary>
    public static Task WhenReady => readyCompletion.Task;

    /// <summary>
    /// Kicks off the one-time dependency check and sink install. The
    /// TaskCompletionSource is completed in all paths, so awaiting
    /// <see cref="WhenReady"/> cannot hang a caller.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (initializeStarted)
        {
            return;
        }

        initializeStarted = true;
        _ = InitializeAsync();
    }

    private static async Task InitializeAsync()
    {
        try
        {
            DependencyStatus status = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (status != DependencyStatus.Available)
            {
                Debug.LogWarning(
                    LogPrefix + " dependencies unavailable (" + status +
                    "); Firebase features stay off this session.");
                return;
            }

            // Dev builds keep Warning noise out of release: verbose logs in
            // the editor and dev player, errors only in release builds.
            // (FirebaseApp.LogLevel is static and applies to every app in the
            // process.)
            FirebaseApp.LogLevel = Debug.isDebugBuild ? LogLevel.Warning : LogLevel.Error;

            IsReady = true;

            // Install the sink BEFORE applying the consent gate: the gate
            // calls into the SDK and needs IsReady true to pass the service's
            // own readiness guard. A null/absent key reads as enabled (the
            // documented default).
            FirebaseAnalyticsService firebaseAnalytics = new FirebaseAnalyticsService();
            GameAnalytics.Service = firebaseAnalytics;
            firebaseAnalytics.SetCollectionEnabled(PlayerPrefs.GetInt(AnalyticsEnabledKey, 1) == 1);

            Debug.Log(LogPrefix + " ready; analytics sink installed.");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning(
                LogPrefix + " initialization failed (" + e.Message +
                "); continuing without Firebase.");
        }
        finally
        {
            // Always complete: the game gates on this and must never hang.
            readyCompletion.TrySetResult(true);
        }
    }
}
