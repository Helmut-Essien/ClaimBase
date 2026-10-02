using ClaimBase.Application.Features.Campuses;
using ClaimBase.Application.Features.Courses;
using ClaimBase.Application.Features.Departments;
using ClaimBase.Application.Features.Faculties;
using ClaimBase.Application.Features.Qualifications;
using ClaimBase.Application.Features.Semesters;
using ClaimBase.Application.Features.Staff;
using ClaimBase.Shared.Academic;
using ClaimBase.Shared.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClaimBase.Api.Controllers;

/// <summary>Campus setup. Tenant admin and admin only. The role is checked from the database.</summary>
[ApiController]
[Authorize]
[Route("api/campuses")]
public sealed class CampusesController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>Creates the controller.</summary>
    /// <param name="sender">MediatR sender.</param>
    public CampusesController(ISender sender)
    {
        ArgumentNullException.ThrowIfNull(sender);
        _sender = sender;
    }

    /// <summary>Lists campuses.</summary>
    /// <param name="page">1-based page.</param>
    /// <param name="pageSize">Page size, 1 to 100.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>One page of campuses.</returns>
    [HttpGet]
    public async Task<ActionResult<PagedResult<CampusResponse>>> List(
        [FromQuery] int page = PageLimits.DefaultPage,
        [FromQuery] int pageSize = PageLimits.DefaultSize,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _sender.Send(new ListCampusesQuery(page, pageSize), cancellationToken));
    }

    /// <summary>Creates a campus.</summary>
    /// <param name="request">Campus name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new campus.</returns>
    [HttpPost]
    public async Task<ActionResult<CampusResponse>> Create([FromBody] CreateCampusRequest request, CancellationToken cancellationToken)
    {
        var created = await _sender.Send(new CreateCampusCommand(request.Name), cancellationToken);
        return Created("/api/campuses", created);
    }
}

/// <summary>Faculty setup. Tenant admin and admin only. The role is checked from the database.</summary>
[ApiController]
[Authorize]
[Route("api/faculties")]
public sealed class FacultiesController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>Creates the controller.</summary>
    /// <param name="sender">MediatR sender.</param>
    public FacultiesController(ISender sender)
    {
        ArgumentNullException.ThrowIfNull(sender);
        _sender = sender;
    }

    /// <summary>Lists faculties.</summary>
    /// <param name="page">1-based page.</param>
    /// <param name="pageSize">Page size, 1 to 100.</param>
    /// <param name="campusId">Optional campus filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>One page of faculties.</returns>
    [HttpGet]
    public async Task<ActionResult<PagedResult<FacultyResponse>>> List(
        [FromQuery] int page = PageLimits.DefaultPage,
        [FromQuery] int pageSize = PageLimits.DefaultSize,
        [FromQuery] string? campusId = null,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _sender.Send(new ListFacultiesQuery(page, pageSize, campusId), cancellationToken));
    }

    /// <summary>Creates a faculty.</summary>
    /// <param name="request">Campus and faculty name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new faculty.</returns>
    [HttpPost]
    public async Task<ActionResult<FacultyResponse>> Create([FromBody] CreateFacultyRequest request, CancellationToken cancellationToken)
    {
        var created = await _sender.Send(new CreateFacultyCommand(request.CampusId, request.Name), cancellationToken);
        return Created("/api/faculties", created);
    }
}

