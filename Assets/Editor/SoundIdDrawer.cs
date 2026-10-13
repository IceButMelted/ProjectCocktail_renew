using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Dropdown of SoundData ids for fields marked [SoundId]. Falls back to a text field if no SoundData exists.</summary>
[CustomPropertyDrawer(typeof(SoundIdAttribute))]
public class SoundIdDrawer : PropertyDrawer
{
    private static SoundData s_Data;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var ids = property.propertyType == SerializedPropertyType.String ? GetIds(ResolveChannel(property)) : null;
        if (ids == null)
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        label = EditorGUI.BeginProperty(position, label, property);

        string current = property.stringValue;
        bool missing = !string.IsNullOrEmpty(current) && !ids.Contains(current);

        var options = new List<GUIContent> { new GUIContent("(none)") };
        foreach (var id in ids) options.Add(new GUIContent(id));
        if (missing) options.Add(new GUIContent(current + "  (missing)"));

        int index = string.IsNullOrEmpty(current) ? 0 : (missing ? options.Count - 1 : ids.IndexOf(current) + 1);

        EditorGUI.BeginChangeCheck();
        int picked = EditorGUI.Popup(position, label, index, options.ToArray());
        if (EditorGUI.EndChangeCheck() && !(missing && picked == options.Count - 1))
            property.stringValue = picked == 0 ? "" : ids[picked - 1];

        EditorGUI.EndProperty();
    }

    private SoundChannel ResolveChannel(SerializedProperty property)
    {
        var attr = (SoundIdAttribute)attribute;
        if (string.IsNullOrEmpty(attr.ChannelField)) return attr.Channel;

        // sibling field lives next to this one (also works inside nested structs / lists)
        string path = property.propertyPath;
        int dot = path.LastIndexOf('.');
        var sibling = property.serializedObject.FindProperty(dot < 0 ? attr.ChannelField : path.Substring(0, dot + 1) + attr.ChannelField);
        return sibling != null ? (SoundChannel)sibling.enumValueIndex : SoundChannel.SFX;
    }

    private static List<string> GetIds(SoundChannel channel)
    {
        if (s_Data == null)
        {
            var guids = AssetDatabase.FindAssets("t:SoundData");
            if (guids.Length == 0) return null;
            s_Data = AssetDatabase.LoadAssetAtPath<SoundData>(AssetDatabase.GUIDToAssetPath(guids[0]));
            if (s_Data == null) return null;
        }

        var entries = channel switch
        {
            SoundChannel.BGM => s_Data.bgms,
            SoundChannel.Ambient => s_Data.ambients,
            SoundChannel.SFX => s_Data.effects,
            SoundChannel.UiSFX => s_Data.uiSfx,
            _ => s_Data.voices,
        };

        var ids = new List<string>();
        foreach (var e in entries)
            if (!string.IsNullOrEmpty(e.id) && !ids.Contains(e.id)) ids.Add(e.id);
        return ids;
    }
}
