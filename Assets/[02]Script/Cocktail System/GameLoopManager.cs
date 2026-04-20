using System.Collections;
using UnityEngine;
using Yarn.Unity;
using static E_Cocktail;

public class GameLoopManager : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────

    [Header("Yarn")]
    [SerializeField] private DialogueRunner _dialogueRunner;

    [Header("Systems")]
    [SerializeField] private CocktailSystemManager _cocktailSystem;
    [SerializeField] private MinigameSystemManager  _minigameSystem;
    [SerializeField] private CocktailShaker         _cocktailShaker;

    // ── Yarn Variable Names ────────────────────────────────

    // Written by this manager, read by Yarn scripts
    private const string VAR_SATISFACTION   = "$satisfaction";    // "Perfect" | "Acceptable" | "Fail"
    private const string VAR_MINIGAME_WIN   = "$minigame_win";    // bool
    private const string VAR_COCKTAIL_TYPE  = "$cocktail_type";   // "HighAlcohol" | "LowAlcohol" | "NoneAlcohol"

    // ── State ──────────────────────────────────────────────

    private bool _minigameComplete = false;
    private bool _minigameSuccess  = false;

    // ── Unity ──────────────────────────────────────────────

    private void Awake()
    {
        // Register all Yarn Commands on this MonoBehaviour
        // Yarnspinner will call these when it sees <<command_name>> in a .yarn file

        _dialogueRunner.AddCommandHandler<string>(
            "set_order", SetOrder);

        _dialogueRunner.AddCommandHandler<string>(
            "play_minigame", cmd => StartCoroutine(PlayMinigame(cmd)));

        _dialogueRunner.AddCommandHandler(
            "serve_cocktail", ServeCocktail);

        _dialogueRunner.AddCommandHandler(
            "reset_shaker", ResetShaker);
    }

    // ── Yarn Commands ──────────────────────────────────────

    /// <summary>
    /// <<set_order [type]>>
    /// Picks a random cocktail of the given TypeOfCocktail for the customer.
    /// Type can be: "HighAlcohol", "LowAlcohol", "NoneAlcohol", or "Any"
    ///
    /// Example Yarn:
    ///   <<set_order "LowAlcohol">>
    ///   <<set_order "Any">>
    /// </summary>
    private void SetOrder(string type)
    {
        S_Drink order;

        if (type == "Any" || !System.Enum.TryParse<TypeOfCocktail>(type, out var parsedType))
        {
            order = _cocktailSystem.RandomCocktail();
        }
        else
        {
            order = _cocktailSystem.RandomCocktail(parsedType);
        }

        // Write cocktail type to Yarn so dialogue can reference it
        _dialogueRunner.VariableStorage.SetValue(
            VAR_COCKTAIL_TYPE, order.AlcoholStrength.ToString());

        Debug.Log($"[GameLoop] Order set: {order.Name} ({order.AlcoholStrength})");
    }

    /// <summary>
    /// <<play_minigame [type]>>
    /// Starts a minigame and SUSPENDS Yarn dialogue until it completes.
    /// Type: "Shaking" or "Mixing"
    ///
    /// After the minigame ends, $minigame_win is set in Yarn.
    ///
    /// Example Yarn:
    ///   <<play_minigame "Shaking">>
    ///   <<if $minigame_win>>
    ///       You shook it perfectly!
    ///   <<else>>
    ///       Hmm, something went wrong...
    ///   <<endif>>
    /// </summary>
    private IEnumerator PlayMinigame(string type)
    {
        _minigameComplete = false;
        _minigameSuccess  = false;

        // Subscribe to result before starting so we never miss it
        BaseMiniGame game = SelectMinigame(type);
        if (game == null)
        {
            Debug.LogWarning($"[GameLoop] Unknown minigame type: '{type}'");
            yield break;
        }

        game.OnGameEnd += OnMinigameEnded;

        // Start the correct minigame via the manager
        if (type == "Shaking")
            _minigameSystem.StartShakingMinigame();
        else if (type == "Mixing")
            _minigameSystem.StartMixingMinigame();

        // Suspend Yarn until the minigame fires OnGameEnd
        yield return new WaitUntil(() => _minigameComplete);

        game.OnGameEnd -= OnMinigameEnded;

        // Write result to Yarn
        _dialogueRunner.VariableStorage.SetValue(VAR_MINIGAME_WIN, _minigameSuccess);

        Debug.Log($"[GameLoop] Minigame '{type}' ended — success: {_minigameSuccess}");
    }

    /// <summary>
    /// <<serve_cocktail>>
    /// Compares the player's cocktail against the target recipe,
    /// writes $satisfaction ("Perfect" / "Acceptable" / "Fail") to Yarn,
    /// then resets the shaker for the next round.
    ///
    /// Example Yarn:
    ///   <<serve_cocktail>>
    ///   <<if $satisfaction == "Perfect">>
    ///       Customer: Wow, this is amazing!
    ///   <<elseif $satisfaction == "Acceptable">>
    ///       Customer: It's decent, thanks.
    ///   <<else>>
    ///       Customer: This doesn't taste right...
    ///   <<endif>>
    /// </summary>
    private void ServeCocktail()
    {
        Satisfaction result = _cocktailSystem.CalculateSatisfaction();

        _dialogueRunner.VariableStorage.SetValue(VAR_SATISFACTION, result.ToString());

        Debug.Log($"[GameLoop] Cocktail served — satisfaction: {result}");

        _cocktailShaker.ResetShaker();
    }

    /// <summary>
    /// <<reset_shaker>>
    /// Clears the shaker without serving (e.g. player cancels).
    /// </summary>
    private void ResetShaker()
    {
        _cocktailShaker.ResetShaker();
        Debug.Log("[GameLoop] Shaker reset.");
    }

    // ── Private Helpers ────────────────────────────────────

    private void OnMinigameEnded(bool success)
    {
        _minigameSuccess  = success;
        _minigameComplete = true;
    }

    private BaseMiniGame SelectMinigame(string type) => type switch
    {
        "Shaking" => _minigameSystem.GetShakingMinigame(),
        "Mixing"  => _minigameSystem.GetMixingMinigame(),
        _         => null
    };
}
