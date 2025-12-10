using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using Lira.Jql;
using Lira.Objects;
using Lira.StateMachines;
using LiraPS.Completers;
using LiraPS.Transformers;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;
using Lira.Extensions;
namespace LiraPS.Cmdlets;

[Cmdlet(VerbsCommon.Find, "LiraIssue", DefaultParameterSetName = "NoFullFetch")]
[OutputType(typeof(IssueCommon), ParameterSetName = ["NoFullFetch"])]
[OutputType(typeof(Issue), ParameterSetName = ["FullFetch"])]


[Alias("Find-Issue")]
public sealed class FindIssue : LiraCmdlet
{
    const int IssuePaginationProgressId = 1742;

    private string[] _reporter = [];
    private string[] _assignee = [];
    private readonly List<IssueCommon> _issues = [];

    [AllowNull]
    [JqlDateTransformer(mode: DateMode.Start)]
    [ArgumentCompleter(typeof(JqlDateStartArgumentCompleter))]
    [Parameter(ParameterSetName = "MANUALDATE")]
    public IJqlDate? DateStarted { get; set; } = null;
    [AllowNull]
    [JqlDateTransformer(mode: DateMode.Start)]
    [ArgumentCompleter(typeof(JqlDateStartArgumentCompleter))]
    [Parameter(ParameterSetName = "MANUALDATE")]
    public IJqlDate? DateModified { get; set; } = null;

    [Parameter(ValueFromPipeline = true)]
    [UserDetailsToStringTransformer]
    [ValidateNotNullOrEmpty]
    public string[] Reporter { get => _reporter; set => _reporter = value ?? []; }

    [Parameter(ValueFromPipeline = true)]
    [UserDetailsToStringTransformer]
    [ValidateNotNullOrEmpty]
    public string[] Assignee { get => _assignee; set => _assignee = value ?? []; }

    [AllowNull]
    [Parameter(ValueFromPipeline = true)]
    public string[] Labels { get; set; } = [];
    [AllowNull]
    [Parameter(ValueFromPipeline = true)]
    public string[] Components { get; set; } = [];
    [AllowNull]
    [Parameter(ValueFromPipeline = true)]
    public string[] Status { get; set; } = [];
    [Parameter]
    public int Chunk { get; set; } = -1;
    [Parameter]
    [Alias("Refresh", "Force")]
    public SwitchParameter ForceRefresh { get; set; }
    [Parameter(Mandatory = true, ParameterSetName = "FullFetch")]
    public SwitchParameter FullFetch { get; set; }
    protected override void ProcessRecord()
    {
        for (int i = 0; i < Assignee.Length; i++)
        {
            Assignee[i] = ReplaceCurrentUserAlias(Assignee[i]);
        }
        var distinctAssignees = Assignee.Distinct(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < Reporter.Length; i++)
        {
            Reporter[i] = ReplaceCurrentUserAlias(Reporter[i]);
        }
        var distinctReporters = Reporter.Distinct(StringComparer.OrdinalIgnoreCase);
        var assignees = DivideConditionCollection(distinctAssignees);
        var reporters = DivideConditionCollection(distinctReporters);
        var components = DivideConditionCollection(Components);
        var status = DivideConditionCollection(Status);
        var labels = DivideConditionCollection(Labels);
        var query = new JqlQuery()
            .WhereIssueCreatedOn(DateStarted)
            .WhereIssueUpdatedOn(DateModified)
            .WhereIssueCreatorMatches(reporters)
            .WhereIssueAssigneeMatches(assignees)
            .WhereIssueComponentsMatch(components)
            .WhereIssueStatusMatches(status)
            .WhereIssueLabelsMatch(labels)
        ;
        WriteVerbose($"Running query: {query.BuildQueryString(LiraSession.Client)}");
        var machine = new FindIssueStateMachine(LiraSession.Client) { QueryLimit = Chunk };
        var state = machine.GetStartState(query);
        while (!state.IsFinished)
        {
            var t = machine.Process(state).GetAwaiter();
            state = t.GetResult();
            PrintLogs();
            CommentState(in state);
        }
        _issues.AddRange(state.Payload);
        Lira.Extensions.CollectionExtensions.LoadWorklogs(_issues,LiraSession.Client).GetAwaiter().GetResult();
    }
    protected override void EndProcessing()
    {

        if (FullFetch.IsPresent)
        {
            List<Issue> issues= [];
            List<string> idsOfLite = [];
            foreach (var item in _issues)
            {
                if (item is Issue issue)
                {
                    issues.Add(issue);

                }
                {
                    idsOfLite.Add(item.Key);
                }
            }
            GetLiraIssue.FetchIssues(idsOfLite, ForceRefresh.IsPresent, issues, this);
            var sorted = issues.OrderBy(x => x.Created).ToList();
            WriteObject(sorted, enumerateCollection: true);
        }
        else
        {
            var sorted = _issues.OrderBy(x => x.Created).ToList();
            WriteObject(sorted, enumerateCollection: true);
        }
            base.EndProcessing();
    }
    private void CommentState(in FindIssueStateMachine.State currState)
    {
        WriteVerbose($"{currState.FinishedStep} => {currState.NextStep}");
        if (currState.FinishedStep < FindIssueStateMachine.Steps.QueryForIssues && currState.FinishedStep == FindIssueStateMachine.Steps.QueryForIssues)
        {
            long issueCount = currState.PaginationState.Pagination.Total;
            string issuePlural = issueCount == 1 ? "issue" : "issues";
            WriteVerbose($"Received query response. Found {issueCount} {issuePlural} matching query.");
        }
        if (currState.NextStep == FindIssueStateMachine.Steps.QueryForIssues)
        {
            WritePaginationProgress(in currState, false);
        }
        if (currState.NextStep > FindIssueStateMachine.Steps.QueryForIssues)
        {
            WritePaginationProgress(in currState, true);
        }
    }
    private void WritePaginationProgress(in FindIssueStateMachine.State currState, bool finished)
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
