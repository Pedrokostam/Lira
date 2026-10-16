using Lira.Parsing;

namespace Lira.Tests;

public class DurationParserTests
{
    [Theory]
    [InlineData("1h", 60)]
    [InlineData("30m", 30)]
    [InlineData("90m", 90)]
    [InlineData("1h30m", 90)]
    [InlineData("1h 30m", 90)]
    [InlineData("2H 15M", 135)]
    [InlineData("1.5h", 90)]
    [InlineData("1,5h", 90)]
    [InlineData("0.25h", 15)]
    [InlineData("-1h", -60)]
    [InlineData("- 30m", -30)]
    public void ParsesMinutes(string input, double expectedMinutes)
    {
        Assert.Equal(TimeSpan.FromMinutes(expectedMinutes), DurationParser.ParseTime(input));
    }

    [Fact]
    public void ParsesSeconds()
    {
        Assert.Equal(new TimeSpan(1, 30, 15), DurationParser.ParseTime("1h30m15s"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("30")]
    [InlineData("h")]
    [InlineData("1d")]
    [InlineData("1.2.3h")]
    [InlineData("--1h")]
    public void InvalidInputThrows(string input)
    {
        Assert.Throws<ArgumentException>(() => DurationParser.ParseTime(input));
    }

    // Unknown characters and dangling numbers must not be silently dropped or merged into the next number.
    [Theory]
    [InlineData("1d2h")]
    [InlineData("1x2h")]
    [InlineData("1 2h")]
    [InlineData("1h30")]
    [InlineData("1hh")]
    [InlineData("1h -30m")]
    [InlineData("1-2h")]
    public void GarbageBetweenNumbersThrows(string input)
    {
        Assert.Throws<ArgumentException>(() => DurationParser.ParseTime(input));
    }

    [Fact]
    public void TooLongNumberThrowsBufferExceeded()
    {
        Assert.Throws<DurationParser.BufferExceededException>(() => DurationParser.ParseTime(new string('1', 30) + "h"));
    }
}
