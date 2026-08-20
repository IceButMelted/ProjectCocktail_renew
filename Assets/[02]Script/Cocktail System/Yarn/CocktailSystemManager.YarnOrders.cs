// ============================================================
//  CocktailSystemManager.YarnOrders.cs
//
//  Yarn FUNCTIONS that place an order and hand the writer a string
//  to drop into a line, plus the mode-5 command GDD §12 asks for.
//
//  The four Order_Cocktail_* names are load-bearing: they are what
//  the .yarn files already call. New surface is added alongside them,
//  never in place of them.
// ============================================================

using UnityEngine;
using Yarn.Unity;
using static E_Cocktail;

public partial class CocktailSystemManager
{
    private static CocktailSystemManager _instance;

    /// <summary>
    /// Cached lookup for the static Yarn functions. The old code called
    /// FindAnyObjectByType on every single order.
    /// </summary>
    private static CocktailSystemManager Resolve()
    {
        if (_instance == null) _instance = FindAnyObjectByType<CocktailSystemManager>();
        if (_instance == null) Debug.LogError("[CocktailSystemManager] No instance in the scene — Yarn order functions will return empty.");

        return _instance;
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    // ── GDD §12 modes 3-4 — random from the customer's preferences ──

    /// <summary>Returns the name of a random cocktail suited to the given NPC.</summary>
    [YarnFunction("Order_Cocktail_OutName")]
    public static string RandomOrderCocktail_OutName(int NPC)
        => Resolve()?.PlacePreferenceOrder((NPC_Name)NPC, byFlavor: false) ?? string.Empty;

    /// <summary>Returns the description of a random cocktail suited to the given NPC.</summary>
    [YarnFunction("Order_Cocktail_OutDescription")]
    public static string RandomOrderCocktail_OutDescription(int NPC)
        => Resolve()?.PlacePreferenceOrder((NPC_Name)NPC, byFlavor: true) ?? string.Empty;

    // ── GDD §12 modes 1-2 — a named recipe ─────────────────

    /// <summary>Returns the name of a specific cocktail by its name.</summary>
    [YarnFunction("Order_Cocktail_ByName_OutName")]
    public static string OrderCocktail_OutSpecificName(string Name)
        => Resolve()?.PlaceNamedOrder(Name, byFlavor: false) ?? string.Empty;

    [YarnFunction("Order_Cocktail_ByName_OutDescription")]
    public static string OrderCocktail_OutSpecificDescription(string Name)
        => Resolve()?.PlaceNamedOrder(Name, byFlavor: true) ?? string.Empty;

    // ── GDD §12 mode 5 — a type only (plan gap S9) ─────────

    /// <summary>
    /// &lt;&lt;order_by_type SystemGame HighAlcohol&gt;&gt;
    /// The customer names a category, not a drink; anything of that type satisfies GDD §18.
    /// </summary>
    [YarnCommand("order_by_type")]
    public void OrderByTypeCommand(string drinkType)
    {
        if (!System.Enum.TryParse(drinkType, ignoreCase: true, out TypeOfCocktail parsed))
        {
            Debug.LogWarning($"[CocktailSystemManager] '{drinkType}' is not a TypeOfCocktail. Order unchanged.", this);
            return;
        }

        if (Orders.OrderByType(Order, Order.Customer, parsed))
            ResolvePostItText(parsed.ToString());
    }

    /// <summary>Names the customer for the order about to be placed. Optional.</summary>
    [YarnCommand("order_customer")]
    public void SetOrderCustomer(string npcName)
    {
        if (!System.Enum.TryParse(npcName, ignoreCase: true, out NPC_Name parsed))
        {
            Debug.LogWarning($"[CocktailSystemManager] '{npcName}' is not an NPC_Name.", this);
            return;
        }

        Order.BeginOrder(parsed, Order.Mode, Order.Target, Order.OrderedType);
    }

    // ── Read-back functions ────────────────────────────────

    [YarnFunction("order_name")]
    public static string OrderName() => Resolve()?.Order.TargetName ?? string.Empty;

    [YarnFunction("order_flavor")]
    public static string OrderFlavor() => Resolve()?.Order.TargetDescription ?? string.Empty;

    [YarnFunction("order_type")]
    public static string OrderType() => Resolve()?.Order.OrderedType.ToString() ?? string.Empty;

    [YarnFunction("order_satisfaction")]
    public static string OrderSatisfaction() => Resolve()?.Order.Result.ToString() ?? string.Empty;

    // ── Internals ──────────────────────────────────────────

    /// <summary>
    /// Places (or reuses) a preference-based order and returns the requested text.
    ///
    /// Plan bug B2: Order_Cocktail_OutName and Order_Cocktail_OutDescription each rolled a
    /// NEW random drink, so a node that called both named one cocktail and described a
    /// different one. A standing, unserved order for the same customer is now reused.
    /// </summary>
    private string PlacePreferenceOrder(NPC_Name customer, bool byFlavor)
    {
        bool reuse = Order.HasOrder && Order.Customer == customer && !Order.IsScored && Order.Target != null;

        if (!reuse && !Orders.OrderByPreference(Order, customer, byFlavor))
        {
            Debug.Log($"Can't find cocktail for NPC with ID '{customer}'");
            Order.Clear();
            ResolvePostItText("None");
            return string.Empty;
        }

        string text = byFlavor ? Order.TargetDescription : Order.TargetName;
        ResolvePostItText(text);
        return text;
    }

    /// <summary>Places (or reuses) a named order and returns the requested text.</summary>
    private string PlaceNamedOrder(string recipeName, bool byFlavor)
    {
        bool reuse = Order.HasOrder && !Order.IsScored && Order.Target != null
                     && string.Equals(Order.TargetName, recipeName, System.StringComparison.OrdinalIgnoreCase);

        if (!reuse && !Orders.OrderByName(Order, Order.Customer, recipeName, byFlavor))
        {
            Debug.Log($"Can't find cocktail with name '{recipeName}'");
            Order.Clear();
            ResolvePostItText("None");
            return string.Empty;
        }

        string text = byFlavor ? Order.TargetDescription : Order.TargetName;
        ResolvePostItText(text);
        return text;
    }

    /// <summary>
    /// Pushes the order text onto the Post-It.
    ///
    /// Plan bug B3: this used FindFirstObjectByType on every order despite _postItOrder
    /// already being a serialized reference on this component.
    /// </summary>
    private bool ResolvePostItText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning("[CocktailSystemManager] Empty text provided to SetPostItText.");
            return false;
        }

        if (_postItOrder == null)
        {
            Debug.LogWarning("[CocktailSystemManager] Post_It_Order is not assigned.", this);
            return false;
        }

        _postItOrder.SetPostItOrderText("Order : " + text);
        return true;
    }
}
