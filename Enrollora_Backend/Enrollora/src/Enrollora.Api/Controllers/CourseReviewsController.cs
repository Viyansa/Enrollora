
using System.Security.Claims;
using Enrollora.Api.DTOs;
using Enrollora.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Enrollora.Api.Controllers;

[ApiController]
[Route("api/reviews")]
[Authorize]
public sealed class CourseReviewsController(
    ICourseReviewService service) : ControllerBase
{
    [HttpGet("course/{courseId:int}")]
    [Authorize(Roles = "Student,Administrator")]
    public Task<CourseReviewSummaryResponse> GetForCourse(
        int courseId,
        CancellationToken ct)
    {
        return service.GetForCourseAsync(courseId, ct);
    }

    [HttpGet("my/course/{courseId:int}")]
    [Authorize(Roles = "Student")]
    public Task<CourseReviewResponse?> GetMineForCourse(
        int courseId,
        CancellationToken ct)
    {
        return service.GetMineForCourseAsync(
            CurrentStudentId(), courseId, ct);
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Administrator")]
    public Task<IReadOnlyList<CourseReviewResponse>> GetAll(
        CancellationToken ct)
    {
        return service.GetAllAsync(ct);
    }

    [HttpPost("course/{courseId:int}")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Create(
        int courseId,
        SaveCourseReviewRequest request,
        CancellationToken ct)
    {
        var result = await service.CreateAsync(
            CurrentStudentId(), courseId, request, ct);

        return CreatedAtAction(
            nameof(GetMineForCourse),
            new { courseId },
            result
        );
    }

    [HttpPut("{reviewId:int}")]
    [Authorize(Roles = "Student")]
    public Task<CourseReviewResponse> Update(
        int reviewId,
        SaveCourseReviewRequest request,
        CancellationToken ct)
    {
        return service.UpdateAsync(
            CurrentStudentId(), reviewId, request, ct);
    }

    [HttpDelete("{reviewId:int}")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Delete(
        int reviewId,
        CancellationToken ct)
    {
        await service.DeleteAsync(
            CurrentStudentId(), reviewId, ct);

        return NoContent();
    }

    private int CurrentStudentId()
    {
        var studentId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(studentId, out var id))
        {
            throw new UnauthorizedAccessException(
                "Student identity is missing.");
        }

        return id;
    }
}
