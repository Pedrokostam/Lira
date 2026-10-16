using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Lira.Objects;

namespace LiraPS;
/// <summary>
/// Provides a small in-memory cache of recently accessed issues and persists them to disk.
/// </summary>
/// <remarks>
/// The recents are stored in a JSON file named "RecentIssues.lbi" under the user's local application data
/// on Windows or under "~/.config/LiraPS" on non-Windows platforms. This static helper exposes methods to
/// add or remove entries and to enumerate the current recents. Operations that modify the collection persist
/// the list immediately.
/// </remarks>
public static class RecentIssues
{
    /// <summary>
    /// Represents a minimal summary of a recent issue: its key and a short summary text.
    /// </summary>
    /// <param name="Key">The unique issue key (e.g. "PROJ-123").</param>
    /// <param name="Summary">A plain-text summary of the issue.</param>
    public readonly record struct RecentItem(string Key, string Summary)
    {
        /// <summary>
        /// Creates a <see cref="RecentItem"/> from an <see cref="IssueCommon"/> instance.
        /// </summary>
        /// <param name="issue">The source issue to convert.</param>
        /// <returns>A new <see cref="RecentItem"/> containing the issue key and plain summary.</returns>
        public static RecentItem FromIssue(IssueCommon issue)
        {
            return new(issue.Key, issue.SummaryPlain);
        }
    }

    /// <summary>
    /// Compute and return the full path to the recents storage file.
    /// </summary>
    /// <remarks>
    /// On Windows the file is placed in %LocalAppData%\LiraPS\RecentIssues.lbi.
    /// On non-Windows platforms the file is placed in $HOME/.config/LiraPS/RecentIssues.lbi.
    /// The method ensures the directory exists (creates it if necessary).
    /// </remarks>
    /// <returns>The absolute path to the recents file.</returns>
    private static string GetRecentsPath()
    {
        string filename = "RecentIssues.lbi";
        string path;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var localAppdata = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            path = Path.Combine(localAppdata, nameof(LiraPS), filename);
        }
        else
        {
            var home = Environment.GetEnvironmentVariable("HOME")!;
            path = Path.Combine(home, ".config", nameof(LiraPS), filename);
        }
        string dir = Path.GetDirectoryName(path)!;
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        return path;
    }

    /// <summary>
    /// Backing store for the list of recent items kept in memory.
    /// </summary>
    /// <remarks>
    /// This list is initialized in the static constructor by reading the persisted JSON file if present.
    /// Modification methods update this list and then call <see cref="Save"/> to persist changes.
    /// </remarks>
    private static List<RecentItem> _ids;

    /// <summary>
    /// Returns a read-only view of the current recent items.
    /// </summary>
    /// <returns>An <see cref="IEnumerable{RecentItem}"/> representing the current recents in order (most recent first).</returns>
    public static IEnumerable<RecentItem> GetRecentIDs() => _ids.AsReadOnly();

    /// <summary>
    /// Returns the count of current recent items.
    /// </summary>
    public static int Count => _ids.Count;

    /// <summary>
    /// Static constructor. Initializes the in-memory recents list by loading persisted data if available.
    /// </summary>
    /// <remarks>
    /// If the recents file is missing or deserialization fails the list is initialized empty.
    /// Any deserialization exceptions are swallowed to avoid failing static initialization.
    /// </remarks>
    static RecentIssues()
    {
        var path = GetRecentsPath();
        _ids = [];
        if (File.Exists(path))
        {
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                _ids = System.Text.Json.JsonSerializer.Deserialize<List<RecentItem>>(fs) ?? [];
            }
            catch (Exception)
            {
            }
        }
    }

    /// <summary>
    /// Gets or sets the identifier of the most recent log entry that has not been completed. It's auto-nullified when <see cref="Add"/> is called.
    /// </summary>
    public static string? LastNotFinishedLogId { get; set; }

    /// <summary>
    /// Persist the current in-memory recents list to the configured storage file.
    /// </summary>
    /// <remarks>
    /// Opens the file with <see cref="FileMode.OpenOrCreate"/> and writes the JSON-serialized representation
    /// of <see cref="_ids"/>. Overwrites existing content.
    /// </remarks>
    private static void Save()
    {
        using var fs = new FileStream(GetRecentsPath(), FileMode.OpenOrCreate, FileAccess.Write);
        System.Text.Json.JsonSerializer.Serialize(fs, _ids);
    }

    /// <summary>
    /// Adds the specified issue to the front of the recent list and persists the change.
    /// </summary>
    /// <param name="issue">The issue to add to recents.</param>
    /// <remarks>
    /// The method prepends a <see cref="RecentItem"/> for <paramref name="issue"/> then appends existing entries
    /// from the current list, skipping any item whose key equals <paramref name="issue"/>. Key comparisons are
    /// case-insensitive. The resulting collection is truncated so that, besides the newly added item, at most
    /// nine previous items are retained (maximum total count of 10). The final list is assigned to the internal
    /// store and persisted via <see cref="Save"/>.
    /// </remarks>
    public static void Add(IssueCommon issue)
    {
        List<RecentItem> newlist = [RecentItem.FromIssue(issue)];
        int counter = 1;
        foreach (var item in _ids)
        {
            if(string.Equals(item.Key,issue.Key, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            if(counter > 9)
            {
                break;
            }
            newlist.Add(item);
            counter++;
        }
        _ids = newlist;
        LastNotFinishedLogId = null;
        Save();
    }

    /// <summary>
    /// Removes all recent entries that match the specified issue key (case-insensitive) and persists the change.
    /// </summary>
    /// <param name="issue">The issue whose matching entries should be removed from recents.</param>
    public static void Remove(IssueCommon issue)
    {
        _ids.RemoveAll(x => x.Key.Equals(issue.Key, StringComparison.OrdinalIgnoreCase));
        Save();
    }

}
