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
    [Cmdlet(VerbsData.Sync, "LiraJiraToggl", DefaultParameterSetName = "NOCOMMENTS")]
    [OutputType(typeof(Worklog))]
    public sealed class SyncJiraToggl : TogglCmdlet
    {
        private enum SyncState { Take, Skip }
        private enum CommentState { Use, Clear }


        private const int ProgressId = 31;

        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        public TogglJiraComparison Comparison { get; set; }

        [Parameter(ParameterSetName = "FORCEALL")]
        [Parameter(ParameterSetName = "NOCOMMENTS")]
        public SwitchParameter NoComments { get; set; }

        [Parameter(ParameterSetName = "FORCECOMMENTS")]
        public SwitchParameter ForceComments { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "FORCEALL")]
        public SwitchParameter Force { get; set; }

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
            int issueWidth = _suggestions.Max(x => x.Issue.Length);
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
                    .Select(x => new MenuItem(FormatLine(x, issueWidth), x))
                    .Select(x => new ToggleMenu<SyncState>.MenuItemWithState(x, SyncState.Take))
                    .ToList();
                ShowToggleMenu(menuItemsInitial, "Select logs to add", issueWidth, SyncFormat);
                var selectedMenuItems = menuItemsInitial.Where(x => x.State == SyncState.Take).Select(x => x.Item).ToList();
                if (selectedMenuItems.Count == 0)
                {
                    WriteWarning("No worklogs selected.");
                    return;
                }

                var commented = selectedMenuItems.Where(x => !string.IsNullOrWhiteSpace((x.Payload as TogglJiraSuggestion)?.Comment));

                if (!NoComments.IsPresent && !ForceComments.IsPresent)
                {
                    if (!commented.Any())
                    {
                        NoComments = true; // remove one case - no comments in suggestion set NoComments and takes its path
                    }
                    else if (ChoiceYesNo("Do you want to remove comments from some of the logs?", ChoiceOptions.No) == ChoiceOptions.No)
                    {
                        ForceComments = true; // remove one case - now if the user decline it will take the path of force comments
                    }
                }

                if (NoComments.IsPresent)
                {
                    selected = [.. selectedMenuItems.Select(x => (TogglJiraSuggestion)x.Payload! with { Comment = null })];
                }
                else if (ForceComments.IsPresent)
                {
                    selected = [.. selectedMenuItems.Select(x => (TogglJiraSuggestion)x.Payload!)];
                }
                else
                {
                    var items = commented.Select(x => new ToggleMenu<CommentState>.MenuItemWithState(x, CommentState.Use)).ToList();
                    ShowToggleMenu(items, "Select comments to clear", issueWidth, CommentFormat);
                    var cleared = items.Where(x => x.State == CommentState.Clear).Select(x => x.Payload).OfType<TogglJiraSuggestion>().ToHashSet(ReferenceEqualityComparer.Instance);
                    selected.Clear();
                    for (int i = 0; i < selectedMenuItems.Count; i++)
                    {
                        var curr = selectedMenuItems[i].Payload as TogglJiraSuggestion;
                        if (cleared.Contains(curr))
                        {
                            selected.Add(curr! with { Comment = null });
                        }
                        else
                        {
                            selected.Add(curr!);
                        }
                    }
                }
                selected = TogglJiraComparer.MergeUncommented(selected);
                Confirm(selected, issueWidth);
            }
            AddLogs(selected);
            base.EndProcessing();
        }

        private static void ShowToggleMenu<TState>(List<ToggleMenu<TState>.MenuItemWithState> items, string preamble, int issueWidth, Func<TState, string>? formatter) where TState : struct, Enum
        {
            int stateWidth = Enum.GetNames<TState>().Max(x => x.Length);
            var header = new string(' ', stateWidth + 4) + FormatHeader(issueWidth);
            var menu = new ToggleMenu<TState>(header, items, new Part(preamble), stateFormatter: formatter ?? (x => x.ToString().PadRight(stateWidth)), clearAfterFinish: true);
            menu.Show();
        }

        private void Confirm(List<TogglJiraSuggestion> pending, int issueWidth)
        {
            WriteHost("The following worklogs will be added", ConsoleColor.Cyan);
            WriteHost("");
            WriteHost(FormatHeader(issueWidth));
            foreach (var log in pending)
            {
                WriteHost(FormatLine(log, issueWidth));
            }
            WriteHost("");
            if (ChoiceYesNo("Add these worklogs?", "Send the worklogs to the Jira server", "Abort and send nothing to the Jira server") == ChoiceOptions.No)
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
        private static string FormatLine(TogglJiraSuggestion suggestion, int issueWidth)
        {
            return $"{suggestion.Issue.PadRight(issueWidth)}   {suggestion.Started.PrettyDate()}   {suggestion.Duration.PrettyTime(0),8}   {suggestion.Comment ?? $"{Dim}No comment{Reset}"}";
        }
        private string SyncFormat(SyncState state)
        {
            return state switch
            {
                SyncState.Take => "Take",

                _ => $"{Dim}Skip{Reset}",
            };
        }
        private string CommentFormat(CommentState state)
        {
            return state switch
            {
                CommentState.Use => "Use  ",

                _ => $"{Dim}Clear{Reset}",
            };
        }
    }
}
