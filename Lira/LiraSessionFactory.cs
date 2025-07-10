using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lira.Authorization;
using Lira.Converters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lira;
public class LiraSessionFactory
{
    private LiraSessionFactory(Uri baseAddress)
    {
        BaseAddress = baseAddress;
    }
    public Uri BaseAddress { get; }
    public ClientMode ClientMode { get; set; }
    public ILogger<LiraClient> Logger { get; private set; } = NullLogger<LiraClient>.Instance;
    [AllowNull]
    public IAuthorization Authorization { get; private set; } = NoAuthorization.Instance;
    protected async Task Authorize(LiraClient lira)
    {
        await Authorization.Authorize(lira).ConfigureAwait(false);
        Logger.Authorized(Authorization.GetType());
        lira.Authorization = Authorization;
    }
    public async Task<LiraClient> Initialize()
    {
        var lira = new LiraClient(BaseAddress, Logger, ClientMode);
        Logger.CreatedNewInstance(BaseAddress);
        await Authorize(lira).ConfigureAwait(false);
        await lira.GetCurrentUser().ConfigureAwait(false);
        return lira;
    }
    public LiraSessionFactory WithLogger(ILogger<LiraClient>? logger)
    {
        Logger = logger ?? NullLogger<LiraClient>.Instance;
        return this;
    }
    public LiraSessionFactory AuthorizedBy(IAuthorization? authorization)
    {
        Authorization = authorization ?? NoAuthorization.Instance;
        return this;
    }
    public LiraSessionFactory AuthorizedByCredentials(string username, string password)
        => AuthorizedBy(new CookieProvider(username, password));
    public LiraSessionFactory AuthorizedByPersonalAccessToken(string personalAccessToken)
        => AuthorizedBy(new PersonalAccessToken(personalAccessToken));

    public LiraSessionFactory AuthorizedByAtlassianApiKey(string userEmail, string atlassianApiKey)
        => AuthorizedBy(new AtlassianApiKey(userEmail, atlassianApiKey));

    public LiraSessionFactory WithMode(ClientMode mode)
    {
        ClientMode = mode;
        return this;
    }
    public LiraSessionFactory Online() => WithMode(ClientMode.Online);
    public LiraSessionFactory Offline() => WithMode(ClientMode.Offline);
    public LiraSessionFactory ReadOnly() => WithMode(ClientMode.ReadOnly);

    public static LiraSessionFactory Create(Uri baseAddress)
    {
        return new LiraSessionFactory(baseAddress);
    }
    public static LiraSessionFactory Create(string baseAddress)
        => Create(LiraClient.GetJiraServerUrl(baseAddress));
}