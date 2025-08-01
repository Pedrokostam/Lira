﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
using LiraPS.Arguments;
using LiraPS.Completers;
using LiraPS.Transformers;
using Serilog.Events;
using Serilog.Formatting.Display;
using AllowNullAttribute = System.Management.Automation.AllowNullAttribute;

namespace LiraPS.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "LiraWorklog", DefaultParameterSetName = "PERIOD")]
    [OutputType(typeof(Worklog))]
    [Alias("Get-Worklog", "Get-Worklogs", "Get-LiraWorklogs")]
    public class GetLiraWorklog : LiraCmdlet

    {
        [Parameter(Position = 0, ParameterSetName = "PERIOD")]
        public Period Period { get; set; } = Period.ThisMonth;
        [AllowNull]
        [DateTransformer(outputIJqlDate: true, mode: DateMode.Start)]
        [ArgumentCompleter(typeof(JqlDateStartArgumentCompletionAttribute))]
        [Parameter(ParameterSetName = "MANUALDATE")]
        public IJqlDate? StartDate { get; set; } = null;

        [AllowNull]
        [DateTransformer(outputIJqlDate: true, mode: DateMode.Start)]
        [ArgumentCompleter(typeof(JqlDateEndArgumentCompletionAttribute))]
        [Parameter(ParameterSetName = "MANUALDATE")]
        public IJqlDate? EndDate { get; set; } = null;

        [Parameter(ValueFromPipeline = true)]
        [UserDetailsToStringTransformer]
        [ValidateNotNullOrEmpty]
        public string[] User { get => user; set => user = value ?? []; }
        [Parameter]
        [AllowNull]
        [System.Diagnostics.CodeAnalysis.AllowNull]
        public string[] Issue { get => issue; set => issue = value ?? []; }
        [Parameter]
        public int Chunk { get; set; } = -1;

        [Parameter]
        [Alias("Refresh", "Force")]
        public SwitchParameter ForceRefresh { get; set; }

        const int IssuePaginationProgressId = 12;
        private string[] user = ["CurrentUser"];
        private string[] issue = [];
        private readonly List<Worklog> _worklogs = [];


        // This method will be called for each input received from the pipeline to this cmdlet; if no input is received, this method is not called
        protected override void ProcessRecord()
        {
            if (User.Length == 0)
            {
                WriteWarning("Querying for worklogs without specifying any user may take a long time. Consider yourself warned.");
            }
            for (int i = 0; i < User.Length; i++)
            {
                User[i] = ReplaceCurrentUserAlias(User[i]);
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
            if (ForceRefresh.IsPresent)
            {
                LiraSession.Client.RemoveFromQueryCache(query.BuildQueryString(LiraSession.Client));
            }
            var machine = new WorklogStateMachine(LiraSession.Client) { QueryLimit = Chunk };
            var state = machine.GetStartState(query);
            while (!state.IsFinished)
            {
                var t = machine.Process(state).GetAwaiter();
                state = t.GetResult();
                PrintLogs();
                CommentState(in state);
            }
            _worklogs.AddRange(state.Worklogs);
        }
        protected override void EndProcessing()
        {
            var sorted = _worklogs.OrderBy(x => x.Started).ToList();
            WriteObject(sorted, enumerateCollection: true);
            SetGlobal("LiraLastWorklogs", sorted);
            base.EndProcessing();   
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
                _ => throw new PSNotSupportedException(),
            };
            StartDate = start;
            EndDate = end;
        }


        private void CommentState(in WorklogStateMachine.State currState)
        {
            WriteVerbose($"{currState.FinishedStep} => {currState.NextStep}");
            if (currState.FinishedStep < WorklogStateMachine.Steps.QueryForIssues && currState.FinishedStep == WorklogStateMachine.Steps.QueryForIssues)
            {
                long issueCount = currState.PaginationState.Pagination.Total;
                string issuePlural = issueCount == 1 ? "issue" : "issues";
                WriteVerbose($"Received query response. Found {issueCount} {issuePlural} matching query.");
            }
            if (currState.NextStep == WorklogStateMachine.Steps.QueryForIssues)
            {
                WritePaginationProgress(in currState, false);
            }
            if (currState.NextStep > WorklogStateMachine.Steps.QueryForIssues)
            {
                WritePaginationProgress(in currState, true);
            }
        }

        private void WritePaginationProgress(in WorklogStateMachine.State currState, bool finished)
        {
            var got = currState.PaginationState.Pagination.EndsAt;
            long totalCount = currState.PaginationState.Pagination.Total;
            var totalString = totalCount.ToString();
            var perc = totalCount == 0 ? -1 : (int)(currState.PaginationState.Progress * 100);
            var status = totalCount == 0 ? "Fetching issues..." : $"Paginating results {got}/{totalString} issues ({perc:d2}%)...";
            var record = new ProgressRecord(IssuePaginationProgressId, "Gathering issues", status)
            {
                PercentComplete = perc,
                RecordType = finished ? ProgressRecordType.Completed : ProgressRecordType.Processing
            };
            WriteProgress(record);
        }
    }
}
