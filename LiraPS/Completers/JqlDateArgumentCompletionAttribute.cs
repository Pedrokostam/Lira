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
using ConsoleMenu;
namespace LiraPS.Completers;
/// <summary>
/// Provides argument completion for JQL date parameters in PowerShell cmdlets.
/// Suggests possible date values, keywords, and relative dates based on user input,
/// enabling enhanced tab-completion and user experience for date arguments in JQL queries.
/// </summary>
public abstract class JqlDateArgumentCompletionBase : IArgumentCompleter, ISimpleArgumentCompleter, ICompleter
{
    public abstract DateMode Mode { get; }
    public abstract bool WrapStringsWithSpaces { get; }

    public IEnumerable<ICompleter.Completion> Complete(string item)
    {
        return CompleteArgument(item).Select(x => new ICompleter.Completion(x.CompletionText, x.ListItemText, x.ToolTip));
    }
    public IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commandAst, IDictionary fakeBoundParameters) => CompleteArgument(wordToComplete);
    public IEnumerable<CompletionResult> CompleteArgument(string wordToComplete)
    {
        wordToComplete = (wordToComplete ?? "").Trim();

        if (LiraPS.Extensions.TimeExtensions.TryParseDateTimeOffset(wordToComplete, out var dto))
        {
            yield return DateCompletionHelper.CreateCompletion(dto.NumericalForm(), dto.UnambiguousForm(), CompletionResultType.ParameterValue, "Current date", !WrapStringsWithSpaces);
        }
        if (wordToComplete.Length == 0 || char.IsLetter(wordToComplete[0]))
        {
            foreach (var item in DateCompletionHelper.GetEnumCompletions(wordToComplete, Mode, !WrapStringsWithSpaces))
            {
                yield return item;
            }
        }
        if (DateCompletionHelper.GetIntCompletions(wordToComplete, Mode, out var intCompletion, !WrapStringsWithSpaces))
        {
            yield return intCompletion;
        }
        if (wordToComplete.Length == 0 || wordToComplete.All(x => char.IsDigit(x)))
        {
            var completions = DateCompletionHelper.MatchDate(wordToComplete, Mode, !WrapStringsWithSpaces);
            foreach (var item in completions)
            {
                yield return item;
            }
        }
    }
}
public class JqlDateArgumentCompleter : JqlDateArgumentCompletionBase
{
    private readonly DateMode _mode;
    private readonly bool _wrapStringsWithSpaces;

    public JqlDateArgumentCompleter(DateMode mode = DateMode.Current, bool wrapStringsWithSpaces = false)
    {
        _mode = mode;
        _wrapStringsWithSpaces = wrapStringsWithSpaces;
    }

    public override DateMode Mode => _mode;
    public override bool WrapStringsWithSpaces => _wrapStringsWithSpaces;
}
/// <inheritdoc />
/// <remarks>String-based dates without time component will assume the start of the day.</remarks>
public class JqlDateStartArgumentCompletionAttribute : JqlDateArgumentCompletionBase
{
    public override DateMode Mode => DateMode.Start;

    public override bool WrapStringsWithSpaces => true;
}
/// <inheritdoc />
/// <remarks>String-based dates without time component will assume the end of the day.</remarks>
public class JqlDateEndArgumentCompletionAttribute : JqlDateArgumentCompletionBase
{
    public override DateMode Mode => DateMode.End;
    public override bool WrapStringsWithSpaces => true;
}
/// <inheritdoc />
/// <remarks>String-based dates without time component will assume the time of parsing.</remarks>
public class JqlDateCurrentArgumentCompletionAttribute : JqlDateArgumentCompletionBase
{
    public override DateMode Mode => DateMode.Current;
    public override bool WrapStringsWithSpaces => true;
}
