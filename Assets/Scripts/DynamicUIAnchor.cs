using UnityEngine;

/// <summary>
/// Keeps the Kangaroo (bottom-left) and the Keyboard BG (bottom-centre)
/// grounded on the bottom edge of the visible screen, lifted above device
/// cutouts such as the iOS home indicator.
///
/// Attach to a full-screen RectTransform (in GamePlay that is the HUD). It
/// reads Screen.safeArea on start / resolution / orientation changes and
/// converts the screen-space safe area into canvas (design) units using the
/// reference rect, then:
///   - Kangaroo: anchors + pivot (0,0) at the bottom-left safe corner.
///   - Keyboard BG: anchors + pivot (0.5,0) at the bottom-centre safe corner.
///
/// If an AdaptiveCanvasScaler is assigned, any canvas overlap past the screen
/// bottom is also added so the elements never sink below the visible display
/// on letterboxed/tall aspect ratios.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class DynamicUIAnchor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform kangarooTransform;
    [SerializeField] private RectTransform keyboardTransform;

    [Header("Layout")]
    [SerializeField] private float groundMargin = 8f;
    [SerializeField] private AdaptiveCanvasScaler adaptiveScaler;

    private RectTransform reference;
    private Vector2 lastScreenSize;
    private Rect lastSafeArea;

    private void Awake()
    {
        reference = transform as RectTransform;
    }

    private void OnEnable()
    {
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

    public void Apply()
    {
        if (reference == null)
        {
            reference = transform as RectTransform;
            return;
        }

        lastScreenSize = new Vector2(Screen.width, Screen.height);
        lastSafeArea = Screen.safeArea;

        Rect safe = Screen.safeArea;
        float x = safe.xMin / Screen.width;
        float y = safe.yMin / Screen.height;

        Vector2 groundOrigin = new Vector2(
            x * reference.rect.width,
            y * reference.rect.height);

        float overflowY = adaptiveScaler != null
            ? adaptiveScaler.Overflow.y
            : 0f;
        float bottomY = groundOrigin.y + overflowY + groundMargin;

        if (kangarooTransform != null)
        {
            kangarooTransform.anchorMin = Vector2.zero;
            kangarooTransform.anchorMax = Vector2.zero;
            kangarooTransform.pivot = Vector2.zero;
            kangarooTransform.anchoredPosition = new Vector2(
                groundOrigin.x + groundMargin,
                bottomY);
        }

        if (keyboardTransform != null)
        {
            keyboardTransform.anchorMin = new Vector2(0.5f, 0f);
            keyboardTransform.anchorMax = new Vector2(0.5f, 0f);
            keyboardTransform.pivot = new Vector2(0.5f, 0f);
            keyboardTransform.anchoredPosition = new Vector2(0f, bottomY);
        }
    }
}