/// <summary>
/// Outcome of validating a single word entry. Carries a human-readable reason
/// when invalid so loaders and editor tooling can report exactly what is wrong
/// rather than a generic "bad word".
/// </summary>
public sealed class WordValidationResult
{
    public bool IsValid { get; }

    /// <summary>Empty when <see cref="IsValid"/> is true.</summary>
    public string Reason { get; }

    private WordValidationResult(bool isValid, string reason)
    {
        IsValid = isValid;
        Reason = reason;
    }

    public static WordValidationResult Valid()
    {
        return new WordValidationResult(true, string.Empty);
    }

    public static WordValidationResult Invalid(string reason)
    {
        return new WordValidationResult(false, reason);
    }

    public override string ToString()
    {
        return IsValid ? "valid" : Reason;
    }
}
