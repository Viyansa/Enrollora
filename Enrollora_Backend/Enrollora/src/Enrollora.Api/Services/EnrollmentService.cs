
using Enrollora.Api.Data;
using Enrollora.Api.DTOs;
using Enrollora.Api.Models;
using Enrollora.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Enrollora.Api.Services;

public sealed class EnrollmentService(
    ApplicationDbContext db,
    IRepository<Enrollment> enrollments,
    IRepository<Student> students,
    IRepository<Course> courses) : IEnrollmentService
{
    public Task<PagedResult<EnrollmentResponse>> GetAllAsync(
        EnrollmentQuery query,
        CancellationToken ct)
    {
        var enrollmentQuery = enrollments.Query();

        return GetPageAsync(enrollmentQuery, query, ct);
    }

    public Task<PagedResult<EnrollmentResponse>> GetMineAsync(
        int studentId,
        EnrollmentQuery query,
        CancellationToken ct)
    {
        var enrollmentQuery = enrollments.Query()
            .Where(x => x.StudentId == studentId);

        return GetPageAsync(enrollmentQuery, query, ct);
    }

    public Task<PagedResult<EnrollmentResponse>> GetForStudentAsync(
        int studentId,
        EnrollmentQuery query,
        CancellationToken ct)
    {
        var enrollmentQuery = enrollments.Query()
            .Where(x => x.StudentId == studentId);

        return GetPageAsync(enrollmentQuery, query, ct);
    }

    public Task<PagedResult<EnrollmentResponse>> GetForCourseAsync(
        int courseId,
        EnrollmentQuery query,
        CancellationToken ct)
    {
        var enrollmentQuery = enrollments.Query()
            .Where(x => x.CourseId == courseId);

        return GetPageAsync(enrollmentQuery, query, ct);
    }

    public async Task<EnrollmentResponse> CreateAsync(
        int studentId,
        int courseId,
        CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, ct);

        var student = await students.Query()
            .FirstOrDefaultAsync(x => x.StudentId == studentId, ct)
            ?? throw new KeyNotFoundException("Student was not found.");

        if (student.Status != StudentStatus.Approved)
        {
            throw new InvalidOperationException(
                "Only approved students can enroll.");
        }

        var course = await courses.Query(true)
            .FirstOrDefaultAsync(x => x.CourseId == courseId, ct)
            ?? throw new KeyNotFoundException("Course was not found.");

        if (!course.IsActive)
        {
            throw new InvalidOperationException(
                "This course is not available for enrollment.");
        }

        var alreadyEnrolled = await enrollments.Query()
            .AnyAsync(
                x => x.StudentId == studentId &&
                     x.CourseId == courseId &&
                     x.Status == EnrollmentStatus.Active,
                ct);

        if (alreadyEnrolled)
        {
            throw new InvalidOperationException(
                "You already have an active enrollment in this course.");
        }

        var activeCount = await enrollments.Query()
            .CountAsync(
                x => x.CourseId == courseId &&
                     x.Status == EnrollmentStatus.Active,
                ct);

        if (activeCount >= course.Capacity)
        {
            throw new InvalidOperationException(
                "This course has reached its enrollment capacity.");
        }

        var enrollment = new Enrollment
        {
            StudentId = studentId,
            CourseId = courseId,
            EnrolledAt = DateTime.UtcNow,
            Status = EnrollmentStatus.Active
        };

        await enrollments.AddAsync(enrollment, ct);
        await enrollments.SaveChangesAsync(ct);

        await transaction.CommitAsync(ct);

        return new EnrollmentResponse(
            enrollment.EnrollmentId,
            studentId,
            student.Name,
            student.Email,
            courseId,
            course.CourseName,
            enrollment.EnrolledAt,
            enrollment.Status);
    }

    private async Task<PagedResult<EnrollmentResponse>> GetPageAsync(
        IQueryable<Enrollment> query,
        EnrollmentQuery filter,
        CancellationToken ct)
    {
        filter.Normalize();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var searchTerm = filter.Search.Trim();

            query = query.Where(x =>
                x.Student.Name.Contains(searchTerm) ||
                x.Student.Email.Contains(searchTerm));
        }

        if (filter.CourseId.HasValue)
        {
            query = query.Where(x =>
                x.CourseId == filter.CourseId.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(x =>
                x.Status == filter.Status.Value);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(x =>
                x.EnrolledAt >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(x =>
                x.EnrolledAt <= filter.ToDate.Value);
        }

        query = filter.SortBy?.ToLowerInvariant() switch
        {
            "student" => filter.SortOrder == "desc"
                ? query.OrderByDescending(x => x.Student.Name)
                    .ThenBy(x => x.EnrollmentId)
                : query.OrderBy(x => x.Student.Name)
                    .ThenBy(x => x.EnrollmentId),

            "course" => filter.SortOrder == "desc"
                ? query.OrderByDescending(x => x.Course.CourseName)
                    .ThenBy(x => x.EnrollmentId)
                : query.OrderBy(x => x.Course.CourseName)
                    .ThenBy(x => x.EnrollmentId),

            _ => filter.SortOrder == "desc"
                ? query.OrderByDescending(x => x.EnrolledAt)
                    .ThenBy(x => x.EnrollmentId)
                : query.OrderBy(x => x.EnrolledAt)
                    .ThenBy(x => x.EnrollmentId)
        };

        var totalRecords = await query.CountAsync(ct);

        var totalPages = (int)Math.Ceiling(
            totalRecords / (double)filter.PageSize);

        var items = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(x => new EnrollmentResponse(
                x.EnrollmentId,
                x.StudentId,
                x.Student.Name,
                x.Student.Email,
                x.CourseId,
                x.Course.CourseName,
                x.EnrolledAt,
                x.Status))
            .ToListAsync(ct);

        return new PagedResult<EnrollmentResponse>(
            items,
            filter.PageNumber,
            filter.PageSize,
            totalRecords,
            totalPages);
    }
}
