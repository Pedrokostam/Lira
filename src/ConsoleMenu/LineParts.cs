using System.Collections;

namespace ConsoleMenu;

public class LineParts : IEnumerable<Part>
{
    private readonly List<Part> _parts = new(6);
    public bool Empty => _parts.Count == 0;
    public int Length => _parts.Sum(part => part.Length);
    public void Add(Part part) => _parts.Add(part);
    public void Clear() => _parts.Clear();
    /// <summary>
    /// 
    /// </summary>
    /// <param name="minLength">Minimal length of the string, will be padded with spaces.</param>
    /// <param name="plainText"></param>
    /// <returns></returns>
    public (string Text, int PrintableLength) Format(int minLength, bool plainText = false)
    {
        var lengthToPad = minLength - Length;
        if (lengthToPad > 0)
        {
            Add(new("".PadLeft(lengthToPad)));
        }

        return (string.Join("", _parts.Select(x => x.GetConsoleString(plainText))), Length);
    }

    public IEnumerator<Part> GetEnumerator() => _parts.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
