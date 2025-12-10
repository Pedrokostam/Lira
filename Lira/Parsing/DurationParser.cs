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
        for (int i = 0; i < text.Length; i++)
        {
            char czar = text[i];
            TimeUnit unit = czar switch
            {
                'h' or 'H' => TimeUnit.Hours,
                'm' or 'M' => TimeUnit.Minutes,
                's' or 'S' => TimeUnit.Seconds,
                _ => TimeUnit.None,
            };
            if (unit == TimeUnit.None)
            {
                // If no time unit is present, append a valid character and skip to next iteration.
                if (buffer.CanAppend && (char.IsDigit(czar) || czar == '.' || czar == ','))
                {
                    if (czar == ',')
                    {
                        czar = '.';
                    }
                    buffer.Append(czar);
                }
                continue;
            }
            if (double.TryParse(buffer.GetString(), NumberParseStyle, CultureInfo.InvariantCulture, out var number))
            {
                // this happens only if a time unit has just been added
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
        return result;
    }
}
