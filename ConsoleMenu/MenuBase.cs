using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace ConsoleMenu;

public abstract partial class MenuBase<T>
{
    /// <summary>
    /// LineParts to be printed
    /// </summary>
    protected List<LineParts> QueuedLines = new(10);
    /// <summary>
    /// Length of each of previously printed lines
    /// </summary>
    protected List<int> PreviousLineLengths = new(10);
    protected int BufferWidth { get; set; }
    /// <summary>
    /// Index of <see cref="QueuedLines"/> that is currently being written to.
    /// </summary>
    protected int Index { get; private set; }
    protected void AdvanceLine()
    {
        Index++;
    }
 
    protected void Append(string text, GraphicModes wrap) => Append(text, wrap, GraphicModes.Reset);
    protected void Append(string text) => Append(text, GraphicModes.None, GraphicModes.None);
    protected void Append(string text, GraphicModes pre, GraphicModes post)
    {
        var lines = Part.NewLineFinder().Split(text);

        foreach (var line in lines)
        {
            while (QueuedLines.Count <= Index)
            {
                QueuedLines.Add([]);
            }
            var thisQueuedLine = QueuedLines[Index];
            int diff = BufferWidth - thisQueuedLine.Length;
            if (line.Length < diff)
            {
                thisQueuedLine.Add(new Part(line, pre, post));
            }
            else
            {
                thisQueuedLine.Add(new Part(CroPad(line,diff-1), pre, post));
            }
        }
    }
    //protected void AppendLine(string text, GraphicModes wrap) => AppendLine(text, wrap, GraphicModes.Reset);
    //protected void AppendLine(string text) => AppendLine(text, GraphicModes.None, GraphicModes.None);
    //protected void AppendLine(string text, GraphicModes pre, GraphicModes post)
    //{
    //    Append(text, pre, post);
    //    AdvanceLine();
    //}
    protected void Print()
    {
        AdvanceLine();
        int currentWidth = Console.BufferWidth;
        //if (_bufferWidth != currentWidth)
        //{
        //    Console.Clear();
        //}
        BufferWidth = currentWidth;
        // When print is called, the cursor should already be at the beginning!
        Debug.WriteLine("Prevlines {0}", PreviousLineLengths.Count);
        Debug.WriteLine("QueuedLines {0}", QueuedLines.Count);
        Debug.WriteLine("Index {0}", Index);

        var toClear = PreviousLineLengths.ToArray();
        PreviousLineLengths.Clear();
        var s = "";
        for (int i = 0; i < QueuedLines.Count; i++)
        {
            if (i <= Index)
            {
                var len = toClear.ElementAtOrDefault(i);
                var (text, printableLength) = QueuedLines[i].Format(len);
                var (plainText, _) = QueuedLines[i].Format(len, true);
                s += plainText + "\r\n";
                PreviousLineLengths.Add(printableLength);
                Console.WriteLine(text);
            }
            QueuedLines[i].Clear();
        }
        Index = 0;

    }
    protected static void MoveCursorUp(int lineCount)
    {
        Console.SetCursorPosition(0, Console.CursorTop - lineCount);
    }
    protected void FinalCleanUp()
    {
        foreach (var item in PreviousLineLengths)
        {
            Console.WriteLine("".PadLeft(item));
        }
        MoveCursorUp(PreviousLineLengths.Count);
    }
    protected void CatchCtrlC()
    {
        Console.TreatControlCAsInput = true;
    }
    protected void ReleaseCtrlC()
    {
        Console.TreatControlCAsInput = false;
    }
    protected void SetCursor(bool enabled) => Console.CursorVisible = enabled;

    public abstract T Show();
    [DoesNotReturn]
    protected static void Terminate()
    {
        throw new UserCancelationException();
    }
    protected static string CroPad(string s, int length)
    {
        if (s.Length == length)
        {
            return s;
        }
        if (s.Length > length)
        {
            if (s.Length == 1)
            {
                return s;
            }
            return s[..(length - 1)] + "~";
        }
        return s.PadRight(length, ' ');
    }
    protected void UpdateBufferWidth()
    {
        var interim = Console.BufferWidth;
        if (interim < BufferWidth)
        {
            Console.Clear();
            PreviousLineLengths.Clear();
        }
        BufferWidth = interim;
    }
}
