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
    [SerializeField, Tooltip("Vertical offset of keyboard from the bottom edge")]
    private float keyboardBottomOffset = 30f;
    [SerializeField, Tooltip("Ground kangaroo to the bottom floor. Keep false so Kangaroo stays anchored beside letter slots")]
    private bool anchorKangarooToGround = false;
    [SerializeField] private AdaptiveCanvasScaler adaptiveScaler;

    [Header("Keyboard Responsiveness")]
    [SerializeField] private bool autoScaleKeyboard = true;
    [SerializeField, Range(0.5f, 1f), Tooltip("Fraction of safe area width to fill on portrait screens")]
    private float portraitWidthFraction = 0.94f;
    [SerializeField, Range(0.3f, 1f), Tooltip("Fraction of safe area width to fill on landscape screens")]
    private float landscapeWidthFraction = 0.65f;
    [SerializeField, Tooltip("Maximum width of keyboard on landscape in canvas units (0 for unconstrained)")]
    private float maxLandscapeWidth = 1250f;
    [SerializeField, Range(0.1f, 0.5f), Tooltip("Maximum fraction of safe area height the keyboard may occupy on portrait")]
    private float maxKeyboardHeightFractionPortrait = 0.22f;
    [SerializeField, Range(0.1f, 0.5f), Tooltip("Maximum fraction of safe area height the keyboard may occupy on landscape")]
    private float maxKeyboardHeightFractionLandscape = 0.28f;
    [SerializeField] private float minKeyboardScale = 0.5f;
    [SerializeField] private float maxKeyboardScale = 4.5f;

    private RectTransform reference;
    private Vector2 lastScreenSize;
    private Rect lastSafeArea;
    private Vector2 lastReferenceSize;

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
        Rect safeArea = SafeAreaUtil.GetNormalizedSafeArea();
        Vector2 refSize = reference != null ? reference.rect.size : Vector2.zero;
        if (size != lastScreenSize || safeArea != lastSafeArea || refSize != lastReferenceSize)
        {
            lastScreenSize = size;
            lastSafeArea = safeArea;
            lastReferenceSize = refSize;
            Apply();
        }
    }

    private void OnRectTransformDimensionsChange()
    {
        Apply();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying && isActiveAndEnabled)
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null) Apply();
            };
        }
    }
#endif

    [ContextMenu("Apply Layout")]
    public void Apply()
    {
        if (reference == null)
        {
            reference = transform as RectTransform;
            if (reference == null)
                return;
        }

        lastScreenSize = new Vector2(Screen.width, Screen.height);
        lastSafeArea = SafeAreaUtil.GetNormalizedSafeArea();
        lastReferenceSize = reference.rect.size;

        Vector2 minFraction = SafeAreaUtil.GetNormalizedSafeAreaMinFraction();
        Vector2 maxFraction = SafeAreaUtil.GetNormalizedSafeAreaMaxFraction();

        float safeAreaW = (maxFraction.x - minFraction.x) * reference.rect.width;
        float safeAreaH = (maxFraction.y - minFraction.y) * reference.rect.height;

        bool isPortrait = reference.rect.height > reference.rect.width;

        float overflowY = adaptiveScaler != null
            ? adaptiveScaler.Overflow.y
            : 0f;
        float effectiveBottomOffset = keyboardBottomOffset > 0f
            ? keyboardBottomOffset
            : (keyboardTransform != null ? keyboardTransform.anchoredPosition.y : 207f);
        float bottomOffset = isPortrait
            ? effectiveBottomOffset
            : Mathf.Min(effectiveBottomOffset, safeAreaH * 0.12f);
        float bottomY = minFraction.y * reference.rect.height + overflowY + bottomOffset;

        float renderedKbW = 0f;
        float renderedKbH = 0f;

        if (keyboardTransform != null)
        {
            float baseW = Mathf.Max(keyboardTransform.rect.width, 1f);
            float baseH = Mathf.Max(keyboardTransform.rect.height, 1f);

            if (autoScaleKeyboard)
            {
                float widthFraction = isPortrait ? portraitWidthFraction : landscapeWidthFraction;
                float targetW = safeAreaW * widthFraction;
                if (!isPortrait && maxLandscapeWidth > 0f)
                {
                    targetW = Mathf.Min(targetW, maxLandscapeWidth);
                }

                float heightFraction = isPortrait ? maxKeyboardHeightFractionPortrait : maxKeyboardHeightFractionLandscape;
                float maxH = safeAreaH * heightFraction;

                float scaleW = targetW / baseW;
                float scaleH = maxH / baseH;
                float scale = Mathf.Min(scaleW, scaleH);
                scale = Mathf.Clamp(scale, minKeyboardScale, maxKeyboardScale);

                keyboardTransform.localScale = Vector3.one * scale;
            }

            renderedKbW = baseW * keyboardTransform.localScale.x;
            renderedKbH = baseH * keyboardTransform.localScale.y;

            // Center keyboard horizontally within the safe area
            float safeCenterFractionX = (minFraction.x + maxFraction.x) * 0.5f;
            float safeCenterX = (safeCenterFractionX - 0.5f) * reference.rect.width;

            keyboardTransform.anchorMin = new Vector2(0.5f, 0f);
            keyboardTransform.anchorMax = new Vector2(0.5f, 0f);
            keyboardTransform.pivot = new Vector2(0.5f, 0f);
            keyboardTransform.anchoredPosition = new Vector2(safeCenterX, bottomY);
        }

        if (kangarooTransform != null && anchorKangarooToGround)
        {
            kangarooTransform.anchorMin = Vector2.zero;
            kangarooTransform.anchorMax = Vector2.zero;
            kangarooTransform.pivot = Vector2.zero;

            float kangarooW = kangarooTransform.rect.width * kangarooTransform.localScale.x;
            float leftSpace = (safeAreaW - renderedKbW) * 0.5f;

            if (leftSpace >= kangarooW + 16f)
            {
                // Plenty of room beside keyboard: ground in bottom-left safe corner
                kangarooTransform.anchoredPosition = new Vector2(
                    minFraction.x * reference.rect.width + 8f,
                    bottomY);
            }
            else
            {
                // Narrow/portrait: lift Kangaroo above keyboard to prevent overlap
                kangarooTransform.anchoredPosition = new Vector2(
                    minFraction.x * reference.rect.width + 16f,
                    bottomY + renderedKbH + 8f);
            }
        }
    }
}