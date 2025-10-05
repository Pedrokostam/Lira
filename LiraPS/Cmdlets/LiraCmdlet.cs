using System;
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

namespace LiraPS.Cmdlets
{
    public abstract class LiraCmdlet : PSCmdlet
    {
        protected void EnsureNotEmpty(string text, string name)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                Terminate(new PSArgumentException($"{name} cannot be empty"), $"Empty{name}");
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
        /// <summary>
        /// Check if the given argument was specified in the command line
        /// </summary>
        /// <param name="name"></param>
        /// <returns>If parameter was explicitly specified - <see langword="true"/>. If not - <see langword="false"/></returns>
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
            LiraSession.ValidateWorklogCache();
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
                throw new PSInvalidOperationException("You have to configure jira session parameters first. Call Set-Configuration.");
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
                case LogLevel.Trace:
                case LogLevel.Debug:
                    WriteDebug(txt);
                    break;
                case LogLevel.Information:
                    WriteVerbose(txt);
                    break;
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

        protected record MenuItem
        {
            private string? tooltip;

            public required string Name { get; init; }
            public string? Tooltip
            {
                get => tooltip; init
                {
                    TooltipLines = value?.Split(["\n", "\r\n", "\r"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.RemoveEmptyEntries) ?? [];
                    TooltipHeight = TooltipLines.Length;
                    TooltipWidth = TooltipLines.Length == 0 ? 0 : TooltipLines.Max(x => x.Length);
                    tooltip = string.Join(Environment.NewLine, TooltipLines);
                }
            }
            public string[] TooltipLines { get; private init; } = [];
            public int TooltipHeight { get; private init; }
            public int TooltipWidth { get; private init; }
            public required object? Payload { get; init; }
            public bool HasTooltip => !string.IsNullOrEmpty(Tooltip);
            [SetsRequiredMembers]
            public MenuItem(string name, object? payload, string? tooltip = null)
            {
                Name = name;
                Tooltip = tooltip;
                Payload = payload;
            }
        }

        protected const string Reset = "\u001b[0m";
        protected const string Invert = "\u001b[7m";
        protected const string Bold = "\u001b[1m";
        protected const string Dim = "\u001b[2m";
        protected const string Italics = "\u001b[3m";
        protected bool MenuYesNo(string header)
        {
            return (bool)Menu(header, new MenuItem("Yes", true), new MenuItem("No", false))!;
        }
        protected bool MenuNoYes(string header)
        {
            return (bool)Menu(header, new MenuItem("No", false), new MenuItem("Yes", true))!;
        }
        protected enum ChoiceOptions
        {
            No,
            Yes,
            Cancel,
            YesToAll,
        }
        [Flags]
        protected enum ChoiceSettings
        {
            None = 0,
            YesNo = 1 << 1,
            Cancel = 1 << 2,
            YesToAll = 1 << 3,
            YesNoCancel = YesNo | Cancel,
            YesNoYesToAll = YesNo | YesToAll,
            AllOptions = YesNo | Cancel | YesToAll,
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="header"></param>
        /// <param name="preselection"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        protected ChoiceOptions ChoiceYesNo(string header, ChoiceOptions? preselection = null, ChoiceSettings settings = ChoiceSettings.YesNo, string? message = null)
        {
            if (settings == ChoiceSettings.None)
            {
                settings = ChoiceSettings.YesNo;
            }
            var yes = new ChoiceDescription("&Yes");
            var no = new ChoiceDescription("&No");
            var yesAll = new ChoiceDescription("Yes to &All");
            var cancel = new ChoiceDescription("&Cancel");
            List<ChoiceOptions> outputs = [ChoiceOptions.Yes];
            var options = new System.Collections.ObjectModel.Collection<ChoiceDescription>() {
                yes,
            };
            if (settings.HasFlag(ChoiceSettings.YesToAll))
            {
                outputs.Add(ChoiceOptions.YesToAll);
                options.Add(yesAll);
            }
            outputs.Add(ChoiceOptions.No);
            options.Add(no);
            if (settings.HasFlag(ChoiceSettings.Cancel))
            {
                outputs.Add(ChoiceOptions.Cancel);
                options.Add(cancel);
            }
            int startIndex = preselection switch
            {
                ChoiceOptions.No => options.IndexOf(no),
                ChoiceOptions.Yes => 0,
                ChoiceOptions.Cancel => options.IndexOf(cancel),
                ChoiceOptions.YesToAll => options.IndexOf(yesAll),
                _ => -1,
            };
            var ch = Host.UI.PromptForChoice(header, message ?? "\n", options, startIndex);
            return outputs[ch];

        }
        protected object? Menu(string header, params MenuItem[] options)
        {
            if (Console.IsInputRedirected)
            {
                Terminate(new PSInvalidOperationException("Host does not allow interactivity"), "UnsupportedHost", ErrorCategory.InvalidOperation);
            }

