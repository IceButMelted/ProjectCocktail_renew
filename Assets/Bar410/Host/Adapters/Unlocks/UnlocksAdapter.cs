using System;
using System.Collections.Generic;
using Bar410.Contracts;

namespace Bar410.Host
{
    [Serializable]
    public sealed class UnlockState { public List<string> storyUnlocked = new(); public List<string> notified = new(); }

    /// <summary>IIngredientUnlocks + ISnapshotSource "unlocks" over IngredientUnlocks (G3-T6). The old class stays unchanged.</summary>
    public sealed class UnlocksAdapter : IIngredientUnlocks, ISnapshotSource, IDisposable
    {
        readonly IngredientUnlocks _unlocks;

        public UnlocksAdapter(IngredientUnlocks unlocks) { _unlocks = unlocks; _unlocks.Changed += OnChanged; }

        public void Dispose() { if (_unlocks != null) _unlocks.Changed -= OnChanged; }

        public event Action Changed;
        void OnChanged() => Changed?.Invoke();

        public bool IsUnlocked(string ingredientKey) => _unlocks.IsUnlocked(ingredientKey);
        public bool IsRecipeComplete(S_Drink drink) => _unlocks.IsRecipeComplete(drink);
        public void Unlock(string ingredientKey) => _unlocks.Unlock(ingredientKey);

        public IReadOnlyList<UnlockNotice> TakeNewUnlocks()
        {
            var list = new List<UnlockNotice>();
            foreach (var row in _unlocks.TakeNewUnlocks())
                list.Add(new UnlockNotice { key = row.id.Key, displayName = row.DisplayOrName, icon = row.icon });
            return list;
        }

        public string Key => "unlocks";
        public Type StateType => typeof(UnlockState);

        public object Capture()
        {
            var d = _unlocks.Snapshot();
            return new UnlockState { storyUnlocked = new List<string>(d.StoryUnlocked), notified = new List<string>(d.Notified) };
        }

        public void Restore(object state)
        {
            // Missing key: leave as is. A load starts on a fresh scene, so the Awake state (day-1 rows already announced) is the reset state.
            if (state is not UnlockState s) return;
            _unlocks.Restore(new SaveUnlockData { StoryUnlocked = new List<string>(s.storyUnlocked), Notified = new List<string>(s.notified) });
        }
    }
}
