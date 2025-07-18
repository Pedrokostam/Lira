
namespace ConsoleMenu.Prototype;

internal class Program
{
    public class Trans : ITransform<string>
    {
        public string Transform(string item) => item;
    }
    public class Comp : ICompleter
    {
        public IEnumerable<ICompleter.Completion> Complete(string item)
        {
            if (item.Length > 0 && item.All(x => char.IsDigit(x)))
            {
                int parsed = int.Parse(item);
                if (parsed < 10)
                {
                    yield return new(parsed.ToString(), null, tooltip: $"Liczba {parsed}");
                    parsed *= 10;
                }
                if (parsed < 100)
                {
                    yield return new(parsed.ToString(), null, tooltip: $"Liczba {parsed}");
                    parsed *= 10;
                }
                if (parsed < 1000)
                {
                    yield return new(parsed.ToString(), null, tooltip: $"Liczba {parsed}");
                    parsed *= 10;
                }
                if (parsed < 10000)
                {
                    yield return new(parsed.ToString(), null, tooltip: $"Liczba {parsed}");
                    parsed *= 10;
                }
                if (parsed < 100000)
                {
                    yield return new(parsed.ToString(), null, tooltip: $"Liczba {parsed}");
                    parsed *= 10;
                }
                if (parsed < 1000000)
                {
                    yield return new(parsed.ToString(), null, tooltip: null);
                    parsed *= 10;
                }
                if (parsed < 10000000)
                {
                    yield return new(parsed.ToString(), null, tooltip: $"Liczba {parsed}");
                    parsed *= 10;
                }
                if (parsed < 100000000)
                {
                    yield return new(parsed.ToString(), null, tooltip: $"Liczba {parsed}");
                    parsed *= 10;
                }
                if (parsed < 1000000000)
                {
                    yield return new(parsed.ToString(), parsed.ToString() + " KONIEC", ":(");
                }
            }
        }
    }
    private static void Inter()
    {
        var p = new InteractiveMenu<string>("dawaj, dawaj", new Comp(), new Trans()).Show();
        Console.WriteLine("Wybrałeś opcję {0}", p);
    }
    private static void Choice()
    {

        var m = new ChoiceMenu("Siemano", [
            new("opcja a",123,"To je liczba"),
            new("JAMES","Jim","To je imie"),
            new("eruifh8234hfi345urdh43urv9u8erwh8fi234h80fcher53wuiyu8934hcu8y34y7cghwerybchweujgvui92w34hfuiertg89fu2349uvbweruincouiw3e4rd890uj89vuebwriuhfd934",null,"To je nic"),
            new("DUCH!",null,"To je nic"),
            new("DUCH!",null,"To je nic"),
            new("DUCH!",null,null),
            new("DUCH!",null,"To je nic"),
            new("DUCH!",null,"To je nic"),
            new("DUCH!",null,"To je nic"),
            ]
            );
        var t = m.Show();
        Console.WriteLine();
        Console.WriteLine("Wybrałeś opcję {0}", t);
    }
    static void Main(string[] args)
    {
        Console.WriteLine("YOU HAVE TO KEEP IT SAFE!");
        Console.WriteLine("PROTECT IT");
        Console.WriteLine(new String('2',100));

        Inter();
        Choice();
    }
}
