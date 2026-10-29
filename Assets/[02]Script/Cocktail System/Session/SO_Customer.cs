// ============================================================
//  SO_Customer.cs / SO_CustomerRoster.cs — GDD §19.1.
//
//  One asset per character instead of one dictionary on a scene
//  MonoBehaviour (plan S11): editable without opening a scene, and
//  readable in a git diff.
//
//  relationshipValue is deliberately ABSENT. GDD §19.1 lists it as a
//  mirror of Yarn's $rel_<id>, but §22 makes that Yarn variable the
//  single source of truth, and an SO written at runtime keeps its
//  value between Play sessions in the Editor. D8 resolves the
//  conflict for §22 — read from DialogueRunner.VariableStorage, never here.
// ============================================================

using System;
using System.Collections.Generic;
using System.Text;
using Sirenix.OdinInspector;
using UnityEngine;
using static E_Cocktail;

[CreateAssetMenu(fileName = "Customer_New", menuName = "Bar410/Customer")]
public class SO_Customer : ScriptableObject
{
    // ── Info ───────────────────────────────────────────────

    [TabGroup("T", "Info")]
    [Tooltip("Must match the CharacterName used by dialogue and CharacterView.")]
    public CharacterName Id = CharacterName.None;

    [TabGroup("T", "Info")]
    public string CustomerName;

    [TabGroup("T", "Info")]
    [Tooltip("Colour of this character's name in the Event Log and name boxes.")]
    public Color NameColor = Color.white;

    // ── Drinks ─────────────────────────────────────────────

    [TabGroup("T", "Drinks")]
    [Tooltip("Drink TYPES this customer likes, not specific recipes (GDD §7).")]
    public List<TypeOfCocktail> PreferredDrinkTypes = new List<TypeOfCocktail>();

    // ── Visual (plan: Docs/Bar410_CharacterPresentation_Plan.md) ──

    // LEGACY VISUAL: single-sheet art. Remove when every character has layered art (Docs/Bar410_LegacyCharacterVisual_Plan.md).
    [TabGroup("T", "Visual"), ToggleLeft]
    [InfoBox("Single-sheet art: one whole-body picture per expression, plus whole-body Look Forward / Left / Right. The last command wins: an expression replaces the look picture, a look replaces the expression picture. Tick only while this character has no layered body/face art.")]
    public bool UseLegacyVisual;

    // LEGACY VISUAL: single-sheet art. Remove when every character has layered art (Docs/Bar410_LegacyCharacterVisual_Plan.md).
    [TabGroup("T", "Visual"), ShowIf(nameof(UseLegacyVisual))]
    [Tooltip("Index 0 is the default (Neutral). Whole body, facing forward. <<Isla 1>> uses the index shown here.")]
    [TableList(ShowIndexLabels = true)]
    [ValidateInput(nameof(ValidateLegacyExpressions), "Expression ids must be unique and not empty, Look, Enter or Exit.")]
    public List<LegacyExpression> LegacyExpressions = new List<LegacyExpression>();

    // LEGACY VISUAL: single-sheet art. Remove when every character has layered art (Docs/Bar410_LegacyCharacterVisual_Plan.md).
    [TabGroup("T", "Visual"), ShowIf(nameof(UseLegacyVisual)), HorizontalGroup("T/Visual/LegacyLooks"), PreviewField(80), LabelText("Look F")]
    [Tooltip("Whole body, face included. Shown by Look F / L / R until the next expression. Empty side: the other side mirrored; nothing: the current expression (warns).")]
    public Sprite LegacyLookForward;

    // LEGACY VISUAL: single-sheet art. Remove when every character has layered art (Docs/Bar410_LegacyCharacterVisual_Plan.md).
    [TabGroup("T", "Visual"), ShowIf(nameof(UseLegacyVisual)), HorizontalGroup("T/Visual/LegacyLooks"), PreviewField(80), LabelText("Look L")]
    public Sprite LegacyLookLeft;

    // LEGACY VISUAL: single-sheet art. Remove when every character has layered art (Docs/Bar410_LegacyCharacterVisual_Plan.md).
    [TabGroup("T", "Visual"), ShowIf(nameof(UseLegacyVisual)), HorizontalGroup("T/Visual/LegacyLooks"), PreviewField(80), LabelText("Look R")]
    public Sprite LegacyLookRight;

    [TabGroup("T", "Visual"), HideIf(nameof(UseLegacyVisual)), HorizontalGroup("T/Visual/Looks"), LabelText("Forward")]
    public LookSprites Forward;

    [TabGroup("T", "Visual"), HideIf(nameof(UseLegacyVisual)), HorizontalGroup("T/Visual/Looks"), LabelText("Left")]
    public LookSprites Left;

    [TabGroup("T", "Visual"), HideIf(nameof(UseLegacyVisual)), HorizontalGroup("T/Visual/Looks"), LabelText("Right")]
    public LookSprites Right;

    [TabGroup("T", "Visual"), HideIf(nameof(UseLegacyVisual))]
    [Tooltip("Index 0 is the default face (Neutral). The index shown here is the number Yarn can use: <<Isla 1>>.")]
    [TableList(ShowIndexLabels = true)]
    [ValidateInput(nameof(ValidateExpressions), "Expression ids must be unique and not empty, Look, Enter or Exit.")]
    public List<ExpressionRow> Expressions = new List<ExpressionRow>();