            if (options.Length == 0)
            {
                return null;
            }
            try
            {
                Console.TreatControlCAsInput = true;
                Console.CursorVisible = false;

                int maxTooltipHeight = options.Max(x => x.TooltipHeight);
                int maxTooltipWidth = options.Max(x => x.TooltipWidth);
                int count = options.Length;
                int max = count - 1;
                int choice = 0;

                Console.WriteLine();
                //Console.WriteLine($"{Bold}Cancel{Reset} = Backspace, Ctrl-C || {Bold}Move selection{Reset} = Arrows, Digits || {Bold}Accept{Reset} = Enter ");
                WriteHost(header);
                Console.WriteLine();
                while (true)
                {
                    // Print menu options
                    for (int i = 0; i < count; i++)
                    {
                        if (choice == i)
                        {
                            Console.WriteLine($"  [{i + 1}] {Invert}{options[i].Name}{Reset}        ");
                        }
                        else
                        {
                            Console.WriteLine($"   {i + 1}  {options[i].Name}           ");
                        }
                    }

                    var selected = options[choice];
                    Console.WriteLine();
                    // Print tooltip if available
                    if (selected.HasTooltip)
                    {
                        foreach (var item in selected.TooltipLines)
                        {
                            Console.WriteLine("  " + Italics + item.PadRight(maxTooltipWidth + 1) + Reset);
                        }
                    }
                    // Fill remaining tooltip space for consistent redraw
                    for (int t = selected.TooltipHeight; t < maxTooltipHeight; t++)
                    {
                        Console.WriteLine("".PadRight(maxTooltipWidth + 1));
                    }

                    // Read user input
                    var info = Console.ReadKey(intercept: true);

                    if (info.Modifiers.HasFlag(ConsoleModifiers.Control) && info.Key == ConsoleKey.C)
                    {
                        Console.WriteLine();
                        Terminate(new PipelineStoppedException("Exited menu"), "MenuCancelled", ErrorCategory.OperationStopped);
                    }

                    switch (info.Key)
                    {
                        case ConsoleKey.DownArrow:
                        case ConsoleKey.RightArrow:
                        case ConsoleKey.S:
                        case ConsoleKey.D:
                            choice = Math.Clamp(choice + 1, 0, max);
                            break;
                        case ConsoleKey.UpArrow:
                        case ConsoleKey.LeftArrow:
                        case ConsoleKey.W:
                        case ConsoleKey.A:
                            choice = Math.Clamp(choice - 1, 0, max);
                            break;
                        case ConsoleKey.Backspace:
                            Terminate(new PipelineStoppedException("Exited menu"), "MenuCancelled", ErrorCategory.OperationStopped);
                            break;
                        case ConsoleKey.Enter:
                            return selected.Payload;
                    }

                    // Direct number selection (1-9)
                    if (info.Key >= ConsoleKey.D1 && info.Key <= ConsoleKey.D9)
                    {
                        int val = info.Key - ConsoleKey.D1;
                        choice = Math.Clamp(val, 0, max);
                    }

                    // Calculate lines to move cursor up for redraw
                    int linesToMoveUp = count + maxTooltipHeight + 1; // options + tooltip + 2 newlines (after options and spacing)
                    Console.SetCursorPosition(0, Console.CursorTop - linesToMoveUp);
                }
            }
            finally
            {
                Console.CursorVisible = true;
                Console.TreatControlCAsInput = false;
            }
        }

