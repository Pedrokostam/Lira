using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ConsoleMenu;

namespace LiraPS.Completers;

internal class RecentIssuesCompleter : IArgumentCompleter, ISimpleArgumentCompleter, ICompleter
{
    public static RecentIssuesCompleter Instance = new();
    public IEnumerable<ICompleter.Completion> Complete(string item)
    {
        return CompleteArgument(item).Select(x => new ICompleter.Completion(x.CompletionText, x.ListItemText, x.ToolTip));
    }

    public IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commandAst, IDictionary fakeBoundParameters)
    {
        return CompleteArgument(wordToComplete);
    }

    public IEnumerable<CompletionResult> CompleteArgument(string wordToComplete)
    {
        wordToComplete = wordToComplete?.Trim() ?? string.Empty;
        foreach (var item in RecentIssues.GetRecentIDs())
        {
            if (string.IsNullOrWhiteSpace(wordToComplete) || item.Key.Contains(wordToComplete, StringComparison.OrdinalIgnoreCase))
            {
                yield return new CompletionResult(item.Key, item.Key, CompletionResultType.ParameterValue, item.Summary);
            }
        }
    }
}
