using System;
using System.Collections.Generic;

/// <summary>
/// Maps a Firestore word document to a validated <see cref="WordData"/>
/// (Story 2.4, issue #33).
///
/// The mapper deliberately takes a plain field dictionary instead of a Firebase
/// <c>DocumentSnapshot</c>: <c>DocumentSnapshot.Data</c>/<c>ToDictionary()</c>
/// hands over exactly this shape, so the Firebase type stays at the call site
/// (Story 2.3's repository, once the SDK lands in issue #30) and this mapper - and
/// the model it produces - compiles and unit-tests without the SDK. That also keeps
/// <see cref="WordData"/> free of any Firebase type, as issue #33 requires.
///
/// Field names follow <c>docs/FIRESTORE_SCHEMA.md</c> (<c>categoryId</c>,
/// <c>difficultyId</c>) where the bundled <c>words.json</c> uses <c>category</c> /
/// <c>difficulty</c>; both shapes funnel through the same
/// <see cref="WordData.Normalize"/> and <see cref="WordValidator"/> steps, so
/// identical content produces identical model output on either path.
/// </summary>
public static class FirestoreWordMapper
{
    /// <summary>
    /// Maps one document's fields to a validated <see cref="WordData"/>. Returns
    /// false with <paramref name="reason"/> set when the document breaks the shared
    /// content rules (empty fields, answers outside <c>A</c>-<c>Z</c>, an unknown
    /// category or difficulty, a band mismatch, a copy-hygiene giveaway) or when a
    /// present field has an impossible type.
    ///
    /// Absent optional fields take the schema defaults - weight 1, isActive true -
    /// the same defaults a bundled entry without those keys receives.
    /// The length band defaults to the shared union (4-12); callers that validate
    /// against a single difficulty pass their own bounds.
    /// </summary>
    public static bool TryMap(IReadOnlyDictionary<string, object> fields, out WordData word, out string reason,
        int minLength = WordValidator.MinAnswerLength, int maxLength = WordValidator.MaxAnswerLength)
    {
        word = null;
        reason = string.Empty;

        if (fields == null)
        {
            reason = "document has no fields";
            return false;
        }

        int weight;
        if (!TryWeight(fields, out weight, out reason))
            return false;

        bool isActive;
        if (!TryIsActive(fields, out isActive, out reason))
            return false;

        var raw = new WordData(
            StringField(fields, "clue"),
            StringField(fields, "answer"),
            StringField(fields, "hint"),
            StringField(fields, "categoryId"),
            StringField(fields, "difficultyId"),
            weight,
            isActive);

        WordData normalized = WordData.Normalize(raw);
        WordValidationResult check = WordValidator.Validate(normalized, minLength, maxLength);

        if (!check.IsValid)
        {
            reason = check.Reason;
            return false;
        }

        word = normalized;
        return true;
    }

    /// <summary>Present string fields as-is; absent or null fields read as empty (validated later).</summary>
    private static string StringField(IReadOnlyDictionary<string, object> fields, string key)
    {
        object value;
        return fields.TryGetValue(key, out value) && value != null ? value.ToString() : string.Empty;
    }

    /// <summary>
    /// The weight field: schema default 1 when absent. Firestore integers arrive
    /// boxed as long, so conversion is required; anything non-numeric is a schema
    /// violation and is reported rather than silently defaulted.
    /// </summary>
    private static bool TryWeight(IReadOnlyDictionary<string, object> fields, out int weight, out string reason)
    {
        weight = 1;
        reason = string.Empty;

        object value;
        if (!fields.TryGetValue("weight", out value) || value == null)
            return true;

        if (value is int || value is long || value is short || value is byte || value is float || value is double)
        {
            weight = Convert.ToInt32(value);
            return true;
        }

        reason = "weight is not an integer";
        return false;
    }

    /// <summary>The isActive flag: schema default true when absent.</summary>
    private static bool TryIsActive(IReadOnlyDictionary<string, object> fields, out bool isActive, out string reason)
    {
        isActive = true;
        reason = string.Empty;

        object value;
        if (!fields.TryGetValue("isActive", out value) || value == null)
            return true;

        if (value is bool)
        {
            isActive = (bool)value;
            return true;
        }

        reason = "isActive is not a boolean";
        return false;
    }
}
