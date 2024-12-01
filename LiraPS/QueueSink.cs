//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Management.Automation;
//using System.Threading;
//using Microsoft.Extensions.Logging;

//namespace LiraPS;
//public class PSCmdletSink : ILogEventSink
//{
//    public class PSCmdletContext : IDisposable
//    {
//        internal PSCmdletContext(PSCmdletSink parent, PSCmdlet currentCmdlet)
//        {
//            Parent = parent;
//            CurrentCmdlet = currentCmdlet;
//        }

//        private PSCmdletSink Parent { get; }
//        PSCmdlet CurrentCmdlet { get; }

//        public void Dispose()
//        {
//            Parent.Remove(this);
//            GC.SuppressFinalize(this);
//        }
//        public void Print(LogEvent log)
//        {
//            var txt = log.RenderMessage();
//            switch (log.Level)
//            {
//                case LogEventLevel.Verbose:
//                    CurrentCmdlet.WriteVerbose(txt);
//                    break;
//                case LogEventLevel.Debug:
//                    CurrentCmdlet.WriteDebug(txt);
//                    break;
//                case LogEventLevel.Information:
//                    CurrentCmdlet.Host.UI.WriteLine(txt);
//                    break;
//                case LogEventLevel.Warning:
//                    CurrentCmdlet.WriteWarning(txt);
//                    break;
//                case LogEventLevel.Error:
//                case LogEventLevel.Fatal:
//                    CurrentCmdlet.WriteError(new ErrorRecord(log.Exception ?? new Exception(), txt, ErrorCategory.NotSpecified, null));
//                    break;
//            }
//        }
//    }
//    private static AsyncLocal<PSCmdlet?> _currentCmdlet = new();
//    //public PSCmdletContext SetContext(PSCmdlet currentCmdlet)
//    //{
//    //    var context = new PSCmdletContext(this, currentCmdlet);
//    //    _stack.Add(context);
//    //    return context;
//    //}
//    private List<PSCmdletContext> _stack = [];
//    private void Remove(PSCmdletContext context)
//    {
//        _stack.Remove(context);
//    }
//    public void Emit(LogEvent logEvent)
//    {
//        if (_currentCmdlet.Value is null)
//        {
//            return;
//        }
//        Print(logEvent);
//    }
//    public void SetContext(PSCmdlet? cmdlet)
//    {
//        _currentCmdlet.Value = cmdlet;
//    }
//    public void Print(LogEvent log)
//    {
//        Debug.WriteLine(Thread.CurrentThread.ManagedThreadId);
//        var txt = log.RenderMessage();
//        var CurrentCmdlet = _currentCmdlet.Value!;
//        switch (log.Level)
//        {
//            case LogEventLevel.Verbose:
//                CurrentCmdlet.WriteVerbose(txt);
//                break;
//            case LogEventLevel.Debug:
//                CurrentCmdlet.WriteDebug(txt);
//                break;
//            case LogEventLevel.Information:
//                CurrentCmdlet.Host.UI.WriteLine(txt);
//                break;
//            case LogEventLevel.Warning:
//                CurrentCmdlet.WriteWarning(txt);
//                break;
//            case LogEventLevel.Error:
//            case LogEventLevel.Fatal:
//                CurrentCmdlet.WriteError(new ErrorRecord(log.Exception ?? new Exception(), txt, ErrorCategory.NotSpecified, null));
//                break;
//        }
//    }

//}
//public class QueueSink : ILogEventSink, IEnumerable<LogEvent>
//{
//    private readonly Queue<LogEvent> _queue = new();
//    public QueueSink()
//    {

//    }
//    public void Emit(LogEvent logEvent)
//    {
//        _queue.Enqueue(logEvent);
//    }

//    public IEnumerator<LogEvent> GetEnumerator()
//    {
//        while (_queue.Count > 0)
//        {
//            yield return _queue.Dequeue();
//        }
//    }

//    IEnumerator IEnumerable.GetEnumerator()
//    {
//        return GetEnumerator();
//    }
//}

//[Target("CustomLogSink")]
//public class CustomLogSinkTarget : TargetWithLayout, IEnumerable<CustomLogSinkTarget.Log>
//{
//    public readonly record struct Log(string RenderedString, Microsoft.Extensions.Logging.LogLevel LogLevel)
//    {
//    }
//    private readonly Queue<Log> _queue = new();

//    // Accessor to retrieve messages later
//    public IEnumerator<Log> GetEnumerator()
//    {
//        while (_queue.Count > 0)
//        {
//            yield return _queue.Dequeue();
//        }
//    }

//    IEnumerator IEnumerable.GetEnumerator()
//    {
//        return GetEnumerator();
//    }

//    protected override void Write(LogEventInfo logEvent)
//    {
//        // Add the log message to the queue
//        _queue.Enqueue(new(Layout.Render(logEvent),(Microsoft.Extensions.Logging.LogLevel)logEvent.Level.Ordinal));
//    }
//}