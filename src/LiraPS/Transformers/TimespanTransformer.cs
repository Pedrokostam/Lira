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
using Lira.Parsing;
namespace LiraPS.Transformers;
//public interface ITransformer
//{
//    object? TransformString(string inputData);
//}
public class TimespanTransformer(bool passScriptBlock=false) : ArgumentTransformationAttribute, ITransformer<TimeSpan>, IReasonableValidator
{
    public static readonly TimespanTransformer Instance = new();
    public bool PassScriptBlock { get; } = passScriptBlock;
    public TimeSpan Transform(string inputData) => DurationParser.ParseTime(inputData);
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
        return DurationParser.ParseTime(s);
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
