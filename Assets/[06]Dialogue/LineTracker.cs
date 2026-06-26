using Yarn.Unity;
using UnityEngine;
using System.Threading;

public class LineTracker : DialoguePresenterBase
{
    public LocalizedLine CurrentLine { get; private set; }

    public override YarnTask OnDialogueCompleteAsync()
    {
        throw new System.NotImplementedException();
    }

    public override YarnTask OnDialogueStartedAsync()
    {
        throw new System.NotImplementedException();
    }

    public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        CurrentLine = line;
        await YarnTask.CompletedTask;
    }
}