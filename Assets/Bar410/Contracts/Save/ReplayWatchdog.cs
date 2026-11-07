using System;

namespace Bar410.Contracts
{
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
