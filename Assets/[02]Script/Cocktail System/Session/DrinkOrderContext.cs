// ============================================================
//  DrinkOrderContext.cs — Everything known about the order the
//  player is currently working on. Plain C#, no UnityEngine.
//
//  This is the DrinkOrderContext that Bar410_StateMachine_Implementation.md
//  §8 open question 4 asks for: built up across the Open Bar steps and
//  scored at serve time.
//
//  It also replaces five scattered pieces of state that used to live on
//  CocktailSystemManager and be mutated from anywhere:
//      _targetCocktail   -> Target
//      _TaskDone         -> IsScored
//      _satisfaction     -> Result
//      _cocktailType     -> ServedType   (which nothing ever assigned — bug B1)
//      IsWaitingForTask  -> read by YarnTaskGate
// ============================================================

using System.Collections.Generic;
using static E_Cocktail;

/// <summary>GDD §12 — how the customer phrased the order. Writers pick this per Yarn node.</summary>
public enum OrderMode
{
    None,
    /// <summary>Mode 1 — a specific recipe, presented by name.</summary>
    FixedByName,
    /// <summary>Mode 2 — a specific recipe, presented by flavour description.</summary>
    FixedByFlavor,
    /// <summary>Mode 3 — random from the customer's preferred types, presented by name.</summary>
    RandomByPreferenceName,
    /// <summary>Mode 4 — random from the customer's preferred types, presented by flavour.</summary>
    RandomByPreferenceFlavor,
    /// <summary>Mode 5 — a DrinkType only. Any recipe of that type satisfies GDD §18.</summary>
    FixedByType
}

public class DrinkOrderContext
{
    private readonly List<MinigameOutcome> _minigameResults = new List<MinigameOutcome>();

    // ── The order (written when the customer asks) ─────────

    public NPC_Name Customer { get; private set; } = NPC_Name.None;
    public OrderMode Mode { get; private set; } = OrderMode.None;

    /// <summary>The recipe asked for. Null in <see cref="OrderMode.FixedByType"/>.</summary>
    public S_Drink Target { get; private set; }

    /// <summary>
    /// The type the customer expects. GDD §18 cases 3/4 need this to tell Acceptable from
    /// Fail (a); before this class existed the value was never stored anywhere, so case 4
    /// was unreachable.
    /// </summary>
    public TypeOfCocktail OrderedType { get; private set; } = TypeOfCocktail.None;

    public bool HasOrder => Mode != OrderMode.None;

    // ── The result (written at serve time) ─────────────────

    public RecipeMatch Match { get; private set; } = RecipeMatch.None;
    public Satisfaction Result { get; private set; } = Satisfaction.None;
    public TypeOfCocktail ServedType { get; private set; } = TypeOfCocktail.None;

    /// <summary>GDD §18.1 — what the customer pays.</summary>
    public float Payout { get; private set; }

    /// <summary>GDD §18.2 — how much $rel_&lt;id&gt; moves after the reaction.</summary>
    public float RelationshipDelta { get; private set; }

    public bool IsScored { get; private set; }

    public IReadOnlyList<MinigameOutcome> MinigameResults => _minigameResults;

    // ── Mutation ───────────────────────────────────────────

    /// <summary>Records a new order and clears any previous result.</summary>
    public void BeginOrder(NPC_Name customer, OrderMode mode, S_Drink target, TypeOfCocktail orderedType)
    {
        Customer = customer;
        Mode = mode;
        Target = target;
        OrderedType = orderedType;

        ClearResult();
    }

    /// <summary>Appends one minigame outcome. Called per ingredient from the minigame bridge.</summary>
    public void RecordMinigame(MinigameOutcome outcome) => _minigameResults.Add(outcome);

    /// <summary>Stores the scored result. Called once, from ServeState.</summary>
    public void Score(in RecipeMatch match, Satisfaction result, TypeOfCocktail servedType,
                      float payout, float relationshipDelta)
    {
        Match = match;
        Result = result;
        ServedType = servedType;
        Payout = payout;
        RelationshipDelta = relationshipDelta;
        IsScored = true;
    }

    /// <summary>Wipes the result but keeps the order — used when the player remakes a drink.</summary>
    public void ClearResult()
    {
        Match = RecipeMatch.None;
        Result = Satisfaction.None;
        ServedType = TypeOfCocktail.None;
        Payout = 0f;
        RelationshipDelta = 0f;
        IsScored = false;
        _minigameResults.Clear();
    }

    /// <summary>Wipes everything. Ready for the next customer.</summary>
    public void Clear()
    {
        Customer = NPC_Name.None;
        Mode = OrderMode.None;
        Target = null;
        OrderedType = TypeOfCocktail.None;
        ClearResult();
    }

    /// <summary>Display name of the order, or empty when the order is type-only.</summary>
    public string TargetName => Target != null ? Target.Name : string.Empty;

    /// <summary>Flavour description of the order, or empty when the order is type-only.</summary>
    public string TargetDescription => Target != null ? Target.Description : string.Empty;
}
