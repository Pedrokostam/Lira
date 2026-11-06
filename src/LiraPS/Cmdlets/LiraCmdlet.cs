using System;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Host;
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
using LiraPS.Completers;
using LiraPS.Transformers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Events;
using Serilog.Formatting.Display;
using ConsoleMenu;

namespace LiraPS.Cmdlets
{
    public abstract class LiraCmdlet : PSCmdlet, IDisposable
    {
        /// <summary>
        /// Terminates if the <paramref name="text"/> is empty or whitespace
        /// </summary>
        /// <param name="text"></param>
        /// <param name="name"></param>
        protected void EnsureNotEmpty(string text, string name)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                Terminate(new PSArgumentException($"{name} cannot be empty"), $"Empty{name}");
            }
        }
        protected static void ThrowIfNull([NotNull] object? argument, [CallerArgumentExpression("argument")] string? paramName = null)
        {
            if (argument is null)
            {
                throw new PSArgumentNullException(paramName, "Argument must not be null");
            }
        }
        static LiraCmdlet()
        {
            Console.CancelKeyPress += DumpLogEvent;
        }
        private static int _active;
        protected LiraCmdlet() : base()
        {
            // Drop logs left unprinted by a previous invocation (e.g. interrupted with Ctrl-C).
            // Only the outermost invocation clears; nested/pipelined Lira cmdlets share the queue.
            if (Interlocked.Increment(ref _active) == 1)
            {
                LiraSession.Logger.ClearStd();
            }
        }
        // PowerShell disposes cmdlets when the pipeline ends, including when stopped with Ctrl-C.
        public void Dispose() => Interlocked.Decrement(ref _active);
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
        /// <summary>
        /// Check if the given argument was specified in the command line
        /// </summary>
        /// <param name="name"></param>
        /// <returns>If parameter was explicitly specified - <see langword="true"/>. If not - <see langword="false"/></returns>
        protected bool TestBoundParameter(string name)
        {
            return MyInvocation.BoundParameters.ContainsKey(name);
        }

        public static void DumpLog()
        {
            Debug.WriteLine("Dumping remaining log to file...");
            LiraSession.Logger.UpdateFileLogs().GetAwaiter().GetResult();
            //LiraSession.Logger.PrintToStd(this);
        }
        public static void DumpLogEvent(object? s, ConsoleCancelEventArgs e)
        {
            Debug.WriteLine("Caught Ctrl-C...");
            LiraSession.Logger.LogError("Operation cancelled by user (Ctrl-C)");
            DumpLog();
            e.Cancel = false;
        }

        protected override void BeginProcessing()
        {
            TestLiraSession();
        }
        public void PrintLogs()
        {
            LiraSession.Logger.PrintToStd(this);
        }
        protected override void EndProcessing()
        {
            base.EndProcessing();
            LiraSession.ValidateWorklogCache();
            DumpLog();
        }

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
        protected void TestLiraSession()
        {
            if (!LiraSession.TestSessionDataAvailable())
            {
                throw new PSInvalidOperationException("You have to configure Jira session parameters first. Call Set-LiraConfiguration.");
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
                WriteInformation(message, ["PSHOST"]);
            }
        }
        public void WriteHost(LineParts parts) => WriteHost(parts.Format().Text);

        [System.Diagnostics.CodeAnalysis.DoesNotReturn]
        public void Terminate<T>(T error, string id, ErrorCategory category = ErrorCategory.InvalidArgument, object? target = null) where T : Exception
        {
            DumpLog();
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

        protected static JqlQuery.GoodBadPair DivideConditionCollection(IEnumerable<string> items)
        {
            List<string> good = [];
            List<string> bad = [];
            foreach (var item in items)
            {
                var trim = item.Trim();
                if (trim.StartsWith('!'))
                {
                    bad.Add(trim.TrimStart('!').TrimStart());
                }
                else
                {
                    good.Add(trim);
                }
            }
            return new(good, bad);
        }

        // ChoiceMenu always starts at the first item, so defaultNo puts "No" first.
        protected static bool ChoiceYesNo(string header, string? yesTooltip = null, string? noTooltip = null, bool defaultNo = false)
        {
            MenuItem<bool> yes = new("Yes", true, yesTooltip);
            MenuItem<bool> no = new("No", false, noTooltip);
            return new ChoiceMenu<bool>(header, defaultNo ? [no, yes] : [yes, no]).Show();
        }

        /// <summary>
        /// YOU CAN ONLY TOUCH AVP-425
        /// </summary>
        /// <param name="issue"></param>
        /// <exception cref="InvalidOperationException"></exception>
        protected static void ENSURE_TESTING(string issue)
        {
#if DEBUG
            if (!"AVP-425".Equals(issue, StringComparison.OrdinalIgnoreCase))
            {
                throw new PSInvalidOperationException("DO NOT TEST ON ANYTHING BUT AVP-425!!!!");
            }
#endif
        }

        [System.Diagnostics.CodeAnalysis.DoesNotReturn]
        protected void UserCancel(string operation)
        {
            var pascalCase = string.Join("", operation.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.RemoveEmptyEntries).Select(x => char.ToUpperInvariant(x[0]) + x[1..]));
            Terminate(new PSInvalidOperationException($"Reporter cancelled {operation}"), $"{pascalCase}Cancelled", ErrorCategory.InvalidOperation);
        }
    }
}
