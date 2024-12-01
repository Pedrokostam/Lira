using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace LiraPS.Extensions;
internal static class DateTimeExtensions
{
    public const string DateFormatString = "yyyy-MM-dd";
    private const string UnambiguousFormat = "dd MMMM yyyy";
    public static string NumericalForm(this DateTimeOffset date) => date.ToString(DateFormatString);
    public static string UnambiguousForm(this DateTimeOffset date) => date.ToString(UnambiguousFormat);
}
