#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom Inspector for <see cref="IngredientButtonUI"/>.
///
/// Shows only the ingredient field relevant to the selected <see cref="IngredientButtonUI.ButtonAction"/>,
/// keeping the Inspector clean and preventing accidental misconfiguration.
///
/// Place this file anywhere inside an Editor/ folder.
/// </summary>
[CustomEditor(typeof(IngredientButtonUI))]
public class IngredientButtonUIEditor : Editor
{
    // ── Serialized Properties ─────────────────────────────
    private SerializedProperty _action;
    private SerializedProperty _mixer;
    private SerializedProperty _alcohol;
    private SerializedProperty _liqueur;

    // ── Label colours ─────────────────────────────────────
    private static readonly Color HeaderColour  = new Color(0.25f, 0.55f, 0.90f);
    private static readonly Color SectionColour = new Color(0.18f, 0.18f, 0.18f);

    private void OnEnable()
    {
        _action  = serializedObject.FindProperty("_action");
        _mixer   = serializedObject.FindProperty("_mixer");
        _alcohol = serializedObject.FindProperty("_alcohol");
        _liqueur = serializedObject.FindProperty("_liqueur");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        base.DrawHeader();
        DrawActionField();
        DrawIngredientField();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawActionField()
    {
        EditorGUILayout.LabelField("Behaviour", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(_action, new GUIContent(
            "Button Action",
            "What this button does when clicked.\n" +
            "Wire Button.OnClick() → Invoke() and this field drives the behaviour."));

    }

    private void DrawIngredientField()
    {
        var action = (IngredientButtonUI.ButtonAction)_action.enumValueIndex;

        // Actions that need no ingredient field
        if (action is IngredientButtonUI.ButtonAction.None
                    or IngredientButtonUI.ButtonAction.SetShaking
                    or IngredientButtonUI.ButtonAction.SetMixing
                    or IngredientButtonUI.ButtonAction.AddIce
                    or IngredientButtonUI.ButtonAction.ResetShaker)
        {
            DrawInfoBox(GetActionDescription(action));
            return;
        }

        // Actions that need an ingredient field
        EditorGUILayout.LabelField("Ingredient", EditorStyles.boldLabel);
        switch (action)
        {
            case IngredientButtonUI.ButtonAction.AddMixer:
                EditorGUILayout.PropertyField(_mixer,
                    new GUIContent("Mixer", "The Mixer enum value added on click."));
                break;

            case IngredientButtonUI.ButtonAction.AddAlcohol:
                EditorGUILayout.PropertyField(_alcohol,
                    new GUIContent("Base Spirit", "The BaseSpirit enum value added on click."));
                break;

            case IngredientButtonUI.ButtonAction.AddLiqueur:
                EditorGUILayout.PropertyField(_liqueur,
                    new GUIContent("Liqueur", "The Liqueur enum value added on click."));
                break;
        }

        //EditorGUI.indentLevel--;
        //EditorGUILayout.Space(4);
        DrawInfoBox(GetActionDescription(action));
    }

    // ── Helpers ───────────────────────────────────────────

    private static void DrawInfoBox(string message)
    {
        EditorGUILayout.HelpBox(message, MessageType.Info);
    }

    private static string GetActionDescription(IngredientButtonUI.ButtonAction action)
        => action switch
        {
            IngredientButtonUI.ButtonAction.None       => "No action assigned.",
            IngredientButtonUI.ButtonAction.AddMixer    => "Fires OnAddMixer + OnAddIngredient on the shaker.",
            IngredientButtonUI.ButtonAction.AddAlcohol  => "Fires OnAddAlcohol + OnAddIngredient on the shaker.",
            IngredientButtonUI.ButtonAction.AddLiqueur  => "Fires OnAddLiqueur + OnAddIngredient on the shaker.",
            IngredientButtonUI.ButtonAction.SetShaking  => "Sets the preparation method to Shaking. No ingredient needed.",
            IngredientButtonUI.ButtonAction.SetMixing   => "Sets the preparation method to Mixing. No ingredient needed.",
            IngredientButtonUI.ButtonAction.AddIce      => "Adds ice and disables this button (one-shot). No ingredient needed.",
            IngredientButtonUI.ButtonAction.ResetShaker => "Fires OnResetedCocktail on the shaker. No ingredient needed.",
            _                                           => string.Empty
        };
}
#endif
