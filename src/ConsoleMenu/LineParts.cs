using System.Collections;
using System.Runtime.InteropServices.Marshalling;

namespace ConsoleMenu;

public class LineParts : IEnumerable<Part>
{
    private readonly List<Part> _parts = new(6);
    public bool Empty => _parts.Count == 0;
    public int Length => _parts.Sum(part => part.Length);
    public void Add(Part part) => _parts.Add(part);
    public void AddRange(IEnumerable<Part> parts) => _parts.AddRange(parts);
    public void Clear() => _parts.Clear();
    /// <summary>
    /// 
    /// </summary>
    /// <param name="minLength">Minimal length of the string, will be padded with spaces.</param>
    /// <param name="plainText"></param>
    /// <returns></returns>
    public (string Text, string PlainText) Format(int minLength = 0, int maxLength = -1)
    {
        var lengthToPad = minLength - Length;
        if (lengthToPad > 0)
        {
            Add(new("".PadLeft(lengthToPad)));
        }
        List<Part> cropParts;
        if (maxLength < 0)
        {
            cropParts = _parts;
        }
        else
        {
            cropParts = [];
            int runLength = 0;
            foreach (var part in _parts)
            {
                int len = part.Length;
                int diff = maxLength - runLength;
                if (diff < len)
                {
                    cropParts.Add(part with { Text = part.Text.Substring(0, diff) });
                    break;
                }
                runLength += len;
            }
        }
        var formatted = string.Join("", _parts.Select(x => x.GetConsoleString(false)));
        var plain = string.Join("", _parts.Select(x => x.GetConsoleString(true)));

        return (formatted, plain);
    }

    public IEnumerator<Part> GetEnumerator() => _parts.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
