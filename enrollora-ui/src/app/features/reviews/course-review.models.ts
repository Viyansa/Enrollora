
export interface SaveCourseReviewRequest {
  rating: number;
  comment: string;
}

export interface CourseReviewResponse {
  courseReviewId: number;
  courseId: number;
  courseName: string;
  studentId: number;
  studentName: string;
  rating: number;
  comment: string;
  createdAt: string;
  updatedAt: string | null;
}

export interface CourseReviewSummaryResponse {
  courseId: number;
  courseName: string;
  averageRating: number;
  totalReviews: number;
  reviews: CourseReviewResponse[];
}
