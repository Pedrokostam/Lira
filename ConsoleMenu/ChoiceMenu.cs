using System.Diagnostics.CodeAnalysis;
using System.Runtime.ConstrainedExecution;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ConsoleMenu;

public class ChoiceMenu : MenuBase
{
    private readonly List<MenuItem> _items = [];
    public IReadOnlyCollection<MenuItem> Items => _items.AsReadOnly();

    public Part Prompt { get; }
    public Part? Preamble { get; }
    public Part? Epilogue { get; }
    public bool ClearAfterFinish { get; }

    public ChoiceMenu Add(MenuItem item)
    {
        _items.Add(item);
        return this;
    }

    public ChoiceMenu(Part prompt, IEnumerable<MenuItem> items, Part? preamble = null, Part? epilogue = null, bool clearAfterFinish = false)
    {
        _items.AddRange(items);
        Prompt = prompt;
        Preamble = preamble;
        Epilogue = epilogue;
        ClearAfterFinish = clearAfterFinish;
    }
    public object? Show()
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
                MoveCursorUp(PrevLines.Count);
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
        int indexWidth = (int)Math.Log10(_items.Count)+1;
        while (true)
        {
            MenuItem selectedItem = _items[selectedIndex];
            WriteLine(Prompt);
            for (int i = 0; i < _items.Count; i++)
            {
                string formatedIndex = (i+1).ToString().PadLeft(indexWidth, '0');
                if (i == selectedIndex)
                {
                    Write($" [{formatedIndex}] ");
                    WriteLine(new(_items[i].Name, GraphicModes.Invert));
                }
                else
                {
                    Write($"  {formatedIndex}  ");
                    WriteLine(_items[i].Name);
                }
            }
            AdvanceLine();
            if (selectedItem.HasTooltip)
            {
                foreach (var tLine in selectedItem.TooltipLines)
                {
                    WriteLine(tLine);
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
            MoveCursorUp(PrevLines.Count);
        }

    }
    [DoesNotReturn]
    private static void Terminate()
    {
        throw new UserCancelationException();
    }
}
