using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LiraPS;
public readonly record struct Log(string Message, LogLevel Level, Exception? Exception)
{
}
public class PSLogger<T>(string filepath) : ILogger<T>, IEnumerable<Log>
{
    public string FilePath { get; } = filepath;
    private readonly Queue<Log> _queue = new();
    private readonly object _lock = new object();
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        throw new NotImplementedException();
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var log = new Log(formatter(state, exception), logLevel, exception);
        _queue.Enqueue(log);
        Debug.WriteLine(log.Message);
        lock (_lock)
            File.AppendAllText(FilePath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss:fff}: {log.Message}\n");
    }

    public IEnumerator<Log> GetEnumerator()
    {
        while (_queue.Count > 0)
        {
            yield return _queue.Dequeue();
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
