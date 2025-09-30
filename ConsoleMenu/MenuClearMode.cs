namespace ConsoleMenu;

public enum MenuClearMode
{
    /// <summary>
    /// Does not remove anything - text continues after menu
    /// </summary>
    None,
    /// <summary>
    /// Removes everything below last prompt
    /// </summary>
    ToPrompt,
    /// <summary>
    /// Clears everything
    /// </summary>
    Everything
}
