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
using Lira;
using LiraPS.Cmdlets;
using LiraPS.Extensions;

namespace LiraPS.Completers;

internal class CachedWorklogCompleter : IArgumentCompleter, ISimpleArgumentCompleter, ICompleter
{
    public static CachedWorklogCompleter Instance = new();
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
        var matching = LiraSession.GetMatchingCachedWorklogs(wordToComplete).OrderByDescending(x => x.Started);
        var placeholder = $"{LiraCmdlet.Dim}No comment{LiraCmdlet.Reset}";
        foreach (var item in matching)
        {
            var comment = string.IsNullOrWhiteSpace(item.CommentPlain) ? placeholder : item.CommentPlain;
            var tooltip = $"{item.Issue.Key} - {item.Issue.SummaryPlain} - {item.Started.NumericalForm()} - {comment}";
            yield return new CompletionResult(
                item.ID,
                $"{item.Issue.Key} {item.Started.Date:MMM-dd} {item.TimeSpent.PrettyTime()}",
                CompletionResultType.ParameterValue,
                tooltip);
        }
    }
}
