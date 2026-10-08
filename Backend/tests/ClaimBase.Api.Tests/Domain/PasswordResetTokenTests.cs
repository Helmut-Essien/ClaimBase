using ClaimBase.Domain.Identity;
using FluentAssertions;

namespace ClaimBase.Api.Tests.Domain;

public class PasswordResetTokenTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 10, 7, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Issue_StoresTheHashInLowercase()
    {
        var token = PasswordResetToken.Issue(Id("token"), Id("tenant"), Id("user"), new string('A', 64), CreatedAt.AddHours(1), CreatedAt);

        token.TokenHash.Should().Be(new string('a', 64));
        token.UsedAt.Should().BeNull();
    }

    [Fact]
    public void Issue_RejectsAShortHash()
    {
        var act = () => PasswordResetToken.Issue(Id("token"), Id("tenant"), Id("user"), "abc", CreatedAt.AddHours(1), CreatedAt);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Consume_MarksTheLinkUsedOnce()
    {
        var token = PasswordResetToken.Issue(Id("token"), Id("tenant"), Id("user"), new string('b', 64), CreatedAt.AddHours(1), CreatedAt);

        token.Consume(CreatedAt.AddMinutes(5));

        token.UsedAt.Should().Be(CreatedAt.AddMinutes(5));
        var act = () => token.Consume(CreatedAt.AddMinutes(6));
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ChangePassword_TruncatesTheStampToWholeSeconds()
    {
        var user = User.Create(Id("user"), Id("tenant"), "ada@claimbase.test", "Ada", "hash-value", UserRole.Admin, null, null, CreatedAt);

        user.ChangePassword("new-hash", CreatedAt.AddMinutes(1).AddMilliseconds(250));

        user.PasswordHash.Should().Be("new-hash");
        user.PasswordChangedAt.Should().Be(CreatedAt.AddMinutes(1));
    }

    private static string Id(string suffix) => ("01JRESET" + suffix).PadRight(26, '0')[..26];
}
