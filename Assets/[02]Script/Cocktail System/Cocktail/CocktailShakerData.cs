// ============================================================
//  CocktailShakerData.cs — COMPATIBILITY SHIM.
//
//  This class used to hold five unrelated jobs: the live drink,
//  the glass visuals, the ingredient-button roster, the book-UI
//  roster, and the hover tooltip. Plan §4.2 splits those into
//  ShakerContents / ShakerVisualPresenter / IngredientButtonGroup /
//  ShakerTooltip, all in Cocktail/Shaker/.
//
//  It still exists because five scenes and prefabs reference it by
//  GUID and bind eight of its methods to UnityEvents. Deleting it
//  outright would turn those into Missing Script and silently drop
//  the ingredient lists and glass table stored on them.
//
//  So it now owns NO behaviour: it keeps the serialized data the
//  scenes already hold, hands that data to the real components on
//  Awake, and forwards every call. Once the manual migration in
//  Docs/Bar410_CocktailSystem_Manual_Setup.md is done, this file
//  can be deleted.
// ============================================================

using AYellowpaper.SerializedCollections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Yarn.Unity;
using static E_Cocktail;
using static WaterSlosh;

[System.Serializable] public class AlcoholEvent : UnityEvent<BaseSpirit, int> { }
[System.Serializable] public class LiqueurEvent : UnityEvent<Liqueur, int> { }
[System.Serializable] public class MixerEvent : UnityEvent<Mixer, int> { }

public class CocktailShakerData : MonoBehaviour, IIngredientReceiver, ITooltipProvider
{
    // ── Legacy serialized data (still authored in the scenes) ──

    [Header("Glass Cocktail")]
    public WaterSlosh glassWaterSlosh;

    [Header("Ingredient Events")]
    public AlcoholEvent OnAddAlcohol;
    public LiqueurEvent OnAddLiqueur;
    public MixerEvent OnAddMixer;
    public UnityEvent OnAddIngredient;
    public UnityEvent OnResetedCocktail;

    [Header("Ingredient Buttons")]
    public List<GameObject> ingredientButtons = new List<GameObject>();
    public List<GameObject> bookUi = new List<GameObject>();

    [Header("Glass And Ice")]
    [Tooltip("LEGACY. Author SO_GlassVisualTable instead and assign it below — see plan D5.")]
    [SerializedDictionary("Glass Type", "Visual")]
    public SerializableDictionary<GlassType, VisualCocktailGlass> GlassVisualData = new SerializableDictionary<GlassType, VisualCocktailGlass>();

    [Tooltip("Preferred source of glass visuals. When set, GlassVisualData above is ignored.")]
    [SerializeField] private SO_GlassVisualTable _glassVisualTable;

    // ── Real components ────────────────────────────────────

    private ShakerContents _contents;
    private ShakerVisualPresenter _visuals;
    private IngredientButtonGroup _ingredientGroup;
    private IngredientButtonGroup _bookGroup;

    /// <summary>Live drink. Now owned by <see cref="ShakerContents"/>.</summary>
    public S_Drink CurrentCocktail => _contents != null ? _contents.CurrentCocktail : null;

    /// <summary>Most recent comparison against the recipe database.</summary>
    public RecipeMatch LastMatch => _contents != null ? _contents.LastMatch : RecipeMatch.None;

    public ShakerContents Contents => _contents;
    public ShakerVisualPresenter Visuals => _visuals;
    public IngredientButtonGroup IngredientGroup => _ingredientGroup;
    public IngredientButtonGroup BookGroup => _bookGroup;

    // ── Unity Lifecycle ────────────────────────────────────

    private void Awake()
    {
        _contents = GetOrAdd<ShakerContents>();

        _visuals = GetComponent<ShakerVisualPresenter>();
        if (_visuals == null) _visuals = gameObject.AddComponent<ShakerVisualPresenter>();
        _visuals.Initialize(glassWaterSlosh, _glassVisualTable != null ? _glassVisualTable : BuildLegacyTable());

        if (GetComponent<ShakerTooltip>() == null) gameObject.AddComponent<ShakerTooltip>();

        ResolveButtonGroups();
    }

    /// <summary>
    /// Converts the per-scene <see cref="GlassVisualData"/> into a throw-away table so an
    /// unmigrated scene keeps its glass visuals. Returns null when there is nothing to convert.
    /// </summary>
    private SO_GlassVisualTable BuildLegacyTable()
    {
        if (GlassVisualData == null || GlassVisualData.Count == 0) return null;

        Debug.LogWarning(
            "[CocktailShakerData] Falling back to this scene's own GlassVisualData. Author one " +
            "SO_GlassVisualTable asset and assign it here so every scene shares a single source " +
            "(Docs/Bar410_CocktailSystem_Manual_Setup.md).", this);

        _runtimeTable = ScriptableObject.CreateInstance<SO_GlassVisualTable>();

        foreach (var entry in GlassVisualData)
        {
            if (entry.Value == null) continue;

            _runtimeTable.Table[entry.Key] = new GlassVisual
            {
                GlassSprite = entry.Value.GlassSprite,
                WaterSprite = entry.Value.WaterSprite,
                IceSprite = entry.Value.IceSprite
            };
        }

        return _runtimeTable;
    }

