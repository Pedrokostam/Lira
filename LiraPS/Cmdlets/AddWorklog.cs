using System;
using System.Management.Automation;
using System.Reflection;
using Lira.Objects;
using LiraPS.Completers;
using LiraPS.Transformers;

namespace LiraPS.Cmdlets
{
    [Cmdlet(VerbsCommon.Add, "Worklog")]
    [OutputType(typeof(Worklog))]
    public class AddWorklog : PSCmdlet//: LiraCmdlet
    {
        [Parameter]
        public string Issue { get; set; } = string.Empty;
        [Parameter]
        [DateTransformer(false)]
        [ArgumentCompleter(typeof(JqlDateArgumentCompletionAttribute))]
        public DateTimeOffset Started { get; set; } = default;
        [Parameter]
        [TimespanTransformer]
        public TimeSpan Duration { get; set; } = default;
        [Parameter]
        [AllowNull]
        public string? Comment { get; set; }
        protected override void ProcessRecord()
        {
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
