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

namespace LiraPS.Cmdlets;
public enum ConfigurationType
{
    ManualChoice,
    Pat,
    PersonalAccessToken = Pat,
    Credentials,
    AttlasianApiKey,
}

[Cmdlet(VerbsCommon.Set, "Configuration", DefaultParameterSetName = "MANUAL")]
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
    private int Clamp(int x, int min, int max)
    {
        if (x < min)
        {
            return min;
        }
        if (x > max)
        {
            return max;
        }
        return x;
    }
    private void SelectYourCharacter()
    {
        if (Type == ConfigurationType.ManualChoice)
        {
            if (Console.IsInputRedirected)
            {
                Terminate(new ArgumentException("Host does not allow interactivity"), "UnsupportedHost", ErrorCategory.InvalidOperation);
            }
            WriteHost("Choose what kind of authentication you want to use:", ConsoleColor.Green);
            WriteHost("1 - Personal Access Token", ConsoleColor.Cyan);
            WriteHost("2 - Username and password", ConsoleColor.Cyan);
            WriteHost("3 - Atlassian API key", ConsoleColor.Cyan);
            WriteHost("Use arrow keys or numbers, accept with enter", ConsoleColor.Yellow);
            int choice = 1;
            int min = 1;
            int max = 3;
            ConsoleKey key;
            Console.CursorVisible = false;
            do
            {
                string info = choice switch
                {
                    1 => "Personal Access Token - use token created via website",
                    2 => "Username and password - log in with user name and password",
                    3 => "Atlassian API key - use email and API key",
                    _ => "",
                };
                Console.Write($"\rChoice: \x1b[1m{choice}\x1b[0m | {info}");
                key = Console.ReadKey(intercept: true).Key;
                switch (key)
                {
                    case ConsoleKey.D1:
                        choice = 1;
                        break;
                    case ConsoleKey.D2:
                        choice = 2;
                        break;
                    case ConsoleKey.D3:
                        choice = 3;
                        break;
                    case ConsoleKey.DownArrow:
                    case ConsoleKey.LeftArrow:
                    case ConsoleKey.S:
                    case ConsoleKey.A:
                        choice = Clamp(choice - 1, min, max);
                        break;
                    case ConsoleKey.UpArrow:
                    case ConsoleKey.RightArrow:
                    case ConsoleKey.W:
                    case ConsoleKey.D:
                        choice = Clamp(choice + 1, min, max);
                        break;
                    default:
                        break;
                }
            } while (key != ConsoleKey.Enter);
            Console.CursorVisible = true;
            Console.WriteLine();
            Type = choice switch
            {
                1 => ConfigurationType.PersonalAccessToken,
                2 => ConfigurationType.Credentials,
                3 => ConfigurationType.AttlasianApiKey,
                _ => throw new NotSupportedException($"Choice {choice} is not supported"),
            };
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
                    throw new InvalidOperationException("You were not supposed to get here");
                case ConfigurationType.Pat:
                    var pat = ReadInput("Personal access token", asSecure: true);
                    EnsureNotEmpty(pat, "Token");
                    Authorization = new PersonalAccessToken(pat);
                    break;
                case ConfigurationType.Credentials:
                    var username = ReadInput("User username");
                    EnsureNotEmpty(username, "Username");
                    var passwordC = ReadInput("Password", asSecure: true);
                    EnsureNotEmpty(passwordC, "Password");
                    Authorization = new CookieProvider(username, passwordC);
                    break;
                case ConfigurationType.AttlasianApiKey:
                    var userEmail = ReadInput("User email");
                    EnsureNotEmpty(userEmail, "User Email");
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
            Name = ReadInput("Do you want to specify custom username for this configuration? Leave empty to use the \"DefaultConfig\"");
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
        PrintLogs();
    }
    private void SetAddressManuallyIfMissing()
    {
        bool paramSet = TestBoundParameter(nameof(ServerAddress));
        if (paramSet && !string.IsNullOrWhiteSpace(ServerAddress))
        {
            return;
        }
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
