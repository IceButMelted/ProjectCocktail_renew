using System.Threading;
using UnityEngine;

namespace Bar410.Contracts
{
    public enum DayPart { None, Event, Story, Open, Close }

    /// <summary>impl: G2 DialogueDirector.</summary>
    public interface IStory
    {
        int Day { get; }                    // Yarn $day (Yarn is the only store)
        void AdvanceDay();                  // $day+1; called inside the Journal verb flow_next_day
        void StartStory(string node);       // queues until the runner is idle; NEVER awaited by Flow
    }

    /// <summary>impl: Host DayUiAdapter.</summary>
    public interface IDayUi
    {
        Awaitable WaitTransitionAsync(CancellationToken ct);
        Awaitable ShowDayEventsAsync(CancellationToken ct);
        Awaitable ShowChapterTitleAsync(string chapterKey, bool wait, CancellationToken ct);
    }

    /// <summary>impl: G1 DayRunner.</summary>
    public interface IDayResume
    {
        Awaitable ResumeAsync(DayPart part, string rootNode, CancellationToken ct);
    }
}
