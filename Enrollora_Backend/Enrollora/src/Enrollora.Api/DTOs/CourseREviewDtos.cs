
using System.ComponentModel.DataAnnotations;

namespace Enrollora.Api.DTOs;

public sealed class SaveCourseReviewRequest
{
    [Range(1, 5)]
    public int Rating { get; set; }

    [Required]
    [MaxLength(1000)]
    public string Comment { get; set; } = string.Empty;
}

public sealed record CourseReviewResponse(
    int CourseReviewId,
    int CourseId,
    string CourseName,
    int StudentId,
    string StudentName,
    int Rating,
    string Comment,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public sealed record CourseReviewSummaryResponse(
    int CourseId,
    string CourseName,
    double AverageRating,
    int TotalReviews,
    IReadOnlyList<CourseReviewResponse> Reviews
);
