using System;
using System.Threading;
using Bar410.Contracts;
using Bar410.Core;
using UnityEngine;

namespace Bar410.Flow
{
    /// <summary>
    /// The day around the bar: boot → Event → Story → Open (talking mode + chapter title) → Close → next day,
    /// plus resume after a load. Every guard warns; none is silent.
    /// </summary>
    public sealed class DayRunner : IDayResume
    {
        readonly FlowPorts _ports;
        readonly SO_DaySchedule _schedule;
        readonly FlowState _state;
        readonly BartendingSession _session;
        readonly CancellationToken _lifetime;

        public DayRunner(FlowPorts ports, SO_DaySchedule schedule, FlowState state, BartendingSession session, CancellationToken lifetime)
        {
            _ports = ports; _schedule = schedule; _state = state; _session = session; _lifetime = lifetime;
            _session.DrinkServed += () => Milestone?.Invoke(FlowMilestone.DrinkServed);
        }

        /// <summary>DayStarted (Event done, story about to start; not on resume), DrinkServed, DayClosed.</summary>
        public event Action<FlowMilestone> Milestone;

        /// <summary>New game: lock the bar, wait for the opening transition, start the day.</summary>
        public async Awaitable BootAsync(CancellationToken ct)
        {
            LockBar();
            _state.SetPart(DayPart.None);
            await _ports.DayUi.WaitTransitionAsync(ct);
            await StartDayAsync(ct);
        }

        /// <summary>Camera to the bar, day events, then the story of <see cref="IStory.Day"/>.</summary>
        public async Awaitable StartDayAsync(CancellationToken ct)
        {
            CutToBar();
            await RunEventsAsync(ct);
            Milestone?.Invoke(FlowMilestone.DayStarted);
            StartTodaysStory();
        }

        /// <summary>flow_check_events: Story → Event → Story, mid-story. The story keeps running.</summary>
        public Awaitable CheckEventsAsync(CancellationToken ct)
            => Guard("flow_check_events", DayPart.Story) ? RunEventsAsync(ct) : Done();

        /// <summary>flow_open_bar: Story → Open; talking mode, then the chapter title (awaited only when <paramref name="waitForTitle"/>).</summary>
        public async Awaitable OpenBarAsync(bool waitForTitle, CancellationToken ct)
        {
            if (!Guard("flow_open_bar", DayPart.Story)) return;
            _state.SetPart(DayPart.Open);
            EnterTalkingMode();
            if (TryGetToday(out var row))
                await _ports.DayUi.ShowChapterTitleAsync(row.chapterName, waitForTitle, ct);
        }

        /// <summary>flow_close_bar: Open → Close, refused while a drink is being made.</summary>
        public bool CloseBar()
        {
            if (!Guard("flow_close_bar", DayPart.Open)) return false;
            if (_session.IsRunning) { Debug.LogWarning("[Flow] flow_close_bar ignored: a drink is being made"); return false; }
            _state.SetPart(DayPart.Close);
            Milestone?.Invoke(FlowMilestone.DayClosed);
            return true;
        }

        /// <summary>
        /// flow_next_day: Close → $day+1 → new day. Fire-and-forget: the new day's story queues behind the running
        /// dialogue, so a Yarn command that awaited it could never end (deadlock trap).
        /// </summary>
        public bool NextDay()
        {
            if (!Guard("flow_next_day", DayPart.Close)) return false;
            _ports.Story.AdvanceDay();
            FlowAwait.Run(StartDayAsync, _lifetime, "DayRunner.NextDay");
            return true;
        }

        /// <summary>
        /// After a load: waits for the transition, sets the part without Event or chapter title (Open → talking mode),
        /// then starts <paramref name="rootNode"/>. None/Event → error log + a normal day start.
        /// </summary>
        public async Awaitable ResumeAsync(DayPart part, string rootNode, CancellationToken ct)
        {
            LockBar();
            await _ports.DayUi.WaitTransitionAsync(ct);
            if (part is DayPart.None or DayPart.Event)
            {
                Debug.LogError($"[Flow] Resume at {part} is not possible; starting the day instead");
                await StartDayAsync(ct);
                return;
            }
            CutToBar();
            _ports.Bar.RefreshUnlocked();
            _state.SetPart(part);
            if (part == DayPart.Open) EnterTalkingMode();
            _ports.Story.StartStory(rootNode);
        }

        /// <summary>Step 1 (talking): bar locked, look fixed, book closed, bar shot.</summary>
        public void EnterTalkingMode()
        {
            _state.SetStep(BarStep.Idle);
            LockBar();
            _ports.Camera.Show(CameraShot.Bar);
        }

        async Awaitable RunEventsAsync(CancellationToken ct)
        {
            _state.SetPart(DayPart.Event);
            LockBar();
            _ports.Bar.RefreshUnlocked(); // a new $day may have unlocked bottles
            // A cancel by any token but the lifetime (e.g. a dialogue-scoped one) must not leave the day stuck in Event.
            try { await _ports.DayUi.ShowDayEventsAsync(ct); }
            finally { if (!_lifetime.IsCancellationRequested) _state.SetPart(DayPart.Story); }
        }

        // A new day always opens on the bar, look fixed and forward. Not on flow_check_events.
        void CutToBar()
        {
            _ports.Camera.SetLookFree(false);
            _ports.Camera.ResetLook();
            _ports.Camera.Cut(CameraShot.Bar);
        }

        void LockBar()
        {
            _ports.Bar.LockIngredients();
            _ports.Camera.SetLookFree(false);
            _ports.Bar.CloseBook();
        }

        void StartTodaysStory()
        {
            if (TryGetToday(out var row)) _ports.Story.StartStory(row.startNode);
            else Debug.LogError($"[Flow] {(_schedule != null ? _schedule.name : "Day schedule")} has no row for day {_ports.Story.Day}: story not started");
        }

        bool TryGetToday(out SO_DaySchedule.Day row)
        {
            row = null;
            return _schedule != null && _schedule.TryGet(_ports.Story.Day, out row);
        }

        bool Guard(string command, DayPart required)
        {
            if (_state.Part == required) return true;
            Debug.LogWarning($"[Flow] {command} ignored: not in {required} (now {_state.Part})");
            return false;
        }

        static Awaitable Done() { var s = new AwaitableCompletionSource(); s.SetResult(); return s.Awaitable; }
    }
}
