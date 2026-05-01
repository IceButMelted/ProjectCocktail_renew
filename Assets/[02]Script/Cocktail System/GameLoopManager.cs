using System.Collections;
using UnityEngine;
using Yarn.Unity;
using static E_Cocktail;

public class GameLoopManager : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────
    [Header("Yarn")]
    [SerializeField] private DialogueRunner _dialogueRunner;

    [Header("Systems")]
    [SerializeField] private CocktailSystemManager _cocktailSystem;
    [SerializeField] private MinigameSystemManager _minigameSystem;
    [SerializeField] private CocktailShaker        _cocktailShaker;

    // ── Yarn Variable Names ───────────────────────────────
    private const string VAR_SATISFACTION  = "$satisfaction";
    private const string VAR_MINIGAME_WIN  = "$minigame_win";
    private const string VAR_COCKTAIL_TYPE = "$cocktail_type";

    // ── State ─────────────────────────────────────────────
    private bool _minigameComplete;
    private bool _minigameSuccess;

    // ── Unity ─────────────────────────────────────────────
    private void Awake()
    {
        _dialogueRunner.AddCommandHandler<string>("set_order",     SetOrder);
        _dialogueRunner.AddCommandHandler<string>("play_minigame", t => StartCoroutine(PlayMinigame(t)));
        _dialogueRunner.AddCommandHandler("serve_cocktail",        ServeCocktail);
        _dialogueRunner.AddCommandHandler("reset_shaker",          ResetShaker);
    }

    // ── Yarn Commands ─────────────────────────────────────

    /// <<set_order [type]>> — "LowAlcohol" | "HighAlcohol" | "NoneAlcohol" | "Any"
private void SetOrder(string type)
    {
        S_Drink order;
        if (type == "Any" || !System.Enum.TryParse<TypeOfCocktail>(type, out TypeOfCocktail parsed))
            order = _cocktailSystem.RandomCocktail();
        else
            order = _cocktailSystem.RandomCocktail(parsed);

        _dialogueRunner.VariableStorage.SetValue(VAR_COCKTAIL_TYPE, order.AlcoholStrength.ToString());
        Debug.Log($"[GameLoop] Order set: {order.Name} ({order.AlcoholStrength})");
    }

    /// <<play_minigame [type]>> — "Shaking" | "Mixing"
    /// Suspends Yarn until the minigame fires OnGameEnd.
    private IEnumerator PlayMinigame(string type)
    {
        _minigameComplete = false;
        _minigameSuccess  = false;

        BaseMiniGame game = SelectMinigame(type);
        if (game == null)
        {
            Debug.LogWarning($"[GameLoop] Unknown minigame type: '{type}'");
            yield break;
        }

        // Subscribe BEFORE starting so we never miss the event
        game.OnGameEnd += OnMinigameEnded;

        if      (type == "Shaking") _minigameSystem.StartShakingMinigame();
        else if (type == "Mixing")  _minigameSystem.StartMixingMinigame();

        yield return new WaitUntil(() => _minigameComplete);

        game.OnGameEnd -= OnMinigameEnded;

        _dialogueRunner.VariableStorage.SetValue(VAR_MINIGAME_WIN, _minigameSuccess);
        Debug.Log($"[GameLoop] Minigame '{type}' ended — success: {_minigameSuccess}");
    }

    /// <<serve_cocktail>> — evaluates and resets the shaker
    private void ServeCocktail()
    {
        Satisfaction result = _cocktailSystem.CalculateSatisfaction();
        _dialogueRunner.VariableStorage.SetValue(VAR_SATISFACTION, result.ToString());
        Debug.Log($"[GameLoop] Served — satisfaction: {result}");
        _cocktailShaker.ResetShaker();
    }

    /// <<reset_shaker>> — clears without serving
    private void ResetShaker()
    {
        _cocktailShaker.ResetShaker();
        Debug.Log("[GameLoop] Shaker reset.");
    }

    // ── Helpers ───────────────────────────────────────────
    private void OnMinigameEnded(bool success)
    {
        _minigameSuccess  = success;
        _minigameComplete = true;
    }

    private BaseMiniGame SelectMinigame(string type) => type switch
    {
        "Shaking" => _minigameSystem.GetShakingMinigame(),
        "Mixing"  => _minigameSystem.GetMixingMinigame(),
        _          => null
    };
}
