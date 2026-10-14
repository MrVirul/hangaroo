using UnityEngine;

/// <summary>
/// The single choke point for the active difficulty.
///
/// Every other system resolves difficulty through this type - no script may read
/// or write the "Difficulty" PlayerPrefs key directly. Funnelling access here is
/// what makes difficulty testable today and overridable by Remote Config later.
///
/// The asset lives in <c>Resources</c> so it can be resolved from a
/// <see cref="RuntimeInitializeOnLoadMethodAttribute"/> bootstrap, before any
/// <c>Start()</c> runs (lives are seeded at run start, not lazily).
/// </summary>
[CreateAssetMenu(fileName = "DifficultyLibrary", menuName = "Hangaroo/Difficulty Library")]
public class DifficultyLibrary : ScriptableObject
{
    /// <summary>PlayerPrefs key owned exclusively by this type.</summary>
    public const string PlayerPrefsKey = "Difficulty";

    private const string ResourcePath = "Difficulty/DifficultyLibrary";

    [SerializeField]
    private DifficultyConfig[] configs = new DifficultyConfig[0];

    private static DifficultyLibrary instance;
    private static bool loaded;
    private static DifficultyId current = DifficultyIdExtensions.Default;

    /// <summary>The registry asset, loaded on demand. May be null if the asset is missing.</summary>
    public static DifficultyLibrary Instance
    {
        get
        {
            EnsureLoaded();
            return instance;
        }
    }

    /// <summary>The active difficulty for this run.</summary>
    public static DifficultyId Current
    {
        get
        {
            EnsureLoaded();
            return current;
        }
    }

    /// <summary>Convenience accessor for the active difficulty's config.</summary>
    public static DifficultyConfig CurrentConfig
    {
        get
        {
            EnsureLoaded();
            return instance != null ? instance.Get(current) : null;
        }
    }

    /// <summary>Sets and persists the active difficulty.</summary>
    public static void SetCurrent(DifficultyId id)
    {
        EnsureLoaded();
        current = id;

        PlayerPrefs.SetInt(PlayerPrefsKey, id.ToPlayerPrefs());
        PlayerPrefs.Save();
    }

    /// <summary>Returns the config for a difficulty, or null if it is not authored.</summary>
    public static DifficultyConfig GetConfig(DifficultyId id)
    {
        EnsureLoaded();
        return instance != null ? instance.Get(id) : null;
    }

    /// <summary>Returns the config for a difficulty, or null if it is not authored.</summary>
    public DifficultyConfig Get(DifficultyId id)
    {
        if (configs == null)
        {
            return null;
        }

        foreach (DifficultyConfig config in configs)
        {
            if (config != null && config.id == id)
            {
                return config;
            }
        }

        return null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureLoaded();
    }

    private static void EnsureLoaded()
    {
        if (loaded)
        {
            return;
        }

        loaded = true;

        instance = Resources.Load<DifficultyLibrary>(ResourcePath);

        if (instance == null)
        {
            Debug.LogError($"[DifficultyLibrary] Could not load '{ResourcePath}' from Resources; using {DifficultyIdExtensions.Default}.");
            current = DifficultyIdExtensions.Default;
            return;
        }

        current = DifficultyIdExtensions.FromPlayerPrefs(
            PlayerPrefs.GetInt(PlayerPrefsKey, DifficultyIdExtensions.Default.ToPlayerPrefs()));
    }

    void OnValidate()
    {
        if (configs == null)
        {
            return;
        }

        foreach (DifficultyConfig config in configs)
        {
            if (config != null)
            {
                config.Validate();
            }
        }
    }
}
