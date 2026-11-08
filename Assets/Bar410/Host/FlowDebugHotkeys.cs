#if UNITY_EDITOR
using Bar410.Flow;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Bar410.Host
{
    /// <summary>
    /// Editor-only dev tool (G1-14), the FlowCommands twin of GameFlowDebugHotkeys. Drop it on any object in a scene with a GameRoot.
    /// 1 OpenBar · 2 PrepareDrinks · 3 IngredientAdded · 4 AnotherIngredient · 5 DrinkComplete · 6 GarnishDone ·
    /// 7 ServeDone · 8 RemakeDrink · 9 CloseBar · 0 NextDay. Ignored while Shift is held (Shift+1/2 are the minigame manager's keys).
    /// Debug keys are not input actions (Contracts_Final / API_G4 2.1).
    /// </summary>
    public sealed class FlowDebugHotkeys : MonoBehaviour
    {
        [SerializeField] bool _showOverlay = true;

        void Update()
        {
            var k = Keyboard.current;
            if (k == null || k.shiftKey.isPressed) return;

            if (k.digit1Key.wasPressedThisFrame) RunAsync("1 OpenBar", f => f.OpenBarAsync(false, destroyCancellationToken));
            else if (k.digit2Key.wasPressedThisFrame) Run("2 PrepareDrinks", f => f.PrepareDrinks());
            else if (k.digit3Key.wasPressedThisFrame) Run("3 IngredientAdded", f => f.IngredientAdded());
            else if (k.digit4Key.wasPressedThisFrame) Run("4 AnotherIngredient", f => f.AnotherIngredient());
            else if (k.digit5Key.wasPressedThisFrame) Run("5 DrinkComplete", f => f.DrinkComplete());
            else if (k.digit6Key.wasPressedThisFrame) Run("6 GarnishDone", f => f.GarnishDone());
            else if (k.digit7Key.wasPressedThisFrame) Run("7 ServeDone", f => f.ServeDone());
            else if (k.digit8Key.wasPressedThisFrame) Run("8 RemakeDrink", f => f.RemakeDrink());
            else if (k.digit9Key.wasPressedThisFrame) Run("9 CloseBar", f => f.CloseBar());
            else if (k.digit0Key.wasPressedThisFrame) Run("0 NextDay", f => f.NextDay());
        }

        // OpenBar is async: its overload returns an Awaitable, so run it through a tiny shim.
        static void RunAsync(string label, System.Func<FlowCommands, Awaitable> command)
            => Run(label, f => Bar410.Core.FlowAwait.Run(ct => command(f), GameRoot.Current.Lifetime, label));

        static void Run(string label, System.Action<FlowCommands> command)
        {
            var root = GameRoot.Current;
            if (root == null) { Debug.LogWarning($"[FlowDebugHotkeys] {label} ignored: no GameRoot in the scene"); return; }
            Debug.Log($"[FlowDebugHotkeys] {label}");
            command(root.Commands);
        }

        void OnGUI()
        {
            if (!_showOverlay) return;
            var s = GameRoot.Current?.State;
            GUILayout.BeginArea(new Rect(10, 10, 320, 220), GUI.skin.box);
            GUILayout.Label("<b>Flow Debug Hotkeys</b>");
            GUILayout.Label($"Phase: {s?.PhaseString}  Step: {s?.StepString}");
            GUILayout.Space(4);
            GUILayout.Label("1 OpenBar");
            GUILayout.Label("2 PrepareDrinks    6 GarnishDone");
            GUILayout.Label("3 IngredientAdded  7 ServeDone");
            GUILayout.Label("4 AnotherIngredient 8 RemakeDrink");
            GUILayout.Label("5 DrinkComplete    9 CloseBar, 0 NextDay");
            GUILayout.EndArea();
        }
    }
}
#endif
