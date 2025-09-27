using System;
using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation;
using System.Runtime.CompilerServices;
using System.Text;
using ConsoleMenu;
using LiraPS.Extensions;
namespace LiraPS.Transformers;
//public interface ITransformer
//{
//    object? TransformString(string inputData);
//}
public class TimespanTransformer : ArgumentTransformationAttribute, ITransform<TimeSpan>
{
    private ref struct XD
    {
        Span<char> Buffer { get; }
        public int Position { get; private set; }
        public XD(Span<char> buffer)
        {
            Buffer = buffer;
        }
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
        //if(inputData is int seconds)
        //{
        //    return TimeSpan.FromSeconds(seconds);
        //}
        if (inputData is not string s)
        {
            throw new ArgumentTransformationMetadataException($"Could not convert {inputData.GetType().FullName} to TimeSpan");
        }
        return ParseTime(s);
    }
    public static TimeSpan ParseTime(string s)
    {
        var buffer = new XD(stackalloc char[24]);
        var span = s.AsSpan();
        TimeSpan result = TimeSpan.Zero;
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
                if (buffer.CanAppend && (char.IsDigit(czar) || czar == '.'))
                {
                    buffer.Append(czar);
                }
                continue;
            }
            if (double.TryParse(buffer.GetString(), NumberParseStyle, CultureInfo.InvariantCulture, out var number))
            {
                result += unit switch
                {
                    TimeUnit.Seconds => TimeSpan.FromSeconds(number),
                    TimeUnit.Minutes => TimeSpan.FromMinutes(number),
                    TimeUnit.Hours => TimeSpan.FromHours(number),
                    _ => throw new PSInvalidOperationException()
                };
            }

        }
        return result;
    }

    public string? DescriptiveTransform(string? item)
    {
        if(item is null)
        {
            return null;
        }
        try
        {
            return Transform(item).PrettyTime();
        }
        catch (Exception)
        {

            throw;
        }
    }
    
}
