using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class IngredientButton : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    // ── Types ──────────────────────────────────────────────

    private enum Behaviability
    {
        None,
        Mixer,
        Alcohol,
        Shaking,
        Mixing,
        AddIce,
        Reset,
    }

    // ── Inspector ──────────────────────────────────────────

    [SerializeField] private Material m_Material;
    [SerializeField] private Texture2D T_Default;
    [SerializeField] private Texture2D T_Hover;
    [SerializeField] private Texture2D T_Clicked;

    [SerializeField] private bool ShouldCanClick = true;
    [SerializeField] private Behaviability TypeIngredient;
    [SerializeField] private E_Cocktail.Mixer mixer;
    [SerializeField] private E_Cocktail.Alcohol alcohol;

    private CocktailShaker _cocktailMaker;
    private bool _canClick;

    private void Awake()
    {
        _cocktailMaker = FindFirstObjectByType<CocktailShaker>();

        if (!ShouldCanClick) return;

        m_Material = GetComponent<MeshRenderer>().material;
        m_Material.SetFloat("_EmssionStrength", 0);
        m_Material.SetTexture("_CurrentTexture", T_Default);
    }

    // ── Pointer Events ─────────────────────────────────────

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!ShouldCanClick) return;

        _canClick = true;
        m_Material.SetFloat("_EmssionStrength", 0.25f);
        m_Material.SetTexture("_CurrentTexture", T_Hover);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!ShouldCanClick) return;

        _canClick = false;
        m_Material.SetFloat("_EmssionStrength", 0);
        m_Material.SetTexture("_CurrentTexture", T_Default);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!ShouldCanClick || !_canClick) return;

        // ── FIX ───────────────────────────────────────────
        // Removed: if (Mouse.current.leftButton.wasPressedThisFrame)
        //
        // Reason: OnPointerDown only fires when the EventSystem's raycaster
        // determines this object is the top-most hit. If a Canvas with a
        // GraphicRaycaster (Blocking Mask = 2.5D UI) sits in front, the
        // EventSystem stops here and OnPointerDown never reaches this object.
        // Adding a raw Mouse.current check on top of that does nothing useful
        // and can cause the opposite problem — it evaluates the raw input state
        // independently of the raycaster, so it never correctly blocks.
        //
        // OnPointerDown receiving the call IS the left-click confirmation.
        // ─────────────────────────────────────────────────

        if (eventData.button != PointerEventData.InputButton.Left) return;

        ApplyIngredient();

        

        m_Material.SetFloat("_EmssionStrength", 0.125f);
        m_Material.SetTexture("_CurrentTexture", T_Clicked);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!ShouldCanClick) return;

        if (TypeIngredient == Behaviability.AddIce)
        {
            ShouldCanClick = false;
        }

        m_Material.SetFloat("_EmssionStrength", 0);
        m_Material.SetTexture("_CurrentTexture", T_Default);
    }

    // ── Public ─────────────────────────────────────────────

    /// <summary>Can also be called directly (e.g. from keyboard shortcut).</summary>
    public void SetCocktailIngredient() => ApplyIngredient();

    // ── Private ────────────────────────────────────────────

    private void ApplyIngredient()
    {
        switch (TypeIngredient)
        {
            case Behaviability.Mixer:
                _cocktailMaker.OnAddMixer?.Invoke(mixer, 1);
                _cocktailMaker.OnAddIngredient?.Invoke();
                break;

            case Behaviability.Alcohol:
                _cocktailMaker.OnAddAlcohol?.Invoke(alcohol, 1);
                _cocktailMaker.OnAddIngredient?.Invoke();
                break;

            case Behaviability.Shaking:
                _cocktailMaker.SetMethod(E_Cocktail.Method.Shaking);
                break;

            case Behaviability.Mixing:
                _cocktailMaker.SetMethod(E_Cocktail.Method.Mixing);
                break;
            case Behaviability.AddIce:
                _cocktailMaker.SetIceAddIce();
                break;
        }
    }
}