// ============================================================
//  CharacterView.cs — One person at the bar, as the scene sees them:
//  a body, a face, and the three things that change over time
//  (expression, facing, whether they are in the shop).
//
//  Knows nothing about Yarn or the game flow. TalkToCustomerManager
//  finds a view by name and drives it (see TalkToCustomerManager.Characters.cs).
//  The pictures and their positions come from the character's SO_Customer.
//  Plan: Docs/Bar410_CharacterPresentation_Plan.md
// ============================================================

using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using static E_Cocktail;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>Which way a character faces. Left and Right are the player's screen, not the character's.</summary>
public enum Facing { Forward, Left, Right }

/// <summary>Small up-and-down steps while a character fades in or out, as if walking. Default = no motion.</summary>
[System.Serializable]
public struct WalkBob
{
    [Tooltip("Height of each step in metres. 0 = no motion.")]
    [Min(0f)] public float Height;
    [Tooltip("Steps (hops) during one fade.")]
    [Min(0)] public int Steps;
}

/// <summary>A slow, small up-and-down while a character stands at a spot (breathing). Default = still.</summary>
[System.Serializable]
public struct IdleBob
{
    [Tooltip("Height of the movement in metres. 0 = still.")]
    [Min(0f)] public float Height;
    [Tooltip("Up-and-down cycles per second.")]
    [Min(0f)] public float Frequency;
}

public class CharacterView : MonoBehaviour
{
    [SerializeField, Required] private SO_Customer _data;
    [SerializeField, Required] private SpriteRenderer _body;
    [SerializeField, Required] private SpriteRenderer _face;

    /// <summary>The character's name, from the data asset.</summary>
    public CharacterName Name => _data != null ? _data.Id : CharacterName.None;
    public bool UsesLegacyVisual => _data != null && _data.UseLegacyVisual; // LEGACY VISUAL: single-sheet art. Remove when every character has layered art (Docs/Bar410_LegacyCharacterVisual_Plan.md).

    public int ExpressionIndex { get; private set; }
    public Facing Look { get; private set; }
    public bool IsPresent { get; private set; }
    // LEGACY VISUAL: single-sheet art. Remove when every character has layered art (Docs/Bar410_LegacyCharacterVisual_Plan.md).
    /// <summary>Legacy art only: true while the last command was a look (its picture shows), false after an expression.</summary>
    public bool LegacyShowsLook { get; private set; }
    /// <summary>Index into the manager's spots while present, otherwise the last one used.</summary>
    public int Spot { get; set; }
    /// <summary>Movement while standing at a spot. Set by TalkToCustomerManager for every character.</summary>
    public IdleBob Idle { get; set; }

    // Idle movement: where the character stands, and since when (so it starts at 0, right where the fade in ends).
    private bool _idling;
    private Vector3 _standing;
    private float _idleSince;

    private void Awake()
    {
        // The scene is authored with everyone visible; at runtime they start outside the shop.
        Apply();
        SetTint(0f);
    }

    private void Update()
    {
        if (!_idling) return;
        float t = (Time.time - _idleSince) * Idle.Frequency * 2f * Mathf.PI;
        transform.position = _standing + Vector3.up * (Idle.Height * Mathf.Sin(t));
    }

    private void StartIdle()
    {
        _standing = transform.position;
        _idleSince = Time.time;
        _idling = true;
    }

    /// <summary>Puts the character back on its standing position and stops the idle movement.</summary>
    private void StopIdle()
    {
        if (!_idling) return;
        transform.position = _standing;
        _idling = false;
    }

    // ── Look ───────────────────────────────────────────────

    /// <summary>
    /// Sets the expression by number (index into the data's list) or by id (case-insensitive).
    /// An unknown one warns and keeps the current face.
    /// </summary>
    public bool SetExpression(string idOrIndex)
    {
        int index = int.TryParse(idOrIndex, out int n) ? n : _data.IndexOf(idOrIndex);
        if (index < 0 || index >= _data.ExpressionCount)
        {
            Debug.LogWarning($"[CharacterView] {Name} has no expression '{idOrIndex}'.", this);
            return false;
        }

        ExpressionIndex = index;
        if (_data.UseLegacyVisual) { Look = Facing.Forward; LegacyShowsLook = false; } // LEGACY VISUAL: single-sheet art. Remove when every character has layered art (Docs/Bar410_LegacyCharacterVisual_Plan.md).
        Apply();
        return true;
    }

    public void SetLook(Facing look)
    {
        Look = look;
        LegacyShowsLook = true; // LEGACY VISUAL: single-sheet art. Remove when every character has layered art (Docs/Bar410_LegacyCharacterVisual_Plan.md).
        Apply();
    }

    /// <summary>
    /// The only place that changes sprites, flip and face position.
    /// v2 (2D bone rig): becomes SpriteResolver.SetCategoryAndLabel("Face", id) for the face, and
    /// switches on the child object of the current look instead of swapping the body sprite.
    /// </summary>
    public void Apply()
    {
        if (_data == null || _data.ExpressionCount == 0) return;

        // LEGACY VISUAL: single-sheet art. Remove when every character has layered art (Docs/Bar410_LegacyCharacterVisual_Plan.md).
        if (_data.UseLegacyVisual)
        {
            _face.sprite = null;
            int expression = Mathf.Clamp(ExpressionIndex, 0, _data.ExpressionCount - 1);
            bool legacyFlip = false;
            _body.sprite = LegacyShowsLook
                ? _data.GetLegacyLook(expression, Look, out legacyFlip)
                : _data.LegacyExpressions[expression].Sprite;
            _body.flipX = legacyFlip;
            return;
        }

        var look = _data.GetLook(Look);
        _body.sprite = look.Body;
        _face.sprite = _data.GetFace(Mathf.Clamp(ExpressionIndex, 0, _data.ExpressionCount - 1), Look, out bool flip);
        _face.flipX = flip;

        var p = _face.transform.localPosition;
        _face.transform.localPosition = new Vector3(look.FaceOffset.x, look.FaceOffset.y, p.z);
    }