        protected object? Prompt(string prompt, ISimpleArgumentCompleter argumentCompleter)
        {
            if (Console.IsInputRedirected)
            {
                Terminate(new PSInvalidOperationException("Host does not allow interactivity"), "UnsupportedHost", ErrorCategory.InvalidOperation);
            }
            int bufferLength = 50;
            try
            {
                Console.TreatControlCAsInput = true;
                Console.CursorVisible = false;

                StringBuilder stb = new(10);
                int selectedCompletionIndex = -1;
                List<CompletionResult> completions = [];
                Console.WriteLine();
                //Console.WriteLine($"{Bold}Cancel{Reset} = Backspace, Ctrl-C || {Bold}Move selection{Reset} = Arrows, Digits || {Bold}Accept{Reset} = Enter ");
                Console.WriteLine();
                int lineCounter = 0;
                bool showCompletions = false;
                int completionLinesCount = 0;
                int lastIterationCompletionLineCount = 0;
                while (true)
                {
                    var workingText = stb.ToString();
                    var selectedCompletion = completions.ElementAtOrDefault(selectedCompletionIndex);
                    string interactiveLine = prompt + ": " + workingText;
                    if (selectedCompletion is CompletionResult res && res.CompletionText.Contains(workingText, StringComparison.OrdinalIgnoreCase))
                    {
                        interactiveLine += $"{Invert}{res.CompletionText[workingText.Length..]}{Reset}";
                    }
                    Console.WriteLine(interactiveLine.PadRight(Console.BufferWidth - 2));
                    lineCounter++;

                    int currentCompletionLineCount = 0;
                    // Print completions
                    if (completions.Count > 0 && showCompletions)
                    {
                        lastIterationCompletionLineCount = 1;
                        int maxCompletionWidth = completions.Max(x => x.ListItemText.Length) + 2;
                        var columns = Math.Clamp((Console.BufferWidth - 5) / (maxCompletionWidth + 2), 1, 99);
                        for (int i = 0; i < completions.Count; i++)
                        {
                            string padded = completions[i].ListItemText.PadRight(maxCompletionWidth);
                            if (i != 0 && i % columns == 0)
                            {
                                Console.WriteLine();
                                lineCounter++;
                                lastIterationCompletionLineCount++;
                            }
                            if (selectedCompletionIndex == i)
                            {
                                Console.Write($"{Invert}{padded}{Reset}");
                            }
                            else
                            {
                                Console.Write(padded);
                            }
                        }
                        Console.WriteLine();
                        lineCounter++;
                        if (selectedCompletion is not null && !string.IsNullOrWhiteSpace(selectedCompletion.ToolTip))
                        {
                            Console.WriteLine(selectedCompletion.ToolTip.PadRight(Console.BufferWidth - 2));
                            lastIterationCompletionLineCount++;
                            lineCounter++;
                        }
                    }
                    else
                    {
                        for (int i = 0; i < lastIterationCompletionLineCount; i++)
                        {
                            Console.WriteLine("".PadRight(Console.BufferWidth - 2));
                            lineCounter++;
                        }
                        //Console.WriteLine();
                        //lineCounter++;
                        lastIterationCompletionLineCount = 0;
                    }
                    // Read user input
                    var info = Console.ReadKey(intercept: true);

                    if (info.Modifiers.HasFlag(ConsoleModifiers.Control) && info.Key == ConsoleKey.C)
                    {
                        Console.WriteLine();
                        Terminate(new PipelineStoppedException("Exited menu"), "MenuCancelled", ErrorCategory.OperationStopped);
                    }
                    var comepletions = argumentCompleter.CompleteArgument(workingText);
                    switch (info.Key)
                    {
                        case ConsoleKey.Escape:
                            selectedCompletionIndex = -1;
                            showCompletions = false;
                            break;
                        case ConsoleKey.Tab:
                            if (completions.Count > 0)
                            {
                                selectedCompletionIndex = selectedCompletionIndex >= 0 ? selectedCompletionIndex + 1 : -1;
                            }
                            showCompletions = true;
                            break;
                        case ConsoleKey.DownArrow:
                        case ConsoleKey.UpArrow:
                            break;
                        //case ConsoleKey.DownArrow:
                        case ConsoleKey.RightArrow:
                            selectedCompletionIndex = completions.Count > 0 ? (selectedCompletionIndex + 1) % completions.Count : -1;
                            break;
                        // case ConsoleKey.UpArrow:
                        case ConsoleKey.LeftArrow:
                            selectedCompletionIndex = completions.Count > 0 ? (selectedCompletionIndex - 1) % completions.Count : -1;
                            break;
                        case ConsoleKey.Backspace:
                            if (stb.Length > 0)
                                stb.Remove(stb.Length - 1, 1);
                            break;
                        case ConsoleKey.Enter:
                            if (selectedCompletion is not null)
                            {
                                return selectedCompletion.CompletionText;
                            }
                            else
                            {
                                return workingText;
                            }
                        default:
                            stb.Append(info.KeyChar);
                            break;
                    }
                    completions.Clear();
                    completions.AddRange(argumentCompleter.CompleteArgument(stb.ToString()).Where(x => x.CompletionText.StartsWith(stb.ToString(), StringComparison.OrdinalIgnoreCase)));
                    if (showCompletions && completions.Count > 0 && selectedCompletionIndex == -1)
                    {
                        selectedCompletionIndex = 0;
                    }
                    // Calculate lines to move cursor up for redraw
                    Console.SetCursorPosition(0, Console.CursorTop - lineCounter);
                    lineCounter = 0;
                }
            }
            finally
            {
                Console.CursorVisible = true;
                Console.TreatControlCAsInput = false;
            }
        }

        //protected string InteractivePrompt(string prompt, )

        /// <summary>
        /// YOU CAN ONLY TOUCH AVP-425
        /// </summary>
        /// <param name="issue"></param>
        /// <exception cref="InvalidOperationException"></exception>
        protected void ENSURE_TESTING(string issue)
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
            Terminate(new PSInvalidOperationException($"User cancelled {operation}"), $"{pascalCase}Cancelled", ErrorCategory.InvalidOperation);
        }
    }
}
