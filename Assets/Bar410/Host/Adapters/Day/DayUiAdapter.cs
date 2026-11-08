using System.Collections.Generic;
using System.Threading;
using Bar410.Contracts;
using Bar410.Core;
using UnityEngine;

namespace Bar410.Host
{
    /// <summary>
    /// IDayUi over PanelTransition + EventPopupQueue + IIngredientUnlocks + ChapterTitleUI (G3-T6).
    /// Any of the UI parts may be null: that step then completes at once. Text is passed through as-is until localization (R7).
    /// </summary>
    public sealed class DayUiAdapter : IDayUi
    {
        readonly MonoBehaviour _runner;   // always-active object that hosts the transition coroutine
        readonly PanelTransition _transition;
        readonly EventPopupQueue _popups;
        readonly IIngredientUnlocks _unlocks;
        readonly ChapterTitleUI _title;

        public DayUiAdapter(MonoBehaviour runner, PanelTransition transition, EventPopupQueue popups, IIngredientUnlocks unlocks, ChapterTitleUI title)
        { _runner = runner; _transition = transition; _popups = popups; _unlocks = unlocks; _title = title; }

        public async Awaitable WaitTransitionAsync(CancellationToken ct)
        {
            if (_transition == null) return;
            await FlowAwait.FromCoroutine(_runner, _transition.WaitUntilFinished(), ct);   // 5 s cap lives in the transition
        }

        public async Awaitable ShowDayEventsAsync(CancellationToken ct)
        {
            if (_popups == null) return;

            // One popup for everything unlocked since the last Event.
            var notices = _unlocks?.TakeNewUnlocks();
            if (notices != null && notices.Count > 0)
            {
                UnlockPopup.From(notices, out var title, out var body, out var icons);
                _popups.Enqueue(title, body, icons);
            }

            // Subscribe BEFORE ShowAll: an empty queue raises AllClosed synchronously.
            var done = new AwaitableCompletionSource();
            void OnClosed() => done.TrySetResult();
            _popups.AllClosed += OnClosed;
            try
            {
                _popups.ShowAll();
                await FlowAwait.Wait(done, ct);
            }
            finally { _popups.AllClosed -= OnClosed; }
        }

        public async Awaitable ShowChapterTitleAsync(string chapterKey, bool wait, CancellationToken ct)
        {
            if (_title == null) return;
            _title.Show(chapterKey);
            if (!wait) return;
            while (_title.IsPlaying) await Awaitable.NextFrameAsync(ct);   // Pause freezes the tween, so the wait stretches (correct)
        }
    }

    /// <summary>Composes the single "Unlock New Ingredient" popup (was inline in FlowWiring.OnEventEntered).</summary>
    public static class UnlockPopup
    {
        public const string Title = "Unlock New Ingredient";   // key "event.unlock.title" in R7

        public static void From(IReadOnlyList<UnlockNotice> notices, out string title, out string body, out List<Sprite> icons)
        {
            var names = new List<string>();
            icons = new List<Sprite>();
            foreach (var n in notices)
            {
                names.Add(n.displayName);
                if (n.icon != null) icons.Add(n.icon);
            }
            title = Title;
            body = string.Join("\n", names);
        }
    }
}
