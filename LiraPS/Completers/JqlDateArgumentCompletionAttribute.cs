using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text.RegularExpressions;
using Lira.Jql;
using Lira.Extensions;
using static LiraPS.StringFormatter;
using System.Diagnostics.CodeAnalysis;
using LiraPS.Extensions;
namespace LiraPS.Completers;
/// <summary>
/// Provides argument completion for JQL date parameters in PowerShell cmdlets.
/// Suggests possible date values, keywords, and relative dates based on user input,
/// enabling enhanced tab-completion and user experience for date arguments in JQL queries.
/// </summary>
internal class JqlDateArgumentCompletionAttribute : IArgumentCompleter
{
    public IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commandAst, IDictionary fakeBoundParameters)
    {
        wordToComplete = (wordToComplete ?? "").Trim();
        if (wordToComplete.Length == 0 || char.IsLetter(wordToComplete[0]))
        {
            foreach (var item in DateCompletionHelper.GetEnumCompletions(wordToComplete))
            {
                yield return item;
            }
        }
        if (DateCompletionHelper.GetIntCompletions(wordToComplete, out var intCompletion))
        {
            yield return intCompletion;
        }
        bool isStartDate = parameterName.Contains("start", StringComparison.OrdinalIgnoreCase);
        if (wordToComplete.Length == 0 || char.IsDigit(wordToComplete[0]))
        {
            var completions = DateCompletionHelper.MatchDate(wordToComplete, isStartDate);
            foreach (var item in completions)
            {
                yield return item;
            }
        }
    }

}
