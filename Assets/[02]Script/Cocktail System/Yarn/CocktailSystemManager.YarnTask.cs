// ============================================================
//  CocktailSystemManager.YarnTask.cs
//
//  Yarn COMMANDS driving the game. Command names are a public contract —
//  Day1_Demo.yarn calls <<wait_for_task SystemGame>>,
//  <<Enable_InteractableObject SystemGame false>>, <<Reset_Variable SystemGame>>
//  and <<Can_End_Shift SystemGame>>. Do not rename commands.
//
//  They are static and ignore the object name the .yarn passes: Yarn finds an
//  instance command's target with GameObject.Find, and once the object was
//  renamed ("SystemGame" -> "[CocktailSystem]") every one of those calls failed
//  with "doesn't have the correct component" and wait_for_task never waited.
//  A static command reaches the manager through Resolve() (the same cached
//  lookup the order functions use), so the name in the .yarn file, old or
//  new, no longer matters.
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

    [Header("UI Post-It Order")]
    [SerializeField] private Post_It_Order _postItOrder;

    // ── Task gate ──────────────────────────────────────────

    /// <summary>
    /// PURE query — safe to poll every frame from WaitUntil.
    /// Bug B5: old UpdateVariableInYarn() served as this predicate AND wrote 3 Yarn vars,
    /// disabled ingredient buttons, hid the Post-It on every true. Query/command now split.
    /// </summary>
    public bool IsTaskComplete => Order.IsScored;

    /// <summary>
    /// COMMAND — publishes the scored order (<see cref="OrderScored"/>, which writes it into Yarn)
    /// and closes the drink-making UI. Called once, from <see cref="UpdateVariableInYarnTrigger"/>.
    /// </summary>
    private void CommitTaskResult()
    {
        EnableButtonInYarn(false);
        OrderScored?.Invoke(Order);

        if (_postItOrder != null) _postItOrder.ClearOrder();
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
    /// &lt;&lt;wait_for_task SystemGame&gt;&gt; (the name is ignored; none is needed).
    /// Suspends Yarn until the drink has been served and scored.
    /// </summary>
    [YarnCommand("wait_for_task")]
    public static IEnumerator Yarn_WaitForTask(string target = "")
    {
        var manager = Resolve();
        if (manager == null) yield break;

        yield return manager.WaitForTask();
    }

    /// <summary>
    /// Suspends until the drink has been served and scored.
    /// Does NOT freeze Unity — the game keeps running normally.
    /// </summary>
    public IEnumerator WaitForTask()
    {
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
    public static void Yarn_CanEndShift(string target = "") => Resolve()?.CanEndShift();

    public void CanEndShift()
    {
        if (SceneLoaderBridge.IsSilentReplay) return; // skip — UI side effect
        EnableButtonInYarn(false);

        if (_endShiftBTN != null) _endShiftBTN.gameObject.SetActive(true);
        Debug.Log("[CocktailSystemManager] CanEndShift called.");
    }

    /// <summary>
    /// &lt;&lt;Enable_InteractableObject SystemGame false&gt;&gt; or &lt;&lt;Enable_InteractableObject false&gt;&gt;.
    /// The object name, when present, is ignored.
    /// </summary>
    [YarnCommand("Enable_InteractableObject")]
    public static void Yarn_EnableInteractable(string first, string second = "")
    {
        string flag = string.IsNullOrEmpty(second) ? first : second;
        if (!bool.TryParse(flag, out bool enable))
        {
            Debug.LogWarning($"[CocktailSystemManager] Enable_InteractableObject: '{flag}' is not true or false.");
            return;
        }

        Resolve()?.EnableButtonInYarn(enable);
    }

    /// <summary>
    /// Enables/disables everything the player uses to build a drink.
    /// Bug B4: used to call SetIngredientActive then re-walk the button list with a
    /// narrower component set, setting each object twice under two definitions of
    /// "interactable". Now one call, one definition (InteractableToggle).
    /// </summary>
    public void EnableButtonInYarn(bool enable)
    {
        if (SceneLoaderBridge.IsSilentReplay) return; // skip — UI side effect

        if (IngredientButtons != null) IngredientButtons.SetInteractable(enable);
        else Debug.LogWarning("[CocktailSystemManager] No IngredientButtonGroup assigned — " +
                              "Enable_InteractableObject cannot lock the ingredient buttons.", this);
    }

    [YarnCommand("Reset_Variable")]
    public static void Yarn_ResetVariable(string target = "") => Resolve()?.ResetVariableInYarn();

    /// <summary>
    /// &lt;&lt;Reset_Variable SystemGame&gt;&gt;
    /// Resets the order context and tells whoever keeps the dialogue's variables to reset them
    /// (<see cref="OrderCleared"/>).
    /// </summary>
    public void ResetVariableInYarn()
    {
        if (SceneLoaderBridge.IsSilentReplay) return; // vars already restored from save

        Order.Clear();
        OrderCleared?.Invoke();

        Debug.Log("[CocktailSystemManager] All variables reset — ready for next loop.");
    }
}
