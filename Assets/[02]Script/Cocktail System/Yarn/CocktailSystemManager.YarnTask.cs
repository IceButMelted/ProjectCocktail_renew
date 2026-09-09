// ============================================================
//  CocktailSystemManager.YarnTask.cs
//
//  Yarn COMMANDS driving the game. Command names are a public contract —
//  Day1_Demo.yarn calls <<wait_for_task SystemGame>> and
//  <<Enable_InteractableObject SystemGame false>> 7x, and instance commands
//  resolve by GameObject name, so this partial must stay on "SystemGame".
//  Do not rename commands.
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

public partial class CocktailSystemManager
{
    /// <summary>
    /// True while Yarn is parked inside &lt;&lt;wait_for_task&gt;&gt;.
    /// Read by SaveLoadManager to pick checkpoint vs live line (GDD §23.1).
    /// Static per plan decision D3 to keep save system out of this refactor;
    /// proper owner is a future task.
    /// </summary>
    public static bool IsWaitingForTask { get; private set; }

    // Inspector Fields

    [Header("BTN End Shift")]
    [SerializeField] private Button _endShiftBTN;

    [Header("Yarn / Task References")]
    [SerializeField] private DialogueRunner _dialogueRunner;

    [Header("UI Post-It Order")]
    [SerializeField] private Post_It_Order _postItOrder;

    private YarnVariableSync _yarnVariables;

    /// <summary>Lazily built so it survives domain reloads and Awake ordering.</summary>
    private YarnVariableSync Variables => _yarnVariables ??= new YarnVariableSync(_dialogueRunner);

    // ── Task gate ──────────────────────────────────────────

    /// <summary>
    /// PURE query — safe to poll every frame from WaitUntil.
    /// Bug B5: old UpdateVariableInYarn() served as this predicate AND wrote 3 Yarn vars,
    /// disabled ingredient buttons, hid the Post-It on every true. Query/command now split.
    /// </summary>
    public bool IsTaskComplete => Order.IsScored;

    /// <summary>
    /// COMMAND — writes the scored order into Yarn and closes the drink-making UI.
    /// Called once, from <see cref="UpdateVariableInYarnTrigger"/>.
    /// </summary>
    private void CommitTaskResult()
    {
        EnableButtonInYarn(false);
        Variables.WriteResult(Order);
        Variables.ApplyRelationshipDelta(Order.Customer, Order.RelationshipDelta);

        if (_postItOrder != null) _postItOrder.SetOutScreen(true);
    }

    /// <summary>Scores what is in the shaker, then publishes the result. Entry point from ServeDrink().</summary>
    private void UpdateVariableInYarnTrigger()
    {
        var result = CalculateSatisfaction();
        Debug.Log($"[CocktailSystemManager] Satisfaction → {result} | payout {Order.Payout}");

        CommitTaskResult();
    }

    // Yarn Commands

    /// <summary>
    /// &lt;&lt;wait_for_task CocktailSystemManager&gt;&gt;
    /// Suspends Yarn until the drink has been served and scored.
    /// Does NOT freeze Unity — the game keeps running normally.
    /// </summary>
    [YarnCommand("wait_for_task")]
    public IEnumerator WaitForTask()
    {
        //if (_postItOrder != null) _postItOrder.ShowPostItForDefaultDuration();
        //if (_postItOrder != null) _postItOrder.ShowPostIt(); //in Gameloop Wire
        if (SceneLoaderBridge.IsSilentReplay) yield break;

        IsWaitingForTask = true;
        EnableButtonInYarn(true);

        yield return new WaitUntil(() => IsTaskComplete);

        IsWaitingForTask = false;
    }

    [YarnCommand("wait_scene")]         // use <<wait_scene 1>> in .yarn instead of <<wait 1>>
    public static IEnumerator WaitScene(float seconds)
    {
        if (SceneLoaderBridge.IsSilentReplay) yield break;
        yield return new WaitForSeconds(seconds);
    }

    [YarnCommand("Can_End_Shift")]
    public void CanEndShift()
    {
        if (SceneLoaderBridge.IsSilentReplay) return; // skip — UI side effect
        EnableButtonInYarn(false);

        if (_endShiftBTN != null) _endShiftBTN.gameObject.SetActive(true);
        Debug.Log("[CocktailSystemManager] CanEndShift called.");
    }

    /// <summary>
    /// Enables/disables everything the player uses to build a drink.
    /// Bug B4: used to call SetIngredientActive then re-walk the button list with a
    /// narrower component set, setting each object twice under two definitions of
    /// "interactable". Now one call, one definition (InteractableToggle).
    /// </summary>
    [YarnCommand("Enable_InteractableObject")]
    public void EnableButtonInYarn(bool enable)
    {
        if (SceneLoaderBridge.IsSilentReplay) return; // skip — UI side effect

        // Both refs optional: a scene may drive the shaker via IngredientButtonGroup
        // alone, no CocktailShaker. Yarn calls this 7x in Day1_Demo.yarn — an unassigned
        // ref must not crash the conversation with a NullReferenceException.
        if (_cocktailShaker != null) _cocktailShaker.Interactable = enable;

        if (IngredientButtons != null) IngredientButtons.SetInteractable(enable);
        else Debug.LogWarning("[CocktailSystemManager] No IngredientButtonGroup assigned — " +
                              "Enable_InteractableObject cannot lock the ingredient buttons.", this);
    }

    /// <summary>
    /// &lt;&lt;Reset_Variable CocktailSystemManager&gt;&gt;
    /// Resets BOTH the order context AND Yarn storage.
    /// </summary>
    [YarnCommand("Reset_Variable")]
    public void ResetVariableInYarn()
    {
        if (SceneLoaderBridge.IsSilentReplay) return; // vars already restored from save

        Order.Clear();
        Variables.Reset();

        Debug.Log("[CocktailSystemManager] All variables reset — ready for next loop.");
    }
}
