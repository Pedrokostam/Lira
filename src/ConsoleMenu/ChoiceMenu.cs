namespace ConsoleMenu;

public class ChoiceMenu<T> : SelectionMenu<T>
{
    private readonly List<MenuItem<T>> _items = [];
    public IReadOnlyCollection<MenuItem<T>> Items => _items.AsReadOnly();
    public Func<MenuItem<T>, bool, LineParts> ContentFormatter { get; init; }

    public ChoiceMenu<T> Add(MenuItem<T> item)
    {
        _items.Add(item);
        return this;
    }

    public ChoiceMenu(string prompt, IEnumerable<MenuItem<T>> items, Part? preamble = null, Part? epilogue = null, bool clearAfterFinish = false, Func<MenuItem<T>, bool, LineParts>? contentFormatter = null)
        : base(prompt, preamble, epilogue, clearAfterFinish)
    {
        _items.AddRange(items);
        ContentFormatter = contentFormatter ?? ((item, _) => [item.Name]);
    }

    protected override int Count => _items.Count;
    protected override MenuItem ItemAt(int index) => _items[index];
    protected override T Accept(int selectedIndex) => _items[selectedIndex].Payload;
    protected override LineParts FormatRow(int index, bool selected)
    {
        int indexWidth = (int)Math.Log10(_items.Count) + 1;
        LineParts row = [(index + 1).ToString().PadLeft(indexWidth, '0'), " "];
        row.AddRange(ContentFormatter(_items[index], selected));
        return row;
    }
}
