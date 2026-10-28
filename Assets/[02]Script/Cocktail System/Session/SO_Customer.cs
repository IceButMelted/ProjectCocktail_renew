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

    [TabGroup("T", "Visual"), HorizontalGroup("T/Visual/Looks"), LabelText("Forward")]
    public LookSprites Forward;

    [TabGroup("T", "Visual"), HorizontalGroup("T/Visual/Looks"), LabelText("Left")]
    public LookSprites Left;

    [TabGroup("T", "Visual"), HorizontalGroup("T/Visual/Looks"), LabelText("Right")]
    public LookSprites Right;

    [TabGroup("T", "Visual")]
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
            for (int i = 0; i < Expressions.Count; i++)
                sb.Append($"<<{Id} {Expressions[i].Id}>>   <<{Id} {i}>>" + "\n");
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

    /// <summary>Index of the expression with this id (case-insensitive), or -1.</summary>
    public int IndexOf(string id)
    {
        for (int i = 0; i < Expressions.Count; i++)
            if (string.Equals(Expressions[i].Id, id, StringComparison.OrdinalIgnoreCase)) return i;
        return -1;
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
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            string id = row.Id;
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
