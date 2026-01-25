using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(IngredientButton))]
public class IngredientButtonEditor : Editor
{
    SerializedProperty m_Material;
    SerializedProperty T_Default;
    SerializedProperty T_Hover;
    SerializedProperty T_Clicked;

    SerializedProperty ShouldCanClick;

    SerializedProperty TypeIngredient;
    SerializedProperty mixer;
    SerializedProperty alcohol;

    private bool showMaterialSettings = true;

    private void OnEnable()
    {
        // Link serialized properties
        m_Material = serializedObject.FindProperty("m_Material");
        T_Default = serializedObject.FindProperty("T_Default");
        T_Hover = serializedObject.FindProperty("T_Hover");
        T_Clicked = serializedObject.FindProperty("T_Clicked");
        TypeIngredient = serializedObject.FindProperty("TypeIngredient");
        mixer = serializedObject.FindProperty("mixer");
        alcohol = serializedObject.FindProperty("alcohol");
        ShouldCanClick = serializedObject.FindProperty("ShouldCanClick");
    }

    public override void OnInspectorGUI()
    {
        // Update the serialized object
        serializedObject.Update();

        // Draw the default script field (read-only)
        GUI.enabled = false;
        EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((IngredientButton)target), typeof(IngredientButton), false);
        GUI.enabled = true;

        EditorGUILayout.PropertyField(ShouldCanClick);

        EditorGUILayout.Space(5);

        // Material foldout section
        showMaterialSettings = EditorGUILayout.Foldout(showMaterialSettings, "Material Settings", true);
        if (showMaterialSettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_Material);
            EditorGUILayout.PropertyField(T_Default);
            EditorGUILayout.PropertyField(T_Hover);
            EditorGUILayout.PropertyField(T_Clicked);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(10);

        // Draw the TypeIngredient enum field
        EditorGUILayout.PropertyField(TypeIngredient, new GUIContent("Type Ingredient"));

        // Get the current enum value
        int enumValue = TypeIngredient.enumValueIndex;

        // Show fields based on the enum value
        // 0 = None, 1 = Mixer, 2 = Alcohol
        if (enumValue == 1) // Mixer
        {
            EditorGUILayout.PropertyField(mixer);
        }
        else if (enumValue == 2) // Alcohol
        {
            EditorGUILayout.PropertyField(alcohol);
        }

        // Apply changes to the serialized object
        serializedObject.ApplyModifiedProperties();
    }
}