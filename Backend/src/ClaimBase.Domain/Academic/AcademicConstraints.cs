namespace ClaimBase.Domain.Academic;

/// <summary>Column bounds for academic setup. Portal field limits must match these when that screen is built.</summary>
public static class AcademicConstraints
{
    /// <summary>Faculty, department, semester, course, and staff display names.</summary>
    public const int NameMaxLength = 200;

    /// <summary>Qualification name.</summary>
    public const int QualificationNameMaxLength = 80;

    /// <summary>Shared position title. This is the rate-matrix catalog, not a lecturer's appointment.</summary>
    public const int PositionTitleNameMaxLength = 80;

    /// <summary>Course code stored uppercase.</summary>
    public const int CourseCodeMaxLength = 32;

    /// <summary>Staff number unique per tenant.</summary>
    public const int StaffNumberMaxLength = 32;

    /// <summary>Optional device id. When set, it is the id on the biometric device, not a separate alias.</summary>
    public const int BiometricIdMaxLength = 64;
}
