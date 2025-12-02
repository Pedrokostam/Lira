using System.Reflection.PortableExecutable;
using System.Text;
using static ConsoleMenu.ICompleter;

namespace ConsoleMenu;

public class ColFirstMatrix<T>
{
    public IList<T> Items { get; }
    public int Columns { get; }
    public int Rows { get; }
    /// <summary>
    /// 
    /// </summary>
    private int[,] Woa { get; } // Multidim arrays are row-first
    public ColFirstMatrix(IList<T> items, int columns_x)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(columns_x, 0);
        Items = items;
        Columns = columns_x;
        Rows = (int)Math.Ceiling(1f * items.Count / columns_x);
        Woa = new int[Rows, Columns];
        for (int y = 0; y < Rows; y++)
        {
            for (int x = 0; x < Columns; x++)
            {
                var i = x * Columns + y;
                if (i >= Items.Count)
                {
                    i = -1;
                }
                Woa[y, x] = i;
            }
        }
    }
    public IEnumerable<int> GetIndicesFromRow(int row_y)
    {
        for (int i = 0; i < Columns; i++)
        {
            yield return Woa[row_y, i];
        }
    }

    public IEnumerable<T?> GetItemsFromRow(int row_y) => GetIndicesFromRow(row_y).Select(i => Items.ElementAtOrDefault(i));
    public int GetNewIndexAfterMove(int oldIndex, int movementX, int movementY)
    {
        (int x, int y)? position = null;
        for (int y = 0; y < Rows; y++)
        {
            for (int x = 0; x < Columns; x++)
            {
                if (Woa[y, x] == oldIndex)
                {
                    position = (x, y);
                    break;
                }
            }
            if (position is not null)
            {
                break;
            }
        }
        if (position is null)
        {
            return 0;
        }
        (int posX, int posY) = position.Value;
        int newX = (posX + movementX) % Columns;
        int newY = (posY + movementY) % Rows;
        if (newX < 0)
        {
            newX += Columns;
        }
        if (newY < 0)
        {
            newY += Rows;
        }
        return Woa[newY, newX];
    }
}
/// <summary>
/// 
/// </summary>
/// <typeparam name="T"></typeparam>
public class InteractiveMenu<T> : MenuBase<T>
{
    public ITransformer<T> Transformer { get; set; }
    public string Prompt { get; set; } = "Enter value";
    public ICompleter? Completer { get; set; }
    public IValidator? Validator { get; set; }
    public Func<string, bool> ValidatorFunction
    {
        set
        {
            Validator = new SimpleValidator(value);
        }
    }
    public string? PlaceholderValue { get; set; }
    public Hint Hints { get; set; } = Hint.All;
    public MenuClearMode ClearMode { get; set; } = MenuClearMode.ToPrompt;

    private StringBuilder _input = new();
    //private string _currentInput = string.Empty;
    private List<ICompleter.Completion> _completions = [];
    private List<int> _completionsReorderedIndices = [];
    private int _completionIndex = 0;
    private bool _showCompletions = false;
    private const string InterItemPad = "  ";
    private ColFirstMatrix<ICompleter.Completion> _displayMatrix = default!;

    /// <param name="prompt"></param>
    /// <param name="transformer"></param>
    /// <param name="completer"></param>
    public InteractiveMenu(ITransformer<T> transformer)
    {
        Transformer = transformer;
    }
    public InteractiveMenu(ITransformer<T> transformer, string prompt) : this(transformer)
    {
        Prompt = prompt;
    }
    public InteractiveMenu(ITransformer<T> transformer, string prompt, ICompleter completer) : this(transformer, prompt)
    {
        Prompt = prompt;
        Completer = completer;
    }
    public InteractiveMenu(ITransformer<T> transformer, string prompt, string placeholder) : this(transformer, prompt)
    {
        PlaceholderValue = placeholder;
    }
    public InteractiveMenu(ITransformer<T> transformer, string prompt, string placeholder, ICompleter completer) : this(transformer, prompt, completer)
    {
        PlaceholderValue = placeholder;
    }

    private int DisplayColumns { get; set; }
    private int DisplayRows { get; set; }