/// <summary>Department setup. Tenant admin and admin only.</summary>
[ApiController]
[Authorize]
[Route("api/departments")]
public sealed class DepartmentsController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>Creates the controller.</summary>
    /// <param name="sender">MediatR sender.</param>
    public DepartmentsController(ISender sender)
    {
        ArgumentNullException.ThrowIfNull(sender);
        _sender = sender;
    }

    /// <summary>Lists departments.</summary>
    /// <param name="page">1-based page.</param>
    /// <param name="pageSize">Page size, 1 to 100.</param>
    /// <param name="facultyId">Optional faculty filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>One page of departments.</returns>
    [HttpGet]
    public async Task<ActionResult<PagedResult<DepartmentResponse>>> List(
        [FromQuery] int page = PageLimits.DefaultPage,
        [FromQuery] int pageSize = PageLimits.DefaultSize,
        [FromQuery] string? facultyId = null,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _sender.Send(new ListDepartmentsQuery(page, pageSize, facultyId), cancellationToken));
    }

    /// <summary>Creates a department under a faculty.</summary>
    /// <param name="request">Faculty and name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new department.</returns>
    [HttpPost]
    public async Task<ActionResult<DepartmentResponse>> Create([FromBody] CreateDepartmentRequest request, CancellationToken cancellationToken)
    {
        var created = await _sender.Send(new CreateDepartmentCommand(request.FacultyId, request.Name), cancellationToken);
        return Created("/api/departments", created);
    }
}

/// <summary>Semester setup. Tenant admin and admin only.</summary>
[ApiController]
[Authorize]
[Route("api/semesters")]
public sealed class SemestersController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>Creates the controller.</summary>
    /// <param name="sender">MediatR sender.</param>
    public SemestersController(ISender sender)
    {
        ArgumentNullException.ThrowIfNull(sender);
        _sender = sender;
    }

    /// <summary>Lists semesters.</summary>
    /// <param name="page">1-based page.</param>
    /// <param name="pageSize">Page size, 1 to 100.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>One page of semesters.</returns>
    [HttpGet]
    public async Task<ActionResult<PagedResult<SemesterResponse>>> List(
        [FromQuery] int page = PageLimits.DefaultPage,
        [FromQuery] int pageSize = PageLimits.DefaultSize,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _sender.Send(new ListSemestersQuery(page, pageSize), cancellationToken));
    }

    /// <summary>Creates a draft semester.</summary>
    /// <param name="request">Name and inclusive dates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new semester.</returns>
    [HttpPost]
    public async Task<ActionResult<SemesterResponse>> Create([FromBody] CreateSemesterRequest request, CancellationToken cancellationToken)
    {
        var created = await _sender.Send(
            new CreateSemesterCommand(request.Name, request.StartDate, request.EndDate),
            cancellationToken);
        return Created("/api/semesters", created);
    }

    /// <summary>Updates a semester that is not closed.</summary>
    /// <param name="request">Id, name, and inclusive dates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated semester.</returns>
    [HttpPut]
    public async Task<ActionResult<SemesterResponse>> Update([FromBody] UpdateSemesterRequest request, CancellationToken cancellationToken)
    {
        var updated = await _sender.Send(
            new UpdateSemesterCommand(request.Id, request.Name, request.StartDate, request.EndDate),
            cancellationToken);
        return Ok(updated);
    }

    /// <summary>Opens a draft semester.</summary>
    /// <param name="id">Semester id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The open semester.</returns>
    [HttpPost("{id}/open")]
    public async Task<ActionResult<SemesterResponse>> Open(string id, CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new OpenSemesterCommand(id), cancellationToken));
    }

    /// <summary>Closes an open semester.</summary>
    /// <param name="id">Semester id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The closed semester.</returns>
    [HttpPost("{id}/close")]
    public async Task<ActionResult<SemesterResponse>> Close(string id, CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new CloseSemesterCommand(id), cancellationToken));
    }
}