    // ── Presence ───────────────────────────────────────────

    /// <summary>Puts the character in the shop at once, without a fade.</summary>
    public void ShowInstant()
    {
        IsPresent = true;
        SetTint(1f);
        StartIdle();
    }

    /// <summary>Takes the character out of the shop at once, without a fade.</summary>
    public void HideInstant()
    {
        StopIdle();
        IsPresent = false;
        SetTint(0f);
    }

    /// <summary>
    /// Silhouette in: transparent black fades to solid black, then black to the real colours.
    /// Yarn waits on this. v2 (2D bone rig): play the enter motion alongside, the wait stays.
    /// </summary>
    public IEnumerator FadeIn(float duration, WalkBob bob = default)
    {
        return Fade(0f, 1f, duration, bob);
    }

    /// <summary>The reverse of <see cref="FadeIn"/>. v2 (2D bone rig): play the exit motion alongside.</summary>
    public IEnumerator FadeOut(float duration, WalkBob bob = default)
    {
        return Fade(1f, 0f, duration, bob);
    }

    private IEnumerator Fade(float from, float to, float duration, WalkBob bob)
    {
        StopIdle(); // back on the spot itself before the walk starts from it
        SetTint(from);
        Vector3 standing = transform.position;
        if (duration > 0f)
        {
            // One number drives every renderer under the character, so a v2 rig fades together too.
            // Do not key SpriteRenderer colour in animation clips: this tint would fight them.
            // The same progress bobs the character: |sin| gives one small hop per step, 0 at both ends.
            yield return DOVirtual.Float(0f, 1f, duration, p =>
            {
                SetTint(Mathf.Lerp(from, to, p));
                transform.position = standing + Vector3.up * (bob.Height * Mathf.Abs(Mathf.Sin(p * bob.Steps * Mathf.PI)));
            }).SetEase(Ease.Linear).WaitForCompletion();
        }
        transform.position = standing;
        SetTint(to);
        IsPresent = to > 0f;
        if (IsPresent) StartIdle(); // carries on from the fade in, starting at 0
    }

    /// <summary>0 = invisible, 0.5 = solid black, 1 = normal colours.</summary>
    private void SetTint(float t)
    {
        Color c = t < 0.5f ? new Color(0f, 0f, 0f, t * 2f) : Color.Lerp(Color.black, Color.white, (t - 0.5f) * 2f);
        foreach (var r in GetComponentsInChildren<SpriteRenderer>(true)) r.color = c;
    }

#if UNITY_EDITOR
    // ── Edit-mode preview (Undo works, never runs while playing) ──

    private IEnumerable<string> ExpressionIds()
    {
        if (_data != null) for (int i = 0; i < _data.ExpressionCount; i++) yield return _data.ExpressionId(i);
    }

    [SerializeField, ValueDropdown(nameof(ExpressionIds)), HideInPlayMode, PropertyOrder(10)]
    private string _previewExpression = "Neutral";
    [SerializeField, HideInPlayMode, PropertyOrder(10)]
    private Facing _previewLook;

    [Button("Preview"), HideInPlayMode, HideIf(nameof(UsesLegacyVisual)), PropertyOrder(11)]
    private void Preview()
    {
        if (_data == null) return;
        int index = _data.IndexOf(_previewExpression);
        if (index < 0) { Debug.LogWarning($"[CharacterView] No expression '{_previewExpression}'.", this); return; }

        Undo.RecordObjects(new Object[] { this, _body, _face, _face.transform }, "Preview character");
        ExpressionIndex = index;
        Look = _previewLook;
        Apply();
        EditorUtility.SetDirty(this);
    }

    // LEGACY VISUAL: single-sheet art. Remove when every character has layered art (Docs/Bar410_LegacyCharacterVisual_Plan.md).
    // Legacy art shows one picture: the last command wins, so expression and look preview separately.
    [ButtonGroup("LegacyPreview"), Button("Preview Expression"), HideInPlayMode, ShowIf(nameof(UsesLegacyVisual)), PropertyOrder(11)]
    private void PreviewLegacyExpression()
    {
        Undo.RecordObjects(new Object[] { this, _body, _face }, "Preview character");
        SetExpression(_previewExpression);
        EditorUtility.SetDirty(this);
    }

    // LEGACY VISUAL: single-sheet art. Remove when every character has layered art (Docs/Bar410_LegacyCharacterVisual_Plan.md).
    [ButtonGroup("LegacyPreview"), Button("Preview Look"), HideInPlayMode, ShowIf(nameof(UsesLegacyVisual)), PropertyOrder(11)]
    private void PreviewLegacyLook()
    {
        Undo.RecordObjects(new Object[] { this, _body, _face }, "Preview character");
        SetLook(_previewLook);
        EditorUtility.SetDirty(this);
    }
#endif
}
