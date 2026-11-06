using ConsoleMenu;
using Lira.Jql;
using Lira.Objects;
using Lira.Toggl;
using LiraPS.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Net.Http;

namespace LiraPS.Toggl.Cmdlets
{
    [Cmdlet(VerbsData.Sync, "LiraJiraToggl", DefaultParameterSetName = "NORMAL")]
    [OutputType(typeof(Worklog))]
    public sealed class SyncJiraToggl : TogglCmdlet
    {
        private enum SyncState { Take, Skip }
        private enum CommentState { Use, Clear }


        private const int ProgressId = 31;

        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        public TogglJiraComparison Comparison { get; set; }

        [Parameter(ParameterSetName = "FORCEALL")]
        [Parameter(ParameterSetName = "NOCOMMENTS", Mandatory = true)]
        public SwitchParameter NoComments { get; set; }

        [Parameter(ParameterSetName = "FORCECOMMENTS", Mandatory = true)]
        public SwitchParameter ForceComments { get; set; }

        [Parameter(ParameterSetName = "FORCEALL", Mandatory = true)]
        public SwitchParameter Force { get; set; }
        private int IssueWidth { get; set; }

        private readonly List<TogglJiraSuggestion> _suggestions = [];

        protected override void ProcessRecord()
        {
            _suggestions.AddRange(Comparison.Suggestions);
        }


        protected override void EndProcessing()
        {
            if (_suggestions.Count == 0)
            {
                WriteWarning("No worklogs to add.");
                return;
            }
            IssueWidth = _suggestions.Max(x => x.Issue.Length);
            List<TogglJiraSuggestion> selected = _suggestions;
            if (Force.IsPresent)
            {
                if (NoComments.IsPresent)
                {
                    selected = [.. _suggestions.Select(x => x with { Comment = null })];
                }
                selected = TogglJiraComparer.MergeUncommented(selected);
            }
            else
            {
                var menuItemsInitial = _suggestions
                    .Select(x => new MenuItemWithState<SyncState, TogglJiraSuggestion>(new(FormatLine(x, IssueWidth).Format().PlainText, x), SyncState.Take));
                var selectedMenuItems = ShowToggleMenu(menuItemsInitial, "Select logs to add", SyncFormat, ContentFormat)
                    .Where(x => x.State == SyncState.Take).Select(x => x.Item).ToList();
                if (selectedMenuItems.Count == 0)
                {
                    WriteWarning("No worklogs selected.");
                    return;
                }

                var commented = selectedMenuItems.Where(x => !string.IsNullOrWhiteSpace(x.Payload.Comment));

                if (!NoComments.IsPresent && !ForceComments.IsPresent)
                {
                    if (!commented.Any())
                    {
                        NoComments = true; // remove one case - no comments in suggestion set NoComments and takes its path
                    }
                    else if (!ChoiceYesNo("Do you want to remove comments from some of the logs?", defaultNo: true))
                    {
                        ForceComments = true; // remove one case - now if the user decline it will take the path of force comments
                    }
                }

                if (NoComments.IsPresent)
                {
                    selected = [.. selectedMenuItems.Select(x => x.Payload with { Comment = null })];
                }
                else if (ForceComments.IsPresent)
                {
                    selected = [.. selectedMenuItems.Select(x => x.Payload)];
                }
                else
                {
                    var items = commented.Select(x => new MenuItemWithState<CommentState, TogglJiraSuggestion>(x, CommentState.Use));
                    var cleared = ShowToggleMenu(items, "Select comments to clear", CommentFormat, ContentFormatComment)
                        .Where(x => x.State == CommentState.Clear).Select(x => x.Payload).ToHashSet(ReferenceEqualityComparer.Instance);
                    selected.Clear();
                    for (int i = 0; i < selectedMenuItems.Count; i++)
                    {
                        var curr = selectedMenuItems[i].Payload;
                        if (cleared.Contains(curr))
                        {
                            selected.Add(curr with { Comment = null });
                        }
                        else
                        {
                            selected.Add(curr);
                        }
                    }
                }
                selected = TogglJiraComparer.MergeUncommented(selected);
                Confirm(selected);
            }
            AddLogs(selected);
            base.EndProcessing();
        }

        private IReadOnlyList<MenuItemWithState<TState, TogglJiraSuggestion>> ShowToggleMenu<TState>(IEnumerable<MenuItemWithState<TState, TogglJiraSuggestion>> items, string preamble,
            Func<TState, bool, LineParts> stateFormatter, Func<MenuItemWithState<TState, TogglJiraSuggestion>, bool, LineParts> contentFormatter) where TState : struct, Enum
        {
            int stateWidth = Enum.GetNames<TState>().Max(x => x.Length);
            // "==> [" + state + "] " precedes each row
            var header = new string(' ', stateWidth + 7) + FormatHeader(IssueWidth);
            var menu = new ToggleMenu<TState, TogglJiraSuggestion>(header, items, new Part(preamble), clearAfterFinish: true, stateFormatter: stateFormatter, contentFormatter: contentFormatter);
            return menu.Show();
        }

