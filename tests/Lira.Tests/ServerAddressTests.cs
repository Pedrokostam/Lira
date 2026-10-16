namespace Lira.Tests;

public class ServerAddressTests
{
    [Theory]
    [InlineData("jira.example.com", "https://jira.example.com/")]
    [InlineData("https://jira.example.com", "https://jira.example.com/")]
    [InlineData("http://jira.example.com", "http://jira.example.com/")]
    [InlineData("HTTPS://Jira.Example.COM", "https://jira.example.com/")]
    [InlineData("jira.example.com/", "https://jira.example.com/")]
    [InlineData("https://jira.example.com/secure/Dashboard.jspa", "https://jira.example.com/")]
    [InlineData("jira.example.com/browse/ABC-123?focusedId=1", "https://jira.example.com/")]
    [InlineData("my-company.atlassian.net", "https://my-company.atlassian.net/")]
    [InlineData("jira.sub.example.co.uk", "https://jira.sub.example.co.uk/")]
    [InlineData("192.168.1.10", "https://192.168.1.10/")]
    [InlineData(" jira.example.com", "https://jira.example.com/")]
    [InlineData("jira.example.com ", "https://jira.example.com/")]
    [InlineData("\t https://jira.example.com:8080 \r\n", "https://jira.example.com:8080/")]
    public void ValidAddressIsCoercedToSchemeAndHost(string input, string expected)
    {
        Assert.Equal(expected, LiraClient.GetJiraServerUrl(input).AbsoluteUri);
    }

    [Theory]
    [InlineData("jira.example.com:8080", "https://jira.example.com:8080/")]
    [InlineData("http://jira.example.com:8080/jira", "http://jira.example.com:8080/")]
    [InlineData("https://jira.example.com:8443?x=1", "https://jira.example.com:8443/")]
    [InlineData("https://jira.example.com:443", "https://jira.example.com/")]
    public void PortIsPreserved(string input, string expected)
    {
        Assert.Equal(expected, LiraClient.GetJiraServerUrl(input).AbsoluteUri);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("jira")]
    [InlineData("localhost")]
    [InlineData("https://")]
    [InlineData("htps://jira.example.com")]
    [InlineData("https//jira.example.com")]
    [InlineData("ftp://jira.example.com")]
    [InlineData("file:///C:/jira")]
    [InlineData("user@jira.example.com")]
    [InlineData("jira example.com")]
    [InlineData("localhost:8080")]
    [InlineData("jira.example.com:")]
    [InlineData("jira.example.com:abc")]
    [InlineData("jira.example.com:99999")]
    [InlineData("jira.example.com:123456")]
    public void InvalidAddressThrows(string input)
    {
        Assert.Throws<InvalidOperationException>(() => LiraClient.GetJiraServerUrl(input));
    }
}
