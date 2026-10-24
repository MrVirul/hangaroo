/// <summary>
/// The telemetry seam (issue #40): the game logs events through this
/// interface and never sees a Firebase type, so gameplay code compiles and
/// tests run with no SDK installed - the same split issue #33 set up for the
/// word read path.
///
/// Implementations must treat <see cref="LogEvent"/> as best-effort: a failed
/// or unavailable sink is a dropped event, never an exception into gameplay
/// (<see cref="GameAnalytics"/> also guards at the façade for belt and braces).
/// </summary>
public interface IAnalyticsService
{
    /// <summary>
    /// Delivers one event. <paramref name="eventName"/> and the parameter names
    /// come from <see cref="AnalyticsEvent"/>.
    /// </summary>
    void LogEvent(string eventName, params AnalyticsParam[] parameters);
}
