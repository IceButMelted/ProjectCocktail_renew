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
    private SerializedProperty _shaker;
    private SerializedProperty _onPoured;
    private SerializedProperty _onRejected;

    // ── Label colours ─────────────────────────────────────
    private static readonly Color HeaderColour  = new Color(0.25f, 0.55f, 0.90f);
    private static readonly Color SectionColour = new Color(0.18f, 0.18f, 0.18f);

    private void OnEnable()
    {
        _action  = serializedObject.FindProperty("_action");
        _mixer   = serializedObject.FindProperty("_mixer");
        _alcohol = serializedObject.FindProperty("_alcohol");
        _liqueur = serializedObject.FindProperty("_liqueur");
        _shaker = serializedObject.FindProperty("_shaker");
        _onPoured = serializedObject.FindProperty("OnPoured");
        _onRejected = serializedObject.FindProperty("OnRejected");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        base.DrawHeader();
        DrawActionField();
        DrawIngredientField();
        DrawShakerField();
        DrawEventFields();

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>Which shaker this button pours into. Empty = find the one in the scene.</summary>
    private void DrawShakerField()
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Shaker", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_shaker, new GUIContent(
            "Shaker Contents",
            "The ShakerContents this button pours into.\nLeave empty to find the one in the scene on first use."));
    }

    /// <summary>
    /// Designer hooks. Only shown for the pouring actions — the other actions never raise them.
    /// </summary>
    private void DrawEventFields()
    {
        var action = (IngredientButtonUI.ButtonAction)_action.enumValueIndex;

        bool pours = action is IngredientButtonUI.ButtonAction.AddMixer
                            or IngredientButtonUI.ButtonAction.AddAlcohol
                            or IngredientButtonUI.ButtonAction.AddLiqueur;
        if (!pours) return;

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_onPoured, new GUIContent(
            "On Poured", "The pour landed — play the animation, sound, splash here."));
        EditorGUILayout.PropertyField(_onRejected, new GUIContent(
            "On Rejected", "The glass was already full (10 units) so nothing was added."));
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
            IngredientButtonUI.ButtonAction.AddMixer    => "Adds 1 unit of this Mixer to ShakerContents, then raises On Poured (or On Rejected when the glass is full).",
            IngredientButtonUI.ButtonAction.AddAlcohol  => "Adds 1 unit of this Base Spirit to ShakerContents, then raises On Poured (or On Rejected when the glass is full).",
            IngredientButtonUI.ButtonAction.AddLiqueur  => "Adds 1 unit of this Liqueur to ShakerContents, then raises On Poured (or On Rejected when the glass is full).",
            IngredientButtonUI.ButtonAction.SetShaking  => "Sets the preparation method to Shaking. No ingredient needed.",
            IngredientButtonUI.ButtonAction.SetMixing   => "Sets the preparation method to Mixing (Method.Stirring). No ingredient needed.",
            IngredientButtonUI.ButtonAction.AddIce      => "Adds ice and disables this button (one-shot). No ingredient needed.",
            IngredientButtonUI.ButtonAction.ResetShaker => "Empties the shaker — ShakerContents.Clear(), which raises its Cleared event. No ingredient needed.",
            _                                           => string.Empty
        };
}
#endif
