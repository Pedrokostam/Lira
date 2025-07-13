using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;
using LiraPS.Cmdlets;

namespace LiraPS.Completers;
public class ConfigurationCompletionAttribute : IArgumentCompleter,ISimpleArgumentCompleter
{
    public IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commandAst, IDictionary fakeBoundParameters) => CompleteArgument(wordToComplete);
    public IEnumerable<CompletionResult> CompleteArgument(string wordToComplete)
    {
        foreach (var info in GetAvailableConfigurations.GetAvailable())
        {
            if (info.Name.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase))
            {
                yield return new CompletionResult(info.Name,info.Name,CompletionResultType.Text,$"{info.Type} - {info.ServerAddress}");
            }
        }
    }
}
