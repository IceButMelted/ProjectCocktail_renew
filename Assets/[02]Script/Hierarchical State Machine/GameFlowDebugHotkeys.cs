using UnityEngine;

namespace Bar410.GameFlow
{
    // ── Debug Hotkeys ──────────────────────────────────────

    /// <summary>
    /// Editor-only dev tool: drives <see cref="GameFlowCommands"/> straight from the number
    /// row so the flow can be exercised without playing through dialogue/UI each time.
    /// Drop this on any GameObject in a scene that already has <see cref="GameFlowCommands"/>.
    ///
    /// 1 OpenBar · 2 PrepareDrinks · 3 IngredientAdded · 4 AnotherIngredient ·
    /// 5 DrinkComplete · 6 GarnishDone · 7 ServeDone · 8 RemakeDrink · 9 CloseBar ·
    /// 0 NextDay
    /// </summary>
    public class GameFlowDebugHotkeys : MonoBehaviour
    {
#if UNITY_EDITOR
        [SerializeField] private bool _showOverlay = true;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) Run("1 OpenBar", f => f.OpenBar());
            else if (Input.GetKeyDown(KeyCode.Alpha2)) Run("2 PrepareDrinks", f => f.PrepareDrinks());
            else if (Input.GetKeyDown(KeyCode.Alpha3)) Run("3 IngredientAdded", f => f.IngredientAdded());
            else if (Input.GetKeyDown(KeyCode.Alpha4)) Run("4 AnotherIngredient", f => f.AnotherIngredient());
            else if (Input.GetKeyDown(KeyCode.Alpha5)) Run("5 DrinkComplete", f => f.DrinkComplete());
            else if (Input.GetKeyDown(KeyCode.Alpha6)) Run("6 GarnishDone", f => f.GarnishDone());
            else if (Input.GetKeyDown(KeyCode.Alpha7)) Run("7 ServeDone", f => f.ServeDone());
            else if (Input.GetKeyDown(KeyCode.Alpha8)) Run("8 RemakeDrink", f => f.RemakeDrink());
            else if (Input.GetKeyDown(KeyCode.Alpha9)) Run("9 CloseBar", f => f.CloseBar());
            else if (Input.GetKeyDown(KeyCode.Alpha0)) Run("0 NextDay", f => f.NextDay());
        }

        private static void Run(string label, System.Action<GameFlowCommands> command)
        {
            var flow = GameFlowCommands.Instance;
            if (flow == null)
            {
                Debug.LogWarning($"[GameFlowDebugHotkeys] {label} ignored — no GameFlowCommands in the scene.");
                return;
            }

            Debug.Log($"[GameFlowDebugHotkeys] {label}");
            command(flow);
        }

        private void OnGUI()
        {
            if (!_showOverlay) return;

            GUILayout.BeginArea(new Rect(10, 10, 320, 220), GUI.skin.box);
            GUILayout.Label("<b>GameFlow Debug Hotkeys</b>");
            GUILayout.Label($"Phase: {GameFlowCommands.Yarn_CurrentPhase()}  Step: {GameFlowCommands.Yarn_CurrentStep()}");
            GUILayout.Space(4);
            GUILayout.Label("1 OpenBar          6 GarnishDone");
            GUILayout.Label("2 PrepareDrinks    7 ServeDone");
            GUILayout.Label("3 IngredientAdded  8 RemakeDrink");
            GUILayout.Label("4 AnotherIngredient 9 CloseBar");
            GUILayout.Label("5 DrinkComplete    0 NextDay");
            GUILayout.EndArea();
        }
#endif
    }
}
