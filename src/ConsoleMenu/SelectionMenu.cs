namespace ConsoleMenu;

public abstract class SelectionMenu<TResult> : MenuBase<TResult>
{
    public string Prompt { get; }
    public Part? Preamble { get; }
    public Part? Epilogue { get; }
    public bool ClearAfterFinish { get; }

    protected SelectionMenu(string prompt, Part? preamble, Part? epilogue, bool clearAfterFinish)
    {
        Prompt = prompt;
        Preamble = preamble;
        Epilogue = epilogue;
        ClearAfterFinish = clearAfterFinish;
    }

    protected abstract int Count { get; }
    protected abstract MenuItem ItemAt(int index);
    protected abstract LineParts FormatRow(int index, bool selected);
    protected abstract TResult Accept(int selectedIndex);
    protected virtual void OnKey(ConsoleKeyInfo key, int selectedIndex) { }
    protected virtual IEnumerable<LineParts> ExtraHints => [];

    public override TResult Show()
    {
        if (Count == 0)
        {
            throw new InvalidOperationException("Cannot show menu with no items.");
        }
        try
        {
            SetCursor(false);
            CatchCtrlC();

            if (Preamble is not null)
            {
                Console.WriteLine(Preamble.GetConsoleString());
            }
            var result = ShowImpl();
            if (ClearAfterFinish)
            {
                MoveCursorUp(PreviousLineLengths.Count);
                FinalCleanUp();
            }
            if (Epilogue is not null)
            {
                Console.WriteLine(Epilogue.GetConsoleString());
            }
            return result;
        }
        finally
        {
            ReleaseCtrlC();
            SetCursor(true);
        }
    }

    private TResult ShowImpl()
    {
        int selectedIndex = 0;
        while (true)
        {
            UpdateBufferWidth();
            MenuItem selectedItem = ItemAt(selectedIndex);
            Append(Prompt);
            AdvanceLine();
            for (int i = 0; i < Count; i++)
            {
                bool selected = i == selectedIndex;
                Append(selected ? "==> " : "    ");
                Append(FormatRow(i, selected));
                Append(selected ? " <==" : "    ");
                AdvanceLine();
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

            Append(["Press Up/Down or W/S to change selection, [", new("1-9", GraphicModes.Bold), "] to jump"]);
            AdvanceLine();
            foreach (var hint in ExtraHints)
            {
                Append(hint);
                AdvanceLine();
            }
            Append(["Press [", new("ENTER", GraphicModes.Bold), "] to accept the current view"]);
            AdvanceLine();
            Append(["Press [", new("BACKSPACE", GraphicModes.Bold), "] or [", new("CTRL-C", GraphicModes.Bold), "] to abort"]);
            Print();
            var info = Console.ReadKey(intercept: true);

            if (info.Modifiers.HasFlag(ConsoleModifiers.Control) && info.Key == ConsoleKey.C)
            {
                Terminate();
            }

            switch (info.Key)
            {
                case ConsoleKey.DownArrow:
                case ConsoleKey.S:
                    selectedIndex = WrapIndex(selectedIndex, +1, Count);
                    break;
                case ConsoleKey.UpArrow:
                case ConsoleKey.W:
                    selectedIndex = WrapIndex(selectedIndex, -1, Count);
                    break;
                case ConsoleKey.Backspace:
                    Terminate();
                    break;
                case ConsoleKey.Enter:
                    return Accept(selectedIndex);
                case >= ConsoleKey.D1 and <= ConsoleKey.D9:
                    selectedIndex = Math.Min(info.Key - ConsoleKey.D1, Count - 1);
                    break;
                default:
                    OnKey(info, selectedIndex);
                    break;
            }
            MoveCursorUp(PreviousLineLengths.Count);
        }
    }
}
