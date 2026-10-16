using System.Collections.Generic;
using System.Management.Automation;
namespace LiraPS.Completers;

public interface ISimpleArgumentCompleter
{
    IEnumerable<CompletionResult> CompleteArgument(string wordToComplete);
}
