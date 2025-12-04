using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Management.Automation;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using ConsoleMenu;
using LiraPS.Extensions;
namespace LiraPS.Transformers;
//public interface ITransformer
//{
//    object? TransformString(string inputData);
//}
public class TimespanTransformer(bool passScriptBlock=false) : ArgumentTransformationAttribute, ITransformer<TimeSpan>, IReasonableValidator
{
    public static readonly TimespanTransformer Instance = new();
    private ref struct TimeParsingBuffer
    {
        Span<char> Buffer { get; }
        public int Position { get; private set; }
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
                throw new PSArgumentOutOfRangeException(nameof(Position));
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

    public bool PassScriptBlock { get; } = passScriptBlock;

    private enum TimeUnit
    {
        None,
        Seconds,
        Minutes,
        Hours,
    }
    public TimeSpan Transform(string inputData) => ParseTime(inputData);
    public override object Transform(EngineIntrinsics engineIntrinsics, object inputData)
    {
        if (inputData is TimeSpan ts)
        {
            return ts;
        }
        if (PassScriptBlock && inputData is ScriptBlock sb)
        {
            return sb;
        }
        if (inputData is not string s)
        {
            throw new ArgumentTransformationMetadataException($"Could not convert {inputData.GetType().FullName} to TimeSpan");
        }
        return ParseTime(s);
    }
    public static TimeSpan ParseTime(string s)
    {
        var buffer = new TimeParsingBuffer(stackalloc char[24]);
        var span = s.AsSpan();
        TimeSpan result = TimeSpan.Zero;
        bool hasParsed = false;
        for (int i = 0; i < s.Length; i++)
        {
            char czar = s[i];
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
                if (buffer.CanAppend && (char.IsDigit(czar) || czar == '.'))
                {
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
                    _ => throw new PSInvalidOperationException()
                };
            }

        }
        if (!hasParsed)
        {
            throw new ArgumentException($"Cannot conver \"{s}\" to TimeSpan");
        }
        return result;
    }

    public string? DescriptiveTransform(string? item)
    {
        if(item is null)
        {
            return null;
        }
        if(TryTransform(item, out var result))
        {
            return result.PrettyTime();
        }
        return null;
    }

    public bool TryTransform(string item, [NotNullWhen(true)] out TimeSpan value)
    {
        try
        {
            value = Transform(item)!;
            return true;
        }
        catch (Exception)
        {
            value = default!;
            return false;
        }
    }

    public (bool valid, string? reason) ValidateWithReason(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return (false, "Duration cannot be an empty string");
        }
        if(TryTransform(value,out var r))
        {
            if(r == TimeSpan.Zero)
            {
                return (false, "Duration cannot be zero");
            }
            return (true, null);
        }
        return (false, "Cannot convert to timespan");
    }

    public bool Validate(string value)
    {
        return ValidateWithReason(value).valid;
    }
}
