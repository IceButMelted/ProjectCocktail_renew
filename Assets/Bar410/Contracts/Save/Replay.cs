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

    /// <summary>
    /// Signature only (API_G4 1.4). Logic is task G4-S2; every member throws until then.
    /// </summary>
    public sealed class ReplayWatchdog
    {
        public ReplayWatchdog(int targetIndex, string targetLineId, int extraLineBudget = 500, float stallSeconds = 5f) { }
        public enum Verdict { Continue, Target, Abort }
        public Verdict OnLine(int lineIndex, string lineId, out ReplayAbort reason) => throw new NotImplementedException("G4-S2");
        public ReplayAbort? OnNodeStart(bool isJump) => throw new NotImplementedException("G4-S2");
        public ReplayAbort? OnOptionsWithEmptyQueue() => throw new NotImplementedException("G4-S2");
        public ReplayAbort? OnDialogueComplete() => throw new NotImplementedException("G4-S2");
        public ReplayAbort? OnJournalMiss(string verb) => throw new NotImplementedException("G4-S2");
        public ReplayAbort? Tick(float realtimeNow) => throw new NotImplementedException("G4-S2");
        public void Progress(float realtimeNow) => throw new NotImplementedException("G4-S2");
    }
}
