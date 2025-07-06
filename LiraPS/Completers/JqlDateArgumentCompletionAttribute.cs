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
using LiraPS.Transformers;
using System.Data;
namespace LiraPS.Completers;

/// <summary>
/// Provides argument completion for JQL date parameters in PowerShell cmdlets.
/// Suggests possible date values, keywords, and relative dates based on user input,
/// enabling enhanced tab-completion and user experience for date arguments in JQL queries.
/// </summary>
internal abstract class JqlDateArgumentCompletionAttributeBase : IArgumentCompleter
{
    public abstract DateMode Mode { get; }
    public IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commandAst, IDictionary fakeBoundParameters)
    {
        wordToComplete = (wordToComplete ?? "").Trim();

        if (LiraPS.Extensions.DateTimeExtensions.TryParseDateTimeOffset(wordToComplete,out var dto))
        {
            yield return DateCompletionHelper.CreateCompletion(dto.NumericalForm(), dto.UnambiguousForm(), CompletionResultType.ParameterValue, "Current date");
        }
        if (wordToComplete.Length == 0 || char.IsLetter(wordToComplete[0]))
        {
            foreach (var item in DateCompletionHelper.GetEnumCompletions(wordToComplete, Mode))
            {
                yield return item;
            }
        }
        if (DateCompletionHelper.GetIntCompletions(wordToComplete, Mode, out var intCompletion))
        {
            yield return intCompletion;
        }
        if (wordToComplete.Length == 0 || wordToComplete.All(x => char.IsDigit(x)))
        {
            var completions = DateCompletionHelper.MatchDate(wordToComplete, Mode);
            foreach (var item in completions)
            {
                yield return item;
            }
        }
    }
}

/// <inheritdoc />
/// <remarks>String-based dates without time component will assume the start of the day.</remarks>
internal class JqlDateStartArgumentCompletionAttribute : JqlDateArgumentCompletionAttributeBase
{
    public override DateMode Mode => DateMode.Start;
}
/// <inheritdoc />
/// <remarks>String-based dates without time component will assume the end of the day.</remarks>
internal class JqlDateEndArgumentCompletionAttribute : JqlDateArgumentCompletionAttributeBase
{
    public override DateMode Mode => DateMode.End;
}
/// <inheritdoc />
/// <remarks>String-based dates without time component will assume the time of parsing.</remarks>
internal class JqlDateCurrentArgumentCompletionAttribute : JqlDateArgumentCompletionAttributeBase
{
    public override DateMode Mode => DateMode.Current;
}
