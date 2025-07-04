using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Lira;
using Lira.Jql;
using Lira.Objects;
using Lira.StateMachines;
using LiraPS.Cmdlets;
using LiraPS.Transformers;
using Microsoft.Extensions.Logging;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace LiraPS.Cmdlets
{
    public abstract class LiraCmdlet : PSCmdlet
    {
        protected void EnsureNotEmpty(string text, string name)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                Terminate(new ArgumentException($"{name} cannot be empty"), $"Empty{name}");
            }
        }
        protected LiraCmdlet() : base()
        {

        }
        protected void SetGlobal(string name, object data)
        {
            var variable = new PSVariable(name, data, ScopedItemOptions.AllScope);
            SessionState.PSVariable.Set(variable);
        }
        protected object GetGlobal(string name)
        {
            return SessionState.PSVariable.GetValue(name);
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
                using var secure = Host.UI.ReadLineAsSecureString();
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
        public void WriteHost(string message, ConsoleColor? foregroundColor = null, ConsoleColor? backgroundColor = null)
        {
            // If colors are specified, use Host.UI.WriteLine for color output
            if (foregroundColor.HasValue || backgroundColor.HasValue)
            {
                Host.UI.WriteLine(
                    foregroundColor ?? Host.UI.RawUI.ForegroundColor,
                    backgroundColor ?? Host.UI.RawUI.BackgroundColor,
                    message
                );
            }
            else
            {
                // Otherwise, use WriteInformation to write to the information stream
                WriteInformation(message, new string[] { "PSHOST" });
            }
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
                    Terminate(log.Exception ?? new Exception(), txt, ErrorCategory.NotSpecified);
                    break;
            }
        }
        [System.Diagnostics.CodeAnalysis.DoesNotReturn]
        protected void Terminate<T>(T error, string id, ErrorCategory category = ErrorCategory.InvalidArgument, object? target = null) where T : Exception
        {
            ThrowTerminatingError(
                   new ErrorRecord(
                       error,
                       id,
                       category,
                       target));
        }
        protected static string ReplaceCurrentUserAlias(string value)
        {
            var l = value.ToLowerInvariant();
            var replacement = l switch
            {
                "me" or "myself" or "current" or "currentuser" or "ooh! a clone of myself" => LiraSession.Client.Myself.Name,
                _ => l
            };
            return replacement;
        }

    }

}
