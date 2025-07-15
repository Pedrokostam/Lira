using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static ConsoleMenu.ICompleter;

namespace ConsoleMenu;

public class ChoiceMenu : MenuBase<object?>
{
    private readonly List<MenuItem> _items = [];
    public IReadOnlyCollection<MenuItem> Items => _items.AsReadOnly();

    public string Prompt { get; }
    public Part? Preamble { get; }
    public Part? Epilogue { get; }
    public bool ClearAfterFinish { get; }

    public ChoiceMenu Add(MenuItem item)
    {
        _items.Add(item);
        return this;
    }

    public ChoiceMenu(string prompt, IEnumerable<MenuItem> items, Part? preamble = null, Part? epilogue = null, bool clearAfterFinish = false)
    {
        _items.AddRange(items);
        Prompt = prompt;
        Preamble = preamble;
        Epilogue = epilogue;
        ClearAfterFinish = clearAfterFinish;
    }
    public override object? Show()
    {
        if (_items.Count == 0)
        {
            throw new ArgumentException("Cannot show menu with no items.", nameof(Items));
        }
        try
        {
            SetCursor(false);
            CatchCtrlC();

            if (Preamble is not null)
            {
                Console.WriteLine(Preamble.GetConsoleString());
            }
            var payload = ShowImpl();
            if (ClearAfterFinish)
            {
                MoveCursorUp(PreviousLineLengths.Count);
                FinalCleanUp();
            }
            if (Epilogue is not null)
            {
                Console.WriteLine(Epilogue.GetConsoleString());
            }
            return payload;
        }
        finally
        {
            ReleaseCtrlC();
            SetCursor(true);
        }
    }

    private object? ShowImpl()
    {
        int selectedIndex = 0;
        int indexWidth = (int)Math.Log10(_items.Count) + 1;
        while (true)
        {
            UpdateBufferWidth();
            MenuItem selectedItem = _items[selectedIndex];
            Append(Prompt);
            AdvanceLine();
            for (int i = 0; i < _items.Count; i++)
            {
                string formatedIndex = (i + 1).ToString().PadLeft(indexWidth, '0');
                if (i == selectedIndex)
                {
                    Append($" [{formatedIndex}] ");
                    Append(_items[i].Name, GraphicModes.Invert);
                    AdvanceLine();
                }
                else
                {
                    Append($"  {formatedIndex}  ");
                    Append(_items[i].Name);
                    AdvanceLine();
                }
            }
            AdvanceLine();
            if (selectedItem.HasTooltip)
            {
                foreach (var tLine in selectedItem.TooltipLines)
                {
                    Append(tLine);
                    AdvanceLine();
                }
            }

            Print();
            var info = Console.ReadKey(intercept: true);

            if (info.Modifiers.HasFlag(ConsoleModifiers.Control) && info.Key == ConsoleKey.C)
            {
                Terminate();
            }

            int maxIndex = _items.Count - 1;
            switch (info.Key)
            {
                case ConsoleKey.DownArrow:
                case ConsoleKey.S:
                    selectedIndex = Math.Clamp(selectedIndex + 1, 0, maxIndex);
                    break;
                case ConsoleKey.UpArrow:
                case ConsoleKey.W:
                    selectedIndex = Math.Clamp(selectedIndex - 1, 0, maxIndex);
                    break;
                case ConsoleKey.Backspace:
                    Terminate();
                    break;
                case ConsoleKey.Enter:
                    return selectedItem.Payload;
            }
            // Direct number selection (1-9)
            if (info.Key >= ConsoleKey.D1 && info.Key <= ConsoleKey.D9)
            {
                int val = info.Key - ConsoleKey.D1;
                selectedIndex = Math.Clamp(val, 0, maxIndex);
            }
            MoveCursorUp(PreviousLineLengths.Count);
        }

    }
}
public class InteractiveMenu<T>(string prompt, ICompleter completer, ITransform<T> transformer) : MenuBase<T>
{
    public string Prompt { get; } = prompt;
    public ICompleter Completer { get; } = completer;
    public ITransform<T> Transformer { get; } = transformer;
    public bool ClearAfterFinish { get; private set; }
    private StringBuilder _input = new();
    //private string _currentInput = string.Empty;
    private List<ICompleter.Completion> _completions = [];
    private int _completionIndex = 1;
    private bool _showCompletions = false;
    private const string Pad = "  ";
    public override T Show()
    {
        try
        {
            SetCursor(false);
            CatchCtrlC();


            var payload = ShowImpl();
            if (ClearAfterFinish)
            {
                MoveCursorUp(PreviousLineLengths.Count);
                FinalCleanUp();
            }

            return Transformer.Transform(ShowImpl());
        }
        finally
        {
            ReleaseCtrlC();
            SetCursor(true);
        }
    }

