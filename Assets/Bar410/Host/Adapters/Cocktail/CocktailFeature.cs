using System;
using Bar410.Contracts;
using UnityEngine;
using static E_Cocktail;

namespace Bar410.Host
{
    /// <summary>
    /// Thin adapter over the old cocktail objects, implementing IOrderDesk + IBarCounter (G3-T4).
    /// Holds the bar-side half of FlowWiring (shaker / bottles / method panel / fill bars / post-it) and nothing else:
    /// camera, hub and look reactions belong to the stations. Events are subscribed in the ctor and removed in Dispose.
    /// ponytail: ISnapshotSource "order" is not here yet (needs the recipe key, N4, and restoring a scored order).
    /// </summary>
    public sealed class CocktailFeature : IOrderDesk, IBarCounter, IDisposable
    {
        readonly CocktailSystemManager _manager;
        readonly ShakerContents _shaker;
        readonly ShakerPanelController _panels;
        readonly IngredientButtonGroup _bottles;
        readonly MethodPanel _method;
        readonly VisualizeCocktail _bars;
        readonly Post_It_Order _note;
        readonly BookUI_V2 _book;
        readonly IIngredientUnlocks _unlocks;   // null = everything unlocked

        public CocktailFeature(CocktailSystemManager manager, ShakerContents shaker, ShakerPanelController panels,
            IngredientButtonGroup bottles, MethodPanel method, VisualizeCocktail bars, Post_It_Order note,
            BookUI_V2 book, IIngredientUnlocks unlocks)
        {
            _manager = manager; _shaker = shaker; _panels = panels; _bottles = bottles; _method = method;
            _bars = bars; _note = note; _book = book; _unlocks = unlocks;

            if (_shaker != null)
            {
                _shaker.Changed.AddListener(OnShakerChanged);
                _shaker.Cleared.AddListener(OnShakerCleared);
                _shaker.OnFulled.AddListener(OnShakerFull);
            }
            if (_method != null)
            {
                _method.ShakingChosen += OnShaking;
                _method.MixingChosen += OnMixing;
                _method.ResetChosen += OnReset;
            }
            if (_manager != null)
            {
                _manager.OrderScored += OnOrderScored;
                _manager.OrderCleared += OnOrderCleared;
            }
        }

        public void Dispose()
        {
            if (_shaker != null)
            {
                _shaker.Changed.RemoveListener(OnShakerChanged);
                _shaker.Cleared.RemoveListener(OnShakerCleared);
                _shaker.OnFulled.RemoveListener(OnShakerFull);
            }
            if (_method != null)
            {
                _method.ShakingChosen -= OnShaking;
                _method.MixingChosen -= OnMixing;
                _method.ResetChosen -= OnReset;
            }
            if (_manager != null)
            {
                _manager.OrderScored -= OnOrderScored;
                _manager.OrderCleared -= OnOrderCleared;
            }
        }

        // ── IBarCounter ────────────────────────────────────

        public bool HasRoom => _shaker != null && _shaker.HasRoom;
        public bool IsEmpty => _shaker == null || _shaker.IsEmpty;
        public Enum_MiniGameType RequiredMinigame => _shaker != null ? _shaker.RequiredMinigame : Enum_MiniGameType.None;

        public event Action ShakerFull;
        public event Action ShakerCleared;
        public event Action<Enum_MiniGameType> MethodChosen;
        public event Action ResetChosen;

        public void BeginDrink()
        {
            _shaker?.Clear();                    // raises ShakerCleared synchronously
            _panels?.ResetPermissions();
            _manager?.Order.ClearResult();
        }

        public void OpenIngredients()
        {
            RefreshUnlocked();
            _bottles?.SetInteractable(true);
            _bottles?.EnableInteractablePrepareDrinksPhase();
            // Re-entry with contents (cancelled minigame): no event fires, so bring both panels back by hand.
            _panels?.ShowMethod();
            _bars?.UpdateCocktailBars();
        }

        public void CloseIngredients()
        {
            _bottles?.SetInteractable(false);
            _panels?.HideMethod();
            _bars?.ResetVisualBars();
        }

