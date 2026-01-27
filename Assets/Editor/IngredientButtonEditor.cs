using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(IngredientButton))]
public class IngredientButtonEditor : Editor
{
    SerializedProperty T_Default;
    SerializedProperty T_Hover;
    SerializedProperty T_Clicked;

    SerializedProperty pointAction;
    SerializedProperty clickAction;

    SerializedProperty ShouldCanClick;
    SerializedProperty TypeIngredient;
    SerializedProperty mixer;
    SerializedProperty alcohol;

    private bool showVisualSettings = true;
    private bool showInputSettings = true;

    private void OnEnable()
    {
        T_Default = serializedObject.FindProperty("T_Default");
        T_Hover = serializedObject.FindProperty("T_Hover");
        T_Clicked = serializedObject.FindProperty("T_Clicked");

        pointAction = serializedObject.FindProperty("pointAction");
        clickAction = serializedObject.FindProperty("clickAction");

        ShouldCanClick = serializedObject.FindProperty("ShouldCanClick");
        TypeIngredient = serializedObject.FindProperty("TypeIngredient");
        mixer = serializedObject.FindProperty("mixer");
        alcohol = serializedObject.FindProperty("alcohol");
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
        EditorGUILayout.PropertyField(ShouldCanClick);

        // =========================
        // Input Settings
        // =========================
        showInputSettings = EditorGUILayout.Foldout(showInputSettings, "Input Settings", true);
        if (showInputSettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(pointAction, new GUIContent("Point Action"));
            EditorGUILayout.PropertyField(clickAction, new GUIContent("Click Action"));
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(5);

        // =========================
        // Visual Settings
        // =========================
        showVisualSettings = EditorGUILayout.Foldout(showVisualSettings, "Visual Settings", true);
        if (showVisualSettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(T_Default);
            EditorGUILayout.PropertyField(T_Hover);
            EditorGUILayout.PropertyField(T_Clicked);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(10);

        // =========================
        // Ingredient Logic
        // =========================
        EditorGUILayout.PropertyField(TypeIngredient, new GUIContent("Type Ingredient"));

        int enumValue = TypeIngredient.enumValueIndex;

        if (enumValue == 1) // Mixer
        {
            EditorGUILayout.PropertyField(mixer);
        }
        else if (enumValue == 2) // Alcohol
        {
            EditorGUILayout.PropertyField(alcohol);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
