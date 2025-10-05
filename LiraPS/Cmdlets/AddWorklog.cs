using System;
using System.Management.Automation;
using System.Net.Mail;
using System.Reflection;
using System.Threading;
using ConsoleMenu;
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
        [ArgumentCompleter(typeof(RecentIssuesCompleter))]
        [AllowEmptyString]
        [AllowNull]
        [Alias("Key")]
        public string Issue { get; set; } = string.Empty;
        [Parameter]
        [Alias("Date")]
        [DateTimeOffsetDateTransformer(mode: DateMode.Current)]
        [ArgumentCompleter(typeof(JqlDateCurrentArgumentCompleter))]
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
                    var ismen = InteractiveStringMenu.CreateNonWhitespace("Enter issue id", LiraSession.LastAddedLogId);
                    ismen.Completer = RecentIssuesCompleter.Instance;
                    var id = ismen.Show();
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
            LiraSession.LastAddedLogId = Issue;
            if (Started == default)
            {
                var now = DateTimeOffset.Now;
                var dateMenu = new InteractiveMenu<DateTimeOffset>(
                    new DateTimeOffsetDateTransformerAttribute(DateMode.Current) { UseLastLogDate=true},
                    "Enter date of work",
                    LiraSession.LastAddedLogDate is null ? DateCompletionHelper.NowKeyword : $"{DateCompletionHelper.LastLogDateKeyword} +1",
                    new JqlDateArgumentCompleter() { UseLastLogDate = true });

                while (true)
                {
                    try
                    {
                        Started = dateMenu.Show();
                        break;
                    }
                    catch (PromptTransformException)
                    {
                        WriteWarning("Invalid date");
                    }
                }
            }
            if (Duration == default)
            {
                var timeMenu = new InteractiveMenu<TimeSpan>(
                TimespanTransformer.Instance,
                "Enter time spent")
                {
                    Validator = TimespanTransformer.Instance
                };
                while (true)
                {
                    try
                    {
                        Duration = timeMenu.Show();
                        break;
                    }
                    catch (PromptTransformException)
                    {
                        WriteWarning("Invalid timespan");
                    }
                }
            }
            if (string.IsNullOrWhiteSpace(Comment) && !TestBoundParameter(nameof(Comment)))
            {
                Comment = ReadInput("Enter optional comment");
                if (string.IsNullOrWhiteSpace(Comment))
                {
                    Comment = null;
                }
            }
            LiraSession.LastAddedLogDate = Started;
            var worklogToAdd = new WorklogToAdd(Started, Duration, Comment);
            if (!NoConfirm.IsPresent)
            {
                WriteHost("");
                WriteHost($"The following worklog will be added", ConsoleColor.Cyan);
                WriteHost("");
                WriteHost($"     Issue: {Bold}{Issue}{Reset}");
                WriteHost($"   Started: {Bold}{worklogToAdd.Started.UnambiguousForm()}{Reset}");
                WriteHost($" TimeSpent: {Bold}{worklogToAdd.TimeSpent.PrettyTime()}{Reset}");
                string com = string.IsNullOrWhiteSpace(worklogToAdd.Comment) ? $"{Dim}None{Reset}" : worklogToAdd.Comment;
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
                LiraSession.CacheWorklog(added);
                LiraSession.Logger.LogInformation("Added worklog {id}", added.ID);
                WriteObject(added);
                RecentIssues.Add(added.Issue);
            }
            else
            {
                LiraSession.Logger.LogError("Failed adding worklog");
            }
        }
    }
}
