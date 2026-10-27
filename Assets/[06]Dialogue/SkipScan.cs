using System.Collections.Generic;
using Yarn;
using Yarn.Unity;

/// <summary>
/// Look-ahead over the compiled Yarn program: is there a drink order (the
/// flow_prepare_drinks command) ahead, and would the player meet options on the way?
/// </summary>
public static class SkipScan
{
    const string OrderCommand = "flow_prepare_drinks";

    /// <summary>
    /// Walks instructions from the line on screen, following node jumps.
    /// Branches are scanned linearly (conservative: an option in any branch counts).
    /// Returns true when the order command is reached.
    /// </summary>
    public static bool FindOrderAhead(DialogueRunner runner, out bool hasOptions)
    {
        hasOptions = false;
        var program = runner != null && runner.YarnProject != null ? runner.YarnProject.Program : null;
        if (program == null || !runner.Dialogue.IsActive) return false;
        return FindOrderAhead(program, runner.Dialogue.CurrentNode, DialogueControls.CurrentLineId, out hasOptions);
    }

    /// <summary>Same walk from an explicit node and line (a null lineId starts at the node's first instruction).</summary>
    public static bool FindOrderAhead(Yarn.Program program, string nodeName, string lineId, out bool hasOptions)
    {
        hasOptions = false;
        if (string.IsNullOrEmpty(nodeName) || !program.Nodes.TryGetValue(nodeName, out var node)) return false;

        // Start at the line on screen; fall back to the node start when it isn't found.
        int index = 0;
        int linesAhead = 0; // lines to skip; the one on screen doesn't count
        if (!string.IsNullOrEmpty(lineId))
        {
            for (int i = 0; i < node.Instructions.Count; i++)
            {
                var ins = node.Instructions[i];
                if (ins.InstructionTypeCase == Instruction.InstructionTypeOneofCase.RunLine && ins.RunLine.LineID == lineId)
                {
                    index = i;
                    linesAhead = -1;
                    break;
                }
            }
        }

        var visited = new HashSet<string> { nodeName };
        for (; index < node.Instructions.Count; index++)
        {
            var ins = node.Instructions[index];
            switch (ins.InstructionTypeCase)
            {
                case Instruction.InstructionTypeOneofCase.RunLine:
                    linesAhead++;
                    break;

                case Instruction.InstructionTypeOneofCase.AddOption:
                case Instruction.InstructionTypeOneofCase.ShowOptions:
                    hasOptions = true;
                    break;

                case Instruction.InstructionTypeOneofCase.RunCommand:
                    if (ins.RunCommand.CommandText.StartsWith(OrderCommand)) return linesAhead > 0;
                    break;

                case Instruction.InstructionTypeOneofCase.Stop:
                    return false;

                case Instruction.InstructionTypeOneofCase.RunNode:
                case Instruction.InstructionTypeOneofCase.DetourToNode:
                    string target = ins.InstructionTypeCase == Instruction.InstructionTypeOneofCase.RunNode
                        ? ins.RunNode.NodeName
                        : ins.DetourToNode.NodeName;
                    if (!visited.Add(target) || !program.Nodes.TryGetValue(target, out var next)) break;
                    node = next;
                    index = -1; // loop increment lands on 0
                    break;
            }
        }
        return false; // end of node without a jump: no order ahead
    }
}
