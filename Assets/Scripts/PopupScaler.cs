using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Fits a UI popup card uniformly inside the screen safe area, preserving the
/// card's aspect ratio. Attach to the card (e.g. "Sqaure" inside
/// Settings_Panel). The card's parent must stretch over the whole canvas.
///
/// The card's authored size (read in Awake from the RectTransform) is the
/// design size. On each layout change the script computes:
///   scale = min(availableW / designW, availableH / designH)
/// and applies it uniformly, also re-centring the card inside the safe area.
/// Nothing is recomputed per frame - only when the screen resolution or safe
/// area actually changes.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class PopupScaler : MonoBehaviour
{
    [Header("Sizing")]
    [SerializeField, Tooltip("Margin (in parent/canvas units) left around the card.")]
    private float padding = 24f;

    [SerializeField, Tooltip("Lower bound for the uniform scale (very small screens).")]
    private float minScale = 0.6f;

    [SerializeField, Tooltip("Upper bound for the uniform scale (very large screens).")]
    private float maxScale = 1.2f;

    [Header("Events")]
    [Tooltip("Raised after the card is resized/re-centred. Use for one-off relayouts.")]
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
            Apply();
    }

    private void Start()
    {
        Apply();
    }

    private void Update()
    {
        Vector2 size = new Vector2(Screen.width, Screen.height);
        if (size != lastScreenSize || Screen.safeArea != lastSafeArea)
        {
            lastScreenSize = size;
            lastSafeArea = Screen.safeArea;
            Apply();
        }
    }

    /// <summary>
    /// Recomputes the uniform scale and centring for the current screen/safe
    /// area. Cheap and event-driven; call manually only when something moved.
    /// </summary>
    public void Apply()
    {
        if (rect == null || reference == null)
            return;

        lastScreenSize = new Vector2(Screen.width, Screen.height);
        lastSafeArea = Screen.safeArea;

        Vector2 safeMin = SafeAreaMin();
        Vector2 safeMax = SafeAreaMax();

        float availableW = (safeMax.x - safeMin.x) - padding * 2f;
        float availableH = (safeMax.y - safeMin.y) - padding * 2f;

        float scaleW = availableW / Mathf.Max(designSize.x, 1f);
        float scaleH = availableH / Mathf.Max(designSize.y, 1f);
        float scale = Mathf.Min(scaleW, scaleH); // smallest = fit entirely
        scale = Mathf.Clamp(scale, minScale, maxScale);

        rect.localScale = Vector3.one * scale;
        rect.anchoredPosition = new Vector2(
            (safeMin.x + safeMax.x) * 0.5f,
            (safeMin.y + safeMax.y) * 0.5f);

        onLayoutChanged.Invoke();
    }

    /// <summary>
    /// Bottom-left corner of the safe area in the parent's local space. The
    /// parent is expected to stretch across the full canvas (anchor 0..1).
    /// </summary>
    private Vector2 SafeAreaMin()
    {
        Rect safe = Screen.safeArea;
        return new Vector2(
            (safe.xMin / Screen.width - 0.5f) * reference.rect.width,
            (safe.yMin / Screen.height - 0.5f) * reference.rect.height);
    }

    /// <summary>
    /// Top-right corner of the safe area in the parent's local space.
    /// </summary>
    private Vector2 SafeAreaMax()
    {
        Rect safe = Screen.safeArea;
        return new Vector2(
            (safe.xMax / Screen.width - 0.5f) * reference.rect.width,
            (safe.yMax / Screen.height - 0.5f) * reference.rect.height);
    }
}