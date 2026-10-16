using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Lira.Objects;

namespace Lira.Jql;

public class JqlQuery
{
    public readonly record struct GoodBadPair(IList<string> Good, IList<string> Bad) { }
    ///// <summary>
    ///// Jira function returning currently logged user
    ///// </summary>
    //public const string CurrentUser = "currentUser()";
    public RequiredRejectedTesterString<IssueCommon> IssueId { get; set; }
        = new("issue", i => i.Key);
    public RequiredRejectedTesterString<IssueCommon> IssueStatus { get; set; }
        = new("status", i => i.Status);
    public RequiredRejectedTesterManyStrings<IssueCommon> IssueLabels { get; set; }
        = new("labels", i => i.Labels);
    public RequiredRejectedTesterManyStrings<IssueCommon> IssueComponents { get; set; }
        = new("component", i => i.Components);
    public RequiredRejectedTesterUserDetails<IssueCommon> IssueAssignee { get; set; }
        = new("assignee", i => i.Assignee);
    public RequiredRejectedTesterUserDetails<IssueCommon> IssueReporter { get; set; }
        = new("reporter", i => i.Reporter);
    public RequiredRejectedTesterUserDetails<IssueCommon> IssueCreator { get; set; }
        = new("creator", i => i.Creator);
    public StartEndDateTester<IssueCommon> IssueUpdatedDate { get; set; }
        = new("updated", i => i.Updated);
    public StartEndDateTester<IssueCommon> IssueCreatedDate { get; set; }
        = new("created", i => i.Created);
    public RequiredRejectedTesterUserDetails<Worklog> WorklogAuthor { get; set; }
        = new("worklogAuthor", w => w.Author);
    public StartEndDateTester<Worklog> WorklogDate { get; set; }
        = new("worklogDate", w => w.Started);

    private IEnumerable<IJqlQueryItem> AllFields => [
        IssueId,
        IssueStatus,
        IssueLabels,
        IssueComponents,
        IssueAssignee,
        IssueReporter,
        IssueCreator,
        IssueUpdatedDate,
        IssueCreatedDate,
        WorklogAuthor,
        WorklogDate,
    ];
    public string BuildQueryString(LiraClient client)
    {
        var values = AllFields.OfType<IJqlQueryItem>().Select(x => x.GetJqlQuery(client));
        return string.Join(" AND ", values.OfType<string>());
    }
    public bool Filter(object item, LiraClient client)
    {
        return AllFields.OfType<IJqlQueryItem>().All(x => x.Filter(item, client));
    }
    public IEnumerable Filter(IEnumerable items, LiraClient client)
    {
        foreach (var item in items)
        {
            var good = Filter(item, client);
            if (good)
            {
                yield return item;
            }
        }

    }
    public bool FilterItem<TObject>(TObject? item, LiraClient client)
    {
        return AllFields.OfType<IJqlQueryItem<TObject>>().All(x => x.Filter(item, client));
    }
    public IEnumerable<TObject> FilterItems<TObject>(IEnumerable<TObject?> items, LiraClient client)
    {
        foreach (var item in items)
        {
            if (item == null)
            {
                continue;
            }
            var good = FilterItem(item, client);
            if (good)
            {
                yield return item;
            }
        }
    }
    public JqlQuery WhereIssueIs(params string[] issueId)
    {
        IssueId.Good = issueId;
        return this;
    }
    public JqlQuery WhereIssueIsNot(params string[] issueId)
    {
        IssueId.Bad = issueId;
        return this;
    }

    public JqlQuery WhereIssueMatches(GoodBadPair conditions)
    {
        IssueId.Good = conditions.Good;
        IssueId.Bad = conditions.Bad;
        return this;
    }

    public JqlQuery WhereIssueStatusIs(params string[] statuses)
    {
        IssueStatus.Good = statuses;
        return this;
    }

    public JqlQuery WhereIssueStatusIsNot(params string[] statuses)
    {
        IssueStatus.Bad = statuses;
        return this;
    }

    public JqlQuery WhereIssueStatusMatches(GoodBadPair conditions)
    {
        IssueStatus.Good = conditions.Good;
        IssueStatus.Bad = conditions.Bad;
        return this;
    }

    public JqlQuery WhereIssueLabelsAre(params string[] labels)
    {
        IssueLabels.Good = labels;
        return this;
    }

    public JqlQuery WhereIssueLabelsAreNot(params string[] labels)
    {
        IssueLabels.Bad = labels;
        return this;
    }

