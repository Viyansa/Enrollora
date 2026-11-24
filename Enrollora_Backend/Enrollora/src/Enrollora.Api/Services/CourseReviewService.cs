
using Enrollora.Api.Data;
using Enrollora.Api.DTOs;
using Enrollora.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Enrollora.Api.Services;

public sealed class CourseReviewService(ApplicationDbContext db)
    : ICourseReviewService
{
    public async Task<CourseReviewSummaryResponse> GetForCourseAsync(
        int courseId,
        CancellationToken ct)
    {
        var course = await db.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CourseId == courseId, ct)
            ?? throw new KeyNotFoundException("Course was not found.");

        var reviews = await db.CourseReviews
            .AsNoTracking()
            .Where(x => x.CourseId == courseId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new CourseReviewResponse(
                x.CourseReviewId,
                x.CourseId,
                x.Course.CourseName,
                x.StudentId,
                x.Student.Name,
                x.Rating,
                x.Comment,
                x.CreatedAt,
                x.UpdatedAt
            ))
            .ToListAsync(ct);

        var average = reviews.Count == 0
            ? 0
            : Math.Round(reviews.Average(x => x.Rating), 1);

        return new CourseReviewSummaryResponse(
            courseId,
            course.CourseName,
            average,
            reviews.Count,
            reviews
        );
    }

    public async Task<CourseReviewResponse?> GetMineForCourseAsync(
        int studentId,
        int courseId,
        CancellationToken ct)
    {
        return await db.CourseReviews
            .AsNoTracking()
            .Where(x => x.StudentId == studentId &&
                        x.CourseId == courseId)
            .Select(x => new CourseReviewResponse(
                x.CourseReviewId,
                x.CourseId,
                x.Course.CourseName,
                x.StudentId,
                x.Student.Name,
                x.Rating,
                x.Comment,
                x.CreatedAt,
                x.UpdatedAt
            ))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<CourseReviewResponse>> GetAllAsync(
        CancellationToken ct)
    {
        return await db.CourseReviews
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new CourseReviewResponse(
                x.CourseReviewId,
                x.CourseId,
                x.Course.CourseName,
                x.StudentId,
                x.Student.Name,
                x.Rating,
                x.Comment,
                x.CreatedAt,
                x.UpdatedAt
            ))
            .ToListAsync(ct);
    }

    public async Task<CourseReviewResponse> CreateAsync(
        int studentId,
        int courseId,
        SaveCourseReviewRequest request,
        CancellationToken ct)
    {
        var hasActiveEnrollment = await db.Enrollments.AnyAsync(
            x => x.StudentId == studentId &&
                 x.CourseId == courseId &&
                 x.Status == EnrollmentStatus.Active,
            ct
        );

        if (!hasActiveEnrollment)
        {
            throw new InvalidOperationException(
                "You can review a course only when you have an active enrollment."
            );
        }

        var alreadyReviewed = await db.CourseReviews.AnyAsync(
            x => x.StudentId == studentId &&
                 x.CourseId == courseId,
            ct
        );

        if (alreadyReviewed)
        {
            throw new InvalidOperationException(
                "You have already reviewed this course. Update your existing review instead."
            );
        }

        var review = new CourseReview
        {
            StudentId = studentId,
            CourseId = courseId,
            Rating = request.Rating,
            Comment = request.Comment.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        db.CourseReviews.Add(review);

        await db.SaveChangesAsync(ct);

        return await GetReviewAsync(review.CourseReviewId, ct);
    }

    public async Task<CourseReviewResponse> UpdateAsync(
        int studentId,
        int reviewId,
        SaveCourseReviewRequest request,
        CancellationToken ct)
    {
        var review = await db.CourseReviews.FirstOrDefaultAsync(
            x => x.CourseReviewId == reviewId &&
                 x.StudentId == studentId,
            ct
        );

        if (review == null)
        {
            throw new KeyNotFoundException("Your review was not found.");
        }

        review.Rating = request.Rating;
        review.Comment = request.Comment.Trim();
        review.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return await GetReviewAsync(reviewId, ct);
    }

    public async Task DeleteAsync(
        int studentId,
        int reviewId,
        CancellationToken ct)
    {
        var review = await db.CourseReviews.FirstOrDefaultAsync(
            x => x.CourseReviewId == reviewId &&
                 x.StudentId == studentId,
            ct
        );

        if (review == null)
        {
            throw new KeyNotFoundException("Your review was not found.");
        }

        db.CourseReviews.Remove(review);

        await db.SaveChangesAsync(ct);
    }

    private async Task<CourseReviewResponse> GetReviewAsync(
        int reviewId,
        CancellationToken ct)
    {
        return await db.CourseReviews
            .AsNoTracking()
            .Where(x => x.CourseReviewId == reviewId)
            .Select(x => new CourseReviewResponse(
                x.CourseReviewId,
                x.CourseId,
                x.Course.CourseName,
                x.StudentId,
                x.Student.Name,
                x.Rating,
                x.Comment,
                x.CreatedAt,
                x.UpdatedAt
            ))
            .FirstAsync(ct);
    }
}
