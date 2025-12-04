using System;
using System.Diagnostics.Metrics;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ConsoleMenu;
using Lira.Authorization;
using Lira.Objects;
using LiraPS.Validators;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace LiraPS.Cmdlets;
public enum ConfigurationType
{
    ManualChoice,
    Pat,
    PersonalAccessToken = Pat,
    Credentials,
    AttlasianApiKey,
}

[Cmdlet(VerbsCommon.Set, "LiraConfiguration", DefaultParameterSetName = "MANUAL")]
[OutputType(typeof(UserDetails))]
public class SetConfiguration : LiraCmdlet
{
    [Parameter(ParameterSetName = "MANUAL")]
    public ConfigurationType Type { get; set; } = ConfigurationType.ManualChoice;
    [Alias("Address")]
    public string ServerAddress { get; set; } = default!;

    public PSCredential Credential { get; set; } = default!;

    [Alias("PAT")]
    [Parameter(Mandatory = true, ParameterSetName = "PAT")]
    public string PersonalAccessToken { get; set; } = default!;

    [Parameter(DontShow = true, Mandatory = true, ParameterSetName = "ATLASSIAN")]
    public string AtlassianApiKey { get; set; } = default!;

    [Parameter(DontShow = true, Mandatory = true, ParameterSetName = "ATLASSIAN")]
    public string UserEmail { get; set; } = default!;

    public SwitchParameter NoSwitch { get; set; }

    [Alias("Profile")]
    public string? Name { get; set; }
    private IAuthorization Authorization { get; set; } = default!;
    protected override void BeginProcessing()
    {
        // don't load anything yet
        //base.BeginProcessing();
    }
    private void SelectYourCharacter()
    {
        if (Type == ConfigurationType.ManualChoice)
        {
            Type = (ConfigurationType)Menu("Choose what kind of authentication you want to use",
                                           new MenuItem(
                                               "Personal Access Token",
                                               ConfigurationType.PersonalAccessToken,
                                               "You will be asked to provide a Personal Access Token.\nThe token can be created in your account's settings.\nhttps://developer.atlassian.com/server/jira/platform/personal-access-token/"),
                                           new MenuItem(
                                               "Username and password",
                                               ConfigurationType.Credentials,
                                               "You will be asked to provide your username and password"),
                                           new MenuItem(
                                               "Atlassian API key",
                                               ConfigurationType.AttlasianApiKey,
                                               "You will be asked to provide your e-mail and an API key.\nThe key can be created in your Atlassian account's settings.\nhttps://support.atlassian.com/atlassian-account/docs/manage-api-tokens-for-your-atlassian-account/")
                                           )!;
        }
    }
    // This method will be called for each input received from the pipeline to this cmdlet; if no input is received, this method is not called
    protected override void ProcessRecord()
    {
        LiraSession.Logger.LogTrace("Setting configuration");

        if (ParameterSetName == "MANUAL")
        {
            SelectYourCharacter();
            switch (Type)
            {
                case ConfigurationType.ManualChoice:
                    throw new PSInvalidOperationException("You were not supposed to get here");
                case ConfigurationType.Pat:
                    var pat = ReadInput("Personal access token", asSecure: true);
                    EnsureNotEmpty(pat, "Token");
                    Authorization = new PersonalAccessToken(pat);
                    break;
                case ConfigurationType.Credentials:
                    var username = ReadInput("Reporter username");
                    EnsureNotEmpty(username, "Username");
                    var passwordC = ReadInput("Password", asSecure: true);
                    EnsureNotEmpty(passwordC, "Password");
                    Authorization = new CookieProvider(username, passwordC);
                    break;
                case ConfigurationType.AttlasianApiKey:
                    var userEmail = ReadInput("Reporter email");
                    EnsureNotEmpty(userEmail, "Reporter Email");
                    var passwordA = ReadInput("Atlassian API key", asSecure: true);
                    EnsureNotEmpty(passwordA, "Password");
                    Authorization = new AtlassianApiKey(userEmail, passwordA);
                    break;
            }
            SetAddressManuallyIfMissing();
            LiraSession.Logger.LogDebug("Created new authorization of type {type}", Authorization.GetType().Name);
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
        else
        {
            LiraSession.Logger.LogCritical("Invalid ParameterSetName: {Name}", ParameterSetName);
        }
        if (!TestBoundParameter(nameof(Name)))
        {
            Name = InteractiveStringMenu.Create("Enter name for this configuration", "DefaultConfig", x=>!string.IsNullOrWhiteSpace(x)).Show();
        }
        var c = Configuration.Create(Authorization, ServerAddress, Name);
        c.Save();
        if (NoSwitch.IsPresent && LiraSession.HasConfig)
        {
            LiraSession.Logger.LogWarning("Configuration {new} saved. Configuration {old} is still active.", c.Name, LiraSession.Config.Name);
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
        base.EndProcessing();
    }
    private void SetAddressManuallyIfMissing()
    {
        bool paramSet = TestBoundParameter(nameof(ServerAddress));
        if (paramSet && !string.IsNullOrWhiteSpace(ServerAddress))
        {
            return;
        }
        bool validAddress = LiraSession.Config.IsInitialized;
        var prompt = validAddress ? "Enter server address" : "Enter server address (usually \"https://jira.(company).com\")";
        var placeholder = validAddress ? LiraSession.Config.ServerAddress : null;
        var addresMenu = InteractiveStringMenu.Create(prompt, placeholder, AddressValidator.Instance);
        ServerAddress = addresMenu.Show();
        EnsureNotEmpty(ServerAddress, nameof(ServerAddress));
    }
}