    public JqlQuery WhereIssueLabelsMatch(GoodBadPair conditions)
    {
        IssueLabels.Good = conditions.Good;
        IssueLabels.Bad = conditions.Bad;
        return this;
    }

    public JqlQuery WhereIssueComponentsAre(params string[] components)
    {
        IssueComponents.Good = components;
        return this;
    }

    public JqlQuery WhereIssueComponentsAreNot(params string[] components)
    {
        IssueComponents.Bad = components;
        return this;
    }

    public JqlQuery WhereIssueComponentsMatch(GoodBadPair conditions)
    {
        IssueComponents.Good = conditions.Good;
        IssueComponents.Bad = conditions.Bad;
        return this;
    }

    public JqlQuery WhereWorklogAuthorIs(params string[] authorId)
    {
        WorklogAuthor.Good = authorId;
        return this;
    }

    public JqlQuery WhereWorklogAuthorIsNot(params string[] authorId)
    {
        WorklogAuthor.Bad = authorId;
        return this;
    }

    public JqlQuery WhereWorklogAuthorMatches(GoodBadPair conditions)
    {
        WorklogAuthor.Good = conditions.Good;
        WorklogAuthor.Bad = conditions.Bad;
        return this;
    }

    public JqlQuery WhereIssueAssigneeIs(params string[] authorId)
    {
        IssueAssignee.Good = authorId;
        return this;
    }

    public JqlQuery WhereIssueAssigneeIsNot(string[] authorId)
    {
        IssueAssignee.Bad = authorId;
        return this;
    }

    public JqlQuery WhereIssueAssigneeMatches(GoodBadPair conditions)
    {
        IssueAssignee.Good = conditions.Good;
        IssueAssignee.Bad = conditions.Bad;
        return this;
    }

    public JqlQuery WhereIssueReporterIs(params string[] authorId)
    {
        IssueReporter.Good = authorId;
        return this;
    }

    public JqlQuery WhereIssueReporterIsNot(params string[] authorId)
    {
        IssueReporter.Bad = authorId;
        return this;
    }

    public JqlQuery WhereIssueReporterMatches(GoodBadPair conditions)
    {
        IssueReporter.Good = conditions.Good;
        IssueReporter.Bad = conditions.Bad;
        return this;
    }

    public JqlQuery WhereIssueCreatorIs(params string[] authorId)
    {
        IssueCreator.Good = authorId;
        return this;
    }

    public JqlQuery WhereIssueCreatorIsNot(params string[] authorId)
    {
        IssueCreator.Bad = authorId;
        return this;
    }

    public JqlQuery WhereIssueCreatorMatches(GoodBadPair conditions)
    {
        IssueCreator.Good = conditions.Good;
        IssueCreator.Bad = conditions.Bad;
        return this;
    }

    public JqlQuery WithWorklogsAfter(IJqlDate? date)
    {
        WorklogDate.StartDate = date;
        return this;
    }

    public JqlQuery WithWorklogsBefore(IJqlDate? date)
    {
        WorklogDate.EndDate = date;
        return this;
    }

    public JqlQuery WhereIssueReportedAfter(IJqlDate? date)
    {
        IssueCreatedDate.StartDate = date;
        return this;
    }

    public JqlQuery WhereIssueReportedBefore(IJqlDate? date)
    {
        IssueCreatedDate.EndDate = date;
        return this;
    }

    public JqlQuery WhereIssueReportedOn(IJqlDate? date)
    {
        if (date is not null)
        {
            IssueCreatedDate.StartDate = new BoundedJqlDate(date, JqlDateBoundary.Exact);
        }
        else
        {
            IssueCreatedDate.StartDate = null;
        }
        IssueCreatedDate.EndDate = null;
        return this;
    }

    public JqlQuery WhereIssueUpdatedAfter(IJqlDate? date)
    {
        IssueUpdatedDate.StartDate = date;
        return this;
    }

    public JqlQuery WhereIssueUpdatedBefore(IJqlDate? date)
    {
        IssueUpdatedDate.EndDate = date;
        return this;
    }
    public JqlQuery WhereIssueUpdatedOn(IJqlDate? date)
    {
        if (date is not null)
        {
            IssueUpdatedDate.StartDate = new BoundedJqlDate(date, JqlDateBoundary.Exact);
        }
        else
        {
            IssueUpdatedDate.StartDate = null;

        }
        IssueUpdatedDate.EndDate = null;
        return this;
    }
}
