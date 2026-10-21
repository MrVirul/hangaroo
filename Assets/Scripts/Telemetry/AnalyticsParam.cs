/// <summary>How an <see cref="AnalyticsParam"/>'s value is carried to the sink.</summary>
public enum AnalyticsParamType
{
    /// <summary>A string value (identifiers: difficulty keys, outcomes, reasons).</summary>
    Text,

    /// <summary>An integer value (counts, versions, scores).</summary>
    Number,

    /// <summary>A floating-point value (multipliers, durations in seconds).</summary>
    Real,
}

/// <summary>
/// One named analytics parameter with a typed value - the wire shape every
/// <see cref="IAnalyticsService"/> receives. Typed values keep numeric fields
/// numeric all the way to Firebase Analytics, so a dashboard can average
/// <c>livesLeft</c> instead of parsing strings.
/// </summary>
public readonly struct AnalyticsParam
{
    public readonly string name;
    public readonly AnalyticsParamType type;
    public readonly string textValue;
    public readonly long numberValue;
    public readonly double realValue;

    private AnalyticsParam(string name, AnalyticsParamType type, string textValue, long numberValue, double realValue)
    {
        this.name = name;
        this.type = type;
        this.textValue = textValue;
        this.numberValue = numberValue;
        this.realValue = realValue;
    }

    /// <summary>A string parameter; null reads as empty so a sink never sees null.</summary>
    public static AnalyticsParam Text(string name, string value)
    {
        return new AnalyticsParam(name, AnalyticsParamType.Text, value ?? string.Empty, 0, 0);
    }

    /// <summary>An integer parameter (bools arrive as 1/0 - Firebase has no bool type).</summary>
    public static AnalyticsParam Number(string name, long value)
    {
        return new AnalyticsParam(name, AnalyticsParamType.Number, null, value, 0);
    }

    /// <summary>A floating-point parameter.</summary>
    public static AnalyticsParam Real(string name, double value)
    {
        return new AnalyticsParam(name, AnalyticsParamType.Real, null, 0, value);
    }
}
