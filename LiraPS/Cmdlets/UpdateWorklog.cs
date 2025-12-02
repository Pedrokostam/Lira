using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    [DateTimeOffsetDateTransformer(mode: DateMode.Current,passScriptBlock:true)]
    [ArgumentCompleter(typeof(JqlDateCurrentArgumentCompleter))]
    public object? NewStarted { get; set; } = default;
    [Parameter]
    [Alias("Time", "NewTime")]
    [TimespanTransformer(true)]
    public object? NewDuration { get; set; } = default!;
    [Parameter()]
    [AllowNull]
    [AllowEmptyString]
    [Alias("Comment")]
    public object? NewComment { get; set; }
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
        WriteHost("    " + from, ConsoleColor.DarkYellow);
        PrettyPrint("To:", GraphicModes.Italics);
        WriteHost("    " + to, ConsoleColor.Green);
    }
    protected override void ProcessRecord()
    {
        DateTimeOffset? date = GetDate();
        TimeSpan? time = GetDuration();
        string? comment =GetComment();

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
    private static T? RunScript<T>(ScriptBlock block, T oldValue, ArgumentTransformationAttribute? transformer)
    {
        object? first = null;
        try
        {
            List<PSVariable> vars = [new PSVariable("old", oldValue), new PSVariable("_", oldValue), new PSVariable("prev", oldValue)];
            var result = block.InvokeWithContext(functionsToDefine: null, variablesToDefine: vars);
            if (result.Count > 1)
            {
                throw new PSInvalidOperationException($"ScriptBlock returned more than 1 value.");
            }
            if (result.Count == 1)
            {
                first = result[0].BaseObject;
                if (transformer is not null && transformer.Transform(null, first) is T item)
                {
                    return item;
                }
                if (typeof(string) == typeof(T))
                {
                    first = first.ToString();
                }
                if (first is T t)
                {
                    return t;
                }
                if (first is null)
                {
                    return default;
                }
            }
            throw new PSInvalidOperationException($"ScriptBlock did not return a valid {typeof(T).Name}");
        }
        finally {
            Debug.WriteLine($"Script run resulted in value {first ?? "<NONE>"} of type {first?.GetType().FullName ?? "null"}");
        }
    }
    private DateTimeOffset? GetDate()
    {
        if (!TestBoundParameter(nameof(NewStarted)) || NewStarted is null)
        {
            return null;
        }
        if (NewStarted is DateTimeOffset dto)
        {
            return dto;
        }
        if (NewStarted is ScriptBlock sb)
        {
            return RunScript(sb, Worklog.Created, new DateTimeOffsetDateTransformerAttribute(DateMode.Current));
        }
        throw new PSInvalidOperationException($"Could not convert type {NewStarted.GetType().FullName} to either DateTimeOffset or ScriptBlock");
    }
    private TimeSpan? GetDuration()
    {
        if (!TestBoundParameter(nameof(NewDuration)) || NewDuration is null)
        {
            return null;
        }
        if (NewDuration is TimeSpan dto)
        {
            return dto;
        }
        if (NewDuration is ScriptBlock sb)
        {
            return RunScript(sb, Worklog.TimeSpent, TimespanTransformer.Instance);
        }
        throw new PSInvalidOperationException($"Could not convert type {NewDuration.GetType().FullName} to either DateTimeOffset or ScriptBlock");
    }
    private string? GetComment()
    {
        if (!TestBoundParameter(nameof(NewComment)) || NewComment is null)
        {
            return null;
        }
        if (NewComment is string dto)
        {
            return dto;
        }
        if (NewComment is ScriptBlock sb)
        {
            return RunScript(sb, Worklog.Comment ?? "", null);
        }
        throw new PSInvalidOperationException($"Could not convert type {NewComment.GetType().FullName} to either DateTimeOffset or ScriptBlock");
    }
}
