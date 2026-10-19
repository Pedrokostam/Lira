using LiraPS.Cmdlets;
using System;
using System.Management.Automation;

namespace LiraPS.Toggl.Cmdlets
{
    public abstract class TogglCmdlet : LiraCmdlet
    {
        protected const string TogglAddonKey = "Toggl";
        protected string GetTogglKey()
        {
            if (LiraSession.Config.Addons.TryGetValue(TogglAddonKey, out string? key))
            {
                return key;
            }
            throw new PSInvalidOperationException("The current configuration has no Toggl key specified. Set it up with Set-LiraTogglConfiguration.");
        }
    }
}
