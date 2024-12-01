using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Lira;
using Lira.Jql;
using Lira.Objects;
using Lira.StateMachines;
using LiraPS.Transformers;
using Serilog.Events;
using Serilog.Formatting.Display;
using AllowNullAttribute = System.Management.Automation.AllowNullAttribute;

namespace LiraPS.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "Worklog", DefaultParameterSetName = "PERIOD")]
    [OutputType(typeof(Worklog))]
    public class GetWorklog : LiraCmdlet
    {
        [Parameter(Position = 0, ParameterSetName = "PERIOD")]
        public Period Period { get; set; } = Period.ThisMonth;
        [AllowNull]
        [DateTransformer(true)]
        [ArgumentCompleter(typeof(JqlDateArgumentCompletionAttribute))]
        [Parameter(ParameterSetName = "MANUALDATE")]
        public IJqlDate? StartDate { get; set; } = null;

        [AllowNull]
        [DateTransformer(true)]
        [ArgumentCompleter(typeof(JqlDateArgumentCompletionAttribute))]
        [Parameter(ParameterSetName = "MANUALDATE")]
        public IJqlDate? EndDate { get; set; } = null;

        [Parameter]
        [AllowNull]
        [System.Diagnostics.CodeAnalysis.AllowNull]
        public string[] User { get => user; set => user = value ?? []; }
        [Parameter]
        [AllowNull]
        [System.Diagnostics.CodeAnalysis.AllowNull]
        public string[] Issue { get => issue; set => issue = value ?? []; }
        [Parameter]
        public int Chunk { get; set; } = -1;
        const int IssuePaginationProgressId = 12;
        private string[] user = ["CurrentUser"];
        private string[] issue = [];
        // This method will be called for each input received from the pipeline to this cmdlet; if no input is received, this method is not called
        protected override void ProcessRecord()
        {
            if (User.Length == 0)
            {
                WriteWarning("Querying for worklogs without specifying any user may take a long time. Consider yourself warned.");
            }
            for (int i = 0; i < User.Length; i++)
            {
                var l = User[i].ToLowerInvariant();
                var replacement = l switch
                {
                    "me" or "myself" or "current" or "currentuser" or "ooh! a clone of myself" => LiraSession.Client.Myself.Name,
                    _ => l
                };
                User[i] = replacement;
            }
            var uniqueUsers = User.Distinct(StringComparer.OrdinalIgnoreCase);
            if (ParameterSetName == "PERIOD")
            {
                PeriodToDates();
            }
            var query = new JqlQuery()
                .WithWorklogsAfter(StartDate)
                .WithWorklogsBefore(EndDate)
                .WhereWorklogAuthorIs(uniqueUsers.ToArray())
                .WhereIssueIs(Issue);
            WriteVerbose($"Running query: {query.BuildQueryString(LiraSession.Client)}");
            var machine = new WorklogMachine(LiraSession.Client) { QueryLimit = Chunk };
            var state = machine.GetStartState(query);
            while (!state.IsFinished)
            {
                var t = machine.Process(state).GetAwaiter();
                state = t.GetResult();
                PrintLogs();
                CommentState(in state);
            }
            WriteObject(state.Worklogs);
            SetGlobal("LiraLastWorklogs", state.Worklogs);
        }

        private void PeriodToDates()
        {
            (JqlKeywordDate start, JqlKeywordDate end) = Period switch
            {
                Period.Today => (JqlKeywordDate.StartOfDay, JqlKeywordDate.EndOfDay),
                Period.ThisWeek => (JqlKeywordDate.StartOfWeek, JqlKeywordDate.EndOfWeek),
                Period.ThisMonth => (JqlKeywordDate.StartOfMonth, JqlKeywordDate.EndOfMonth),
                Period.ThisYear => (JqlKeywordDate.StartOfYear, JqlKeywordDate.EndOfYear),
                Period.Yesterday => (JqlKeywordDate.StartOfDay.WithOffset(-1), JqlKeywordDate.EndOfDay.WithOffset(-1)),
                Period.LastWeek => (JqlKeywordDate.StartOfWeek.WithOffset(-1), JqlKeywordDate.EndOfWeek.WithOffset(-1)),
                Period.LastMonth => (JqlKeywordDate.StartOfMonth.WithOffset(-1), JqlKeywordDate.EndOfMonth.WithOffset(-1)),
                Period.LastYear => (JqlKeywordDate.StartOfYear.WithOffset(-1), JqlKeywordDate.EndOfYear.WithOffset(-1)),
                _ => throw new NotSupportedException(),
            };
            StartDate = start;
            EndDate = end;
        }


        private void CommentState(in WorklogMachine.State currState)
        {
            WriteVerbose($"{currState.FinishedStep} => {currState.NextStep}");
            if (currState.FinishedStep < WorklogMachine.Steps.QueryForIssues && currState.FinishedStep == WorklogMachine.Steps.QueryForIssues)
            {
                long issueCount = currState.PaginationState.Pagination.Total;
                string issuePlural = issueCount == 1 ? "issue" : "issues";
                WriteVerbose($"Received query response. Found {issueCount} {issuePlural} matching query.");
            }
            if (currState.NextStep == WorklogMachine.Steps.QueryForIssues)
            {
                WritePaginationProgress(in currState, false);
            }
            if (currState.NextStep > WorklogMachine.Steps.QueryForIssues)
            {
                WritePaginationProgress(in currState, true);
            }
        }

        private void WritePaginationProgress(in WorklogMachine.State currState, bool finished)
        {
            var got = currState.PaginationState.Pagination.EndsAt;
            var total = currState.PaginationState.Pagination.Total.ToString();
            var perc = (int)(currState.PaginationState.Progress * 100);
            var record = new ProgressRecord(IssuePaginationProgressId, "Gathering issues", $"Paginating results {got}/{total} ({perc:d2}%)")
            {
                PercentComplete = perc,
                RecordType = finished ? ProgressRecordType.Completed : ProgressRecordType.Processing
            };
            WriteProgress(record);
        }
    }
}
