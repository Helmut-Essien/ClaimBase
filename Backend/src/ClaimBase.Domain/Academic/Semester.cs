using ClaimBase.Domain.Common;
using ClaimBase.Domain.Identity;

namespace ClaimBase.Domain.Academic;

/// <summary>
/// A teaching semester. Only one open semester may cover a given date. Closed semesters stay readable and are not edited.
/// </summary>
public sealed class Semester
{
    private Semester()
    {
    }

    /// <summary>ULID primary key.</summary>
    public string Id { get; private set; } = null!;

    /// <summary>Owning tenant.</summary>
    public string TenantId { get; private set; } = null!;

    /// <summary>Semester name.</summary>
    public string Name { get; private set; } = null!;

    /// <summary>First calendar day included.</summary>
    public DateOnly StartDate { get; private set; }

    /// <summary>Last calendar day included.</summary>
    public DateOnly EndDate { get; private set; }

    /// <summary>Draft, open, or closed.</summary>
    public SemesterStatus Status { get; private set; }

    /// <summary>
    /// Creates a draft semester.
    /// </summary>
    /// <param name="id">ULID.</param>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="name">Semester name.</param>
    /// <param name="startDate">First day.</param>
    /// <param name="endDate">Last day. Must be on or after <paramref name="startDate"/>.</param>
    /// <returns>The new draft.</returns>
    public static Semester Create(string id, string tenantId, string name, DateOnly startDate, DateOnly endDate)
    {
        EnsureDateOrder(startDate, endDate);
        return new Semester
        {
            Id = Guard.RequiredId(id, nameof(id), UserConstraints.IdMaxLength),
            TenantId = Guard.RequiredId(tenantId, nameof(tenantId), UserConstraints.IdMaxLength),
            Name = Guard.Required(name, nameof(name), AcademicConstraints.NameMaxLength),
            StartDate = startDate,
            EndDate = endDate,
            Status = SemesterStatus.Draft
        };
    }

    /// <summary>
    /// Updates the name and dates. A closed semester is left unchanged.
    /// </summary>
    /// <param name="name">Semester name.</param>
    /// <param name="startDate">First day.</param>
    /// <param name="endDate">Last day.</param>
    public void UpdateDetails(string name, DateOnly startDate, DateOnly endDate)
    {
        if (Status == SemesterStatus.Closed)
            throw new InvalidOperationException("A closed semester cannot be changed.");

        EnsureDateOrder(startDate, endDate);
        Name = Guard.Required(name, nameof(name), AcademicConstraints.NameMaxLength);
        StartDate = startDate;
        EndDate = endDate;
    }

    /// <summary>Moves a draft to open. The caller must already have rejected an overlapping open semester.</summary>
    public void Open()
    {
        if (Status != SemesterStatus.Draft)
            throw new InvalidOperationException("Only a draft semester can be opened.");

        Status = SemesterStatus.Open;
    }

    /// <summary>Moves an open semester to closed.</summary>
    public void Close()
    {
        if (Status != SemesterStatus.Open)
            throw new InvalidOperationException("Only an open semester can be closed.");

        Status = SemesterStatus.Closed;
    }

    private static void EnsureDateOrder(DateOnly startDate, DateOnly endDate)
    {
        // A missing JSON date binds as 0001-01-01. That is not a semester day.
        if (startDate == default)
            throw new ArgumentException("StartDate is required.", nameof(startDate));

        if (endDate == default)
            throw new ArgumentException("EndDate is required.", nameof(endDate));

        if (endDate < startDate)
            throw new ArgumentException("EndDate must be on or after StartDate.", nameof(endDate));
    }
}