        private void Confirm(List<TogglJiraSuggestion> pending)
        {
            WriteHost("The following worklogs will be added", ConsoleColor.Cyan);
            WriteHost("");
            WriteHost(FormatHeader(IssueWidth));
            foreach (var log in pending)
            {
                WriteHost(FormatLine(log, IssueWidth).Format().Text);
            }
            WriteHost("");
            if (!ChoiceYesNo("Add these worklogs?", "Send the worklogs to the Jira server", "Abort and send nothing to the Jira server"))
            {
                UserCancel("worklog adding");
            }
        }

        private void AddLogs(List<TogglJiraSuggestion> pending)
        {
            bool addedAny = false;
            var progress = new ProgressRecord(ProgressId, "Adding worklogs", "Starting...");
            try
            {
                var machine = LiraSession.Client.GetAddWorklogMachine();
                for (int i = 0; i < pending.Count; i++)
                {
                    var suggestion = pending[i];
                    progress.StatusDescription = $"{i + 1}/{pending.Count}: {suggestion.Issue}";
                    progress.PercentComplete = i * 100 / pending.Count;
                    WriteProgress(progress);
                    ENSURE_TESTING(suggestion.Issue);
                    LiraSession.Logger.LogInformation("Adding worklog to {issue}", suggestion.Issue);
                    try
                    {
                        var state = machine.GetStartState(suggestion.Issue, new WorklogToAdd(suggestion.Started, suggestion.Duration, suggestion.Comment));
                        while (!state.IsFinished)
                        {
                            state = machine.Process(state).GetAwaiter().GetResult();
                            PrintLogs();
                        }
                        if (state.AddedWorklog is Worklog added)
                        {
                            addedAny = true;
                            LiraSession.CacheWorklog(added);
                            RecentIssues.Add(added.Issue);
                            LiraSession.Logger.LogInformation("Added worklog {id}", added.ID);
                            WriteObject(added);
                        }
                        else
                        {
                            LiraSession.Logger.LogError("Failed adding worklog to {issue}", suggestion.Issue);
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        // One rejected worklog should not stop the rest of the batch.
                        WriteError(new ErrorRecord(ex, "AddWorklogFailed", ErrorCategory.InvalidResult, suggestion));
                    }
                }
                progress.RecordType = ProgressRecordType.Completed;
                WriteProgress(progress);
            }
            finally
            {
                // In finally so that an interrupted batch still invalidates queries that may include the added worklogs.
                if (addedAny)
                {
                    LiraSession.ClearQueryCache();
                }
            }
        }

        private static string FormatHeader(int issueWidth)
        {
            return $"{"Issue".PadRight(issueWidth)} | {"Started",16} | {"Duration",8} | Comment";
        }

        // Same columns as the TogglJiraSuggestion view in Formatters/TogglJiraComparison.ps1xml.
        private static LineParts FormatLine(TogglJiraSuggestion suggestion, int issueWidth, GraphicModes mode = GraphicModes.None)
        {
            var commentMode = string.IsNullOrWhiteSpace(suggestion.Comment) ? GraphicModes.Dim | GraphicModes.Italics : GraphicModes.None;
            var com = string.IsNullOrWhiteSpace(suggestion.Comment) ? "No comment" : suggestion.Comment;
            return [
                new Part(suggestion.Issue.PadRight(issueWidth),mode|GraphicModes.Bold),
                " | ",
                new Part(suggestion.Started.PrettyDate(),mode,ConsoleColor.Cyan)
                ," | ",
                new Part(suggestion.Duration.PrettyTime(0).PadRight(8),mode,ConsoleColor.Magenta),
                " | ",
                new Part(com,commentMode),
                ];
        }
        private static LineParts SyncFormat(SyncState state, bool selected)
        {
            return state switch
            {
                SyncState.Take => [new Part("TAKE", Color: ConsoleColor.Green)],
                _ => [new Part("SKIP", StartMode: GraphicModes.Dim)],
            };
        }
        private static LineParts CommentFormat(CommentState state, bool selected)
        {
            return state switch
            {
                CommentState.Use => [new Part(" Use ", Color: ConsoleColor.Green)],
                _ => [new Part("Clear", StartMode: GraphicModes.Dim)],
            };
        }
        private LineParts ContentFormat(MenuItemWithState<SyncState, TogglJiraSuggestion> item, bool selected)
        {
            return FormatLine(item.Payload, IssueWidth);
        }
        private LineParts ContentFormatComment(MenuItemWithState<CommentState, TogglJiraSuggestion> item, bool selected)
        {
            var sugg = item.State switch
            {
                CommentState.Use => item.Payload,
                _ => item.Payload with { Comment = null },
            };
            return FormatLine(sugg, IssueWidth);
        }
    }
}
