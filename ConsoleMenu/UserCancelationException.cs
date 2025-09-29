namespace ConsoleMenu;

public class UserCancelationException : Exception
{
    public UserCancelationException():base("User force-exited the menu")
    {
        
    }
}