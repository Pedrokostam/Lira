using Lira.Jql;
using Lira.Objects;
using Lira.StateMachines;
using Lira.Toggl;
using LiraPS.Arguments;
using LiraPS.Completers;
using LiraPS.Extensions;
using LiraPS.Transformers;
using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace LiraPS.Toggl.Cmdlets
{
    [Cmdlet(VerbsData.Compare, "LiraTogglWorklog", DefaultParameterSetName = "PERIOD")]
    [OutputType(typeof(TogglJiraComparison))]
    public sealed class CompareTogglWorklog : TogglCmdlet
    {
        
        [Parameter(Position = 0, ParameterSetName = "PERIOD")]
        public Period Period { get; set; } = Period.ThisMonth;

        [AllowNull]
        [JqlDateTransformer(mode: DateMode.Start)]
        [ArgumentCompleter(typeof(JqlDateStartArgumentCompleter))]
        [Parameter(ParameterSetName = "MANUALDATE")]
        [Alias("From")]
        public IJqlDate? DateFrom { get; set; } = null;

        [AllowNull]
        [JqlDateTransformer(mode: DateMode.Start)]
        [ArgumentCompleter(typeof(JqlDateEndArgumentCompleter))]
        [Parameter(ParameterSetName = "MANUALDATE")]
        [Alias("To")]
        public IJqlDate? DateTo { get; set; } = null;

        [Parameter(ParameterSetName = "PERIOD")]
        [Parameter(ParameterSetName = "MANUALDATE")]
        [Alias("Refresh", "Force")]
        public SwitchParameter ForceRefresh { get; set; }

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "COLLECTIONS")]
        [AllowEmptyCollection]
        public TogglTimeEntry[] TogglEntry { get; set; } = [];

        /// <summary>
        /// Worklogs of the same user and period as <see cref="TogglEntry"/>.
        /// </summary>
        [Parameter(Mandatory = true, Position = 1, ParameterSetName = "COLLECTIONS")]
        [AllowEmptyCollection]
        public Worklog[] Worklog { get; set; } = [];

        [Parameter]
        public TogglGrouping Grouping { get; set; } = TogglGrouping.ByName;

        protected override void ProcessRecord()
        {
            IEnumerable<TogglTimeEntry> toggl = TogglEntry;
            IEnumerable<Worklog> jira = Worklog;
            if (ParameterSetName == "PERIOD")
            {
                Period.ToKeywordDates(out var from, out var to);
                toggl = FetchToggl(from, to);
                jira = FetchJira(from, to);
            }else if (ParameterSetName == "MANUALDATE")
            {
                ThrowIfNull(DateFrom);
                ThrowIfNull(DateTo);
                toggl = FetchToggl(DateFrom, DateTo);
                jira = FetchJira(DateFrom,DateTo);
            }
            var result = TogglJiraComparer.Compare(toggl, jira, LiraSession.Client.AccountTimezone, Grouping);
            WriteObject(result);
        }

        private IReadOnlyList<TogglTimeEntry> FetchToggl(IJqlDate from, IJqlDate to)
        {
            ThrowIfNull(from);
            ThrowIfNull(to);
            var client = TogglClient.GetTogglClient(GetTogglKey());
            var timezone = LiraSession.Client.AccountTimezone;
            return client.GetEntries(from.ToAccountDatetime(timezone), to.ToAccountDatetime(timezone), ForceRefresh.IsPresent).GetAwaiter().GetResult();
        }

        private IReadOnlyList<Worklog> FetchJira(IJqlDate from, IJqlDate to)
        {
            var query = new JqlQuery()
                .WithWorklogsAfter(from)
                .WithWorklogsBefore(to)
                .WhereWorklogAuthorMatches(DivideConditionCollection([LiraSession.Client.Myself.Name]));
            var queryString = query.BuildQueryString(LiraSession.Client);
            WriteVerbose($"Running query: {queryString}");
            if (ForceRefresh.IsPresent)
            {
                LiraSession.Client.RemoveFromQueryCache(queryString);
            }
            var machine = new FindWorklogByQueryStateMachine(LiraSession.Client);
            var state = machine.GetStartState(query);
            while (!state.IsFinished)
            {
                state = machine.Process(state).GetAwaiter().GetResult();
                PrintLogs();
            }
            foreach (var item in state.Payload)
            {
                LiraSession.CacheWorklog(item);
            }
            return state.Payload;
        }
    }
}
