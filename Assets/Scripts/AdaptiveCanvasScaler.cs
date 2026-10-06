using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime CanvasScaler that keeps the canvas covering the screen on any aspect
/// ratio while never letting it get smaller than the authored design box.
///
/// Attach to the root Canvas GameObject next to its CanvasScaler component.
/// It pins the scaler to ScaleWithScreenSize with the given reference size and
/// picks, for the current screen, the axis whose fill ratio is SMALLER ("contain").
/// That makes the canvas rect at least referenceWidth x referenceHeight on every
/// device, so UI authored inside the design box is never pushed outside the
/// canvas - on a portrait phone the canvas simply gains vertical slack instead of
/// losing horizontal room.
///
/// Because a ScaleWithScreenSize canvas always covers the screen exactly, it never
/// overhangs and Overflow is therefore zero; see the property.
/// </summary>
[RequireComponent(typeof(CanvasScaler))]
public class AdaptiveCanvasScaler : MonoBehaviour
{
    [SerializeField] private float referenceWidth = 1080f;
    [SerializeField] private float referenceHeight = 2400f;
    [SerializeField, Range(0f, 1f)] private float matchWidthOrHeight = 0.5f;

    private CanvasScaler scaler;
    private Vector2 lastScreenSize;
    private Rect lastSafeArea;
    private float scaleFactor = 1f;
    private Vector2 overflow;

    public float ScaleFactor { get { return scaleFactor; } }

    /// <summary>
    /// How far the canvas extends past the visible screen. Always zero for the
    /// ScaleWithScreenSize canvas this component configures, because that canvas
    /// rect is by definition the visible screen area in canvas units.
    /// Kept as a property because SafeAreaAnchoredUI and DynamicUIAnchor read it
    /// when offsetting edge-pinned elements.
    /// </summary>
    public Vector2 Overflow { get { return overflow; } }

    private void Awake()
    {
        scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(referenceWidth, referenceHeight);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = matchWidthOrHeight;
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

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(referenceWidth, referenceHeight);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = matchWidthOrHeight;

        float widthRatio = Screen.width / referenceWidth;
        float heightRatio = Screen.height / referenceHeight;
        scaleFactor = Mathf.Lerp(widthRatio, heightRatio, matchWidthOrHeight);
        overflow = Vector2.zero;
    }
}