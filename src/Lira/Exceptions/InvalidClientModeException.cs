using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lira.Exceptions;
public class InvalidClientModeException(string method, ClientMode mode) : InvalidOperationException($"Method {method} cannot be used with connection mode {mode}")
{
    public string Method { get; } = method;
    public ClientMode Mode { get; } = mode;

    [DoesNotReturn]
    public static void Throw(string method, ClientMode mode) => throw new InvalidClientModeException(method, mode);
    private static void CheckWriting(string method,LiraClient client)
    {
        if (client.ConnectionMode != ClientMode.Online)
        {
            Throw(method.ToUpperInvariant(), client.ConnectionMode);
        }
    }
    private static void CheckReading(string method, LiraClient client)
    {
        if (client.ConnectionMode == ClientMode.Offline)
        {

            Throw(method.ToUpperInvariant(), client.ConnectionMode);
        }
    }
    public static void CheckPut(LiraClient client) => CheckWriting("PUT",client);
    public static void CheckPost(LiraClient client) => CheckWriting("POST", client);
    public static void CheckPatch(LiraClient client) => CheckWriting("PATCH", client);
    public static void CheckDelete(LiraClient client) => CheckWriting("DELETE", client);
    public static void CheckGet(LiraClient client) => CheckReading("GET", client);

}
