namespace ConsoleMenu;

public class MenuItemWithState<TState, T>(MenuItem<T> item, TState state) where TState : struct, Enum
{
    public MenuItem<T> Item { get; } = item;
    public T Payload => Item.Payload;
    public TState State { get; set; } = state;
}

public class ToggleMenu<TState, T> : SelectionMenu<IReadOnlyList<MenuItemWithState<TState, T>>> where TState : struct, Enum
{
    private static readonly int _stateWidth = Enum.GetNames<TState>().Max(x => x.Length);
    private readonly List<MenuItemWithState<TState, T>> _items = [];
    public IReadOnlyList<MenuItemWithState<TState, T>> Items => _items.AsReadOnly();
    public Func<TState, bool, LineParts> StateFormatter { get; init; }
    public Func<MenuItemWithState<TState, T>, bool, LineParts> ContentFormatter { get; init; }
    private readonly List<TState> _states = Enum.GetValues<TState>().ToList();

    public ToggleMenu<TState, T> Add(MenuItemWithState<TState, T> item)
    {
        _items.Add(item);
        return this;
    }

    public ToggleMenu(string prompt, IEnumerable<MenuItemWithState<TState, T>> items, Part? preamble = null, Part? epilogue = null, bool clearAfterFinish = false,
        Func<TState, bool, LineParts>? stateFormatter = null, Func<MenuItemWithState<TState, T>, bool, LineParts>? contentFormatter = null)
        : base(prompt, preamble, epilogue, clearAfterFinish)
    {
        _items.AddRange(items);
        StateFormatter = stateFormatter ?? ((state, _) => [state.ToString().PadRight(_stateWidth)]);
        ContentFormatter = contentFormatter ?? ((item, _) => [item.Item.Name]);
    }

    protected override int Count => _items.Count;
    protected override MenuItem ItemAt(int index) => _items[index].Item;
    protected override IReadOnlyList<MenuItemWithState<TState, T>> Accept(int selectedIndex) => Items;
    protected override LineParts FormatRow(int index, bool selected)
    {
        var item = _items[index];
        LineParts row = ["["];
        row.AddRange(StateFormatter(item.State, selected));
        row.Add("] ");
        row.AddRange(ContentFormatter(item, selected));
        return row;
    }
    protected override IEnumerable<LineParts> ExtraHints =>
        [["Press [", new("SPACE", GraphicModes.Bold), "] to toggle state of selected option"]];
    protected override void OnKey(ConsoleKeyInfo key, int selectedIndex)
    {
        if (key.Key == ConsoleKey.Spacebar)
        {
            var item = _items[selectedIndex];
            item.State = _states[(_states.IndexOf(item.State) + 1) % _states.Count];
        }
    }
}
