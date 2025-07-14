namespace ConsoleMenu.Prototype;

internal class Program
{
    static void Main(string[] args)
    {
        var m = new ChoiceMenu("Siemano", [
            new("opcja a",123,"To je liczba"),
            new("JAMES","Jim","To je imie"),
            new("DUCH!",null,"To je nic"),
            new("DUCH!",null,"To je nic"),
            new("DUCH!",null,"To je nic"),
            new("DUCH!",null,"To je nic"),
            new("DUCH!",null,"To je nic"),
            new("DUCH!",null,"To je nic"),
            new("DUCH!",null,"To je nic"),
            ],
            "tera się przywitam",
            "teraz się pożegnam",
            true);
        var t = m.Show();
        Console.WriteLine();
        Console.WriteLine("Wybrałeś opcję {0}",t);
    }
}
