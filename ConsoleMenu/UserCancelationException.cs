namespace ConsoleMenu;

class UserCancelationException : Exception
{
    public UserCancelationException():base("User force-exited the menu")
    {
        
    }
}