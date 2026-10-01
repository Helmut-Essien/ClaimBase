using ClaimBase.Application.Common;
using ClaimBase.Domain.Academic;
using NUlid;
using ClaimBase.Domain.Identity;
using ClaimBase.Shared.Academic;
using FluentAssertions;

namespace ClaimBase.Api.Tests.Domain;

public class AcademicRulesTests
{
    private const string TenantId = "01JB0000000000000000000TNT";
    private const string EntityId = "01JB0000000000000000000ENT";

    [Fact]
    public void TouchingAppointmentsDoNotOverlap()
    {
        var first = new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 1));
        var next = new DateRange(new DateOnly(2026, 6, 1), null);

        first.Overlaps(next).Should().BeFalse();
        DateRange.OverlapsAny(next, [first]).Should().BeFalse();
    }

    [Fact]
    public void SharedDaysOverlap()
    {
        var first = new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 2));
        var next = new DateRange(new DateOnly(2026, 6, 1), null);

        first.Overlaps(next).Should().BeTrue();
    }

    [Fact]
    public void SemesterOpensOnlyFromDraftAndClosesOnlyFromOpen()
    {
        var semester = Semester.Create(EntityId, TenantId, "2026", new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30));

        semester.Status.Should().Be(SemesterStatus.Draft);
        semester.Open();
        semester.Status.Should().Be(SemesterStatus.Open);
        var reopen = () => semester.Open();
        reopen.Should().Throw<InvalidOperationException>().WithMessage("Only a draft semester can be opened.");

        semester.Close();
        var edit = () => semester.UpdateDetails("2026", new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30));
        edit.Should().Throw<InvalidOperationException>().WithMessage("A closed semester cannot be changed.");
        var closeAgain = () => semester.Close();
        closeAgain.Should().Throw<InvalidOperationException>().WithMessage("Only an open semester can be closed.");
    }

    [Fact]
    public void AppointmentEndMustBeAfterTheStart()
    {
        var act = () => StaffPosition.Create(
            EntityId,
            TenantId,
            EntityId,
            EntityId,
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 6, 1));

        act.Should().Throw<ArgumentException>().WithMessage("EffectiveTo must be after EffectiveFrom.*");
    }

    [Fact]
    public void CourseCodeIsStoredUppercaseAndStaffEmailLowercase()
    {
        var course = Course.Create(EntityId, TenantId, " cs101 ", "Intro", EntityId);
        course.Code.Should().Be("CS101");

        var staff = Staff.Create(EntityId, TenantId, "L1", "Ada", " Ada@School.test ", EmploymentType.FullTime, "  ");
        staff.Email.Should().Be("ada@school.test");
        staff.BiometricId.Should().BeNull();
    }

    [Fact]
    public void NewIdsAreUlidStrings()
    {
        var id = EntityIds.New();

        id.Should().HaveLength(26);
        Ulid.TryParse(id, out _).Should().BeTrue();
    }

    [Fact]
    public void SharedLimitsMatchTheDomain()
    {
        AcademicFieldLimits.Name.Should().Be(AcademicConstraints.NameMaxLength);
        AcademicFieldLimits.ShortName.Should().Be(AcademicConstraints.QualificationNameMaxLength);
        AcademicFieldLimits.ShortName.Should().Be(AcademicConstraints.PositionTitleNameMaxLength);
        AcademicFieldLimits.CourseCode.Should().Be(AcademicConstraints.CourseCodeMaxLength);
        AcademicFieldLimits.StaffNumber.Should().Be(AcademicConstraints.StaffNumberMaxLength);
        AcademicFieldLimits.BiometricId.Should().Be(AcademicConstraints.BiometricIdMaxLength);
        AcademicFieldLimits.Email.Should().Be(UserConstraints.EmailMaxLength);
    }
}
