using UnityEngine;

/// <summary>
/// Keeps the Settings_Panel layout responsive across device resolutions, aspect
/// ratios and notched/rounded-screen safe areas.
///
/// Attach this to the root "Settings_Panel" GameObject (it must have a
/// RectTransform that stretches over the whole canvas). Assign the child
/// RectTransforms in the inspector.
///
/// The script only does work on Awake/Start and whenever the screen resolution
/// or orientation changes - it never runs heavy logic every frame.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SettingsUIResponsiveAdapter : MonoBehaviour
{
    [Header("References")]
    [SerializeField, Tooltip("The Sqaure container RectTransform (the colored panel).")]
    private RectTransform square;
    [SerializeField, Tooltip("The Settings_Icon header RectTransform.")]
    private RectTransform header;
    [SerializeField, Tooltip("The Sounds section RectTransform (labels + slider + toggles).")]
    private RectTransform soundsSection;
    [SerializeField, Tooltip("The Reset_To_Default button RectTransform.")]
    private RectTransform resetButton;
    [SerializeField, Tooltip("The Save button RectTransform.")]
    private RectTransform saveButton;

    [Header("Panel (Sqaure)")]
    [SerializeField, Tooltip("Aspect ratio (width / height) the square is anchored to.")]
    private float panelAspect = 2.0f;
    [SerializeField, Tooltip("Padding (in canvas units) around the panel for small/4:3 screens.")]
    private float panelPadding = 24f;
    [SerializeField, Tooltip("Minimum panel height (in canvas units) that must be kept.")]
    private float minPanelHeight = 420f;

    [Header("Header (Settings_Icon)")]
    [SerializeField, Tooltip("Distance of the header from the top edge of Sqaure.")]
    private float headerTopOffset = 40f;
    [SerializeField, Tooltip("Header width as a fraction of Sqaure width.")]
    [Range(0.1f, 1f)] private float headerWidthFraction = 0.5f;

    [Header("Sounds Section")]
    [SerializeField, Tooltip("Distance of the Sounds section below the header (in canvas units).")]
    private float soundsOffset = 30f;

    [Header("Buttons")]
    [SerializeField, Tooltip("Distance of the buttons row from the bottom of Sqaure.")]
    private float buttonBottomOffset = 30f;
    [SerializeField, Tooltip("Distance between the bottom-center of the panel and the buttons.")]
    private float buttonHeight = 64f;
    [SerializeField, Tooltip("Horizontal gap between the Reset and Save buttons.")]
    private float buttonGap = 20f;

    private RectTransform root;
    private Vector2 lastScreenSize;

    private void Awake()
    {
        root = transform as RectTransform;
    }

    private void Start()
    {
        ApplyLayout();
    }

    private void Update()
    {
        // Cheap check only: re-layout on resolution/orientation changes.
        Vector2 size = new Vector2(Screen.width, Screen.height);
        if (!size.Equals(lastScreenSize))
        {
            lastScreenSize = size;
            ApplyLayout();
        }
    }

    private void OnEnable()
    {
        // Panel may start inactive; make sure it lays out correctly each time
        // it becomes visible (e.g. opened from the main menu).
        if (root != null)
            ApplyLayout();
    }

    /// <summary>
    /// Reapplies the layout based on the current screen and safe-area insets.
    /// Exposed publicly so other scripts (or UI events) can also trigger it.
    /// </summary>
    public void ApplyLayout()
    {
        if (root == null)
            root = transform as RectTransform;

        lastScreenSize = new Vector2(Screen.width, Screen.height);

        // 1. Convert the device safe area (notch / rounded corners) into the
        //    root panel's local size, so content can be centred safely.
        Vector2 safeMin = SafeAreaMin();
        Vector2 safeMax = SafeAreaMax();

        // 2. Compute the available (safe) rectangular area of the root rect.
        float availableW = (safeMax.x - safeMin.x) - panelPadding * 2f;
        float availableH = (safeMax.y - safeMin.y) - panelPadding * 2f;

        // 3. Size the Sqaure, preserving its aspect ratio, inside the safe
        //    area with padding, and never let it become too small (keeps the
        //    buttons from collapsing on 4:3 / tablet screens).
        float w = availableW;
        float h = w / panelAspect;
        if (h > availableH)
        {
            h = availableH;
            w = h * panelAspect;
        }
        h = Mathf.Max(h, minPanelHeight);

        if (square != null)
        {
            // Anchor to the root centre so the square stays centred, then
            // offset it by the safe-area inset to avoid notches.
            square.anchorMin = new Vector2(0.5f, 0.5f);
            square.anchorMax = new Vector2(0.5f, 0.5f);
            square.pivot = new Vector2(0.5f, 0.5f);
            float centreX = (safeMin.x + safeMax.x) * 0.5f;
            float centreY = (safeMin.y + safeMax.y) * 0.5f;
            square.anchoredPosition = new Vector2(centreX, centreY);
            square.sizeDelta = new Vector2(w, h);
        }

        // 4. Layout the header near the top of the square. Only the width is
        //    driven here; a per-element AspectRatioFitter (if present) keeps the
        //    height in proportion so the icon/logo never stretches.
        if (header != null)
        {
            header.anchorMin = new Vector2(0.5f, 1f);
            header.anchorMax = new Vector2(0.5f, 1f);
            header.pivot = new Vector2(0.5f, 0.5f);
            header.anchoredPosition = new Vector2(0f, -headerTopOffset);
            header.sizeDelta = new Vector2(w * headerWidthFraction, header.sizeDelta.y);
        }

        // 5. Place the Sounds section container below the header. It is anchored
        //    to the top-centre and given a usable width so its labels, slider and
        //    toggle children (anchored relative to it in the inspector) stay inside.
        if (soundsSection != null)
        {
            soundsSection.anchorMin = new Vector2(0.5f, 1f);
            soundsSection.anchorMax = new Vector2(0.5f, 1f);
            soundsSection.pivot = new Vector2(0.5f, 0.5f);
            soundsSection.anchoredPosition = new Vector2(0f, -(headerTopOffset + soundsOffset));
            soundsSection.sizeDelta = new Vector2(w - panelPadding * 2f, soundsSection.sizeDelta.y);
        }

        // 6. Layout the two buttons side by side along the bottom of the square.
        LayoutBottomButtons(w);
    }

    private void LayoutBottomButtons(float panelWidth)
    {
        if (resetButton == null && saveButton == null)
            return;

        // Each button keeps its own aspect ratio so its label never stretches;
        // start from the design-time width/ratio and a target height.
        float resetW = resetButton != null ? Mathf.Max(resetButton.rect.width, 40f) : 0f;
        float saveW = saveButton != null ? Mathf.Max(saveButton.rect.width, 40f) : 0f;

        float totalW = resetW + saveW + buttonGap;
        float maxW = Mathf.Max(panelWidth - panelPadding * 2f, 1f);

        // Shrink both buttons together if the panel is too narrow to fit them.
        float scale = Mathf.Min(1f, maxW / totalW);
        float finalW = totalW * scale;

        // The row is centred on the Sqaure's bottom edge.
        float rowLeft = -finalW * 0.5f + resetW * 0.5f * scale;
        float bottomY = buttonBottomOffset + buttonHeight * 0.5f;

        if (resetButton != null)
            PlaceButton(resetButton, rowLeft, bottomY, resetW * scale, buttonHeight);

        if (saveButton != null)
            PlaceButton(saveButton, rowLeft + (resetW + buttonGap) * scale, bottomY, saveW * scale, buttonHeight);
    }

    private static void PlaceButton(RectTransform button, float x, float bottomY, float width, float height)
    {
        button.anchorMin = new Vector2(0f, 0f);
        button.anchorMax = new Vector2(0f, 0f);
        button.pivot = new Vector2(0.5f, 0.5f);
        button.anchoredPosition = new Vector2(x, bottomY);
        button.sizeDelta = new Vector2(width, height);
    }

    /// <summary>
    /// Bottom-left corner of the safe area in the root panel's local space
    /// (root is assumed to be stretched across the full canvas).
    /// </summary>
    private Vector2 SafeAreaMin()
    {
        Rect safe = Screen.safeArea;
        return new Vector2(
            (safe.xMin / Screen.width - 0.5f) * root.rect.width,
            (safe.yMin / Screen.height - 0.5f) * root.rect.height);
    }

    /// <summary>
    /// Top-right corner of the safe area in the root panel's local space.
    /// </summary>
    private Vector2 SafeAreaMax()
    {
        Rect safe = Screen.safeArea;
        return new Vector2(
            (safe.xMax / Screen.width - 0.5f) * root.rect.width,
            (safe.yMax / Screen.height - 0.5f) * root.rect.height);
    }
}
