using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime "Expand" CanvasScaler that makes the canvas always cover the screen
/// (never letterboxes) on any aspect ratio, and exposes how far the canvas
/// pokes past the physical screen so edge-anchored UI can be pulled inward.
///
/// Attach to the root Canvas GameObject next to its CanvasScaler component.
/// It pins the scaler to ScaleWithScreenSize with the given reference size and
/// picks, for the current screen, the axis whose fill ratio is larger - that
/// makes the canvas width OR height land exactly on the device edge and keeps
/// centre-gamed elements scaled up to fill the display.
///
/// Other components (e.g. SafeAreaAnchoredUI) can read Overflow to offset
/// edge-pinned elements by the same amount they would otherwise be clipped.
/// </summary>
[RequireComponent(typeof(CanvasScaler))]
public class AdaptiveCanvasScaler : MonoBehaviour
{
    [SerializeField] private float referenceWidth = 1920f;
    [SerializeField] private float referenceHeight = 1080f;

    private CanvasScaler scaler;
    private Vector2 lastScreenSize;
    private Rect lastSafeArea;
    private float scaleFactor = 1f;
    private Vector2 overflow;

    public float ScaleFactor { get { return scaleFactor; } }

    /// <summary>
    /// How far (in reference/canvas units) the canvas extends past the visible
    /// screen on each side that is not coordinate-matched. Positive = the
    /// canvas edge lies off the screen and edge-anchored elements must be
    /// shifted inward by this amount. Zero on the axis that is exactly matched.
    /// </summary>
    public Vector2 Overflow { get { return overflow; } }

    private void Awake()
    {
        scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(referenceWidth, referenceHeight);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
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
        if (scaler == null)
        {
            scaler = GetComponent<CanvasScaler>();
            return;
        }

        lastScreenSize = new Vector2(Screen.width, Screen.height);
        lastSafeArea = Screen.safeArea;

        float widthRatio = Screen.width / referenceWidth;
        float heightRatio = Screen.height / referenceHeight;
        bool matchWidth = widthRatio >= heightRatio;

        scaler.matchWidthOrHeight = matchWidth ? 0f : 1f;
        scaleFactor = matchWidth ? widthRatio : heightRatio;

        float visibleWidth = Screen.width / scaleFactor;
        float visibleHeight = Screen.height / scaleFactor;
        overflow = new Vector2(
            Mathf.Max((referenceWidth - visibleWidth) * 0.5f, 0f),
            Mathf.Max((referenceHeight - visibleHeight) * 0.5f, 0f));
    }
}