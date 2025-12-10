using System.Collections.Generic;
using System.Threading.Tasks;
using LiraPS.Cmdlets;
using Microsoft.Extensions.Logging;

namespace LiraPS;

public interface IPSLogger<T>:ILogger<T>
{
    void PrintToStd(LiraCmdlet cmdlet);
    Task<bool> UpdateFileLogs(IList<Log>? tempColl=null);
}