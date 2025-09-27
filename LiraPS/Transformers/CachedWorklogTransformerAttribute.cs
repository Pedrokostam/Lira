using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using Lira.Objects;
using Newtonsoft.Json.Linq;

namespace LiraPS.Transformers;
internal class CachedWorklogTransformerAttribute : ArgumentTransformationAttribute
{
    public override object? Transform(EngineIntrinsics engineIntrinsics, object inputData)
    {
        var rawPotentialWorklog = inputData switch
        {
            PSObject pso => pso.BaseObject,
            _ => inputData,
        };
        if (rawPotentialWorklog is Worklog)
        {
            return rawPotentialWorklog;
        }
        if (rawPotentialWorklog is not string logString)
        {
            throw new ArgumentTransformationMetadataException($"Cannot convert {rawPotentialWorklog.GetType().FullName} into a valid worklog");
        }
        if (LiraSession.Client is null)
        {
            throw new ArgumentTransformationMetadataException($"No worklog cache is present - fetch worklogs from server");
        }
        var parts = logString.Split([' ', '\\', '/'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var issue = parts.Length > 1 ? parts[0] : null;
        var id = parts.Length > 1 ? parts[1] : parts[0];
        if (LiraSession.Client.TryGetCachedWorklog(id, issue, out Worklog? log))
        {
            return log;
        }
        throw new ArgumentTransformationMetadataException($"No cached worklog for query: {logString}. Fetch new worklogs or make sure the ID is correct");
    }

}