/// <summary>Qualification lookup. Tenant admin and admin only.</summary>
[ApiController]
[Authorize]
[Route("api/qualifications")]
public sealed class QualificationsController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>Creates the controller.</summary>
    /// <param name="sender">MediatR sender.</param>
    public QualificationsController(ISender sender)
    {
        ArgumentNullException.ThrowIfNull(sender);
        _sender = sender;
    }

    /// <summary>Lists qualifications.</summary>
    /// <param name="page">1-based page.</param>
    /// <param name="pageSize">Page size, 1 to 100.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>One page of qualifications.</returns>
    [HttpGet]
    public async Task<ActionResult<PagedResult<QualificationResponse>>> List(
        [FromQuery] int page = PageLimits.DefaultPage,
        [FromQuery] int pageSize = PageLimits.DefaultSize,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _sender.Send(new ListQualificationsQuery(page, pageSize), cancellationToken));
    }

    /// <summary>Creates a qualification.</summary>
    /// <param name="request">Qualification name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new qualification.</returns>
    [HttpPost]
    public async Task<ActionResult<QualificationResponse>> Create(
        [FromBody] CreateQualificationRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _sender.Send(new CreateQualificationCommand(request.Name), cancellationToken);
        return Created("/api/qualifications", created);
    }
}

/// <summary>Position title lookup. Tenant admin and admin only.</summary>
[ApiController]
[Authorize]
[Route("api/position-titles")]
public sealed class PositionTitlesController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>Creates the controller.</summary>
    /// <param name="sender">MediatR sender.</param>
    public PositionTitlesController(ISender sender)
    {
        ArgumentNullException.ThrowIfNull(sender);
        _sender = sender;
    }

    /// <summary>Lists position titles.</summary>
    /// <param name="page">1-based page.</param>
    /// <param name="pageSize">Page size, 1 to 100.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>One page of titles.</returns>
    [HttpGet]
    public async Task<ActionResult<PagedResult<PositionTitleResponse>>> List(
        [FromQuery] int page = PageLimits.DefaultPage,
        [FromQuery] int pageSize = PageLimits.DefaultSize,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _sender.Send(new ListPositionTitlesQuery(page, pageSize), cancellationToken));
    }

    /// <summary>Creates a position title.</summary>
    /// <param name="request">Title name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new title.</returns>
    [HttpPost]
    public async Task<ActionResult<PositionTitleResponse>> Create(
        [FromBody] CreatePositionTitleRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _sender.Send(new CreatePositionTitleCommand(request.Name), cancellationToken);
        return Created("/api/position-titles", created);
    }
}

/// <summary>Course setup. Tenant admin and admin only.</summary>
[ApiController]
[Authorize]
[Route("api/courses")]
public sealed class CoursesController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>Creates the controller.</summary>
    /// <param name="sender">MediatR sender.</param>
    public CoursesController(ISender sender)
    {
        ArgumentNullException.ThrowIfNull(sender);
        _sender = sender;
    }

    /// <summary>Lists courses.</summary>
    /// <param name="page">1-based page.</param>
    /// <param name="pageSize">Page size, 1 to 100.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>One page of courses.</returns>
    [HttpGet]
    public async Task<ActionResult<PagedResult<CourseResponse>>> List(
        [FromQuery] int page = PageLimits.DefaultPage,
        [FromQuery] int pageSize = PageLimits.DefaultSize,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _sender.Send(new ListCoursesQuery(page, pageSize), cancellationToken));
    }

    /// <summary>Creates a course.</summary>
    /// <param name="request">Code, name, and qualification.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new course.</returns>
    [HttpPost]
    public async Task<ActionResult<CourseResponse>> Create([FromBody] CreateCourseRequest request, CancellationToken cancellationToken)
    {
        var created = await _sender.Send(
            new CreateCourseCommand(request.Code, request.Name, request.QualificationId),
            cancellationToken);
        return Created("/api/courses", created);
    }

    /// <summary>Updates a course.</summary>
    /// <param name="request">Id, code, name, and qualification.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated course.</returns>
    [HttpPut]
    public async Task<ActionResult<CourseResponse>> Update([FromBody] UpdateCourseRequest request, CancellationToken cancellationToken)
    {
        var updated = await _sender.Send(
            new UpdateCourseCommand(request.Id, request.Code, request.Name, request.QualificationId),
            cancellationToken);
        return Ok(updated);
    }
}

