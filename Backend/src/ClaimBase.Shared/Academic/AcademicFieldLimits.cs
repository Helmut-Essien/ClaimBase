namespace ClaimBase.Shared.Academic;

/// <summary>
/// Academic field bounds. These match the domain constraints. Portal <c>*_FIELD_LIMITS</c> are added with the portal screens.
/// </summary>
public static class AcademicFieldLimits
{
    /// <summary>Faculty, department, semester, course, and display names.</summary>
    public const int Name = 200;

    /// <summary>Qualification and position title names.</summary>
    public const int ShortName = 80;

    /// <summary>Course code.</summary>
    public const int CourseCode = 32;

    /// <summary>Staff number.</summary>
    public const int StaffNumber = 32;

    /// <summary>Biometric device id.</summary>
    public const int BiometricId = 64;

    /// <summary>Optional staff email.</summary>
    public const int Email = 320;
}
