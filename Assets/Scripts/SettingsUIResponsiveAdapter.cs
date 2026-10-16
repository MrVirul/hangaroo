using UnityEngine;

/// <summary>
/// Keeps the Settings_Panel layout responsive across device resolutions, aspect
/// ratios and notched/rounded-screen safe areas.
///
/// Attach this to the root "Settings_Panel" GameObject (it must have a
/// RectTransform that stretches over the whole canvas). Assign the child
/// RectTransforms in the inspector.
///
/// Layout is a single top-to-bottom flow inside <see cref="square"/>: the header
/// icon, then each of the <see cref="settingRows"/> (Difficulty, Hints, Show
/// Category, Game Sounds), then the Sounds section. The whole stack is scaled
/// down together so it always fits between the top of the panel and the pinned
/// Reset/Save row - nothing is ever clipped or pushed outside the safe area.
///
/// The script only does work on Awake/Start, whenever it is enabled, and when
/// the screen resolution or orientation changes - never heavy logic per frame.
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
    [SerializeField, Tooltip("The Difficulty, Hints, Show Category and Game Sounds rows, top-to-bottom.")]
    private RectTransform[] settingRows = new RectTransform[0];
    [SerializeField, Tooltip("The Reset_To_Default button RectTransform.")]
    private RectTransform resetButton;
    [SerializeField, Tooltip("The Save button RectTransform.")]
    private RectTransform saveButton;

    [Header("Panel (Sqaure)")]
    [SerializeField, Tooltip("Aspect ratio (width / height) the square is anchored to.")]
    private float panelAspect = 2.0f;
    [SerializeField, Tooltip("Padding (in canvas units) around the panel for small/4:3 screens.")]
    private float panelPadding = 24f;
    [SerializeField, Tooltip("Minimum panel height (in canvas units) that must be kept when it fits.")]
    private float minPanelHeight = 420f;

    [Header("Header (Settings_Icon)")]
    [SerializeField, Tooltip("Distance of the header from the top edge of Sqaure.")]
    private float headerTopOffset = 24f;
    [SerializeField, Tooltip("Header width as a fraction of Sqaure width.")]
    [Range(0.1f, 1f)] private float headerWidthFraction = 0.4f;
    [SerializeField, Tooltip("Aspect ratio (width / height) the header keeps. Used only when the header has no AspectRatioFitter.")]
    private float headerAspect = 2f;

    [Header("Setting Rows")]
    [SerializeField, Tooltip("Design height of one settings row (label + control), before fitting.")]
    private float rowHeight = 56f;
    [SerializeField, Tooltip("Vertical gap between flow blocks, before fitting.")]
    private float rowGap = 10f;

    [Header("Sounds Section")]
    [SerializeField, Tooltip("Design height of the Sounds section, before fitting.")]
    private float soundsHeight = 180f;

    [Header("Buttons")]
    [SerializeField, Tooltip("Distance of the buttons row from the bottom of Sqaure.")]
    private float buttonBottomOffset = 24f;
    [SerializeField, Tooltip("Height of the Reset/Save buttons.")]
    private float buttonHeight = 56f;
    [SerializeField, Tooltip("Horizontal gap between the Reset and Save buttons.")]
    private float buttonGap = 20f;

    private RectTransform root;
    private Vector2 lastScreenSize;
    private float resetDesignWidth;
    private float saveDesignWidth;
    private bool designSizesCached;

    private void Awake()
    {
        root = transform as RectTransform;
        CacheDesignSizes();
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
    /// The largest panel size of the given aspect that fits inside
    /// <paramref name="availableW"/> x <paramref name="availableH"/>, raised to
    /// <paramref name="minHeight"/> only while it still fits. Never returns a
    /// size that exceeds the available area, and always preserves the aspect.
    /// </summary>
    public static Vector2 ComputePanelSize(float availableW, float availableH, float aspect, float minHeight)
    {
        if (availableW <= 0f || availableH <= 0f || aspect <= 0f)
            return Vector2.zero;

        float w = availableW;
        float h = w / aspect;

        if (h > availableH)
        {
            h = availableH;
            w = h * aspect;
        }

        // Raise to the minimum height, but only as far as the available area
        // allows; re-derive the width so the aspect is never broken.
        if (h < minHeight)
        {
            h = Mathf.Min(minHeight, availableH);
            w = h * aspect;

            if (w > availableW)
            {
                w = availableW;
                h = w / aspect;
            }
        }

        return new Vector2(Mathf.Max(1f, w), Mathf.Max(1f, h));
    }

    /// <summary>
    /// Reapplies the layout based on the current screen and safe-area insets.
    /// Exposed publicly so other scripts (or UI events) can also trigger it.
    /// </summary>
    public void ApplyLayout()
    {
        if (root == null)
            root = transform as RectTransform;

        CacheDesignSizes();
        lastScreenSize = new Vector2(Screen.width, Screen.height);

        // 1. Convert the device safe area (notch / rounded corners) into the
        //    root panel's local size, so content can be centred safely.
        Vector2 safeMin = SafeAreaMin();
        Vector2 safeMax = SafeAreaMax();

        // The safe-area ratio is relative to the screen, while the root rect is
        // whatever the panel is stretched to. Clamp the safe rect to the root so
        // the panel can never be pushed outside its own container even if the two
        // disagree (tests, non-full-screen canvases, editor Game View).
        Rect local = root.rect;
        float minX = Mathf.Max(safeMin.x, local.xMin);
        float minY = Mathf.Max(safeMin.y, local.yMin);
        float maxX = Mathf.Min(safeMax.x, local.xMax);
        float maxY = Mathf.Min(safeMax.y, local.yMax);

        if (maxX < minX)
        {
            maxX = minX = local.center.x;
        }

        if (maxY < minY)
        {
            maxY = minY = local.center.y;
        }

        safeMin = new Vector2(minX, minY);
        safeMax = new Vector2(maxX, maxY);

        // 2. Available (safe) rectangular area of the root rect.
        float availableW = (safeMax.x - safeMin.x) - panelPadding * 2f;
        float availableH = (safeMax.y - safeMin.y) - panelPadding * 2f;

        // 3. Size the Sqaure, preserving its aspect ratio, inside the safe area
        //    with padding, and never exceeding it.
        Vector2 panelSize = ComputePanelSize(availableW, availableH, panelAspect, minPanelHeight);
        panelSize = new Vector2(Mathf.Max(1f, panelSize.x), Mathf.Max(1f, panelSize.y));
        float w = panelSize.x;

        if (square != null)
        {
            square.anchorMin = new Vector2(0.5f, 0.5f);
            square.anchorMax = new Vector2(0.5f, 0.5f);
            square.pivot = new Vector2(0.5f, 0.5f);
            square.anchoredPosition = new Vector2(
                (safeMin.x + safeMax.x) * 0.5f,
                (safeMin.y + safeMax.y) * 0.5f);
            square.sizeDelta = panelSize;
        }

        // 4. Flow the content top-to-bottom inside Sqaure. The header, the
        //    setting rows and the Sounds section are scaled together with
        //    localScale so their text and controls shrink with them; resizing
        //    the rects alone would leave the labels at full size and make them
        //    bleed into the neighbouring rows. The stack is scaled down just
        //    enough to fit between the top of the panel and the pinned button
        //    row, so it never overlaps the buttons or leaves the safe area.
        float usable = panelSize.y - panelPadding - (buttonBottomOffset + buttonHeight);

        // The header follows the aspect ratio of its own AspectRatioFitter
        // (the Settings_Icon sprite is 2:1), so its height is derived from its
        // width. Reserving a fixed design height instead let the fitter grow
        // the icon taller than the flow expected and overlap the first row.
        float headerW = w * headerWidthFraction;
        float resolvedHeaderAspect = ResolveHeaderAspect();
        float headerH = headerW / resolvedHeaderAspect;

        int rowCount = settingRows != null ? settingRows.Length : 0;

        int blocks = 0;
        float natural = headerTopOffset;

        if (header != null) { natural += headerH; blocks++; }
        natural += rowHeight * rowCount; blocks += rowCount;
        if (soundsSection != null) { natural += soundsHeight; blocks++; }
        if (blocks > 1) natural += rowGap * (blocks - 1);

        float scale = natural > 1f ? Mathf.Clamp(usable / natural, 0.01f, 1f) : 1f;
        float gap = rowGap * scale;

        // Design width a row/section needs so that, once scaled by "scale", it
        // spans the panel width.
        float rowWidth = Mathf.Max(1f, (w - panelPadding * 2f) / scale);
        float y = -panelPadding - headerTopOffset * scale;

        if (header != null)
        {
            header.anchorMin = new Vector2(0.5f, 1f);
            header.anchorMax = new Vector2(0.5f, 1f);
            header.pivot = new Vector2(0.5f, 0.5f);
            header.localScale = new Vector3(scale, scale, 1f);
            header.sizeDelta = new Vector2(headerW, headerH);
            header.anchoredPosition = new Vector2(0f, y - headerH * scale * 0.5f);
            y -= headerH * scale + gap;
        }

        if (settingRows != null)
        {
            foreach (RectTransform row in settingRows)
            {
                if (row == null)
                    continue;

                row.anchorMin = new Vector2(0.5f, 1f);
                row.anchorMax = new Vector2(0.5f, 1f);
                row.pivot = new Vector2(0.5f, 0.5f);
                row.localScale = new Vector3(scale, scale, 1f);
                row.sizeDelta = new Vector2(rowWidth, rowHeight);
                row.anchoredPosition = new Vector2(0f, y - rowHeight * scale * 0.5f);
                y -= rowHeight * scale + gap;
            }
        }

        if (soundsSection != null)
        {
            // The Sounds block is left-aligned with the setting rows: its
            // children are anchored to its top-left corner, so anchor it to
            // Sqaure's top-left and offset it by the panel padding.
            soundsSection.anchorMin = new Vector2(0f, 1f);
            soundsSection.anchorMax = new Vector2(0f, 1f);
            soundsSection.pivot = new Vector2(0f, 1f);
            soundsSection.localScale = new Vector3(scale, scale, 1f);
            soundsSection.sizeDelta = new Vector2(rowWidth, soundsHeight);
            soundsSection.anchoredPosition = new Vector2(panelPadding, y);
        }

        // 5. Layout the two buttons side by side along the bottom of the square.
        LayoutBottomButtons(w);
    }

    /// <summary>
    /// The aspect ratio the header is locked to. Prefers the header's own
    /// <see cref="UnityEngine.UI.AspectRatioFitter"/> so the height reserved by
    /// the flow always matches the height the fitter enforces, and falls back to
    /// <see cref="headerAspect"/>.
    /// </summary>
    private float ResolveHeaderAspect()
    {
        if (header != null)
        {
            UnityEngine.UI.AspectRatioFitter fitter = header.GetComponent<UnityEngine.UI.AspectRatioFitter>();
            if (fitter != null && fitter.aspectRatio > 0.01f &&
                fitter.aspectMode == UnityEngine.UI.AspectRatioFitter.AspectMode.WidthControlsHeight)
            {
                return fitter.aspectRatio;
            }
        }

        return headerAspect > 0.01f ? headerAspect : 1f;
    }

    private void CacheDesignSizes()
    {
        if (designSizesCached)
            return;

        designSizesCached = true;

        if (resetButton != null)
            resetDesignWidth = Mathf.Max(resetButton.rect.width, 40f);

        if (saveButton != null)
            saveDesignWidth = Mathf.Max(saveButton.rect.width, 40f);
    }

    private void LayoutBottomButtons(float panelWidth)
    {
        if (resetButton == null && saveButton == null)
            return;

        // Design widths are cached once in Awake: deriving them from the live
        // rect (which this method overwrites) made the row never recover after
        // one shrink.
        float resetW = resetDesignWidth;
        float saveW = saveDesignWidth;
        float totalW = resetW + saveW + buttonGap;
        float maxW = Mathf.Max(panelWidth - panelPadding * 2f, 1f);

        // Shrink both buttons together if the panel is too narrow to fit them.
        float scale = Mathf.Min(1f, maxW / Mathf.Max(totalW, 1f));
        float finalW = totalW * scale;

        // The buttons are anchored to Sqaure's bottom-left corner, so x is a
        // distance from the left edge; centre the row with equal margins.
        float left = (panelWidth - finalW) * 0.5f;
        float bottomY = buttonBottomOffset + buttonHeight * 0.5f;

        if (resetButton != null)
            PlaceButton(resetButton, left + resetW * scale * 0.5f, bottomY, resetW * scale, buttonHeight);

        if (saveButton != null)
            PlaceButton(saveButton, left + (resetW + buttonGap) * scale + saveW * scale * 0.5f, bottomY,
                saveW * scale, buttonHeight);
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
