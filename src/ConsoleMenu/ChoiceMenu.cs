using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ConstrainedExecution;
using System.Text.RegularExpressions;
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

    protected object? ShowImpl()
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
                bool selected = i== selectedIndex;
                string formatedIndex = (i + 1).ToString().PadLeft(indexWidth, '0');
                var arrowPre = selected switch
                {
                    false => "==> ",
                    true =>  "    "
                };
                var arrowPost = selected switch
                {
                    false => " <==",
                    true =>  "    "
                };
                Append(arrowPre);
                Append(formatedIndex);
                Append(" ");
                Append(_items[i].Name);
                Append(arrowPost);
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

            Append(["Press Up/Down or W/S to change selection"]);
            AdvanceLine();
            Append(["Press [", new("ENTER", GraphicModes.Bold), "] to accept the current view"]);
            AdvanceLine();
            Append(["Press [", new("BACKSPACE", GraphicModes.Bold), "] or [", new("CTRL-C", GraphicModes.Bold), "] to abort"]);

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
                    selectedIndex = WrapIndex(selectedIndex, +1, maxIndex);
                    break;
                case ConsoleKey.UpArrow:
                case ConsoleKey.W:
                    selectedIndex = WrapIndex(selectedIndex, -1, maxIndex);
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
