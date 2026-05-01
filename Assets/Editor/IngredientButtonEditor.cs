using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(IngredientButton))]
public class IngredientButtonEditor : Editor
{
    // ── Serialized Properties ─────────────────────────────
    // Textures
    SerializedProperty _texDefault;
    SerializedProperty _texHover;
    SerializedProperty _texClicked;

    // Behaviour
    SerializedProperty _interactable;
    SerializedProperty _behaviour;
    SerializedProperty _mixer;
    SerializedProperty _alcohol;

    // Foldout state
    private bool _showVisuals = true;

    private void OnEnable()
    {
        _texDefault   = serializedObject.FindProperty("_texDefault");
        _texHover     = serializedObject.FindProperty("_texHover");
        _texClicked   = serializedObject.FindProperty("_texClicked");

        _interactable = serializedObject.FindProperty("_interactable");
        _behaviour    = serializedObject.FindProperty("_behaviour");
        _mixer        = serializedObject.FindProperty("_mixer");
        _alcohol      = serializedObject.FindProperty("_alcohol");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Script field (read-only)
        GUI.enabled = false;
        EditorGUILayout.ObjectField(
            "Script",
            MonoScript.FromMonoBehaviour((IngredientButton)target),
            typeof(IngredientButton),
            false
        );
        GUI.enabled = true;

        EditorGUILayout.Space(5);

        // ── Interactable Toggle ───────────────────────────
        EditorGUILayout.PropertyField(_interactable, new GUIContent("Interactable"));

        EditorGUILayout.Space(5);

        // ── Visual Settings (foldout) ─────────────────────
        _showVisuals = EditorGUILayout.Foldout(_showVisuals, "Visual Settings", true);
        if (_showVisuals)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_texDefault,  new GUIContent("Default"));
            EditorGUILayout.PropertyField(_texHover,    new GUIContent("Hover"));
            EditorGUILayout.PropertyField(_texClicked,  new GUIContent("Clicked"));
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(10);

        // ── Behaviour + Conditional Fields ───────────────
        EditorGUILayout.PropertyField(_behaviour, new GUIContent("Behaviour"));

        // Behaviour enum: None=0, Mixer=1, Alcohol=2, Shaking=3, Mixing=4, AddIce=5, Reset=6
        int enumIdx = _behaviour.enumValueIndex;
        if (enumIdx == 1) // Mixer
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_mixer, new GUIContent("Mixer Type"));
            EditorGUI.indentLevel--;
        }
        else if (enumIdx == 2) // Alcohol
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_alcohol, new GUIContent("Alcohol Type"));
            EditorGUI.indentLevel--;
        }

        // Info box for one-shot behaviours
        if (enumIdx == 5) // AddIce
        {
            EditorGUILayout.HelpBox(
                "AddIce is a one-shot button — it disables itself after the first click.",
                MessageType.Info
            );
        }

        serializedObject.ApplyModifiedProperties();
    }
}
