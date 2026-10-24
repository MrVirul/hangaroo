/// <summary>
/// The default sink: drops every event. The game ships playable with no
/// Firebase installed and no bootstrap completed (issue #40's consent gate
/// and issue #30's "not ready" both end up here) - telemetry is strictly
/// best-effort and can never become a gameplay dependency.
/// </summary>
public sealed class NullAnalyticsService : IAnalyticsService
{
    public static readonly NullAnalyticsService Instance = new NullAnalyticsService();

    private NullAnalyticsService()
    {
    }

    /// <summary>Intentionally empty: dropping the event is the whole job.</summary>
    public void LogEvent(string eventName, params AnalyticsParam[] parameters)
    {
    }
}
