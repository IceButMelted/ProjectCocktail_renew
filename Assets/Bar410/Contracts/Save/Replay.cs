using System;

namespace Bar410.Contracts
{
    /// <summary>impl: G2 DialogueDirector.</summary>
    public interface IDialogueResumeSource
    {
        bool HasRoot { get; }
        DialogueResumeState CaptureResumePoint();
        event Action LinePresented;
        event Action<string> RootStarted;
    }

    /// <summary>impl: G2 DialogueDirector.</summary>
    public interface IReplayEngine
    {
        bool IsReplaying { get; }
        void Arm(DialogueResumeState target, ReplayWatchdog watchdog);
        event Action ReachedTarget;
        event Action<ReplayAbort> Aborted;
    }

    public enum ReplayAbort { DialogueEnded, OptionQueueEmpty, UnexpectedJump, LineIdMismatch, LineBudget, StallBudget, JournalMismatch }

    public interface IVariableAccess { YarnVars GetAll(); void SetAll(YarnVars v); }
}
