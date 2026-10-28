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

public class CharacterView : MonoBehaviour
{
    [SerializeField, Required] private SO_Customer _data;
    [SerializeField, Required] private SpriteRenderer _body;
    [SerializeField, Required] private SpriteRenderer _face;

    /// <summary>The character's name, from the data asset.</summary>
    public CharacterName Name => _data != null ? _data.Id : CharacterName.None;

    public int ExpressionIndex { get; private set; }
    public Facing Look { get; private set; }
    public bool IsPresent { get; private set; }
    /// <summary>Index into the manager's spots while present, otherwise the last one used.</summary>
    public int Spot { get; set; }

    private void Awake()
    {
        // The scene is authored with everyone visible; at runtime they start outside the shop.
        Apply();
        SetTint(0f);
    }

    // ── Look ───────────────────────────────────────────────

    /// <summary>
    /// Sets the expression by number (index into the data's list) or by id (case-insensitive).
    /// An unknown one warns and keeps the current face.
    /// </summary>
    public bool SetExpression(string idOrIndex)
    {
        int index = int.TryParse(idOrIndex, out int n) ? n : _data.IndexOf(idOrIndex);
        if (index < 0 || index >= _data.Expressions.Count)
        {
            Debug.LogWarning($"[CharacterView] {Name} has no expression '{idOrIndex}'.", this);
            return false;
        }

        ExpressionIndex = index;
        Apply();
        return true;
    }

    public void SetLook(Facing look)
    {
        Look = look;
        Apply();
    }

    /// <summary>
    /// The only place that changes sprites, flip and face position.
    /// v2 (2D bone rig): becomes SpriteResolver.SetCategoryAndLabel("Face", id) for the face, and
    /// switches on the child object of the current look instead of swapping the body sprite.
    /// </summary>
    public void Apply()
    {
        if (_data == null || _data.Expressions.Count == 0) return;

        var look = _data.GetLook(Look);
        _body.sprite = look.Body;
        _face.sprite = _data.GetFace(Mathf.Clamp(ExpressionIndex, 0, _data.Expressions.Count - 1), Look, out bool flip);
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
    }

    /// <summary>Takes the character out of the shop at once, without a fade.</summary>
    public void HideInstant()
    {
        IsPresent = false;
        SetTint(0f);
    }

    /// <summary>
    /// Silhouette in: transparent black fades to solid black, then black to the real colours.
    /// Yarn waits on this. v2 (2D bone rig): play the enter motion alongside, the wait stays.
    /// </summary>
    public IEnumerator FadeIn(float duration)
    {
        return Fade(0f, 1f, duration);
    }

    /// <summary>The reverse of <see cref="FadeIn"/>. v2 (2D bone rig): play the exit motion alongside.</summary>
    public IEnumerator FadeOut(float duration)
    {
        return Fade(1f, 0f, duration);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        SetTint(from);
        if (duration > 0f)
        {
            // One number drives every renderer under the character, so a v2 rig fades together too.
            // Do not key SpriteRenderer colour in animation clips: this tint would fight them.
            yield return DOVirtual.Float(from, to, duration, SetTint).SetEase(Ease.Linear).WaitForCompletion();
        }
        SetTint(to);
        IsPresent = to > 0f;
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
        if (_data != null) foreach (var e in _data.Expressions) yield return e.Id;
    }

    [SerializeField, ValueDropdown(nameof(ExpressionIds)), HideInPlayMode, PropertyOrder(10)]
    private string _previewExpression = "Neutral";
    [SerializeField, HideInPlayMode, PropertyOrder(10)]
    private Facing _previewLook;

    [Button("Preview"), HideInPlayMode, PropertyOrder(11)]
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
#endif
}
