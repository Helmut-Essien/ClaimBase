using ClaimBase.Domain.Identity;
using FluentAssertions;

namespace ClaimBase.Api.Tests.Domain;

public class UserFactoryTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 9, 30, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void StoresEmailInLowercase()
    {
        var user = User.Create(Id("user"), Id("tenant"), " Ada@ClaimBase.test ", "Ada", "hash-value", UserRole.Admin, null, null, CreatedAt);

        user.Email.Should().Be("ada@claimbase.test");
    }

    [Fact]
    public void HeadOfDepartmentRequiresADepartment()
    {
        var act = () => User.Create(Id("user"), Id("tenant"), "hod@claimbase.test", "Hoda", "hash-value", UserRole.HeadOfDepartment, null, null, CreatedAt);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AdminCannotCarryADepartment()
    {
        var act = () => User.Create(Id("user"), Id("tenant"), "ada@claimbase.test", "Ada", "hash-value", UserRole.Admin, Id("dept"), null, CreatedAt);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void LecturerRequiresStaff()
    {
        var act = () => User.Create(Id("user"), Id("tenant"), "lee@claimbase.test", "Lee", "hash-value", UserRole.Lecturer, null, "  ", CreatedAt);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FinanceCannotCarryStaff()
    {
        var act = () => User.Create(Id("user"), Id("tenant"), "fin@claimbase.test", "Fin", "hash-value", UserRole.Finance, null, Id("staff"), CreatedAt);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RejectsACurrencyThatIsNotThreeLetters()
    {
        var act = () => Tenant.Create(Id("tenant"), "University", "ghs1", "Africa/Accra", CreatedAt);

        act.Should().Throw<ArgumentException>();
    }

    private static string Id(string suffix) => ("01JTEST" + suffix).PadRight(26, '0')[..26];
}
