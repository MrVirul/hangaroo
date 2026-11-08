using System;
using Firebase.Analytics;
using UnityEngine;

/// <summary>
/// The Firebase implementation of the telemetry seam (issue #40). This and
/// <see cref="FirebaseBootstrap"/> are the only types in the game that touch
/// <c>Firebase.Analytics</c>.
///
/// Safety rules, because telemetry must never break gameplay:
///
///   * Every SDK call is wrapped - a throwing SDK is a dropped event plus one
///     warning, never an exception into a caller.
///   * Nothing is sent before <see cref="FirebaseBootstrap"/> reports ready.
///   * The consent gate (<see cref="SetCollectionEnabled"/>) is checked on
///     every event AND applied SDK-side, so an opt-out is honoured twice.
///
/// Event and parameter names arrive pre-shaped from the game façade
/// (<see cref="GameAnalytics"/>); this class only maps the typed value onto
/// the SDK's <c>Parameter</c>.
/// </summary>
public sealed class FirebaseAnalyticsService : IAnalyticsService
{
    public const string LogPrefix = "Analytics:";

    private bool collectionEnabled = true;

    /// <summary>
    /// Applies the analytics consent gate (issue #40) both locally (events
    /// are dropped before reaching the SDK) and SDK-side. Safe to call before
    /// the bootstrap finishes - the SDK-side half defers until ready.
    /// </summary>
    public void SetCollectionEnabled(bool enabled)
    {
        collectionEnabled = enabled;

        if (!FirebaseBootstrap.IsReady)
        {
            return;
        }

        try
        {
            FirebaseAnalytics.SetAnalyticsCollectionEnabled(enabled);
        }
        catch (Exception e)
        {
            Debug.LogWarning(LogPrefix + " SetAnalyticsCollectionEnabled failed: " + e.Message);
        }
    }

    /// <summary>
    /// Maps the typed parameter onto the Firebase SDK type and logs the event.
    /// Drops silently (one warning) when disabled, not-yet-ready, or the SDK
    /// throws.
    /// </summary>
    public void LogEvent(string eventName, params AnalyticsParam[] parameters)
    {
        if (!collectionEnabled)
        {
            return;
        }

        if (!FirebaseBootstrap.IsReady)
        {
            return;
        }

        try
        {
            if (parameters == null || parameters.Length == 0)
            {
                FirebaseAnalytics.LogEvent(eventName);
                return;
            }

            Parameter[] mapped = new Parameter[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                mapped[i] = ToParameter(parameters[i]);
            }

            FirebaseAnalytics.LogEvent(eventName, mapped);
        }
        catch (Exception e)
        {
            Debug.LogWarning(LogPrefix + " event '" + eventName + "' was dropped: " + e.Message);
        }
    }

    private static Parameter ToParameter(AnalyticsParam parameter)
    {
        switch (parameter.type)
        {
            case AnalyticsParamType.Number:
                return new Parameter(parameter.name, parameter.numberValue);
            case AnalyticsParamType.Real:
                return new Parameter(parameter.name, parameter.realValue);
            default:
                return new Parameter(parameter.name, parameter.textValue ?? string.Empty);
        }
    }
}
