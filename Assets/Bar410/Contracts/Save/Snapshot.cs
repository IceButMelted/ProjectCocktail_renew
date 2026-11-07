using System;
using System.Collections.Generic;

namespace Bar410.Contracts
{
    public interface ISnapshotSource
    {
        string Key { get; }
        Type StateType { get; }
        object Capture();
        void Restore(object state);
    }

    public sealed class YarnVars
    {
        public Dictionary<string, float> F = new();
        public Dictionary<string, string> S = new();
        public Dictionary<string, bool> B = new();

        /// <summary>Changed + added keys only; exact float compare.</summary>
        public static YarnVars Diff(YarnVars before, YarnVars after)
        {
            var d = new YarnVars();
            foreach (var kv in after.F) if (!before.F.TryGetValue(kv.Key, out var v) || v != kv.Value) d.F[kv.Key] = kv.Value;
            foreach (var kv in after.S) if (!before.S.TryGetValue(kv.Key, out var v) || v != kv.Value) d.S[kv.Key] = kv.Value;
            foreach (var kv in after.B) if (!before.B.TryGetValue(kv.Key, out var v) || v != kv.Value) d.B[kv.Key] = kv.Value;
            return d;
        }
    }

    public sealed class JournalEntry
    {
        public int Seq; public string Verb;
        public string Result;
        public YarnVars Writes;
    }

    public sealed class DialogueResumeState
    {
        public string RootNode;
        public int LineIndex; public string LineId;
        public List<int> OptionChoices = new();
        public List<JournalEntry> Journal = new();
        public YarnVars RootStartVars, LineVars;
        public string DayPart;
    }
}
