using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using Lira.Objects;

namespace LiraPS.Transformers;
/// <summary>
/// Converts strings, numbers and UserDetails into strings representing user names.
/// </summary>
public class UserDetailsToStringTransformerAttribute : ArgumentTransformationAttribute
{
    public static readonly UserDetailsToStringTransformerAttribute Instance = new ();
    public override object Transform(EngineIntrinsics engineIntrinsics, object inputData)
    {
        var rawPotentialCollection = inputData switch
        {
            PSObject pso => pso.BaseObject,
            _ => inputData,
        };
        IEnumerable collection = rawPotentialCollection is IEnumerable c && rawPotentialCollection is not IEnumerable<char> ? c : new object[] { rawPotentialCollection };
        List<string> users = [];
        foreach (var item in collection)
        {
            var raw = item switch
            {
                PSObject pso => pso.BaseObject,
                _ => item,
            };
            var str = raw switch
            {
                string s => s,
                UserDetails ud => ud.Name,
                int i => i.ToString(),
                long i => i.ToString(),
                null => null,
                _ => throw new ArgumentTransformationMetadataException($"Cannot convert {raw.GetType().FullName} into a valid user name"),
            };
            if (str is not null)
            {
                users.Add(str);
            }
        }
        return users;
    }
}
