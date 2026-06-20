using UnityEngine;

/// <summary>
/// Shared Screen.safeArea handling for the responsive UI scripts.
///
/// Screen.safeArea is not guaranteed to be a subset of Screen.width/height.
/// Some devices report a stale rect across an orientation change, and the Editor
/// reports a rect from the player/console window that has nothing to do with the
/// current Game View (e.g. safeArea 1080x2164 while the Game View is 400x779).
///
/// Every script here converts the safe area into canvas units with a
/// safeArea-edge / screen-size ratio, so an oversized rect turns into an enormous
/// inset and throws edge-anchored UI far off the canvas. GetNormalizedSafeArea
/// clamps the rect to the screen first, which makes those ratios always land in
/// 0..1 and keeps the resulting insets inside a sane range.
/// </summary>
public static class SafeAreaUtil
{
    /// <summary>
    /// Screen.safeArea clamped to the current screen, so the result is always a
    /// non-negative rect fully contained in (0,0)-(Screen.width,Screen.height).
    /// A safe area that is absent or inconsistent reads as the full screen.
    /// </summary>
    public static Rect GetNormalizedSafeArea()
    {
        float screenWidth = Mathf.Max(Screen.width, 1);
        float screenHeight = Mathf.Max(Screen.height, 1);

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            return new Rect(0f, 0f, screenWidth, screenHeight);
        }
#endif

        Rect safe = Screen.safeArea;

        float xMin = Mathf.Clamp(safe.xMin, 0f, screenWidth);
        float yMin = Mathf.Clamp(safe.yMin, 0f, screenHeight);
        float xMax = Mathf.Clamp(safe.xMax, xMin, screenWidth);
        float yMax = Mathf.Clamp(safe.yMax, yMin, screenHeight);

        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    /// <summary>
    /// Safe-area edge as a 0..1 fraction of the screen, measured from the
    /// bottom-left. Clamped, so it is safe to multiply by a canvas size.
    /// </summary>
    public static Vector2 GetNormalizedSafeAreaMinFraction()
    {
        Rect safe = GetNormalizedSafeArea();
        return new Vector2(
            safe.xMin / Mathf.Max(Screen.width, 1),
            safe.yMin / Mathf.Max(Screen.height, 1));
    }

    /// <summary>
    /// Opposite safe-area edge as a 0..1 fraction of the screen.
    /// </summary>
    public static Vector2 GetNormalizedSafeAreaMaxFraction()
    {
        Rect safe = GetNormalizedSafeArea();
        return new Vector2(
            safe.xMax / Mathf.Max(Screen.width, 1),
            safe.yMax / Mathf.Max(Screen.height, 1));
    }
}