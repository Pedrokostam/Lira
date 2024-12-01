using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace LiraPS;
[InterpolatedStringHandler]
internal static class StringFormatter
{
    private static readonly CultureInfo English = new("en-GB");
#if !NET8_0
    public static string Format(FormattableString formattableString)
    {
        return formattableString.ToString(English);
    }
#else
    [InterpolatedStringHandler]
    public ref struct EnglishInterpolatedStringHandler
    {
        private DefaultInterpolatedStringHandler _handler;

        public EnglishInterpolatedStringHandler(int literalLength, int formattedCount)
        {
            _handler = new DefaultInterpolatedStringHandler(literalLength, formattedCount, English);
        }

        public void AppendLiteral(string s) => _handler.AppendLiteral(s);

        public void AppendFormatted<T>(T value, int alignment = 0, string? format = null) => _handler.AppendFormatted(value, alignment, format);

        public void AppendFormatted(ReadOnlySpan<char> value, int alignment = 0, string? format = null) => _handler.AppendFormatted(value, alignment, format);

        public void AppendFormatted(object value, int alignment = 0, string? format = null) => _handler.AppendFormatted(value, alignment, format);

        public override string ToString() => _handler.ToString();

        public string ToStringAndClear() => _handler.ToStringAndClear();
    }
    public static string Format(ref EnglishInterpolatedStringHandler interpolatedStringHandler)
    {
        return interpolatedStringHandler.ToStringAndClear();
    }
#endif
}
