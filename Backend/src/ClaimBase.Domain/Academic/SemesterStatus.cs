namespace ClaimBase.Domain.Academic;

/// <summary>Semester lifecycle. Sessions can be logged only while a semester is open, which a later slice enforces.</summary>
public enum SemesterStatus
{
    /// <summary>Editable and not yet used for session logs.</summary>
    Draft = 1,

    /// <summary>The semester lecturers can log against. Open ranges must not overlap.</summary>
    Open = 2,

    /// <summary>Finished. Dates and name stay as they were.</summary>
    Closed = 3
}
