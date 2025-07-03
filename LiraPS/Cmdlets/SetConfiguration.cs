using System;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Threading.Tasks;
using Lira.Authorization;
using Lira.Objects;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace LiraPS.Cmdlets
{
    [Cmdlet(VerbsCommon.Set, "Configuration", DefaultParameterSetName = "MANUAL")]
    [OutputType(typeof(UserDetails))]
    public class SetConfiguration : LiraCmdlet
    {
        [Alias("Address")]
        [Parameter(ParameterSetName = "COOKIE")]
        [Parameter(ParameterSetName = "PAT")]
        [Parameter(ParameterSetName = "MANUAL")]
        [Parameter(ParameterSetName = "ATLASSIAN")]
        public string ServerAddress { get; set; } = default!;

        [Parameter(Mandatory = true, ParameterSetName = "COOKIE")]
        public PSCredential Credential { get; set; } = default!;

        [Alias("PAT")]
        [Parameter(Mandatory = true, ParameterSetName = "PAT")]
        public string PersonalAccessToken { get; set; } = default!;

        [Parameter(DontShow = true, Mandatory = true, ParameterSetName = "ATLASSIAN")]
        public string AtlassianApiKey { get; set; } = default!;
        [Parameter(DontShow = true, Mandatory = true, ParameterSetName = "ATLASSIAN")]
        public string UserEmail { get; set; } = default!;


        [Parameter(Mandatory = true, ParameterSetName = "CLEAR")]
        public SwitchParameter Clear { get; set; }

        [Parameter(ParameterSetName = "COOKIE")]
        [Parameter(ParameterSetName = "PAT")]
        [Parameter(ParameterSetName = "MANUAL")]
        [Parameter(ParameterSetName = "ATLASSIAN")]
        public SwitchParameter NoSwitch { get; set; }
        [Parameter(ParameterSetName = "COOKIE")]
        [Parameter(ParameterSetName = "PAT")]
        [Parameter(ParameterSetName = "MANUAL")]
        [Parameter(ParameterSetName = "ATLASSIAN")]
        [Alias("Name", "Profile")]
        public string ProfileName { get; set; }
        private IAuthorization Authorization { get; set; } = default!;
        protected override void BeginProcessing()
        {
            // don't load anything yet
            //base.BeginProcessing();
        }

        // This method will be called for each input received from the pipeline to this cmdlet; if no input is received, this method is not called
        protected override void ProcessRecord()
        {
            if (LiraSession.TestSessionDateAvailable())
            {
                LiraSession.Logger.LogTrace("Updating configuration: ServerAddress=\"{Address}\", Authorization={AuthorizationType}",
                                            LiraSession.Config!.ServerAddress,
                                            LiraSession.Config.Authorization.GetType().FullName);
            }
            else if (ParameterSetName == "CLEAR")
            {
                LiraSession.Logger.LogTrace("Resetting configuration");
            }
            else
            {
                LiraSession.Logger.LogTrace("Creating new configuration");
            }
            PrintLogs();

            if (ParameterSetName == "MANUAL")
            {
                SetAddressManuallyIfMissing();
                Host.UI.WriteLine(ConsoleColor.Cyan, Host.UI.RawUI.BackgroundColor, "You will be asked to provide your username password.");
                Host.UI.WriteLine(ConsoleColor.Cyan, Host.UI.RawUI.BackgroundColor, "If you want to use personal access token, call this cmdlet with -PersonalAccessKey parameter.");
                var username = ReadInput("Enter your username");
                EnsureNotEmpty(username, "Username");
                var password = ReadInput("Enter your password", asSecure: true);
                EnsureNotEmpty(password, "Password");
                Authorization = new CookieProvider(username, password);
                LiraSession.Logger.LogDebug("Created new authorization for user {username}", username);
            }
            else if (ParameterSetName == "COOKIE")
            {
                SetAddressManuallyIfMissing();
                var password = Credential.GetNetworkCredential().Password;
                Authorization = new CookieProvider(Credential.UserName, password);
                LiraSession.Logger.LogDebug("Created new authorization from credentials user {username}", Credential.UserName);
            }
            else if (ParameterSetName == "PAT")
            {
                SetAddressManuallyIfMissing();
                Authorization = new PersonalAccessToken(PersonalAccessToken);
                LiraSession.Logger.LogDebug("Created new authorization with access token");
            }
            else if (ParameterSetName == "ATLASSIAN")
            {
                SetAddressManuallyIfMissing();
                Authorization = new AtlassianApiKey(UserEmail, AtlassianApiKey);
                LiraSession.Logger.LogDebug("Created new authorization for email {userEmail}", UserEmail);
            }
            else if (ParameterSetName == "CLEAR")
            {
                Authorization = NoAuthorization.Instance;
            }
            else
            {
                LiraSession.Logger.LogCritical("Invalid ParameterSetName: {Name}", ParameterSetName);
            }
            if (!TestBoundParameter(nameof(ProfileName)))
            {
                ProfileName = ReadInput("Do you want to specify custom name for this profile? Leave empty to use the default name");
            }
            var c = Configuration.Create(Authorization, ServerAddress, ProfileName);
            c.Save();
            if (NoSwitch.IsPresent && LiraSession.HasConfig)
            {
                WriteWarning($"Configuration {c.Name} saved. Configuration {LiraSession.Config.Name} still active");
            }
            else
            {
                LiraSession.Config = c;
            }
            PrintLogs();
        }
        protected override void EndProcessing()
        {
            if (LiraSession.TestSessionDateAvailable())
            {
                LiraSession.StartSession().Wait();
            }
            PrintLogs();
        }
        private void SetAddressManuallyIfMissing()
        {
            bool paramNotSet = !TestBoundParameter(nameof(ServerAddress));
            bool validAddress = LiraSession.Config.IsInitialized;
            if (validAddress)
            {
                ServerAddress = ReadInput($"Enter new server address (leave empty to reuse {LiraSession.Config.ServerAddress})");
                if (string.IsNullOrWhiteSpace(ServerAddress))
                {
                    ServerAddress = LiraSession.Config.ServerAddress;
                }
            }
            else
            {
                ServerAddress = ReadInput("Enter server address (usually \"https://jira.(company).com\")");
            }
            EnsureNotEmpty(ServerAddress, nameof(ServerAddress));
        }
    }

}
