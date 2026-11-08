using System;
using System.Threading;
using Bar410.Contracts;
using UnityEngine;
using static E_Cocktail;

namespace Bar410.Flow
{
    /// <summary>
    /// The one door Yarn verbs, shims, hotkeys and buttons call (replaces GameFlowCommands). Plain C#, no Yarn here:
    /// replay gating, StopSkip and waitForTitle = !IsSkipping belong to the verb layer.
    /// A "button press" nobody handled warns; it is never silent.
    /// </summary>
    public sealed class FlowCommands
    {
        readonly DayRunner _day;
        readonly BartendingSession _session;
        readonly FlowState _state;

        public FlowCommands(DayRunner day, BartendingSession session, FlowState state)
        {
            _day = day; _session = session; _state = state;
        }

        // ── Day (guards and warnings live in DayRunner) ──
        public Awaitable OpenBarAsync(bool waitForTitle, CancellationToken ct) => _day.OpenBarAsync(waitForTitle, ct);
        public Awaitable CheckEventsAsync(CancellationToken ct) => _day.CheckEventsAsync(ct);
        /// <summary>Fire-and-forget; never await the new day from Yarn.</summary>
        public void NextDay() => _day.NextDay();
        public void CloseBar() => _day.CloseBar();

        // ── Bar ──
        /// <summary>flow_prepare_drinks: Talking → step 2. The caller stops Skip first.</summary>
        public void PrepareDrinks() => _session.TryBegin(); // guards and warnings live in TryBegin

        /// <summary>flow_ingredient_added [type]: selects the type (if given, valid any time), then presses "method chosen".</summary>
        public void IngredientAdded(string minigameType = "")
        {
            if (!string.IsNullOrWhiteSpace(minigameType)) SelectMinigame(minigameType);
            Press(StepCommand.IngredientAdded, "flow_ingredient_added", "AddIngredient");
        }

        public void AnotherIngredient() => Press(StepCommand.AnotherIngredient, "flow_another_ingredient", "Minigame");
        public void DrinkComplete() => Press(StepCommand.DrinkComplete, "flow_drink_complete", "Minigame");
        public void GarnishDone() => Press(StepCommand.GarnishDone, "flow_garnish_done", "Garnish");
        public void ServeDone() => Press(StepCommand.ServeDone, "flow_serve_done", "Serve");
        public void RemakeDrink() => Press(StepCommand.Remake, "flow_remake_drink", "Garnish or Serve");

        /// <summary>flow_minigame X: selection only, no step change. Unknown names warn and keep the selection.</summary>
        public void SelectMinigame(string type)
        {
            if (TryParseMinigame(type, out var t)) _state.SelectMinigame(t);
        }

        // ── Yarn functions ──
        public string Phase => _state.PhaseString;
        public string Step => _state.StepString;
        /// <summary>"None" before any selection, never "".</summary>
        public string MinigameType => _state.SelectedMinigame.ToString();
        /// <summary>"" before the first run; survives the session's end.</summary>
        public string MinigameResult => _state.LastMinigameOutcome;

        /// <summary>Enum names (case-insensitive) plus "Mixing"/"Stirring" → Stiring. Anything else warns.</summary>
        public static bool TryParseMinigame(string raw, out Enum_MiniGameType type)
        {
            type = Enum_MiniGameType.None;
            if (!string.IsNullOrWhiteSpace(raw))
            {
                var name = raw.Trim();
                if (name.Equals("Mixing", StringComparison.OrdinalIgnoreCase) || name.Equals("Stirring", StringComparison.OrdinalIgnoreCase))
                {
                    type = Enum_MiniGameType.Stiring;
                    return true;
                }
                // IsDefined: Enum.TryParse also accepts numbers ("7").
                if (Enum.TryParse(name, true, out type) && Enum.IsDefined(typeof(Enum_MiniGameType), type)) return true;
            }
            type = Enum_MiniGameType.None;
            Debug.LogWarning($"[Flow] '{raw}' is not a minigame type: selection unchanged");
            return false;
        }

        void Press(StepCommand cmd, string command, string expected)
        {
            if (_session.Handle(cmd)) return;
            var now = _state.StepString.Length > 0 ? _state.StepString : _state.Part.ToString();
            Debug.LogWarning($"[Flow] {command} ignored: not in {expected} (now {now})");
        }
    }
}