        public void LockIngredients() => _bottles?.Disable();

        public void RefreshUnlocked()
        {
            if (_unlocks != null) _bottles?.Refresh(_unlocks.IsUnlocked);
        }

        public S_Drink ResolveDrink()
        {
            _manager?.UpdateCocktailInShaker();
            return _shaker != null ? _shaker.CurrentCocktail : null;
        }

        public void SetIce(bool on) => _shaker?.SetIce(on);

        public void SetShakerGrabbable(bool on)
        {
            if (_shaker != null) InteractableToggle.Apply(_shaker.gameObject, on);
        }

        public void ResetDrink() => _manager?.ResetCocktail();
        public void SetOrderNoteAvailable(bool on) => _note?.SetAvailable(on);
        public void CloseBook() => _book?.CloseBook();

        void OnShakerChanged() => _bars?.UpdateCocktailBars();

        void OnShakerCleared()
        {
            _bars?.ResetVisualBars();
            _panels?.HideMethod();
            _bottles?.EnableInteractablePrepareDrinksPhase();
            ShakerCleared?.Invoke();
        }

        void OnShakerFull()
        {
            _bottles?.Disable();
            _panels?.ShowMethod();
            ShakerFull?.Invoke();
        }

        void OnShaking() { _shaker?.SetMethod(Method.Shaking); MethodChosen?.Invoke(Enum_MiniGameType.Shaking); }
        void OnMixing() { _shaker?.SetMethod(Method.Stirring); MethodChosen?.Invoke(Enum_MiniGameType.Stiring); }

        void OnReset()
        {
            _manager?.ResetCocktail();           // Clear() raises Cleared -> OnShakerCleared
            _bars?.UpdateCocktailBars();
            _method?.Hide();
            ResetChosen?.Invoke();
        }

        // ── IOrderDesk ─────────────────────────────────────

        public DrinkOrderContext Order => _manager.Order;

        public string PlaceNamed(string recipeName, bool byFlavor) => _manager.PlaceNamedOrder(recipeName, byFlavor);
        public string PlaceByPreference(CharacterName customer, bool byFlavor) => _manager.PlacePreferenceOrder(customer, byFlavor);

        public bool PlaceByType(TypeOfCocktail type)
        {
            if (!_manager.Orders.OrderByType(Order, Order.Customer, type)) return false;
            _manager.ResolvePostItText(type.ToString());
            return true;
        }

        public void SetCustomer(CharacterName customer) => Order.BeginOrder(customer, Order.Mode, Order.Target, Order.OrderedType);
        public void ClearOrder() => _manager.ResetVariableInYarn();   // raises OrderCleared -> Cleared

        public bool EnsureOrder()
        {
            if (Order.HasOrder) return false;
            Debug.LogWarning("[CocktailFeature] No order placed: using a random one (the .yarn node should call an Order_Cocktail_* function).");
            _manager.RandomCocktail();
            return Order.HasOrder;
        }

        public ScoreResult CommitServe()
        {
            if (!Order.HasOrder) { _manager.ServeDrink(); return default; }   // warns; isValid=false
            if (!Order.IsScored) _manager.ServeDrink();                        // raises Scored once, via OrderScored
            return Build(Order);
        }

        public void UseRecipeFilter(Func<S_Drink, bool> canMake) => _manager.UseRecipeFilter(canMake);
        public void UsePreferences(ICustomerPreferences prefs) => _manager.UsePreferences(prefs);

        public event Action<ScoreResult> Scored;
        public event Action Cleared;

        void OnOrderScored(DrinkOrderContext order) => Scored?.Invoke(Build(order));
        void OnOrderCleared() => Cleared?.Invoke();

        static ScoreResult Build(DrinkOrderContext o) => new ScoreResult
        {
            customer = o.Customer, result = o.Result, servedType = o.ServedType, payout = o.Payout,
            relationshipDelta = o.RelationshipDelta, recipeName = o.TargetName, isValid = o.IsScored,
        };
    }
}