    private string ShowImpl()
    {
        while (true)
        {
            UpdateBufferWidth();
            var currentInput = _input.ToString();
            Append(Prompt);
            Append(": ");
            var selectedCompletion = _completions.ElementAtOrDefault(_completionIndex);
            if (selectedCompletion is not null)
            {
                if (selectedCompletion.CompletionText.StartsWith(currentInput, StringComparison.OrdinalIgnoreCase))
                {
                    Append(currentInput);
                    var rest = selectedCompletion.CompletionText[currentInput.Length..];
                    Append(rest, GraphicModes.Invert);
                    AdvanceLine();
                }
                else
                {
                    Append(selectedCompletion.CompletionText, GraphicModes.Invert);
                    AdvanceLine();
                }
            }
            else
            {
                Append(currentInput);
                AdvanceLine();
            }
            bool completionsShown = false;
            int columns = 1;
            if (_completions.Count > 0)
            {
                int availableWidth = Console.BufferWidth - Pad.Length * 3;
                int maxWidth = _completions.Max(x => x.ListItem.Length) + Pad.Length;
                columns = Math.Clamp(availableWidth / (maxWidth + Pad.Length), 1, 20);
                maxWidth = availableWidth / columns;
                for (int i = 0; i < _completions.Count; i++)
                {
                    Append(_completions[i].ListItem, i == _completionIndex ? GraphicModes.Invert : GraphicModes.None);
                    if ((i + 1) % columns == 0)
                    {
                        AdvanceLine();
                    }
                    else
                    {
                        Append(Pad);
                    }
                }
                completionsShown = true;
            }

            if (!string.IsNullOrWhiteSpace(selectedCompletion?.Tooltip))
            {
                AdvanceLine();
                Append(selectedCompletion.Tooltip);
                AdvanceLine();
            }

            Print();
            MoveCursorUp(PreviousLineLengths.Count);

            var info = Console.ReadKey(intercept: true);

            if (info.Modifiers.HasFlag(ConsoleModifiers.Control) && info.Key == ConsoleKey.C)
            {
                Console.WriteLine();

                Terminate();
            }
            bool showNewCompletions = completionsShown;
            switch (info.Key)
            {
                case ConsoleKey.Escape:
                    _completionIndex = -1;
                    showNewCompletions = false;
                    break;
                case ConsoleKey.Tab:
                    IncreaseIndex();
                    _showCompletions = true;
                    break;
                case ConsoleKey.DownArrow:
                    ChangeIndex(columns);
                    break;
                case ConsoleKey.UpArrow:
                    ChangeIndex(-columns);
                    break;
                //case ConsoleKey.DownArrow:
                case ConsoleKey.RightArrow:
                    IncreaseIndex();
                    break;
                // case ConsoleKey.UpArrow:
                case ConsoleKey.LeftArrow:
                    DecreaseIndex();
                    break;
                case ConsoleKey.Backspace:
                    RemoveCharacter();
                    showNewCompletions = true;

                    break;
                case ConsoleKey.Enter:
                    if (selectedCompletion is not null)
                    {
                        return selectedCompletion.CompletionText;
                    }
                    else
                    {
                        return currentInput;
                    }
                default:
                    AppendCharacter(info.KeyChar);
                    showNewCompletions = true;
                    break;
            }
            _completions.Clear();
            if (showNewCompletions)
            {
                _completions.AddRange(Completer.Complete(_input.ToString()));
            }
            if (_completions.Count == 0)
            {
                _completionIndex = -1;
            }
            if (_completionIndex < 0 && _completions.Count > 0 && showNewCompletions)
            {
                _completionIndex = 0;
            }
        }
    }
    private void IncreaseIndex() => ChangeIndex(+1);
    private void DecreaseIndex() => ChangeIndex(-1);
    private void ChangeIndex(int i)
    {
        if (_completions.Count == 0)
        {
            _completionIndex = -1;
            return;
        }
        if (_completionIndex < 0)
        {
            _completionIndex = 0;
        }
        var newVal = _completionIndex + i;
        if (newVal < 0)
        {
            newVal += _completions.Count;
        }
        _completionIndex = newVal % _completions.Count;
    }
    private void AppendCharacter(char czar)
    {
        _input.Append(czar);
    }
    private void RemoveCharacter()
    {
        if (_input.Length > 0)
        {
            _input.Remove(_input.Length - 1, 1);
        }
    }
}