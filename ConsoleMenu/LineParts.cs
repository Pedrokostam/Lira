using System.Collections;

namespace ConsoleMenu;

public class LineParts : IEnumerable<Part>
{
    private readonly List<Part> _parts = new(6);
    public bool Empty => _parts.Count == 0;
    public int Length => _parts.Sum(part => part.Length);
    public void Add(Part part) => _parts.Add(part);
    public void Clear() => _parts.Clear();
    public (string Text, int PrintableLength) Format(int minLength)
    {
        var lengthToPad = minLength - Length;
        if (lengthToPad > 0)
        {
            Add("".PadLeft(lengthToPad));
        }

        return (string.Join("", _parts.Select(x => x.GetConsoleString())), Length);
    }

    public IEnumerator<Part> GetEnumerator() => _parts.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
