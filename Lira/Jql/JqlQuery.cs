using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Lira.Objects;

namespace Lira.Jql;
public class JqlQuery : IEquatable<JqlQuery>
{
    ///// <summary>
    ///// Jira function returning currently logged user
    ///// </summary>
    //public const string CurrentUser = "currentUser()";
    public RequiredRejectedTesterString<Issue> IssueId { get; set; }
        = new("issue", i => i.Key);
    public RequiredRejectedTesterUserDetails<Issue> IssueAssignee { get; set; } 
        = new("assignee", i => i.Assignee);
    public RequiredRejectedTesterUserDetails<Issue> IssueReporter { get; set; } 
        = new("reporter", i => i.Reporter);
    public RequiredRejectedTesterUserDetails<Issue> IssueCreator { get; set; } 
        = new("creator", i => i.Creator);
    public StartEndDateTester<Issue> IssueUpdatedDate { get; set; } 
        = new("updated", i => i.Updated);
    public StartEndDateTester<Issue> IssueCreatedDate { get; set; } 
        = new("created", i => i.Created);
    public RequiredRejectedTesterUserDetails<Worklog> WorklogAuthor { get; set; } 
        = new("worklogAuthor", w => w.Author);
    public StartEndDateTester<Worklog> WorklogDate { get; set; }
        = new("worklogDate", w => w.Started);
    private IEnumerable<IJqlQueryItem> AllFields => [
        IssueId,
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
            if(item == null)
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
    public JqlQuery WhereIssueCreatedAfter(IJqlDate? date)
    {
        IssueCreatedDate.StartDate = date;
        return this;
    }
    public JqlQuery WhereIssueCreatedBefore(IJqlDate? date)
    {
        IssueCreatedDate.EndDate = date;
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


    public bool Equals(JqlQuery? other)
    {
        throw new NotImplementedException();
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as JqlQuery);
    }

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }
}