    /// <summary>Copy-paste Yarn commands for this character's expressions.</summary>
    [TabGroup("T", "Visual")]
    [ShowInInspector, ReadOnly, TextArea(1, 12)]
    private string ExampleCommands
    {
        get
        {
            var sb = new StringBuilder();
            for (int i = 0; i < ExpressionCount; i++)
                sb.Append($"<<{Id} {ExpressionId(i)}>>   <<{Id} {i}>>" + "\n");
            sb.Append($"<<{Id} Look L>>   <<{Id} Enter 0>>   <<{Id} Exit>>");
            return sb.ToString();
        }
    }

    /// <summary>The body sprite and face position for one facing.</summary>
    public LookSprites GetLook(Facing look) => look switch
    {
        Facing.Left => Left,
        Facing.Right => Right,
        _ => Forward
    };

    // The expression list of the active mode: callers ask these, never the lists directly.
    public int ExpressionCount => UseLegacyVisual ? LegacyExpressions.Count : Expressions.Count; // LEGACY VISUAL: single-sheet art. Remove when every character has layered art (Docs/Bar410_LegacyCharacterVisual_Plan.md).

    public string ExpressionId(int i) => UseLegacyVisual ? LegacyExpressions[i].Id : Expressions[i].Id; // LEGACY VISUAL: single-sheet art. Remove when every character has layered art (Docs/Bar410_LegacyCharacterVisual_Plan.md).

    /// <summary>Index of the expression with this id (case-insensitive), or -1.</summary>
    public int IndexOf(string id)
    {
        for (int i = 0; i < ExpressionCount; i++)
            if (string.Equals(ExpressionId(i), id, StringComparison.OrdinalIgnoreCase)) return i;
        return -1;
    }

    // LEGACY VISUAL: single-sheet art. Remove when every character has layered art (Docs/Bar410_LegacyCharacterVisual_Plan.md).
    /// <summary>
    /// The whole-body Look picture. A missing side borrows the other side mirrored (<paramref name="flip"/>);
    /// with nothing it falls back to the expression's picture and warns.
    /// </summary>
    public Sprite GetLegacyLook(int expression, Facing look, out bool flip)
    {
        flip = false;
        Sprite own = look == Facing.Left ? LegacyLookLeft : look == Facing.Right ? LegacyLookRight : LegacyLookForward;
        Sprite other = look == Facing.Left ? LegacyLookRight : look == Facing.Right ? LegacyLookLeft : null;
        if (own != null) return own;
        if (other != null) { flip = true; return other; }

        var row = LegacyExpressions[expression];
        Debug.LogWarning($"[SO_Customer] {name}: no Look {look} picture, using expression '{row.Id}'.", this);
        return row.Sprite;
    }

    /// <summary>
    /// The face for an expression and facing. A missing side face borrows the other side's, mirrored
    /// (<paramref name="flip"/>); with neither it falls back to Forward and warns.
    /// </summary>
    public Sprite GetFace(int expression, Facing look, out bool flip)
    {
        flip = false;
        var row = Expressions[expression];
        if (look == Facing.Forward) return row.Forward;

        Sprite own = look == Facing.Left ? row.Left : row.Right;
        Sprite other = look == Facing.Left ? row.Right : row.Left;
        if (own != null) return own;
        if (other != null) { flip = true; return other; }

        Debug.LogWarning($"[SO_Customer] {name}: expression '{row.Id}' has no Left/Right face, using Forward.", this);
        return row.Forward;
    }

    private bool ValidateExpressions(List<ExpressionRow> rows, ref string message)
        => ValidateIds(rows.ConvertAll(r => r.Id), ref message);

    // LEGACY VISUAL: single-sheet art. Remove when every character has layered art (Docs/Bar410_LegacyCharacterVisual_Plan.md).
    private bool ValidateLegacyExpressions(List<LegacyExpression> rows, ref string message)
        => ValidateIds(rows.ConvertAll(r => r.Id), ref message);

    private static bool ValidateIds(List<string> ids, ref string message)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string id in ids)
        {
            if (string.IsNullOrWhiteSpace(id)) { message = "An expression id is empty."; return false; }
            if (id == "Look" || id == "Enter" || id == "Exit") { message = $"'{id}' is a Yarn short-form verb and cannot be an expression id."; return false; }
            if (!seen.Add(id)) { message = $"Duplicate expression id '{id}'."; return false; }
        }
        return true;
    }
}

/// <summary>Body sprite and face position of one facing. The face position belongs to the pose, not the expression.</summary>
[Serializable]
public struct LookSprites
{
    // v2 (2D bone rig): this field becomes a reference to the pose's rig prefab / child object.
    [Required, PreviewField(80), HideLabel]
    public Sprite Body;

    public Vector2 FaceOffset;
}

// LEGACY VISUAL: single-sheet art. Remove when every character has layered art (Docs/Bar410_LegacyCharacterVisual_Plan.md).
/// <summary>One whole-body picture, facing forward.</summary>
[Serializable]
public struct LegacyExpression
{
    public string Id;

    [Required, PreviewField(60)]
    public Sprite Sprite;
}

/// <summary>One face. Left and Right may be empty: the other side is then used mirrored.</summary>
[Serializable]
public struct ExpressionRow
{
    public string Id;

    [Required, PreviewField(50)]
    public Sprite Forward;

    [PreviewField(50)]
    public Sprite Left;

    [PreviewField(50)]
    public Sprite Right;

    /// <summary>Tells the artist which picture a side will really get.</summary>
    [ShowInInspector, ReadOnly]
    public string SideNote =>
        (Left != null && Right != null) ? "own L/R"
        : Left != null ? "Right = Left flipped"
        : Right != null ? "Left = Right flipped"
        : "L/R = Forward (warns)";
}
