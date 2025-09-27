using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using Lira.Extensions;
using Lira.Objects;
using LiraPS.Completers;
using LiraPS.Extensions;
using LiraPS.Transformers;

namespace LiraPS.Cmdlets;

[Cmdlet(VerbsData.Update, "LiraWorklog")]
[Alias("Update-Worklog")]
public class UpdateWorklog : LiraCmdlet
{
    public readonly record struct Change(string Name, object Old, object Updated) { }

    [Parameter(Mandatory = true, ValueFromPipeline = true)]
    [CachedWorklogTransformer]
    public Worklog Worklog { get; set; } = default!;

    [Parameter()]
    [Alias("DateType","NewDate")]
    [DateTimeOffsetDateTransformerAttribute( mode: DateMode.Current)]
    [ArgumentCompleter(typeof(JqlDateCurrentArgumentCompletionAttribute))]
    public DateTimeOffset NewStarted { get; set; } = default;
    [Parameter]
    [Alias("Time", "TimeSpan","NewTime")]
    [TimespanTransformer]
    public TimeSpan NewDuration { get; set; } = default!;
    [Parameter()]
    [AllowNull]
    [AllowEmptyString]
    [Alias("Comment")]
    public string? NewComment { get; set; }
    [Parameter]
    public SwitchParameter Force { get; set; }
    protected override void ProcessRecord()
    {
        DateTimeOffset? date = TestBoundParameter(nameof(NewStarted)) ? NewStarted : null;
        TimeSpan? time = TestBoundParameter(nameof(NewDuration)) ? NewDuration : null;
        string? comment = TestBoundParameter(nameof(NewComment)) ? NewComment : null;
        
        if (!Worklog.GetUpdatePackage(out var payload,date,time,comment))
        {
            Terminate(new PSInvalidOperationException("There is no change in the worklog to commit"), "NoChangeEditWorklog", ErrorCategory.InvalidOperation);
        }
        if (payload.Started is DateTimeOffset payloadDate)
        {
            WriteHost("DateType change", ConsoleColor.Cyan);
            bool showTimezones = Worklog.Started.Offset != TimeZoneInfo.Local.BaseUtcOffset || payloadDate.Offset != TimeZoneInfo.Local.BaseUtcOffset;
            WriteHost($"{Worklog.Started.UnambiguousForm(showTimezones)} => {Bold}{payloadDate.UnambiguousForm(showTimezones)}{Reset}");
            WriteHost("");
        }
        if (payload.TimeSpent is TimeSpan payloadTs)
        {
            WriteHost("Duration change", ConsoleColor.Cyan);
            WriteHost($"{Worklog.TimeSpent.PrettyTime()} => {Bold}{payloadTs.PrettyTime()}{Reset}");
            WriteHost("");
        }
        if (payload.Comment is string payloadString)
        {
            WriteHost("Comment change", ConsoleColor.Cyan);
            WriteHost(Worklog.Comment);
            WriteHost("to");
            WriteHost(payloadString);
            WriteHost("");
        }
        if (!(Force.IsPresent || ShouldContinue($"Updating worklog", $"Do you want to update the worklog?")))
        {
            UserCancel("worklog update");
        }
            ENSURE_TESTING(Worklog.Issue.Key);
        var machine = LiraSession.Client.GetUpdateWorklogMachine();
        var state = machine.GetStartState(Worklog, payload);
        while (!state.IsFinished)
        {
            var t = machine.Process(state).GetAwaiter();
            state = t.GetResult();
            PrintLogs();
        }
    }
}
