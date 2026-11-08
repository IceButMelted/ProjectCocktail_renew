using System.Threading;
using Bar410.Contracts;
using Bar410.Core;
using UnityEngine;
using static E_Cocktail;

namespace Bar410.Host
{
    /// <summary>
    /// DEV STUB for R3 (Main_V2 before the dialogue rewrite). REPLACE with the G2 DialogueDirector in R4 and delete this file.
    /// IStory with the contract's semantics: Day is its own counter (stands in for Yarn $day), AdvanceDay adds one,
    /// StartStory is never awaited and only "opens the bar" (what every day's Yarn does with &lt;&lt;flow_open_bar&gt;&gt;).
    /// A small IMGUI panel then lets one customer arrive with an order: customer enters → preference order →
    /// serve_order (BartendingSession: ingredients → minigame → garnish → serve) → result shown as the reaction → customer leaves.
    /// </summary>
    public sealed class StubStory : MonoBehaviour, IStory
    {
        [Tooltip("Stub day counter. NOT Yarn $day: IngredientUnlocks reads $day from the DialogueRunner, so unlocks do not advance on 'Next day' (fine for R3).")]
        [SerializeField] int _day = 1;
        [SerializeField] TalkToCustomerManager _customers;
        [SerializeField] CharacterName[] _pool = { CharacterName.Walter, CharacterName.Isla, CharacterName.Owen };
        [Tooltip("TalkToCustomerManager spot the customer stands at (1-3 face the bar).")]
        [SerializeField] int _spot = 2;
        [SerializeField] float _reactionSeconds = 2.5f;

        bool _busy;
        string _line = "";

        public int Day => _day;
        public void AdvanceDay() => _day++;

        public void StartStory(string node)
        {
            Debug.Log($"[StubStory] day {_day}: story '{node}' (dev stub: opens the bar)");
            _line = $"Day {_day}";
            FlowAwait.Run(ct => GameRoot.Current.Commands.OpenBarAsync(true, ct), destroyCancellationToken, "StubStory.OpenBar", this);
        }

        async Awaitable CustomerAsync(CancellationToken ct)
        {
            var root = GameRoot.Current;
            var orders = root.Ports.Orders;
            var who = _pool[Random.Range(0, _pool.Length)];
            var view = _customers != null ? _customers.Find(who) : null;
            _busy = true;
            try
            {
                if (view != null) await FlowAwait.FromCoroutine(_customers, _customers.Enter(view, _spot, false), ct);
                var text = orders.PlaceByPreference(who, false);
                orders.SetCustomer(who);
                _line = $"{who}: {text}";

                if (!root.Session.TryBegin()) return;   // serve_order
                while (root.Session.IsRunning) await Awaitable.NextFrameAsync(ct);

                var o = orders.Order;
                _line = o.IsScored ? $"{who}: {o.Result} ({o.ServedType})" : $"{who}: (not scored)";
                await Awaitable.WaitForSecondsAsync(_reactionSeconds, ct);
                orders.ClearOrder();   // Reset_Variable at the end of a customer's node
                if (view != null) await FlowAwait.FromCoroutine(_customers, _customers.Exit(view, false), ct);
            }
            finally { _busy = false; }
        }

        void OnGUI()
        {
            var root = GameRoot.Current;
            if (root == null || root.State == null) return;
            var part = root.State.Part;
            GUILayout.BeginArea(new Rect(Screen.width - 330, 10, 320, 170), GUI.skin.box);
            GUILayout.Label($"<b>StubStory (dev)</b>  day {_day}  part {part}");
            GUILayout.Label(_line);
            GUI.enabled = part == DayPart.Open && !_busy && !root.Session.IsRunning;
            if (GUILayout.Button("Customer arrives (preference order)"))
                FlowAwait.Run(CustomerAsync, destroyCancellationToken, "StubStory.Customer", this);
            GUI.enabled = part == DayPart.Open && !_busy;
            if (GUILayout.Button("Close bar")) root.Commands.CloseBar();
            GUI.enabled = part == DayPart.Close;
            if (GUILayout.Button("Next day")) root.Commands.NextDay();
            GUI.enabled = true;
            GUILayout.EndArea();
        }
    }
}
