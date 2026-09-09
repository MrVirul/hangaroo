using UnityEngine;

/// <summary>
/// Runtime check that the pinned children of a popup card stay inside the card
/// while the card is scaled by PopupScaler. Attach to the card (e.g. "Sqaure").
///
/// In Play mode it logs, for each named child, whether the child's UI-space
/// centre still lies inside the card's UI-space rect. A child whose anchors
/// point at the card edges (Top-Center, Bottom-Left, Bottom-Right) will always
/// report OK, whatever scale PopupScaler applied.
/// </summary>
public class AnchorVerifier : MonoBehaviour
{
    [Header("Children to verify (by GameObject name)")]
    [SerializeField] private string[] childNames = { "Settings_Icon", "Reset_To_Default", "Save" };

    [SerializeField] private bool logEveryLayoutChange = false;

    private RectTransform rect;
    private RectTransform card;
    private Vector2 lastScreenSize;
    private Rect lastSafeArea;

    private void Awake()
    {
        rect = transform as RectTransform;
        card = rect; // verified card == this object
    }

    private void OnEnable()
    {
        Verify();
    }

    private void Start()
    {
        Verify();
    }

    private void Update()
    {
        Vector2 size = new Vector2(Screen.width, Screen.height);
        if (size != lastScreenSize || Screen.safeArea != lastSafeArea)
        {
            lastScreenSize = size;
            lastSafeArea = Screen.safeArea;
            Verify();
        }
    }

    public void Verify()
    {
        if (card == null)
            return;

        foreach (string name in childNames)
        {
            Transform child = card.transform.Find(name);
            if (child == null)
            {
                Debug.LogWarning($"[AnchorVerifier] No child named '{name}' under {card.name} - skipped.");
                continue;
            }

            if (!(child is RectTransform childRect))
            {
                Debug.LogWarning($"[AnchorVerifier] '{name}' is not a RectTransform.");
                continue;
            }

            Rect cardRect = WorldRect(card);
            Rect childBounds = WorldRect(childRect);

            bool inside = childBounds.xMin >= cardRect.xMin - 1f
                       && childBounds.xMax <= cardRect.xMax + 1f
                       && childBounds.yMin >= cardRect.yMin - 1f
                       && childBounds.yMax <= cardRect.yMax + 1f;

            string status = inside ? "OK - attached inside card" : "MISMATCH - outside card";
            Debug.Log($"[AnchorVerifier] '{name}' anchors=({childRect.anchorMin})->({childRect.anchorMax}) " +
                      $"{status}\n  card: {cardRect}\n  child: {childBounds}");

            if (!inside)
                Debug.LogWarning($"[AnchorVerifier] '{name}' is NOT following the card. Its anchors likely " +
                                 "don't point at card edges, or it is being moved by a LayoutGroup.");
        }
    }

    /// <summary>World-space (screen) rect of a RectTransform.</summary>
    private static Rect WorldRect(RectTransform rt)
    {
        Vector3[] c = new Vector3[4];
        rt.GetWorldCorners(c);
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        foreach (Vector3 v in c)
        {
            minX = Mathf.Min(minX, v.x);
            maxX = Mathf.Max(maxX, v.x);
            minY = Mathf.Min(minY, v.y);
            maxY = Mathf.Max(maxY, v.y);
        }
        return Rect.MinMaxRect(minX, minY, maxX, maxY);
    }
}