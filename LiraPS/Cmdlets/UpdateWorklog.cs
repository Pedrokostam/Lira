using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
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
    public Worklog Worklog { get; set; } = default!;

    [Parameter()]
    [Alias("Date")]
    [DateTransformer(outputIJqlDate: false, mode: DateMode.Current)]
    [ArgumentCompleter(typeof(JqlDateCurrentArgumentCompletionAttribute))]
    public DateTimeOffset NewStarted { get; set; } = default;
    [Parameter]
    [Alias("Time", "TimeSpan")]
    [TimespanTransformer]
    public TimeSpan NewDuration { get; set; } = default!;
    [Parameter()]
    [AllowNull]
    [AllowEmptyString]
    public string? NewComment { get; set; }
    [Parameter]
    public SwitchParameter Force { get; set; }
    protected override void ProcessRecord()
    {
        bool dateChanged = TestBoundParameter(nameof(NewStarted)) && NewStarted != Worklog.Started;
        bool timeChanged = TestBoundParameter(nameof(NewDuration)) && NewDuration != Worklog.TimeSpent;
        NewComment ??= "";
        bool commentChanged = TestBoundParameter(nameof(NewComment)) && !NewComment.Equals(Worklog.Comment, StringComparison.OrdinalIgnoreCase);
        bool changedAtAll = dateChanged || timeChanged || commentChanged;
        if (!changedAtAll)
        {
            Terminate(new PSInvalidOperationException("There is no change in the worklog to commit"), "NoChangeEditWorklog", ErrorCategory.InvalidOperation);
        }
        if (dateChanged)
        {
            WriteHost("Date change", ConsoleColor.Cyan);
            bool showTimezones = Worklog.Started.Offset != TimeZoneInfo.Local.BaseUtcOffset || NewStarted.Offset != TimeZoneInfo.Local.BaseUtcOffset;
            WriteHost($"{Worklog.Started.UnambiguousForm(showTimezones)} => {Bold}{NewStarted.UnambiguousForm(showTimezones)}{Reset}");
            WriteHost("");
        }
        if (timeChanged)
        {
            WriteHost("Duration change", ConsoleColor.Cyan);
            WriteHost($"{Worklog.TimeSpent.PrettyTime()} => {Bold}{NewDuration.PrettyTime()}{Reset}");
            WriteHost("");
        }
        if (timeChanged)
        {
            WriteHost("Comment change", ConsoleColor.Cyan);
            WriteHost(Worklog.Comment);
            WriteHost("to");
            WriteHost(NewComment);
            WriteHost("");
        }
        if (Force.IsPresent || ShouldContinue($"Updating worklog", $"Do you want to update the worklog?"))
        {

        }
        base.ProcessRecord();
    }
}
