using System.Collections;

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
        List<Part> parts = [.. _parts];
        var lengthToPad = minLength - Length;

        if (lengthToPad > 0 && (maxLength < 0 || maxLength > lengthToPad))
        {
            parts.Add("".PadLeft(lengthToPad));
        }
        if (maxLength >= 0)
        {
            List<Part> cropParts = [];
            int runLength = 0;
            foreach (var part in parts)
            {
                int diff = maxLength - runLength;
                if (diff < part.Length)
                {
                    if (diff > 0)
                    {
                        cropParts.Add(part with { Text = part.Text[..(diff - 1)] + "~" });
                    }
                    break;
                }
                cropParts.Add(part);
                runLength += part.Length;
            }
            parts = cropParts;
        }
        var formatted = string.Join("", parts.Select(x => x.GetConsoleString(false)));
        var plain = string.Join("", parts.Select(x => x.GetConsoleString(true)));

        return (formatted, plain);
    }

    public IEnumerator<Part> GetEnumerator() => _parts.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
