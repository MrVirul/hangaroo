using UnityEngine;

/// <summary>
/// Keeps an edge-anchored UI element inside the screen safe area (notches,
/// rounded corners, gesture bars) without moving it out of the place you
/// anchored it to.
///
/// Attach to any RectTransform whose anchors are pinned to a screen edge/corner
/// (e.g. Pause Button top-right, Hearts top-centre, Wheel). It reads the
/// element's anchor position to decide which insets to apply and just offsets
/// the authored anchoredPosition by the safe-area inset in that direction,
/// re-run only on screen/safe-area changes.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeAreaAnchoredUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Optional. Defaults to the element's parent; should stretch over the whole canvas.")]
    [SerializeField] private RectTransform referenceCanvas;

    [Header("Layout")]
    [Tooltip("Extra distance kept from the safe-area edge (in canvas units).")]
    [SerializeField] private float insetPadding = 8f;

    [Header("Adaptive")]
    [Tooltip("Optional. When set, also pulls the element inward by the canvas overlap exposed by AdaptiveCanvasScaler, so edge UI stays on screen on aspect ratios where the canvas is larger than the device.")]
    [SerializeField] private AdaptiveCanvasScaler adaptiveScaler;

    private RectTransform rect;
    private RectTransform reference;
    private Vector2 basePosition;
    private Vector2 lastScreenSize;
    private Rect lastSafeArea;

    private void Awake()
    {
        rect = transform as RectTransform;
        reference = referenceCanvas != null ? referenceCanvas : (rect.parent as RectTransform);
        basePosition = rect.anchoredPosition;
    }

    private void OnEnable()
    {
        if (rect != null)
            Apply();
    }

    private void Start()
    {
        Apply();
    }

    private void Update()
    {
        Vector2 size = new Vector2(Screen.width, Screen.height);
        Rect safeArea = SafeAreaUtil.GetNormalizedSafeArea();
        if (size != lastScreenSize || safeArea != lastSafeArea)
        {
            lastScreenSize = size;
            lastSafeArea = safeArea;
            Apply();
        }
    }

    /// <summary>
    /// Offsets the anchored position by the safe-area insets appropriate for the
    /// element's anchors. Elements anchored to the middle of an edge are not
    /// shifted along that axis.
    /// </summary>
    public void Apply()
    {
        if (rect == null || reference == null)
            return;

        lastScreenSize = new Vector2(Screen.width, Screen.height);
        lastSafeArea = SafeAreaUtil.GetNormalizedSafeArea();

        // Distance (in reference-local units) between the reference edges and
        // the safe-area edges. The safe area is clamped to the screen first so
        // these insets can never exceed the reference rect.
        float leftInset = SafeAreaMin().x - reference.rect.xMin + insetPadding;
        float rightInset = reference.rect.xMax - SafeAreaMax().x + insetPadding;
        float bottomInset = SafeAreaMin().y - reference.rect.yMin + insetPadding;
        float topInset = reference.rect.yMax - SafeAreaMax().y + insetPadding;

        // 0 = left/bottom, 0.5 = centre, 1 = right/top along that axis.
        float horizontalAnchor = (rect.anchorMin.x + rect.anchorMax.x) * 0.5f;
        float verticalAnchor = (rect.anchorMin.y + rect.anchorMax.y) * 0.5f;

        float dx = Mathf.Lerp(leftInset, -rightInset, horizontalAnchor);
        float dy = Mathf.Lerp(bottomInset, -topInset, verticalAnchor);

        if (adaptiveScaler != null)
        {
            Vector2 overflow = adaptiveScaler.Overflow;
            dx += overflow.x * (1f - 2f * horizontalAnchor);
            dy += overflow.y * (1f - 2f * verticalAnchor);
        }

        rect.anchoredPosition = basePosition + new Vector2(dx, dy);
    }

    private Vector2 SafeAreaMin()
    {
        Vector2 fraction = SafeAreaUtil.GetNormalizedSafeAreaMinFraction();
        return new Vector2(
            (fraction.x - 0.5f) * reference.rect.width,
            (fraction.y - 0.5f) * reference.rect.height);
    }

    private Vector2 SafeAreaMax()
    {
        Vector2 fraction = SafeAreaUtil.GetNormalizedSafeAreaMaxFraction();
        return new Vector2(
            (fraction.x - 0.5f) * reference.rect.width,
            (fraction.y - 0.5f) * reference.rect.height);
    }
}