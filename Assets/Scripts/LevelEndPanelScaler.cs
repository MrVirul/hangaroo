using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Responsive positioning and scaling for a result panel (Victory / Game Over).
///
/// Centers the content group ("Result" / board) in the middle of the SAFE area
/// and applies ONE uniform scale so the group always fits the visible screen on any
/// aspect ratio (phones and tablets alike). Everything inside the group scales together,
/// preserving internal object placement.
///
/// Attach to the panel GameObject - its RectTransform must stretch the canvas.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class LevelEndPanelScaler : MonoBehaviour
{
    [Header("Board / Content Group")]
    [SerializeField, Tooltip("The board/Result RectTransform to reposition and scale as a group.")]
    private RectTransform board;

    [SerializeField, Range(0.05f, 1.5f),
     Tooltip("Fraction of the safe width the group may occupy (0..1).")]
    private float maxWidthFraction = 0.90f;

    [SerializeField, Range(0.05f, 1.5f),
     Tooltip("Fraction of the safe height the group may occupy (0..1).")]
    private float maxHeightFraction = 0.85f;

    [SerializeField, Tooltip("Vertical offset from safe-area center (canvas units). Default -80 visually centers the Kangaroo + Board group.")]
    private float verticalOffset = -80f;

    [SerializeField, Range(0.2f, 3f)] private float minScale = 0.45f;
    [SerializeField, Range(0.2f, 3f)] private float maxScale = 1.25f;

    [Header("Design Dimensions")]
    [SerializeField, Tooltip("Reference design bounds of the content. If Vector2.zero, automatically computed from the bounding box of board and its children.")]
    private Vector2 customDesignSize = Vector2.zero;

    [Header("Layout Group")]
    [SerializeField, Tooltip("Optional LayoutGroup on the board, disabled at runtime so C# drives the layout.")]
    private LayoutGroup layoutGroup;

    [SerializeField, Tooltip("Disable the LayoutGroup above at runtime (recommended).")]
    private bool disableLayoutGroup = true;

    [Header("Events")]
    [Tooltip("Raised after the board was repositioned/scaled.")]
    public UnityEvent onLayoutChanged = new UnityEvent();

    private RectTransform panelRect;
    private Vector2 lastScreenSize;
    private Rect lastSafeArea;

    private void Awake()
    {
        panelRect = transform as RectTransform;

        if (disableLayoutGroup && layoutGroup != null)
        {
            layoutGroup.enabled = false;
        }
    }

    private void OnEnable()
    {
        if (panelRect == null)
        {
            panelRect = transform as RectTransform;
        }
        ApplyLayout();
    }

    private void Start()
    {
        if (panelRect == null)
        {
            panelRect = transform as RectTransform;
        }
        ApplyLayout();
    }

    private void Update()
    {
        float screenW = Screen.width;
        float screenH = Screen.height;
        Rect safeArea = Screen.safeArea;
        if (safeArea.width > screenW || safeArea.height > screenH)
        {
            if (Screen.currentResolution.width > 0 && Screen.currentResolution.height > 0)
            {
                screenW = Screen.currentResolution.width;
                screenH = Screen.currentResolution.height;
            }
        }

        Vector2 size = new Vector2(screenW, screenH);
        if (size != lastScreenSize || safeArea != lastSafeArea)
        {
            ApplyLayout();
        }
    }

    /// <summary>
    /// Computes the visual bounds of the board and its active children in the board's local coordinate space.
    /// </summary>
    private Vector2 GetContentSize()
    {
        if (customDesignSize.x > 0f && customDesignSize.y > 0f)
        {
            return customDesignSize;
        }

        if (board == null)
        {
            return Vector2.one;
        }

        if (board.childCount == 0)
        {
            return board.rect.size;
        }

        Matrix4x4 rootToLocal = board.worldToLocalMatrix;
        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, 0f);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, 0f);
        bool hasBounds = false;

        void Encapsulate(RectTransform rt)
        {
            if (rt == null || !rt.gameObject.activeInHierarchy) return;
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            for (int i = 0; i < 4; i++)
            {
                Vector3 p = rootToLocal.MultiplyPoint(corners[i]);
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
                hasBounds = true;
            }
            for (int i = 0; i < rt.childCount; i++)
            {
                Encapsulate(rt.GetChild(i) as RectTransform);
            }
        }

        for (int i = 0; i < board.childCount; i++)
        {
            Encapsulate(board.GetChild(i) as RectTransform);
        }

        if (!hasBounds)
        {
            return board.rect.size;
        }

        return new Vector2(max.x - min.x, max.y - min.y);
    }

    /// <summary>
    /// Centers the board in the middle of the safe area and uniformly scales it to fit
    /// the visible screen while keeping all children positioned relatively as a group.
    /// </summary>
    public void ApplyLayout()
    {
        if (panelRect == null)
        {
            panelRect = transform as RectTransform;
        }

        if (panelRect == null || board == null)
        {
            return;
        }

        float screenW = Screen.width;
        float screenH = Screen.height;
        Rect safeArea = Screen.safeArea;

        // In Unity Editor Device Simulator, Screen.safeArea may be reported in simulated
        // device native pixels while Screen.width/height is the docked window size.
        if (safeArea.width > screenW || safeArea.height > screenH)
        {
            if (Screen.currentResolution.width > 0 && Screen.currentResolution.height > 0)
            {
                screenW = Screen.currentResolution.width;
                screenH = Screen.currentResolution.height;
            }
        }

        if (screenW <= 0f || screenH <= 0f)
        {
            return;
        }

        lastScreenSize = new Vector2(screenW, screenH);
        lastSafeArea = safeArea;

        float normXMin = Mathf.Clamp01(safeArea.xMin / screenW);
        float normYMin = Mathf.Clamp01(safeArea.yMin / screenH);
        float normXMax = Mathf.Clamp(safeArea.xMax / screenW, normXMin, 1f);
        float normYMax = Mathf.Clamp(safeArea.yMax / screenH, normYMin, 1f);

        // Coordinates of the safe area relative to the center of the panel
        Vector2 safeMin = new Vector2(
            (normXMin - 0.5f) * panelRect.rect.width,
            (normYMin - 0.5f) * panelRect.rect.height);

        Vector2 safeMax = new Vector2(
            (normXMax - 0.5f) * panelRect.rect.width,
            (normYMax - 0.5f) * panelRect.rect.height);

        Vector2 safeCenter = (safeMin + safeMax) * 0.5f;
        float safeW = safeMax.x - safeMin.x;
        float safeH = safeMax.y - safeMin.y;

        Vector2 contentSize = GetContentSize();
        float scaleW = (safeW * maxWidthFraction) / Mathf.Max(contentSize.x, 1f);
        float scaleH = (safeH * maxHeightFraction) / Mathf.Max(contentSize.y, 1f);
        float scale = Mathf.Clamp(Mathf.Min(scaleW, scaleH), minScale, maxScale);

        // Center anchors and pivot in the middle of the panel
        board.anchorMin = new Vector2(0.5f, 0.5f);
        board.anchorMax = new Vector2(0.5f, 0.5f);
        board.pivot = new Vector2(0.5f, 0.5f);
        board.localScale = Vector3.one * scale;

        // Position group at the center of the safe area with vertical offset scaled uniformly
        board.anchoredPosition = new Vector2(safeCenter.x, safeCenter.y + (verticalOffset * scale));

        onLayoutChanged.Invoke();
    }
}