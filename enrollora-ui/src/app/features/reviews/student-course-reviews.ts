
import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { EnrollmentService } from '../enrollments/enrollment.service';
import { EnrollmentResponse } from '../enrollments/enrollment.models';
import { PagedResult } from '../../shared/models/paged-result.model';

import { CourseReviewService } from './course-review.service';

import {
  CourseReviewResponse,
  CourseReviewSummaryResponse
} from './course-review.models';

@Component({
  selector: 'app-student-course-reviews',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './student-course-reviews.html',
  styleUrl: './student-course-reviews.css'
})
export class StudentCourseReviews implements OnInit {

  private readonly enrollmentsApi = inject(EnrollmentService);
  private readonly reviewsApi = inject(CourseReviewService);

  enrollments = signal<EnrollmentResponse[]>([]);

  summary = signal<CourseReviewSummaryResponse | null>(null);

  mine = signal<CourseReviewResponse | null>(null);

  selectedCourseId = 0;

  rating = 5;

  comment = '';

  loading = signal(false);
  saving = signal(false);

  error = signal('');
  message = signal('');

  ngOnInit(): void {
    this.loadEnrollments();
  }

  loadEnrollments(): void {
    this.enrollmentsApi.getMyEnrollments(1, 100).subscribe({

      next: (result: PagedResult<EnrollmentResponse>) => {
        this.enrollments.set(
          result.items.filter((x: EnrollmentResponse) => x.status === 0)
        );
      },

      error: (err: HttpErrorResponse) => {
        this.error.set(
          this.errorText(err, 'Unable to load enrollments.')
        );
      }

    });
  }

  selectCourse(): void {
    this.error.set('');
    this.message.set('');

    this.rating = 5;
    this.comment = '';

    this.loadCourseData();
  }

  private loadCourseData(): void {
    this.summary.set(null);
    this.mine.set(null);

    if (!this.selectedCourseId) {
      return;
    }

    this.loading.set(true);

    this.reviewsApi
      .getForCourse(this.selectedCourseId)
      .subscribe({

        next: (result: CourseReviewSummaryResponse) => {
          this.summary.set(result);
          this.loading.set(false);
        },

        error: (err: HttpErrorResponse) => {
          this.error.set(
            this.errorText(err, 'Unable to load course reviews.')
          );
          this.loading.set(false);
        }

      });

    this.reviewsApi
      .getMineForCourse(this.selectedCourseId)
      .subscribe({

        next: (review: CourseReviewResponse | null) => {
          this.mine.set(review);

          if (review) {
            this.rating = review.rating;
            this.comment = review.comment;
          }
        },

        error: (err: HttpErrorResponse) => {
          this.error.set(
            this.errorText(err, 'Unable to load your review.')
          );
        }

      });
  }

  saveReview(): void {
    if (!this.selectedCourseId) {
      this.error.set('Please select a course.');
      return;
    }

    if (this.rating < 1 || this.rating > 5) {
      this.error.set('Rating must be between 1 and 5.');
      return;
    }

    if (!this.comment.trim()) {
      this.error.set('Please enter your feedback.');
      return;
    }

    this.saving.set(true);
    this.error.set('');
    this.message.set('');

    const request = {
      rating: this.rating,
      comment: this.comment.trim()
    };

    const existingReview = this.mine();

    const action = existingReview
      ? this.reviewsApi.update(
          existingReview.courseReviewId,
          request
        )
      : this.reviewsApi.create(
          this.selectedCourseId,
          request
        );

    action.subscribe({

      next: (review: CourseReviewResponse) => {
        this.mine.set(review);

        this.message.set(
          existingReview
            ? 'Your review has been updated successfully.'
            : 'Your review has been submitted successfully.'
        );

        this.saving.set(false);

        this.refreshSummary();
      },

      error: (err: HttpErrorResponse) => {
        this.error.set(
          this.errorText(err, 'Unable to save your review.')
        );

        this.saving.set(false);
      }

    });
  }

  deleteReview(): void {
    const review = this.mine();

    if (!review) {
      return;
    }

    if (!confirm('Are you sure you want to delete your review?')) {
      return;
    }

    this.reviewsApi.delete(review.courseReviewId).subscribe({

      next: () => {
        this.mine.set(null);

        this.comment = '';
        this.rating = 5;

        this.message.set(
          'Your review has been deleted successfully.'
        );

        this.refreshSummary();
      },

      error: (err: HttpErrorResponse) => {
        this.error.set(
          this.errorText(err, 'Unable to delete your review.')
        );
      }

    });
  }

  private refreshSummary(): void {
    if (!this.selectedCourseId) {
      return;
    }

    this.reviewsApi
      .getForCourse(this.selectedCourseId)
      .subscribe({

        next: (result: CourseReviewSummaryResponse) => {
          this.summary.set(result);
        },

        error: (err: HttpErrorResponse) => {
          this.error.set(
            this.errorText(err, 'Unable to refresh reviews.')
          );
        }

      });
  }

  private errorText(
    error: HttpErrorResponse,
    fallback: string
  ): string {
    return error.error?.detail
      || error.error?.message
      || error.error?.title
      || fallback;
  }
}
