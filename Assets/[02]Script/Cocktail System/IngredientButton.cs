using UnityEngine;
using UnityEngine.EventSystems;
using static E_Cocktail;

/// <summary>
/// 2.5D ingredient button driven by Unity's EventSystem (Physics / Graphic raycaster).
/// Supports Mixer, Alcohol, method selection, ice, and shaker reset.
/// </summary>
public class IngredientButton : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    private enum Behaviour
    {
        None,
        Mixer,
        Alcohol,
        Shaking,
        Mixing,
        AddIce,
        Reset,
    }

    // ── Inspector ────────────────────────────────────────
    [SerializeField] private Material  _material;
    [SerializeField] private Texture2D _texDefault;
    [SerializeField] private Texture2D _texHover;
    [SerializeField] private Texture2D _texClicked;

    [SerializeField] private bool      _interactable = true;
    [SerializeField] private Behaviour _behaviour;
    [SerializeField] private Mixer     _mixer;
    [SerializeField] private Alcohol   _alcohol;

    // ── Cached Shader IDs ────────────────────────────────
    private static readonly int EmissionID = Shader.PropertyToID("_EmssionStrength");
    private static readonly int TextureID  = Shader.PropertyToID("_CurrentTexture");

    // ── Private State ────────────────────────────────────
    private CocktailShaker _shaker;
    private bool           _pointerOver;

    // ── Unity ────────────────────────────────────────────
    private void Awake()
    {
        _shaker = FindFirstObjectByType<CocktailShaker>();

        if (!_interactable) return;
        _material = GetComponent<MeshRenderer>().material;
        ApplyMaterial(0f, _texDefault);
    }

    // ── Pointer Events ───────────────────────────────────
    public void OnPointerEnter(PointerEventData _)
    {
        if (!_interactable) return;
        _pointerOver = true;
        ApplyMaterial(0.25f, _texHover);
    }

    public void OnPointerExit(PointerEventData _)
    {
        if (!_interactable) return;
        _pointerOver = false;
        ApplyMaterial(0f, _texDefault);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!_interactable || !_pointerOver) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;

        ApplyIngredient();
        ApplyMaterial(0.125f, _texClicked);
    }

    public void OnPointerUp(PointerEventData _)
    {
        if (!_interactable) return;

        // One-shot ice button — disable after use
        if (_behaviour == Behaviour.AddIce)
            _interactable = false;

        ApplyMaterial(0f, _texDefault);
    }

    // ── Public ───────────────────────────────────────────
    /// <summary>Programmatic trigger — e.g. from a keyboard shortcut.</summary>
    public void SetCocktailIngredient() => ApplyIngredient();

    // ── Private ──────────────────────────────────────────
    private void ApplyIngredient()
    {
        switch (_behaviour)
        {
            case Behaviour.Mixer:
                _shaker.OnAddMixer?.Invoke(_mixer, 1);
                _shaker.OnAddIngredient?.Invoke();
                break;

            case Behaviour.Alcohol:
                _shaker.OnAddAlcohol?.Invoke(_alcohol, 1);
                _shaker.OnAddIngredient?.Invoke();
                break;

            case Behaviour.Shaking:
                _shaker.SetMethod(Method.Shaking);
                break;

            case Behaviour.Mixing:
                _shaker.SetMethod(Method.Mixing);
                break;

            case Behaviour.AddIce:
                _shaker.SetIceAddIce();
                break;
        }
    }

    private void ApplyMaterial(float emission, Texture2D tex)
    {
        if (_material == null) return;
        _material.SetFloat(EmissionID, emission);
        _material.SetTexture(TextureID, tex);
    }
}
