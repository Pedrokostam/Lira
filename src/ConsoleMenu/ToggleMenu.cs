using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ConstrainedExecution;
using System.Text.RegularExpressions;
using static ConsoleMenu.ICompleter;

namespace ConsoleMenu;

public class ToggleMenu<TState> : MenuBase<object?> where TState : struct, Enum
{
    public class MenuItemWithState
    {
        public MenuItemWithState(MenuItem item, TState state)
        {
            Item = item;
            State = state;
        }

        public MenuItem Item { get; }
        public string Name { get => Item.Name; }
        public string? Tooltip { get => Item.Tooltip; }
        public bool HasTooltip { get => Item.HasTooltip; }
        public string[] TooltipLines { get => Item.TooltipLines; }
        public object? Payload { get => Item.Payload; }
        public TState State { get; set; }
        public static implicit operator MenuItem(MenuItemWithState mis) => mis.Item;
    }
    private readonly List<MenuItemWithState> _items = [];
    public IReadOnlyCollection<MenuItemWithState> Items => _items.AsReadOnly();

    public string Prompt { get; }
    public Part? Preamble { get; }
    public Part? Epilogue { get; }
    public bool ClearAfterFinish { get; }
    public Func<TState, string> StateFormatter { get; init; }
    private List<TState> _states = Enum.GetValues<TState>().ToList();

    public ToggleMenu<TState> Add(MenuItemWithState item)
    {
        _items.Add(item);
        return this;
    }

    public ToggleMenu(string prompt, IEnumerable<MenuItemWithState> items, Part? preamble = null, Part? epilogue = null, bool clearAfterFinish = false, Func<TState, string>? stateFormatter = null)
    {
        _items.AddRange(items);
        Prompt = prompt;
        Preamble = preamble;
        Epilogue = epilogue;
        ClearAfterFinish = clearAfterFinish;
        StateFormatter = stateFormatter ?? (x => x.ToString());
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

    private TState GetNextState(TState state)
    {
        var i = _states.IndexOf(state);
        int j = (i + 1) % _states.Count;
        return _states[j];
    }

    private object? ShowImpl()
    {
        int selectedIndex = 0;
        int indexWidth = (int)Math.Log10(_items.Count) + 1;
        while (true)
        {
            UpdateBufferWidth();
            MenuItemWithState selectedItem = _items[selectedIndex];
            Append(Prompt);
            AdvanceLine();
            for (int i = 0; i < _items.Count; i++)
            {
                string stateFormat = StateFormatter(_items[i].State);
                Append($" [{stateFormat}] ");
                if (i == selectedIndex)
                {
                    Append(_items[i].Name, GraphicModes.Invert);
                }
                else
                {
                    Append(_items[i].Name);
                }
                AdvanceLine();
            }
            AdvanceLine();
            Append("Press space to change the state for the selected option; press Enter to accept current states");
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
                case ConsoleKey.Spacebar:
                    _items[selectedIndex].State = GetNextState(_items[selectedIndex].State);
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
