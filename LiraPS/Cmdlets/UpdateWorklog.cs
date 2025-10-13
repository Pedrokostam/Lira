using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using ConsoleMenu;
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

    [Parameter(Mandatory = true, ValueFromPipeline = true, Position = 0)]
    [CachedWorklogTransformer]
    public Worklog Worklog { get; set; } = default!;

    [Parameter()]
    [Alias("NewDate", "Started", "Date")]
    [DateTimeOffsetDateTransformer(mode: DateMode.Current)]
    [ArgumentCompleter(typeof(JqlDateCurrentArgumentCompleter))]
    public DateTimeOffset NewStarted { get; set; } = default;
    [Parameter]
    [Alias("Time", "NewTime")]
    [TimespanTransformer]
    public TimeSpan NewDuration { get; set; } = default!;
    [Parameter()]
    [AllowNull]
    [AllowEmptyString]
    [Alias("Comment")]
    public string? NewComment { get; set; }
    [Parameter]
    public SwitchParameter Force { get; set; }
    private void PrettyPrint(string text, GraphicModes mode = GraphicModes.None, ConsoleColor? color = null)
    {
        var s = Part.GetConsoleString(text, mode, color);
        WriteHost(s);
    }
    private void ShowChange(string from, string to, string changeName)
    {
        WriteHost("");
        WriteHost(changeName, ConsoleColor.Cyan);
        PrettyPrint("From:", GraphicModes.Italics);
        WriteHost("      " + from, ConsoleColor.DarkYellow);
        PrettyPrint("To:", GraphicModes.Italics);
        WriteHost("    " + to, ConsoleColor.Green);
    }
    protected override void ProcessRecord()
    {
        DateTimeOffset? date = TestBoundParameter(nameof(NewStarted)) ? NewStarted : null;
        TimeSpan? time = TestBoundParameter(nameof(NewDuration)) ? NewDuration : null;
        string? comment = TestBoundParameter(nameof(NewComment)) ? NewComment : null;

        if (!Worklog.GetUpdatePackage(out var payload, date, time, comment))
        {
            Terminate(new PSInvalidOperationException("There is no change in the worklog to commit"), "NoChangeEditWorklog", ErrorCategory.InvalidOperation);
        }
        if (payload.Started is DateTimeOffset payloadDate)
        {
            bool showTimezones = Worklog.Started.Offset != TimeZoneInfo.Local.BaseUtcOffset || payloadDate.Offset != TimeZoneInfo.Local.BaseUtcOffset;
            ShowChange(
                Worklog.Started.UnambiguousForm(showTimezones),
                payloadDate.UnambiguousForm(showTimezones),
                "Date started change");
        }
        if (payload.TimeSpent is TimeSpan payloadTs)
        {
            ShowChange(Worklog.TimeSpent.PrettyTime(), payloadTs.PrettyTime(), "Time spent change");
        }
        if (payload.Comment is string payloadString)
        {
            string oldCom = String.IsNullOrWhiteSpace(Worklog.Comment) ? Part.GetConsoleString("No comment", GraphicModes.Dim) : Worklog.Comment;
            string newCom = String.IsNullOrWhiteSpace(payloadString) ? Part.GetConsoleString("No comment", GraphicModes.Dim) : payloadString;
            ShowChange(oldCom, newCom, "Comment change");
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
        if (state.UpdateWorklog is Worklog updated)
        {
            LiraSession.CacheWorklog(updated);
        }
    }
}
