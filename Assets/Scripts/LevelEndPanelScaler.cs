using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Responsive positioning and scaling for a result panel (Victory / Game Over).
///
/// Pins the board ("Result") to the bottom-centre of the SAFE area and applies
/// ONE uniform scale so the board always fits the visible screen on any aspect
/// ratio (small phones and wide tablets alike). Everything inside the board
/// scales together, so no other object needs per-frame work.
///
/// Any LayoutGroup that fights manual arranging when enabled can be handed in;
/// it gets disabled at runtime so this script fully drives the layout (undo it
/// in the Inspector if you prefer to keep the group).
///
/// Attach to the panel GameObject - its RectTransform must stretch the canvas.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class LevelEndPanelScaler : MonoBehaviour
{
    [Header("Board")]
    [SerializeField, Tooltip("The board/Result RectTransform to reposition and scale.")]
    private RectTransform board;

    [SerializeField, Range(0.05f, 1.5f),
     Tooltip("Fraction of the safe width the board may occupy (0..1).")]
    private float maxWidthFraction = 0.95f;

    [SerializeField, Range(0.05f, 1.5f),
     Tooltip("Fraction of the safe height the board may occupy (0..1).")]
    private float maxHeightFraction = 0.9f;

    [SerializeField, Tooltip("Gap kept above the safe-area bottom edge (canvas units).")]
    private float groundMargin = 26f;

    [SerializeField, Range(0.2f, 3f)] private float minScale = 0.45f;
    [SerializeField, Range(0.2f, 3f)] private float maxScale = 1.5f;

    [Header("Layout Group")]
    [SerializeField, Tooltip("Optional LayoutGroup on the board, disabled at runtime so C# drives the layout.")]
    private LayoutGroup layoutGroup;

    [SerializeField, Tooltip("Disable the LayoutGroup above at runtime (recommended).")]
    private bool disableLayoutGroup = true;

    [Header("Events")]
    [Tooltip("Raised after the board was repositioned/scaled.")]
    public UnityEvent onLayoutChanged = new UnityEvent();

    private RectTransform panelRect;
    private Vector2 designSize;
    private Vector2 lastScreenSize;
    private Rect lastSafeArea;

    private void Awake()
    {
        panelRect = transform as RectTransform;
        designSize = board != null ? board.rect.size : Vector2.zero;

        if (disableLayoutGroup && layoutGroup != null)
        {
            layoutGroup.enabled = false;
        }
    }

    private void OnEnable()
    {
        if (panelRect != null)
        {
            ApplyLayout();
        }
    }

    private void Start()
    {
        ApplyLayout();
    }

    private void Update()
    {
        Vector2 size = new Vector2(Screen.width, Screen.height);
        if (size != lastScreenSize || Screen.safeArea != lastSafeArea)
        {
            lastScreenSize = size;
            lastSafeArea = Screen.safeArea;
            ApplyLayout();
        }
    }

    /// <summary>
    /// Re-pins the board to the safe-area bottom-centre and re-scales it to fit
    /// the visible screen. Cheap and only runs on layout-affecting changes or
    /// when the panel is shown.
    /// </summary>
    public void ApplyLayout()
    {
        if (panelRect == null || board == null)
        {
            return;
        }

        lastScreenSize = new Vector2(Screen.width, Screen.height);
        lastSafeArea = Screen.safeArea;

        Vector2 safeMin = new Vector2(
            (Screen.safeArea.xMin / Screen.width - 0.5f) * panelRect.rect.width,
            (Screen.safeArea.yMin / Screen.height - 0.5f) * panelRect.rect.height);

        Vector2 safeMax = new Vector2(
            (Screen.safeArea.xMax / Screen.width - 0.5f) * panelRect.rect.width,
            (Screen.safeArea.yMax / Screen.height - 0.5f) * panelRect.rect.height);

        if (designSize.x <= 0f || designSize.y <= 0f)
        {
            designSize = board.rect.size;
        }

        float safeW = safeMax.x - safeMin.x;
        float safeH = safeMax.y - safeMin.y;

        float scaleW = safeW * maxWidthFraction / Mathf.Max(designSize.x, 1f);
        float scaleH = safeH * maxHeightFraction / Mathf.Max(designSize.y, 1f);
        float scale = Mathf.Clamp(Mathf.Min(scaleW, scaleH), minScale, maxScale);

        board.anchorMin = new Vector2(0.5f, 0f);
        board.anchorMax = new Vector2(0.5f, 0f);
        board.pivot = new Vector2(0.5f, 0f);
        board.localScale = Vector3.one * scale;
        board.anchoredPosition = new Vector2((safeMin.x + safeMax.x) * 0.5f, safeMin.y + groundMargin);

        onLayoutChanged.Invoke();
    }
}