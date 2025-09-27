using System;
using System.Management.Automation;
using System.Net.Mail;
using System.Reflection;
using System.Threading;
using Lira.Objects;
using LiraPS.Completers;
using LiraPS.Extensions;
using LiraPS.Transformers;
using Microsoft.Extensions.Logging;

namespace LiraPS.Cmdlets
{
    [Cmdlet(VerbsCommon.Add, "LiraWorklog")]
    [OutputType(typeof(Worklog))]
    [Alias("Add-Worklog")]
    public class AddWorklog : LiraCmdlet
    {
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [AllowEmptyString]
        [AllowNull]
        [Alias("Key")]
        public string Issue { get; set; } = string.Empty;
        [Parameter]
        [Alias("DateType")]
        [DateTimeOffsetDateTransformer(mode: DateMode.Current)]
        [ArgumentCompleter(typeof(JqlDateCurrentArgumentCompletionAttribute))]
        public DateTimeOffset Started { get; set; } = default;
        [Parameter]
        [Alias("Time", "TimeSpan")]
        [TimespanTransformer]
        public TimeSpan Duration { get; set; } = default;
        [Parameter]
        [AllowNull]
        public string? Comment { get; set; }
        [Parameter()]
        [Alias("Yes")]
        public SwitchParameter NoConfirm { get; set; }

        protected override void ProcessRecord()
        {
            if (string.IsNullOrWhiteSpace(Issue))
            {
                while (true)
                {
                    var id = ReadInput("Enter issue id");
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        WriteWarning("Issue id cannot be empty");
                    }
                    else
                    {
                        Issue = id;
                        break;
                    }
                }
            }
            if (Started == default)
            {
                while (true)
                {
                    var now = DateTimeOffset.Now;
                    var dataTrans = new DateTimeOffsetDateTransformerAttribute(mode: DateMode.Current);
                    var tstring = ReadInput($"\nEnter date of work, leave empty to use {now.NumericalForm()}");
                    try
                    {
                        var dto = string.IsNullOrWhiteSpace(tstring) ? now : dataTrans.Transform(tstring)!;
                        var choice = ChoiceYesNo($"Is the following date correct? {Bold}{dto.UnambiguousForm()}{Reset}", ChoiceOptions.Yes, ChoiceSettings.YesNoCancel);
                        if (choice == ChoiceOptions.Yes)
                        {
                            Started = dto;
                            break;
                        }
                        if (choice == ChoiceOptions.Cancel)
                        {
                            UserCancel("worklog adding");
                        }

                    }
                    catch
                    {
                        WriteWarning("Invalid date");
                    }
                }
            }
            if (Duration == default)
            {
                while (true)
                {
                    var tstring = ReadInput("\nEnter time spent (e.g. 2h 15m)");
                    var ts = TimespanTransformer.ParseTime(tstring);
                    if (ts == TimeSpan.Zero)
                    {
                        WriteWarning("Duration cannot be zero");
                    }
                    else
                    {
                        Duration = ts;
                        break;
                    }
                }
            }
            if (string.IsNullOrWhiteSpace(Comment) && !TestBoundParameter(nameof(Comment)))
            {
                Comment = ReadInput("\nEnter optional comment");
                if (string.IsNullOrWhiteSpace(Comment))
                {
                    Comment = null;
                }
            }
            var worklogToAdd = new WorklogToAdd(Started, Duration, Comment);
            if (!NoConfirm.IsPresent)
            {
                WriteHost("");
                WriteHost($"The following worklog will be added", ConsoleColor.Cyan);
                WriteHost("");
                WriteHost($"     Issue: {Bold}{Issue}{Reset}");
                WriteHost($"   Started: {Bold}{worklogToAdd.Started.UnambiguousForm()}{Reset}");
                WriteHost($" TimeSpent: {Bold}{worklogToAdd.TimeSpent.PrettyTime()}{Reset}");
                string com = worklogToAdd.Comment ?? $"{Dim}None{Reset}";
                WriteHost($"   Comment: {Bold}{com}{Reset}");
                WriteHost("");
                var choice = ChoiceYesNo($"Is the worklog correct?", null, ChoiceSettings.YesNo);
                if (choice == ChoiceOptions.No)
                {
                    UserCancel("worklog adding");
                }
            }
            ENSURE_TESTING(Issue);

            LiraSession.Logger.LogInformation("Adding worklog to {issue}", Issue);
            var machine = LiraSession.Client.GetAddWorklogMachine();
            var state = machine.GetStartState(Issue, worklogToAdd);
            while (!state.IsFinished)
            {
                var t = machine.Process(state).GetAwaiter();
                state = t.GetResult();
                PrintLogs();
            }

            if (state.AddedWorklog is Worklog added)
            {
                LiraSession.Logger.LogInformation("Added worklog {id}", added.ID);
                WriteObject(added);
            }
            else
            {
                LiraSession.Logger.LogError("Failed adding worklog");
            }
        }
    }
}