    private SO_GlassVisualTable _runtimeTable;

    private void OnDestroy()
    {
        if (_runtimeTable != null) Destroy(_runtimeTable);
    }

    private T GetOrAdd<T>() where T : Component
    {
        var existing = GetComponent<T>();
        return existing != null ? existing : gameObject.AddComponent<T>();
    }

    /// <summary>
    /// Finds the two IngredientButtonGroups, creating them from the legacy lists when the
    /// scene has not been migrated yet. Two groups on one GameObject is unusual, so the
    /// migrated setup should put them on the shelf and the book instead.
    /// </summary>
    private void ResolveButtonGroups()
    {
        var groups = GetComponents<IngredientButtonGroup>();

        if (groups.Length > 0) _ingredientGroup = groups[0];
        if (groups.Length > 1) _bookGroup = groups[1];

        if (_ingredientGroup == null)
        {
            _ingredientGroup = gameObject.AddComponent<IngredientButtonGroup>();
            _ingredientGroup.SetRoster(ingredientButtons);
        }

        if (_bookGroup == null)
        {
            _bookGroup = gameObject.AddComponent<IngredientButtonGroup>();
            _bookGroup.SetRoster(bookUi);
        }
    }

    // ── Fill passthrough ───────────────────────────────────

    public void StartFill() => _visuals.StartFill();
    public void StopFill() => _visuals.StopFill();
    public void FinishFill() => _visuals.FinishFill();

    // ── Preparation ────────────────────────────────────────

    public void SetMethod(Method method) => _contents.SetMethod(method);
    public void SetMethodToShake() => _contents.SetMethodToShake();
    public void SetMethodToMixing() => _contents.SetMethodToMixing();
    public void ToggleIce() => _contents.ToggleIce();
    public void ToggleIce(bool enable) => _contents.ToggleIce(enable);

    // ── IIngredientReceiver ────────────────────────────────

    public void TryToAddAlcohol(BaseSpirit alcohol, int amount = 1)
    {
        _contents.TryToAddAlcohol(alcohol, amount);
        OnAddIngredient?.Invoke();
    }

    public void TryToAddLiqueur(Liqueur liqueur, int amount = 1)
    {
        _contents.TryToAddLiqueur(liqueur, amount);
        OnAddIngredient?.Invoke();
    }

    public void TryToAddMixer(Mixer mixer, int amount = 1)
    {
        _contents.TryToAddMixer(mixer, amount);
        OnAddIngredient?.Invoke();
    }

    // ── Identity ───────────────────────────────────────────

    public void UpdateCocktailInShaker(IReadOnlyList<S_Drink> recipes) => _contents.UpdateIdentity(recipes);

    public Sprite GetCurrentSprite(IReadOnlyList<S_Drink> recipes, Sprite fallback)
    {
        var match = DrinkDeviation.FindBestMatch(CurrentCocktail, recipes);
        return match.IsRecognised ? (match.Recipe.CocktailSprite ?? fallback) : fallback;
    }

    // ── Reset ──────────────────────────────────────────────

    /// <summary>Fires OnResetedCocktail for Inspector-wired listeners.</summary>
    public void ResetShaker() => OnResetedCocktail?.Invoke();

    /// <summary>Clears the drink and re-enables the ingredient buttons.</summary>
    public void ResetCocktailData()
    {
        _contents.Clear();
        SetIngredientActive(true);
    }

    // ── Ingredient Button Helpers ──────────────────────────

    public void CanIngredientActive() => SetIngredientActive(_contents.HasRoom);

    public void SetIngredientActive(bool active) => _ingredientGroup.SetInteractable(active);

    public void SetBookUiActive(bool active) => _bookGroup.SetInteractable(active);

    // ── Visuals ────────────────────────────────────────────

    public void SetColorAndGlass(WaterSlosh _, S_Drink drink) => _visuals.Apply(drink);

    public string GetTooltipText() => DrinkFormatter.GetCocktailIngredient(CurrentCocktail);

    /// <summary>LEGACY shape of <see cref="GlassVisualData"/>. Replaced by <see cref="GlassVisual"/>.</summary>
    [System.Serializable]
    public class VisualCocktailGlass
    {
        public Sprite GlassSprite;
        public Sprite WaterSprite;
        public Sprite IceSprite;
    }
}