    /// <inheritdoc cref="MenuBase{T}.Show"/>
    /// <exception cref="PromptTransformException"/>
    public override T Show() => Show(true);
    public T Show(bool showInitialCompletions)
    {
        try
        {
            SetCursor(false);
            CatchCtrlC();
            var payload = ShowImpl(showInitialCompletions);
            switch (ClearMode)
            {
                case MenuClearMode.None:
                    break;
                case MenuClearMode.ToPrompt:
                    MoveCursorDown(1);
                    ClearToBottom();
                    break;
                case MenuClearMode.Everything:
                    ClearToBottom();
                    break;
                default:
                    break;
            }
            try
            {
                return Transformer.Transform(payload);
            }
            catch (Exception e)
            {
                throw PromptTransformException.WrapException<T>(payload, e);
            }
        }
        finally
        {
            ReleaseCtrlC();
            SetCursor(true);
        }
    }
    private void DrawValidation(string value, bool dim = false)
    {
        if (!Hints.HasFlag(Hint.Validation))
        {
            return;
        }
        string? reason = null;
        bool isValid = false;
        if (Validator is IReasonableValidator reasoning)
        {
            var r = reasoning.ValidateWithReason(value);
            reason = r.reason;
            isValid = r.valid;
        }
        else
        {
            isValid = Validator?.Validate(value) ?? Transformer.TryTransform(value, out _);
        }
        string validateChar = isValid ? "OK" : "INVALID";
        Append(' ');
        //if (!string.IsNullOrEmpty(value))
        //{
        //}
        ConsoleColor color = isValid ? ConsoleColor.Green : ConsoleColor.Red;
        var mode = GraphicModes.Bold;
        if (dim)
        {
            mode |= GraphicModes.Dim;
        }
        Append(validateChar, mode, color);
        if (!string.IsNullOrWhiteSpace(reason))
        {
            Append($" ({reason})", GraphicModes.Dim);
        }
    }
    private string ShowImpl(bool showInitialCompletions)
    {
        if (showInitialCompletions && Completer is not null)
        {
            _completions.AddRange(Completer.Complete(_input.ToString()));
        }
        while (true)
        {
            UpdateBufferWidth();
            var currentInput = _input.ToString();
            Append(Prompt);
            Append(": ");
            var selectedCompletion = _completions.ElementAtOrDefault(_completionIndex);
            if (!string.IsNullOrWhiteSpace(PlaceholderValue) && currentInput.Length == 0)
            {
                currentInput = PlaceholderValue;
                Append(currentInput, GraphicModes.Dim);
                DrawValidation(currentInput);
            }
            else if (selectedCompletion is not null)
            {
                if (selectedCompletion.CompletionText.StartsWith(currentInput, StringComparison.OrdinalIgnoreCase))
                {
                    Append(currentInput);
                    var rest = selectedCompletion.CompletionText[currentInput.Length..];
                    Append(rest, GraphicModes.Invert);
                    DrawValidation(currentInput);
                }
                else
                {
                    Append(currentInput);
                    DrawValidation(currentInput);
                    Append(" (" + selectedCompletion.CompletionText + ")", GraphicModes.Dim);
                }
            }
            else
            {
                Append(currentInput);
                DrawValidation(currentInput);
            }
            AdvanceLine();
            DisplayColumns = 1;
            var completionsShown = DrawCompletions();

            if (!string.IsNullOrWhiteSpace(selectedCompletion?.Tooltip))
            {
                AdvanceLine();
                Append(selectedCompletion.Tooltip);
            }
            string toParse = selectedCompletion?.CompletionText ?? currentInput;
            AdvanceLine();

            DrawOutput(toParse);

            Print();
            MoveCursorUp(PreviousLineLengths.Count);

            var info = Console.ReadKey(intercept: true);

            if (info.Modifiers.HasFlag(ConsoleModifiers.Control) && info.Key == ConsoleKey.C)
            {
                Console.WriteLine();

                Terminate();
            }
            bool inputChanged = false;
            bool showNewCompletions = completionsShown || showInitialCompletions;
            switch (info.Key)
            {
                case ConsoleKey.Escape:
                    _completionIndex = -1;
                    showNewCompletions = false;
                    break;
                case ConsoleKey.Tab:
                    IndexUp();
                    showNewCompletions = true;
                    break;
                case ConsoleKey.DownArrow:
                    IndexDown();
                    break;
                case ConsoleKey.UpArrow:
                    IndexUp();
                    break;
                //case ConsoleKey.DownArrow:
                case ConsoleKey.RightArrow:
                    IndexRight();
                    break;
                // case ConsoleKey.UpArrow:
                case ConsoleKey.LeftArrow:
                    IndexLeft();
                    break;
                case ConsoleKey.Backspace:
                    RemoveCharacter();
                    showNewCompletions = true;
                    inputChanged = true;
                    break;
                case ConsoleKey.Enter:
                    if (selectedCompletion is not null && !string.Equals(selectedCompletion.CompletionText, currentInput, StringComparison.OrdinalIgnoreCase))
                    {
                        _input.Clear();
                        _input.Append(selectedCompletion.CompletionText);
                        showNewCompletions = false;
                        break;
                    }
                    else
                    {
                        bool isValid = Validator?.Validate(currentInput) ?? Transformer.TryTransform(currentInput, out _);
                        if (isValid || info.Modifiers.HasFlag(ConsoleModifiers.Shift | ConsoleModifiers.Control))
                        {
                            return currentInput;
                        }
                        Append($"Cannot transform {currentInput} into a valid object!", GraphicModes.Bold, ConsoleColor.Red);
                        AdvanceLine();
                        break;
                    }
                default:
                    AppendCharacter(info.KeyChar);
                    inputChanged = true;
                    showNewCompletions = true;
                    break;
            }
            if (!showNewCompletions)
            {
                _completions.Clear();
            }
            if (inputChanged || showNewCompletions)
            {
                _completions.Clear();
                if (Completer is not null)
                {
                    _completions.AddRange(Completer.Complete(_input.ToString()));
                }
                //_completionIndex = 0;
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

    private bool DrawCompletions()
    {
        bool completionsShown = false;
        if (_completions.Count > 0)
        {
            int availableWidth = Console.BufferWidth - 1; // slightly decrease the buffer
            int maxItemWidth = _completions.Max(x => x.ListItem.Length); // length of the longest list item
                                                                         // Between items there is a gap (InterItemPad)
                                                                         // The gap can be skipped if it occurs after the last item in the line
                                                                         // To allow the virtual last gap, increase the buffer width value by gap length
            int gap = InterItemPad.Length;
            DisplayColumns = (availableWidth + gap) / (maxItemWidth + gap);
            // sanity check
            var column_check = maxItemWidth * DisplayColumns + gap * (DisplayColumns - 1);
            if (column_check > availableWidth)
            {
                DisplayColumns--;
            }
            if (DisplayColumns < 1)
            {
                DisplayColumns = 1;
            }
            // now that we know the number of columns we can update with list item width
            maxItemWidth = (availableWidth + gap - DisplayColumns * gap) / DisplayColumns;
            // powershell's completions go like Japanese, first vertically then horizontally
            // but left to right, unlike Japanese
            DisplayRows = (int)Math.Ceiling(1f * _completions.Count / DisplayColumns);
            //_displayMatrix = new(_completions, DisplayColumns);
            _completionsReorderedIndices.Clear();
            for (int row = 0; row < DisplayRows; row++)
            {
                for (int column = 0; column < DisplayColumns; column++)
                {
                    int i = row + column * DisplayRows;
                    if (i >= _completions.Count)
                    {
                        break;
                    }
                    var mode = i == _completionIndex ? GraphicModes.Invert : GraphicModes.None;
                    string cropped = Crop(_completions[i].ListItem, maxItemWidth);
                    Append(cropped, mode);
                    int toPad = maxItemWidth - cropped.Length;
                    Append(new string(' ', toPad));
                    if (column < DisplayColumns - 1)
                    {
                        Append(InterItemPad); // do not write padding for last item
                    }
                    _completionsReorderedIndices.Add(i);
                }
                AdvanceLine();
            }
            completionsShown = true;
        }
        return completionsShown;
    }

    private void DrawOutput(string toParse)
    {
        if (Hints.HasFlag(Hint.ParsedOutput))
        {
            Append("Output: ");
            if (Transformer.DescriptiveTransform(toParse) is string potential)
            {
                Append(potential, GraphicModes.Bold);
            }
            else
            {
                Append("Cannot parse value!", GraphicModes.Bold | GraphicModes.Dim);
            }
            AdvanceLine();
        }
    }

    private void IndexLeft() => ChangeIndex(-1, 0);
    private void IndexRight() => ChangeIndex(1, 0);
    private void IndexUp() => ChangeIndex(0, -1);
    private void IndexDown() => ChangeIndex(0, 1);
    private void ChangeIndex(int x, int y)
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
        var change = y + x * DisplayRows;
        var newVal = _completionIndex + change;
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