using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Lira.Parsing;

public static class DurationParser
{
    public class BufferExceededException : Exception
    {
        public BufferExceededException() : base("The internal parsing buffer was exceeded. The input may be too large or malformed.") { }
    }

    [StructLayout(LayoutKind.Sequential)]
    private ref struct TimeParsingBuffer
    {
        public int Position { get; private set; }
        Span<char> Buffer { get; }
        public TimeParsingBuffer(Span<char> buffer)
        {
            Buffer = buffer;
        }
        /// <summary>
        /// Check if new characters can be added without exceeding buffer size.
        /// </summary>
        public readonly bool CanAppend => Position < Buffer.Length;
        public int Append(char c)
        {
            Buffer[Position] = c;
            Position++;
            if (Position >= Buffer.Length)
            {
                throw new BufferExceededException();
            }
            return Position;
        }
        public void Clear() => Position = 0;
        public ReadOnlySpan<char> GetString()
        {
            var span = Buffer[..Position];
            Clear();
            return span;
        }
    }

    private const NumberStyles NumberParseStyle = NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowDecimalPoint;
    private enum TimeUnit
    {
        None,
        Seconds,
        Minutes,
        Hours,
    }

    public static TimeSpan ParseTime(string text)
    {
        var buffer = new TimeParsingBuffer(stackalloc char[24]);
        var span = text.AsSpan();
        TimeSpan result = TimeSpan.Zero;
        bool hasParsed = false;
        bool negateParse = false;
        for (int i = 0; i < text.Length; i++)
        {
            char czar = text[i];
            // Detect whether the current character is a time unit indicator.
            TimeUnit unit = czar switch
            {
                'h' or 'H' => TimeUnit.Hours,
                'm' or 'M' => TimeUnit.Minutes,
                's' or 'S' => TimeUnit.Seconds,
                _ => TimeUnit.None,
            };

            // If this character is not a unit, it must be either a digit, or a separator.
            if (unit == TimeUnit.None)
            {
                // check for the dash char, mark the result for negation if so
                if (buffer.CanAppend && czar == '-')
                {
                    // Negation is only allowed once.
                    if (negateParse)
                    {
                        throw new ArgumentException($"At most one negative sign (dash) is expected.", nameof(text));
                    }
                    // set the negation flag and skip this character.
                    negateParse = true;
                    continue;
                }
                // Append only valid numeric characters to the internal buffer.
                if (buffer.CanAppend && (char.IsDigit(czar) || czar == '.' || czar == ','))
                {
                    // Normalize comma to dot so parsing with InvariantCulture works.
                    if (czar == ',')
                    {
                        czar = '.';
                    }
                    buffer.Append(czar);
                }
                // Skip the rest of the iteration, do not parse buffer.
                continue;
            }

            // A unit character was encountered.
            // We parse the current buffer value, and add the parsed timespand.
            // Failed parses are ignored. If nothing was parsed at the end of the method
            // An exception is thrown.
            if (double.TryParse(buffer.GetString(), NumberParseStyle, CultureInfo.InvariantCulture, out var number))
            {
                // Successfully parsed a number just before a unit.
                hasParsed |= true;
                result += unit switch
                {
                    TimeUnit.Seconds => TimeSpan.FromSeconds(number),
                    TimeUnit.Minutes => TimeSpan.FromMinutes(number),
                    TimeUnit.Hours => TimeSpan.FromHours(number),
                    _ => throw new ArgumentException("Input does not contain any unit", nameof(text)),
                };
            }
        }
        if (!hasParsed)
        {
            throw new ArgumentException($"Cannot convert to TimeSpan", nameof(text));
        }
        if (negateParse)
        {
            return -result;
        }
        return result;
    }
}
