using System.Reflection;
using PCH.Connectors;

namespace PCH.Tests;

/// <summary>
/// Exercises EmailConnector's private Zoezi parsing via reflection since it has no public
/// surface — the goal is to catch regressions in the subject/body regexes, not to expose them.
/// </summary>
public class ZoeziBookingParsingTests
{
    private static readonly MethodInfo TryParseZoeziBooking = typeof(EmailConnector)
        .GetMethod("TryParseZoeziBooking", BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MethodInfo IsZoeziBooking = typeof(EmailConnector)
        .GetMethod("IsZoeziBooking", BindingFlags.NonPublic | BindingFlags.Static)!;

    private static bool TryParse(string subject, string rawBody, out string className, out DateTimeOffset startTime)
    {
        var args = new object?[] { subject, rawBody, null, default(DateTimeOffset) };
        var ok = (bool)TryParseZoeziBooking.Invoke(null, args)!;
        className = (string)args[2]!;
        startTime = (DateTimeOffset)args[3]!;
        return ok;
    }

    [Fact]
    public void ParsesClassNameAndStartTimeFromSubject()
    {
        var subject = "Pilates Flow startar 2026-07-05 18:00. Välkommen!";

        var ok = TryParse(subject, rawBody: "", out var className, out var startTime);

        Assert.True(ok);
        Assert.Equal("Pilates Flow", className);
        Assert.Equal(new DateTime(2026, 7, 5, 18, 0, 0), startTime.LocalDateTime);
    }

    [Fact]
    public void FallsBackToBodyWhenSubjectDoesNotMatch()
    {
        const string subject = "Din bokning är bekräftad";
        const string body = "Din bokning/lektion:\nPilates Flow\nStartar: 2026-07-05 18:00\nVi ses!";

        var ok = TryParse(subject, body, out var className, out var startTime);

        Assert.True(ok);
        Assert.Equal("Pilates Flow", className);
        Assert.Equal(new DateTime(2026, 7, 5, 18, 0, 0), startTime.LocalDateTime);
    }

    [Fact]
    public void FailsWhenNeitherSubjectNorBodyMatch()
    {
        var ok = TryParse("Your invoice is ready", "Please pay by Friday.", out _, out _);

        Assert.False(ok);
    }

    [Theory]
    [InlineData("noreply@gymsystem.se", "Some subject", true)]
    [InlineData("someone@example.com", "Pilates Flow startar 2026-07-05 18:00. Välkommen!", true)]
    [InlineData("someone@example.com", "Your invoice is ready", false)]
    public void DetectsZoeziEmailsByDomainOrSubject(string senderAddress, string subject, bool expected)
    {
        var result = (bool)IsZoeziBooking.Invoke(null, [senderAddress, subject])!;

        Assert.Equal(expected, result);
    }
}