/// <summary>Staff, department assignments, and position appointments. Tenant admin and admin only.</summary>
[ApiController]
[Authorize]
[Route("api/staff")]
public sealed class StaffController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>Creates the controller.</summary>
    /// <param name="sender">MediatR sender.</param>
    public StaffController(ISender sender)
    {
        ArgumentNullException.ThrowIfNull(sender);
        _sender = sender;
    }

    /// <summary>Lists staff.</summary>
    /// <param name="page">1-based page.</param>
    /// <param name="pageSize">Page size, 1 to 100.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>One page of staff.</returns>
    [HttpGet]
    public async Task<ActionResult<PagedResult<StaffResponse>>> List(
        [FromQuery] int page = PageLimits.DefaultPage,
        [FromQuery] int pageSize = PageLimits.DefaultSize,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _sender.Send(new ListStaffQuery(page, pageSize), cancellationToken));
    }

    /// <summary>Creates a lecturer with at least one department.</summary>
    /// <param name="request">Staff fields and department ids.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new lecturer.</returns>
    [HttpPost]
    public async Task<ActionResult<StaffResponse>> Create([FromBody] CreateStaffRequest request, CancellationToken cancellationToken)
    {
        var created = await _sender.Send(
            new CreateStaffCommand(
                request.StaffNumber,
                request.DisplayName,
                request.Email,
                request.EmploymentType,
                request.BiometricId,
                request.DepartmentIds),
            cancellationToken);
        return Created("/api/staff", created);
    }

    /// <summary>Updates a lecturer. Departments are not changed here.</summary>
    /// <param name="request">Staff fields.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated lecturer.</returns>
    [HttpPut]
    public async Task<ActionResult<StaffResponse>> Update([FromBody] UpdateStaffRequest request, CancellationToken cancellationToken)
    {
        var updated = await _sender.Send(
            new UpdateStaffCommand(
                request.Id,
                request.StaffNumber,
                request.DisplayName,
                request.Email,
                request.EmploymentType,
                request.BiometricId),
            cancellationToken);
        return Ok(updated);
    }

    /// <summary>Assigns another department.</summary>
    /// <param name="id">Staff id.</param>
    /// <param name="request">Department id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The lecturer with the new assignment.</returns>
    [HttpPost("{id}/departments")]
    public async Task<ActionResult<StaffResponse>> AssignDepartment(
        string id,
        [FromBody] AssignDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _sender.Send(new AssignDepartmentCommand(id, request.DepartmentId), cancellationToken);
        return Ok(updated);
    }

    /// <summary>Removes one department assignment. The last assignment is rejected.</summary>
    /// <param name="id">Staff id.</param>
    /// <param name="departmentId">Department id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The lecturer after the removal.</returns>
    [HttpDelete("{id}/departments/{departmentId}")]
    public async Task<ActionResult<StaffResponse>> RemoveDepartment(string id, string departmentId, CancellationToken cancellationToken)
    {
        var updated = await _sender.Send(new RemoveDepartmentCommand(id, departmentId), cancellationToken);
        return Ok(updated);
    }

    /// <summary>Lists position appointments.</summary>
    /// <param name="id">Staff id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Appointments for the lecturer.</returns>
    [HttpGet("{id}/positions")]
    public async Task<ActionResult<IReadOnlyList<StaffPositionResponse>>> ListPositions(string id, CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new ListPositionsQuery(id), cancellationToken));
    }

    /// <summary>Adds a position appointment.</summary>
    /// <param name="id">Staff id.</param>
    /// <param name="request">Title and half-open dates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new appointment.</returns>
    [HttpPost("{id}/positions")]
    public async Task<ActionResult<StaffPositionResponse>> CreatePosition(
        string id,
        [FromBody] CreateStaffPositionRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _sender.Send(
            new CreateStaffPositionCommand(id, request.PositionTitleId, request.EffectiveFrom, request.EffectiveTo),
            cancellationToken);
        return Created($"/api/staff/{id}/positions", created);
    }
}
