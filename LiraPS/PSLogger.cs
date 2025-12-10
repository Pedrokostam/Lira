using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LiraPS.Cmdlets;
using Microsoft.Extensions.Logging;

namespace LiraPS;

public readonly record struct Log(string Message, LogLevel Level, Exception? Exception)
{
    public string LevelSymbol => Level switch
    {
        LogLevel.Trace => "[TRC]",
        LogLevel.Debug => "[DBG]",
        LogLevel.Information => "[INF]",
        LogLevel.Warning => "[WRN]",
        LogLevel.Error => "[ERR]",
        LogLevel.Critical => "[CRT]",
        _ => "[???]",
    };
}
public class PSLogger<T> : IPSLogger<T>
{
    public FileInfo FilePath { get; }
    public bool FileExists { get; private set; }
    public LogLevel MinLevel { get; set; } = LogLevel.Information;
    private readonly ConcurrentQueue<Log> _fileQueue = new();
    private readonly ConcurrentQueue<Log> _stdQueue = new();
    private readonly object _lock = new();
    private const int MaxCount = 10_000;
    private readonly CancellationTokenSource _cts = new();
    public void Cancel()
    {
        _cts.Cancel(false);
    }
    public PSLogger(LogLevel minLvl = LogLevel.Trace)
    {
        MinLevel = minLvl;
        FilePath = new FileInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LiraPS", "log.log"));
        try
        {
            using var f = FilePath.OpenWrite();
            FileExists = true;
            Task.Factory.StartNew(HandleFileLogs, TaskCreationOptions.LongRunning);
        }
        catch (Exception)
        {
            FileExists = false;
        }
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        throw new NotImplementedException();
    }

    public bool IsEnabled(LogLevel logLevel) => logLevel >= MinLevel;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }
        var log = new Log(formatter(state, exception), logLevel, exception);
        _stdQueue.Enqueue(log);
        if (FileExists)
        {
            _fileQueue.Enqueue(log);
        }
    }


    public void PrintToStd(LiraCmdlet cmdlet)
    {
        while (_stdQueue.TryDequeue(out var log))
        {
            PSLogger<T>.Print1Log2Std(log, cmdlet);
        }
    }
    private static void Print1Log2Std(Log log, LiraCmdlet cmdlet)
    {
        var txt = log.Message;
        switch (log.Level)
        {
            case LogLevel.Trace:
            case LogLevel.Debug:
                cmdlet.WriteDebug(txt);
                break;
            case LogLevel.Information:
                cmdlet.WriteVerbose(txt);
                break;
            case LogLevel.Warning:
                cmdlet.WriteWarning(txt);
                break;
            case LogLevel.Error:
                cmdlet.WriteError(new ErrorRecord(log.Exception ?? new Exception(), txt, ErrorCategory.NotSpecified, null));
                break;
            case LogLevel.Critical:
                cmdlet.Terminate(log.Exception ?? new Exception(), txt, ErrorCategory.NotSpecified);
                break;
        }
    }



    private async Task HandleFileLogs()
    {
        if (!FileExists)
        {
            return;
        }
        int counter = 0;
        List<Log> tempColl = [];
        long fileSize = 0;
        while (true)
        {
            try
            {
                counter++;
                await Task.Delay(1500, _cts.Token);

                await UpdateFileLogs(tempColl);
                if (counter > 10 && fileSize != FilePath.Length)
                {
                    fileSize = FilePath.Length;
                    counter = 0;
                    var lines = await File.ReadAllLinesAsync(FilePath.FullName);
                    if (lines.Length <= MaxCount)
                    {
                        continue;
                    }
                    var linesToSkip = lines.Length - MaxCount;
                    Debug.WriteLine(message: $"Trimming file {FilePath} to {MaxCount} lines ({linesToSkip} will be removed).");
                    await File.WriteAllLinesAsync(FilePath.FullName, lines.Skip(linesToSkip), Encoding.UTF8, _cts.Token);
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Failed to write logs to file.");
                FileExists = false;
                return;
            }
        }
    }

    public async Task<bool> UpdateFileLogs(IList<Log>? tempColl = null)
    {
        tempColl ??= [];
        tempColl.Clear();
        while (_fileQueue.TryDequeue(out var log))
        {
            tempColl.Add(log);
        }
        if (tempColl.Count == 0)
        {
            return false;
        }
        Debug.WriteLine($"Writing {tempColl.Count} logs to file.");
        await File.AppendAllLinesAsync(FilePath.FullName, tempColl.Select(log => $"{log.LevelSymbol} {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {log.Message}"), Encoding.UTF8, _cts.Token);
        return true;
    }
}
