using System;
using System.Collections.Generic;
using System.Threading;
using Bar410.Contracts;
using Bar410.Core;
using UnityEngine;
using static E_Cocktail;

namespace Bar410.Host
{
    /// <summary>ISnapshotSource "characters" state: plain DTO, no scene objects.</summary>
    [Serializable]
    public sealed class CharactersState { public List<CharacterState> items = new(); }

    /// <summary>
    /// ICustomerStage over TalkToCustomerManager's character half (G3-T7). Fades stay in the manager (coroutines)
    /// and are bridged with FlowAwait.FromCoroutine: on ct the wait ends with OCE, the running fade finishes by itself.
    /// Also the "characters" snapshot source (restore = no fade).
    /// </summary>
    public sealed class CustomerStage : ICustomerStage, ISnapshotSource
    {
        readonly TalkToCustomerManager _manager;

        public CustomerStage(TalkToCustomerManager manager) { _manager = manager; }

        public bool TryParse(string raw, out CharacterName who)
        {
            who = CharacterName.None;
            if (string.IsNullOrEmpty(raw)) return false;
            string name = raw.Contains('.') ? raw.Split('.')[1] : raw;
            if (Enum.TryParse(name, true, out who) && who != CharacterName.None) return true;
            Debug.LogWarning($"[CustomerStage] '{raw}' is not a character name.");
            who = CharacterName.None;
            return false;
        }

        public bool Exists(CharacterName who) => _manager.Find(who) != null;
        public bool IsPresent(CharacterName who) => _manager.Find(who) is { IsPresent: true };
        public Transform Anchor(CharacterName who) => _manager.Find(who)?.transform;

        public async Awaitable EnterAsync(CharacterName who, int spot, StageTiming timing, CancellationToken ct)
        {
            var view = _manager.Find(who);
            if (view == null) { Debug.LogWarning($"[CustomerStage] No character '{who}'."); return; }
            await FlowAwait.FromCoroutine(_manager, _manager.Enter(view, spot, timing == StageTiming.Instant), ct);
        }

        public async Awaitable ExitAsync(CharacterName who, StageTiming timing, CancellationToken ct)
        {
            var view = _manager.Find(who);
            if (view == null) { Debug.LogWarning($"[CustomerStage] No character '{who}'."); return; }
            await FlowAwait.FromCoroutine(_manager, _manager.Exit(view, timing == StageTiming.Instant), ct);
        }

        public bool SetExpression(CharacterName who, string idOrIndex) => _manager.Find(who)?.SetExpression(idOrIndex) ?? false;
        public void SetLook(CharacterName who, Facing look) => _manager.Find(who)?.SetLook(look);
        public void SetSpeaking(CharacterName who, bool speaking) => _manager.Find(who)?.SetSpeaking(speaking);

        // ── ISnapshotSource "characters" ───────────────────

        public string Key => "characters";
        public Type StateType => typeof(CharactersState);

        public object Capture()
        {
            var s = new CharactersState();
            foreach (var kv in _manager.Snapshot())
                s.items.Add(new CharacterState { name = kv.Key, present = kv.Value.isPresent, spot = kv.Value.spot, expression = kv.Value.expressionIndex, look = kv.Value.look });
            return s;
        }

        public void Restore(object state)
        {
            var saved = new Dictionary<CharacterName, NPCData>();
            if (state is CharactersState s)
                foreach (var c in s.items)
                    saved[c.name] = new NPCData { isPresent = c.present, spot = c.spot, expressionIndex = c.expression, look = c.look };
            else   // key missing: everyone outside the shop
                foreach (var kv in _manager.Snapshot()) saved[kv.Key] = new NPCData { isPresent = false, spot = kv.Value.spot };
            _manager.Restore(saved);
        }
    }
}
