using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleMenu;

namespace LiraPS.Validators;
internal class AddressValidator : IValidator
{
    public static readonly AddressValidator Instance = new AddressValidator();
    public bool Validate(string value)
    {
        try
        {
            _ = Lira.LiraClient.GetJiraServerUrl(value);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
