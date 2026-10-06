// ============================================================
//  Bar410DialogueOrderTest.cs — Manual integration test.
//
//  Bar_410Demo.yarnproject's order nodes (Order_Cocktail_ByName_OutName
//  -> wait_for_task -> $satisfaction branch) predate the Garnish HSM
//  restructure and never call GameFlowCommands at all — they only rely
//  on CocktailSystemManager.ServeDrink() completing the Yarn task.
//  But BTN_Serving is now only reachable by going through the real
//  HSM flow (PrepareDrinks -> AddIngredient -> Minigame -> Garnish ->
//  Pour -> TryFinishGarnish), since Panel - Serve is gated on
//  GameFlowHooks.Serve.OnEnter. This test drives BOTH systems
//  together to check they still connect end to end.
//
//  Dev/test tool only — not part of the shipped game loop.
// ============================================================

using System.Collections;
using UnityEngine;
using Yarn.Unity;
using static E_Cocktail;

public class Bar410DialogueOrderTest : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DialogueRunner _dialogueRunner;
    [SerializeField] private CocktailSystemManager _cocktail;
    [SerializeField] private ShakerContents _shakerContents;
    [SerializeField] private Bar410.GameFlow.GameFlowCommands _commands;
    [SerializeField] private Bar410.GameFlow.GarnishFlowBridge _garnishBridge;
    [SerializeField] private UnityEngine.UI.Button _serveButton;
    [SerializeField] private SO_GlassOption _glassToUse;

    [Header("Which order node to run (see Day1_Demo.yarn)")]
    [SerializeField] private string _orderNode = "D1_02_Walter_Order";

    [SerializeField] private bool _pourExactMatch = true;

    /// <summary>Set once the run finishes (Completed or TimedOut) — for the caller/inspector to read.</summary>
    public Satisfaction LastResult { get; private set; } = Satisfaction.None;
    public string LastTargetName { get; private set; } = string.Empty;
    public bool LastRunTimedOut { get; private set; }

    [ContextMenu("Run Order Test")]
    public void RunTest() => StartCoroutine(RunTestRoutine());

    /// <summary>
    /// Nothing in this test simulates a player clicking to advance a line — Yarn's line
    /// presenter waits for that input forever otherwise. Calls DialogueRunner.RequestNextLine()
    /// on a short tick for as long as dialogue is running; harmless to call when there's nothing
    /// to advance (e.g. while an option choice is pending).
    /// </summary>
    private IEnumerator AutoAdvanceLines()
    {
        while (_dialogueRunner.IsDialogueRunning)
        {
            _dialogueRunner.RequestNextLine();
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator RunTestRoutine()
    {
        LastResult = Satisfaction.None;
        LastTargetName = string.Empty;
        LastRunTimedOut = false;

        Debug.Log($"[Bar410OrderTest] Starting node '{_orderNode}'...");
        _dialogueRunner.StartDialogue(_orderNode);
        StartCoroutine(AutoAdvanceLines());

        // 1. Wait for the yarn node's Order_Cocktail_* call to land.
        float timeout = Time.realtimeSinceStartup + 15f;
        yield return new WaitUntil(() => _cocktail.Order.HasOrder || Time.realtimeSinceStartup > timeout);

        if (!_cocktail.Order.HasOrder)
        {
            Debug.LogError("[Bar410OrderTest] Timed out waiting for an order to be placed — " +
                            "Order_Cocktail_ByName_OutName never resolved. Aborting.", this);
            LastRunTimedOut = true;
            yield break;
        }

        LastTargetName = _cocktail.GetTargetName();
        Debug.Log($"[Bar410OrderTest] Order placed: target='{LastTargetName}'");

        // 2. Drive the real Garnish HSM flow — this is the only way to make
        //    Panel - Serve (and BTN_Serving) reachable at all.
        _commands.PrepareDrinks();

        if (_pourExactMatch && _cocktail.Order.Target != null)
            PourExactMatch(_cocktail.Order.Target);

        _commands.IngredientAdded();
        _commands.DrinkComplete();

        yield return null; // let GarnishFlowBridge.OnGarnishEntered's UpdateCocktailInShaker run

        _garnishBridge.ChooseGlass(_glassToUse);
        _garnishBridge.Pour();

        timeout = Time.realtimeSinceStartup + 10f;
        yield return new WaitUntil(() => _garnishBridge.CanFinishGarnish || Time.realtimeSinceStartup > timeout);

        if (!_garnishBridge.CanFinishGarnish)
        {
            Debug.LogError("[Bar410OrderTest] Pour never finished filling — aborting before Serve.", this);
            LastRunTimedOut = true;
            yield break;
        }

        _garnishBridge.TryFinishGarnish();

        // 3. Click the real Serve button — this is what actually completes
        //    the Yarn wait_for_task AND advances the HSM back to TalkingWithCustomer.
        _serveButton.onClick.Invoke();

        // 4. Wait for the satisfaction result to resolve (set by CommitTaskResult, right when
        //    wait_for_task unblocks) — don't wait for the whole node/conversation to finish,
        //    it may go on into unrelated small talk or a later player choice.
        timeout = Time.realtimeSinceStartup + 15f;
        yield return new WaitUntil(() => _cocktail.Order.Result != Satisfaction.None || Time.realtimeSinceStartup > timeout);

        LastResult = _cocktail.Order.Result;
        Debug.Log(LastResult != Satisfaction.None
            ? $"[Bar410OrderTest] Done. Order.Result = {LastResult}"
            : "[Bar410OrderTest] Timed out waiting for a satisfaction result after Serve.");
    }

    private void PourExactMatch(S_Drink target)
    {
        foreach (var a in target.AlcoholList) _shakerContents.TryToAddAlcohol(a.Type, a.Amount);
        foreach (var l in target.LiqueurList) _shakerContents.TryToAddLiqueur(l.Type, l.Amount);
        foreach (var m in target.MixerList) _shakerContents.TryToAddMixer(m.Type, m.Amount);

        _shakerContents.SetMethod(target.PreparationMethod);
        _shakerContents.SetIce(target.AddIce);
    }
}
