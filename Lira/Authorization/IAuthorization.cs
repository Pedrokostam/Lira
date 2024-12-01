using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Lira.Authorization;

public interface IAuthorization
{
    /// <summary>
    /// Performs authorization, i.e. contact the server if necessary and insert the required headers to the HttpClient DefaultHeaders.
    /// </summary>
    /// <param name="lira"></param>
    /// <returns></returns>
    Task Authorize(LiraClient lira);
    Task<bool> EnsureAuthorized(LiraClient lira);
    //void Save(string filepath);
    Task<Exception?> CreateExceptionForUnauthorized(HttpResponseMessage message);
}
