using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Lira.Objects;

namespace LiraPS;
public static class RecentIssues
{
    public readonly record struct RecentItem(string Key, string Summary)
    {
        public static RecentItem FromIssue(IssueCommon issue)
        {
            return new(issue.Key, issue.SummaryPlain);
        }
    }
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
    private static List<RecentItem> _ids;
    public static IEnumerable<RecentItem> GetRecentIDs() => _ids.AsReadOnly();
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
    private static void Save()
    {
        using var fs = new FileStream(GetRecentsPath(), FileMode.OpenOrCreate, FileAccess.Write);
        System.Text.Json.JsonSerializer.Serialize(fs, _ids);
    }
    public static void Add(IssueCommon issue)
    {
        List<RecentItem> coll = [RecentItem.FromIssue(issue)];
        int counter = 1;
        foreach (var item in _ids)
        {
            if(string.Equals(item.Key,issue.Key, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            if(counter > 10)
            {
                break;
            }
            coll.Add(item);
            counter++;
        }
        _ids = coll;
        Save();
    }
    public static void Remove(IssueCommon issue)
    {
        _ids.RemoveAll(x => x.Key.Equals(issue.Key, StringComparison.OrdinalIgnoreCase));
        Save();
    }

}
