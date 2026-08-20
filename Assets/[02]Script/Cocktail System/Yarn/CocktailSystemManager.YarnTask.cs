// ============================================================
//  CocktailSystemManager.YarnTask.cs
//
//  Yarn COMMANDS: the calls .yarn scripts make to drive the game.
//  Command names are a public contract — Day1_Demo.yarn calls
//  <<wait_for_task SystemGame>> and <<Enable_InteractableObject
//  SystemGame false>> seven times, and instance commands resolve by
//  GameObject name, so this partial must stay on the object named
//  "SystemGame". Do not rename the commands.
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

public partial class CocktailSystemManager
{
    /// <summary>
    /// True while Yarn is parked inside &lt;&lt;wait_for_task&gt;&gt;.
    /// Read by SaveLoadManager to decide whether to save the checkpoint line instead of
    /// the live line (GDD §23.1). Kept static by plan decision D3 so the save system is
    /// not dragged into this refactor; giving it a proper owner is its own task.
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
    ///
    /// Plan bug B5: the old UpdateVariableInYarn() was used as the WaitUntil predicate but
    /// also wrote three Yarn variables, disabled the ingredient buttons and hid the Post-It
    /// every time it returned true. Query and command are now separate.
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
        if (_postItOrder != null) _postItOrder.ShowPostItForDefaultDuration();
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
    /// Enables or disables everything the player uses to build a drink.
    ///
    /// Plan bug B4: this used to call SetIngredientActive and THEN walk the same button
    /// list again with a narrower set of components, setting each object twice with two
    /// different definitions of "interactable". One call now, one definition
    /// (InteractableToggle).
    /// </summary>
    [YarnCommand("Enable_InteractableObject")]
    public void EnableButtonInYarn(bool enable)
    {
        if (SceneLoaderBridge.IsSilentReplay) return; // skip — UI side effect

        _cocktailShaker.Interactable = enable;
        _cocktailShakerData.SetIngredientActive(enable);
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
