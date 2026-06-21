using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// The single "scaling manager" for a UI card that holds a centred gameplay
/// cluster (e.g. Kangaroo, Keyboard BG, LetterSlot BG, clue).
///
/// Attach to the card RectTransform (the 800x600 authored container named
/// "Content" in GamePlay). Its parent must be a RectTransform that stretches
/// over the whole canvas (normally the Canvas itself).
///
/// Behaviour:
///  - Captures the authored card size on Awake.
///  - On start / screen-resolution change / orientation change / safe-area
///    change it applies ONE uniform scale so the card fits inside the safe
///    area, and re-centres it. Nothing else in the card needs per-frame work:
///    every child (edge-pinned or centred) scales together and Layout Groups
///    keep text/keys from colliding.
///  - Fires onLayoutChanged so other objects (e.g. GameManager refitting
///    letter slots) can re-run their one-off layout logic after a resize.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class ResponsiveGameplayScaler : MonoBehaviour
{
    /// <summary>
    /// FitInsideScreen keeps the whole card visible (contain).
    /// FillScreenCrop scales up so the card covers the screen (cover) - content
    /// near the card edges may be clipped, use only if edges are decorative.
    /// </summary>
    public enum FitMode { FitInsideScreen, FillScreenCrop }

    [Header("Sizing")]
    [SerializeField, Tooltip("FitInsideScreen = letterbox to keep everything visible; FillScreenCrop = cover the screen.")]
    private FitMode fitMode = FitMode.FitInsideScreen;

    [SerializeField, Tooltip("Margin (in canvas units) kept around the card on the reference resolution.")]
    private float padding = 12f;

    [SerializeField, Tooltip("Lower bound for the card scale (small screens).")]
    private float minScale = 0.4f;

    [SerializeField, Tooltip("Upper bound for the card scale (huge screens).")]
    private float maxScale = 4f;

    [Header("Events")]
    [Tooltip("Raised after the card is resized/repositioned. Subscribe things that must recompute once (e.g. FitSlotsToWidth).")]
    public UnityEvent onLayoutChanged = new UnityEvent();

    private RectTransform rect;
    private RectTransform reference;
    private Vector2 designSize;
    private Vector2 lastScreenSize;
    private Rect lastSafeArea;

    private void Awake()
    {
        rect = transform as RectTransform;
        reference = rect.parent as RectTransform;
        designSize = rect.rect.size;
    }

    private void OnEnable()
    {
        if (rect != null)
            ApplyLayout();
    }

    private void Start()
    {
        ApplyLayout();
    }

    private void Update()
    {
        Vector2 size = new Vector2(Screen.width, Screen.height);
        Rect safeArea = SafeAreaUtil.GetNormalizedSafeArea();
        if (size != lastScreenSize || safeArea != lastSafeArea)
        {
            lastScreenSize = size;
            lastSafeArea = safeArea;
            ApplyLayout();
        }
    }

    /// <summary>
    /// Recomputes the uniform card scale and centring for the current screen.
    /// Cheap (a few multiplications) and only runs on layout-affecting changes
    /// - never every frame. Exposed so other scripts can force a refresh.
    /// </summary>
    public void ApplyLayout()
    {
        if (rect == null || reference == null)
            return;

        lastScreenSize = new Vector2(Screen.width, Screen.height);
        lastSafeArea = SafeAreaUtil.GetNormalizedSafeArea();

        Vector2 safeMin = SafeAreaMin();
        Vector2 safeMax = SafeAreaMax();

        float availableW = (safeMax.x - safeMin.x) - padding * 2f;
        float availableH = (safeMax.y - safeMin.y) - padding * 2f;

        float scaleW = availableW / Mathf.Max(designSize.x, 1f);
        float scaleH = availableH / Mathf.Max(designSize.y, 1f);
        float scale = fitMode == FitMode.FillScreenCrop
            ? Mathf.Max(scaleW, scaleH)
            : Mathf.Min(scaleW, scaleH);
        scale = Mathf.Clamp(scale, minScale, maxScale);

        rect.localScale = Vector3.one * scale;
        rect.anchoredPosition = new Vector2((safeMin.x + safeMax.x) * 0.5f, (safeMin.y + safeMax.y) * 0.5f);

        onLayoutChanged.Invoke();
    }

    /// <summary>
    /// Bottom-left corner of the safe area in the reference panel's local space
    /// (reference is assumed to stretch across the full canvas).
    /// </summary>
    private Vector2 SafeAreaMin()
    {
        Vector2 fraction = SafeAreaUtil.GetNormalizedSafeAreaMinFraction();
        return new Vector2(
            (fraction.x - 0.5f) * reference.rect.width,
            (fraction.y - 0.5f) * reference.rect.height);
    }

    /// <summary>
    /// Top-right corner of the safe area in the reference panel's local space.
    /// </summary>
    private Vector2 SafeAreaMax()
    {
        Vector2 fraction = SafeAreaUtil.GetNormalizedSafeAreaMaxFraction();
        return new Vector2(
            (fraction.x - 0.5f) * reference.rect.width,
            (fraction.y - 0.5f) * reference.rect.height);
    }
}