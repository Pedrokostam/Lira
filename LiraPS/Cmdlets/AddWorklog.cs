using System;
using System.Management.Automation;
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
                        if (MenuYesNo($"Is the following date correct? {Bold}{dto.UnambiguousForm()}{Reset}"))
                        {
                            Started = dto;
                            break;
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
            }
            var wta = new WorklogToAdd(Started, Duration, Comment);
            if (isSomewhatManual)
            {
                WriteHost("");
                WriteHost($"The following worklog will be added to issue {Issue}", ConsoleColor.Cyan);
                WriteHost(wta.ToString());
                if (!MenuYesNo("Is thw worklog correct?"))
                {
                    Terminate(new InvalidOperationException("User canceled adding worklog"), "WorklogCancel", ErrorCategory.InvalidOperation);
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
