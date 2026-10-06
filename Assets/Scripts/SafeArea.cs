using UnityEngine;

/// <summary>
/// Safe Area Controller for mobile devices (iPhone notch/Dynamic Island,
/// Android display cutouts, curved edges, home indicator).
/// 
/// Automatically adjusts the attached RectTransform's anchorMin and anchorMax
/// to match Screen.safeArea in normalized coordinates (0..1), with zero offsets.
/// 
/// Attach to root panels or container RectTransforms (e.g., HUD, SafeAreaPanel)
/// directly under a Canvas Scaler set to Scale With Screen Size.
/// </summary>
[RequireComponent(typeof(RectTransform))]
[DisallowMultipleComponent]
[ExecuteAlways]
public class SafeArea : MonoBehaviour
{
    [Header("Axis Constraints")]
    [SerializeField]
    [Tooltip("Conform horizontal anchors (left and right) to the safe area.")]
    private bool conformHorizontal = true;

    [SerializeField]
    [Tooltip("Conform vertical anchors (top and bottom) to the safe area.")]
    private bool conformVertical = true;

    private RectTransform rectTransform;
    private Rect lastSafeArea = Rect.zero;
    private Vector2Int lastScreenSize = Vector2Int.zero;
    private ScreenOrientation lastOrientation = ScreenOrientation.Unknown;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        ApplySafeArea();
    }

    private void OnEnable()
    {
        ApplySafeArea();
    }

    private void Update()
    {
        CheckSafeArea();
    }

    private void OnRectTransformDimensionsChange()
    {
        CheckSafeArea();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();
        ApplySafeArea();
    }
#endif

    public void CheckSafeArea()
    {
        Rect safeArea = Screen.safeArea;
        Vector2Int currentScreenSize = new Vector2Int(Screen.width, Screen.height);
        ScreenOrientation currentOrientation = Screen.orientation;

        if (safeArea != lastSafeArea ||
            currentScreenSize != lastScreenSize ||
            currentOrientation != lastOrientation)
        {
            ApplySafeArea();
        }
    }

    /// <summary>
    /// Computes normalized safe area bounds and applies them to anchorMin and anchorMax.
    /// </summary>
    public void ApplySafeArea()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (rectTransform == null)
            return;

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

        if (screenW <= 0 || screenH <= 0)
            return;

        lastSafeArea = safeArea;
        lastScreenSize = new Vector2Int((int)screenW, (int)screenH);
        lastOrientation = Screen.orientation;

        float xMin = Mathf.Clamp01(safeArea.xMin / screenW);
        float yMin = Mathf.Clamp01(safeArea.yMin / screenH);
        float xMax = Mathf.Clamp(safeArea.xMax / screenW, xMin, 1f);
        float yMax = Mathf.Clamp(safeArea.yMax / screenH, yMin, 1f);

        Vector2 anchorMin = new Vector2(xMin, yMin);
        Vector2 anchorMax = new Vector2(xMax, yMax);

        if (!conformHorizontal)
        {
            anchorMin.x = 0f;
            anchorMax.x = 1f;
        }

        if (!conformVertical)
        {
            anchorMin.y = 0f;
            anchorMax.y = 1f;
        }

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
