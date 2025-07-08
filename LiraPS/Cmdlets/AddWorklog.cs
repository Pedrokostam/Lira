using System;
using System.Management.Automation;
using System.Net.Mail;
using System.Reflection;
using Lira.Objects;
using LiraPS.Completers;
using LiraPS.Extensions;
using LiraPS.Transformers;

namespace LiraPS.Cmdlets
{
    [Cmdlet(VerbsCommon.Add, "LiraWorklog")]
    [OutputType(typeof(Worklog))]
    [Alias("Add-Worklog")]
    public class AddWorklog : LiraCmdlet
    {
        [Parameter]
        [AllowEmptyString]
        [AllowNull]
        public string Issue { get; set; } = string.Empty;
        [Parameter]
        [Alias("Date")]
        [DateTransformer(outputIJqlDate: false, mode: DateMode.Current)]
        [ArgumentCompleter(typeof(JqlDateCurrentArgumentCompletionAttribute))]
        public DateTimeOffset Started { get; set; } = default;
        [Parameter]
        [Alias("Time", "TimeSpan")]
        [TimespanTransformer]
        public TimeSpan Duration { get; set; } = default;
        [Parameter]
        [AllowNull]
        public string? Comment { get; set; }
        [System.Diagnostics.CodeAnalysis.DoesNotReturn]
        private void UserCancel()
        {
            Terminate(new InvalidOperationException("User canceled adding worklog"), "WorklogCancel", ErrorCategory.InvalidOperation);
        }
        protected override void ProcessRecord()
        {
            bool isSomewhatManual = string.IsNullOrWhiteSpace(Issue) || Duration == default;
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
            if (isSomewhatManual && Started == default)
            {
                while (true)
                {
                    var now = DateTimeOffset.Now;
                    var dataTrans = new DateTransformerAttribute(outputIJqlDate: false, mode: DateMode.Current);
                    var tstring = ReadInput($"Enter date of work, leave empty to use {now.NumericalForm()}");
                    try
                    {
                        var dto = string.IsNullOrWhiteSpace(tstring) ? now : (DateTimeOffset)dataTrans.Transform(tstring)!;
                        var choice = ChoiceYesNo($"Is the following date correct? {Bold}{dto.UnambiguousForm()}{Reset}", ChoiceOptions.Yes, ChoiceSettings.YesNoCancel);
                        if(choice == ChoiceOptions.Yes)
                        {
                            Started = dto;
                            break;
                        }
                        if (choice == ChoiceOptions.Cancel)
                        {
                            UserCancel();
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
                    var tstring = ReadInput("Enter time spent (e.g. 2h 15m)");
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
            if (string.IsNullOrWhiteSpace(Comment))
            {
                Comment = ReadInput("Enter optional comment");
                if (string.IsNullOrWhiteSpace(Comment))
                {
                    Comment = null;
                }
            }
            var wta = new WorklogToAdd(Started, Duration, Comment);
            if (isSomewhatManual)
            {
                WriteHost("");
                WriteHost($"The following worklog will be added", ConsoleColor.Cyan);
                WriteHost("");
                WriteHost($"     Issue: {Bold}{Issue}{Reset}");
                WriteHost($"   Started: {Bold}{wta.Started.UnambiguousForm()}{Reset}");
                WriteHost($" TimeSpent: {Bold}{wta.TimeSpent.PrettyTime()}{Reset}");
                string com = wta.Comment ?? $"{Dim}None{Reset}";
                WriteHost($"   Comment: {Bold}{com}{Reset}");
                WriteHost("");
                var choice = ChoiceYesNo($"Is the worklog correct?", null, ChoiceSettings.YesNo);
                if (choice == ChoiceOptions.No)
                {
                    UserCancel();
                }
            }
            //var dll = Assembly.LoadFile(@"C:\Users\Pedro\source\repos\Lira\Lira.Prototyper\bin\Debug\net462\Serilog.dll");
            //var type = dll.GetType("LiraPS.LiraSession");
            //var prop = type.GetProperty("LogSWitch").GetValue(null);
            //var worklog = new Worklog()
            //{
            //    Author = LiraSession.Client.Myself,
            //    Comment = Comment ?? "none",
            //    ID = "1",
            //    IssueId = "2",
            //    SelfLink = new Uri("http://google.com"),
            //    UpdateAuthor = LiraSession.Client.Myself,
            //    Started = Started,
            //    TimeSpent = Duration,
            //};
            //WriteObject(worklog);
        }
    }
}
