#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using YarnSpinner.Custom;

[CustomEditor(typeof(BubblePresenter))]
public class BubblePresenterEditor : Editor
{
    // General
    SerializedProperty _bubbleRect;
    SerializedProperty _backGroundText;
    SerializedProperty _lineText;
    SerializedProperty _characterNameText;
    SerializedProperty _bubbleContainer;
    SerializedProperty _tailImage;
    SerializedProperty _tailEdgeInset;
    SerializedProperty _useTypewriterEffect;
    SerializedProperty _typewriterSpeed;
    SerializedProperty _buttonHandler;
    SerializedProperty _defaultAlignment;

    // Mode toggle
    SerializedProperty _useWorldTargetAlignment;

    // Mode B — target found
    SerializedProperty _characterTargets;
    SerializedProperty _bubbleAboveTargetOffset;
    SerializedProperty _centerZoneFraction;
    SerializedProperty _sideShiftMultiplier;
    SerializedProperty _sideShiftExtraPixels;
    SerializedProperty _leftEdgeThreshold;
    SerializedProperty _rightEdgeThreshold;
    SerializedProperty _screenBorderPadding;

    // Mode B — fallback anchor
    SerializedProperty _fallbackCanvasAnchor;
    SerializedProperty _fallbackScreenBorderPadding;

    // Foldout state (top-level only — no nesting)
    bool _foldGeneral  = true;
    bool _foldModeB    = true;
    bool _foldFallback = true;

    void OnEnable()
    {
        _bubbleRect          = serializedObject.FindProperty("bubbleRect");
        _backGroundText      = serializedObject.FindProperty("backGroundText");
        _lineText            = serializedObject.FindProperty("lineText");
        _characterNameText   = serializedObject.FindProperty("characterNameText");
        _bubbleContainer     = serializedObject.FindProperty("bubbleContainer");
        _tailImage           = serializedObject.FindProperty("tailImage");
        _tailEdgeInset       = serializedObject.FindProperty("tailEdgeInset");
        _useTypewriterEffect = serializedObject.FindProperty("useTypewriterEffect");
        _typewriterSpeed     = serializedObject.FindProperty("typewriterSpeed");
        _buttonHandler       = serializedObject.FindProperty("buttonHandler");
        _defaultAlignment    = serializedObject.FindProperty("defaultAlignment");

        _useWorldTargetAlignment = serializedObject.FindProperty("useWorldTargetAlignment");

        _characterTargets        = serializedObject.FindProperty("characterTargets");
        _bubbleAboveTargetOffset = serializedObject.FindProperty("bubbleAboveTargetOffset");
        _centerZoneFraction      = serializedObject.FindProperty("centerZoneFraction");
        _sideShiftMultiplier     = serializedObject.FindProperty("sideShiftMultiplier");
        _sideShiftExtraPixels    = serializedObject.FindProperty("sideShiftExtraPixels");
        _leftEdgeThreshold       = serializedObject.FindProperty("leftEdgeThreshold");
        _rightEdgeThreshold      = serializedObject.FindProperty("rightEdgeThreshold");
        _screenBorderPadding     = serializedObject.FindProperty("screenBorderPadding");

        _fallbackCanvasAnchor        = serializedObject.FindProperty("fallbackCanvasAnchor");
        _fallbackScreenBorderPadding = serializedObject.FindProperty("fallbackScreenBorderPadding");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Script field (read-only)
        GUI.enabled = false;
        EditorGUILayout.ObjectField("Script",
            MonoScript.FromMonoBehaviour((BubblePresenter)target),
            typeof(BubblePresenter), false);
        GUI.enabled = true;

        EditorGUILayout.Space(4);

        // ── General (top-level foldout) ───────────────────
        _foldGeneral = EditorGUILayout.BeginFoldoutHeaderGroup(_foldGeneral, "General");
        EditorGUILayout.EndFoldoutHeaderGroup(); // always end immediately — no nesting

        if (_foldGeneral)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_bubbleRect);
            EditorGUILayout.PropertyField(_backGroundText);
            EditorGUILayout.PropertyField(_lineText);
            EditorGUILayout.PropertyField(_characterNameText);
            EditorGUILayout.PropertyField(_bubbleContainer);
            EditorGUILayout.PropertyField(_tailImage);
            EditorGUILayout.PropertyField(_tailEdgeInset);
            EditorGUILayout.Space(2);
            EditorGUILayout.PropertyField(_useTypewriterEffect);
            if (_useTypewriterEffect.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_typewriterSpeed);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space(2);
            EditorGUILayout.PropertyField(_buttonHandler);
            EditorGUILayout.PropertyField(_defaultAlignment);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(6);

        // ── Mode toggle ───────────────────────────────────
        EditorGUILayout.PropertyField(_useWorldTargetAlignment,
            new GUIContent("Use World Target Alignment  (Mode B)"));

        bool modeB = _useWorldTargetAlignment.boolValue;

        if (modeB)
        {
            EditorGUILayout.Space(4);

            // ── World Target Mapping (top-level foldout) ──
            _foldModeB = EditorGUILayout.BeginFoldoutHeaderGroup(_foldModeB,
                "World Target Mapping  (Mode B \u2014 target found)");
            EditorGUILayout.EndFoldoutHeaderGroup();

            if (_foldModeB)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_characterTargets, true);
                EditorGUILayout.Space(2);
                EditorGUILayout.PropertyField(_bubbleAboveTargetOffset,
                    new GUIContent("Above Target Offset"));
                EditorGUILayout.PropertyField(_centerZoneFraction,
                    new GUIContent("Center Zone Fraction"));

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Bubble Edge Shift", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_sideShiftMultiplier,
                    new GUIContent("Side Shift Multiplier"));
                EditorGUILayout.PropertyField(_sideShiftExtraPixels,
                    new GUIContent("Extra Pixels"));
                EditorGUILayout.PropertyField(_leftEdgeThreshold,
                    new GUIContent("Left Edge Threshold"));
                EditorGUILayout.PropertyField(_rightEdgeThreshold,
                    new GUIContent("Right Edge Threshold"));
                EditorGUI.indentLevel--;

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Screen Boundary Clamping", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_screenBorderPadding,
                    new GUIContent("Border Padding (px)"));
                EditorGUI.indentLevel--;

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(2);

            // ── Fallback Anchor (top-level foldout) ───────
            _foldFallback = EditorGUILayout.BeginFoldoutHeaderGroup(_foldFallback,
                "Fallback Canvas Anchor  (Mode B \u2014 no target found)");
            EditorGUILayout.EndFoldoutHeaderGroup();

            if (_foldFallback)
            {
                EditorGUI.indentLevel++;
                if (_fallbackCanvasAnchor.objectReferenceValue == null)
                    EditorGUILayout.HelpBox(
                        "No fallback anchor assigned. When Mode B is active but no world target " +
                        "is found, the bubble will use screen centre.",
                        MessageType.Warning);

                EditorGUILayout.PropertyField(_fallbackCanvasAnchor,
                    new GUIContent("Fallback Anchor"));
                EditorGUILayout.PropertyField(_fallbackScreenBorderPadding,
                    new GUIContent("Border Padding (px)"));
                EditorGUI.indentLevel--;
            }
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Mode A active \u2014 bubble aligns by character name rules. " +
                "Tick \"Use World Target Alignment\" to show Mode B settings.",
                MessageType.Info);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
