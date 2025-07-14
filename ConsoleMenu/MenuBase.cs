namespace ConsoleMenu;

public abstract partial class MenuBase
{
    protected List<LineParts> Lines = new(10);
    protected List<int> PrevLines = new(10);
    protected int Index { get; private set; }
    protected void AdvanceLine()
    {
        Index++;
    }
    protected void Write(Part part)
    {
        while (Lines.Count <= Index)
        {
            Lines.Add([]);
        }
        Lines[Index].Add(part);
    }
    protected void WriteLine(Part part)
    {
        Write(part);
        AdvanceLine();
    }
    protected void Print()
    {
        // When print is called, the cursor should already be at the beginning!
        var toClear = PrevLines.ToArray();
        PrevLines.Clear();
        for (int i = 0; i < Lines.Count; i++)
        {
            if (i <= Index)
            {
                var (text, printableLength) = Lines[i].Format(toClear.ElementAtOrDefault(i));
                PrevLines.Add(printableLength);
                Console.WriteLine(text);
            }
            Lines[i].Clear();
        }
        Index = 0;;

    }
    protected static void MoveCursorUp(int lineCount)
    {
        Console.SetCursorPosition(0, Console.CursorTop - lineCount);
    }
    protected void FinalCleanUp()
    {
        foreach (var item in PrevLines)
        {
            Console.WriteLine("".PadLeft(item));
        }
        MoveCursorUp(PrevLines.Count);
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

}
