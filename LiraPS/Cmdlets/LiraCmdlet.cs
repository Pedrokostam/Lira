using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Lira.Objects;
using Microsoft.Extensions.Logging;

namespace LiraPS.Cmdlets
{
    public abstract class LiraCmdlet : PSCmdlet
    {
        protected LiraCmdlet() : base()
        {

        }
        protected void SetGlobal(string name, object data)
        {
            var variable = new PSVariable(name, data, ScopedItemOptions.AllScope);
            SessionState.PSVariable.Set(variable);
        }
        protected bool TryGetBoundParameter<T>(string name, [NotNullWhen(true)] out T? value)
        {
            if (TestBoundParameter(name))
            {
                value = (T)MyInvocation.BoundParameters[name];
                return true;
            }
            value = default;
            return false;
        }
        protected bool TestBoundParameter(string name)
        {
            return MyInvocation.BoundParameters.ContainsKey(name);
        }

        protected override void BeginProcessing()
        {
            TestSession();
        }

        protected void PrintLogs()
        {
            foreach (var item in LiraSession.LogQueue)
            {
                Print(item);
            }
        }
        protected override void EndProcessing()
        {
            base.EndProcessing();
            PrintLogs();
        }

        ///// <summary>
        ///// Return a disposable object, that will redirect all logs to the current cmdlet until it is disposed.
        ///// </summary>
        ///// <returns></returns>
        //protected PSCmdletSink.PSCmdletContext SetLogContext()
        //{
        //    return LiraSession.LogSink.SetContext(this);
        //}

        protected string ReadInput(string message, bool asSecure = false)
        {
            Host.UI.Write(message + ": ");
            if (asSecure)
            {
                var secure = Host.UI.ReadLineAsSecureString();
                return new System.Net.NetworkCredential("", secure).Password;
            }
            else
            {
                return Host.UI.ReadLine();
            }
        }
        protected void TestSession()
        {
            if (!LiraSession.TestSessionDateAvailable())
            {
                throw new InvalidOperationException("You have to configure jira session parameters first. Call Set-Configuration.");
            }
            LiraSession.StartSession().Wait();
            PrintLogs();
        }
        protected void Print(Log log)
        {
            var txt = log.Message;
            switch (log.Level)
            {
                case LogLevel.Information:
                case LogLevel.Trace:
                    WriteVerbose(txt);
                    break;
                case LogLevel.Debug:
                    WriteDebug(txt);
                    break;
                //case LogLevel.Information:
                //    WriteInformation(txt, ["Info"]);
                //    break;
                case LogLevel.Warning:
                    WriteWarning(txt);
                    break;
                case LogLevel.Error:
                    WriteError(new ErrorRecord(log.Exception ?? new Exception(), txt, ErrorCategory.NotSpecified, null));
                    break;
                case LogLevel.Critical:
                    ThrowTerminatingError(new ErrorRecord(log.Exception ?? new Exception(), txt, ErrorCategory.NotSpecified, null));
                    break;
            }
        }

    }

}
