using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(IngredientButton))]
public class IngredientButtonEditor : Editor
{
    SerializedProperty TypeIngredient;
    SerializedProperty mixer;
    SerializedProperty alcohol;

    private void OnEnable()
    {
        // Link serialized properties
        TypeIngredient = serializedObject.FindProperty("TypeIngredient");
        mixer = serializedObject.FindProperty("mixer");
        alcohol = serializedObject.FindProperty("alcohol");
    }

    public override void OnInspectorGUI()
    {
        // Update the serialized object
        serializedObject.Update();

        // Draw the default script field (read-only)
        GUI.enabled = false;
        EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((IngredientButton)target), typeof(IngredientButton), false);
        GUI.enabled = true;

        // Draw the MixerOrAlcohol enum field
        EditorGUILayout.PropertyField(TypeIngredient);

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