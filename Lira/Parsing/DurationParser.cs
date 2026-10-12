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
        // Set when whitespace follows a number, so "1 2h" is not read as "12h".
        bool numberClosed = false;
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
                if (char.IsWhiteSpace(czar))
                {
                    numberClosed = buffer.Position > 0;
                    continue;
                }
                // check for the dash char, mark the result for negation if so
                if (czar == '-')
                {
                    // Negation is only allowed once, before any number.
                    if (negateParse || hasParsed || buffer.Position > 0)
                    {
                        throw new ArgumentException($"At most one negative sign (dash) is expected, at the start of the input.", nameof(text));
                    }
                    // set the negation flag and skip this character.
                    negateParse = true;
                    continue;
                }
                if (!(char.IsDigit(czar) || czar == '.' || czar == ','))
                {
                    throw new ArgumentException($"Unexpected character '{czar}' at position {i}.", nameof(text));
                }
                if (numberClosed)
                {
                    throw new ArgumentException($"Number at position {i} is missing a unit.", nameof(text));
                }
                // Normalize comma to dot so parsing with InvariantCulture works.
                if (czar == ',')
                {
                    czar = '.';
                }
                buffer.Append(czar);
                continue;
            }

            // A unit character was encountered.
            // We parse the current buffer value, and add the parsed timespan.
            numberClosed = false;
            if (!double.TryParse(buffer.GetString(), NumberParseStyle, CultureInfo.InvariantCulture, out var number))
            {
                throw new ArgumentException($"Unit '{czar}' at position {i} is not preceded by a valid number.", nameof(text));
            }
            hasParsed = true;
            result += unit switch
            {
                TimeUnit.Seconds => TimeSpan.FromSeconds(number),
                TimeUnit.Minutes => TimeSpan.FromMinutes(number),
                TimeUnit.Hours => TimeSpan.FromHours(number),
                _ => throw new ArgumentException("Input does not contain any unit", nameof(text)),
            };
        }
        if (buffer.Position > 0)
        {
            throw new ArgumentException($"Trailing number is missing a unit.", nameof(text));
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
