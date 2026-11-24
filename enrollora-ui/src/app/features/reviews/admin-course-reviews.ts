
import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';

import { CourseReviewService } from './course-review.service';
import { CourseReviewResponse } from './course-review.models';

@Component({
  selector: 'app-admin-course-reviews',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './admin-course-reviews.html',
  styleUrl: './admin-course-reviews.css'
})
export class AdminCourseReviews implements OnInit {

  private readonly api = inject(CourseReviewService);

  reviews = signal<CourseReviewResponse[]>([]);
  loading = signal(false);
  error = signal('');

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');

    this.api.getAll().subscribe({
      next: (items: CourseReviewResponse[]) => {
        this.reviews.set(items);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.error.set(
          err.error?.detail ||
          err.error?.message ||
          err.error?.title ||
          'Unable to load reviews.'
        );

        this.loading.set(false);
      }
    });
  }

  get average(): number {
    const items = this.reviews();

    if (items.length === 0) {
      return 0;
    }

    const total = items.reduce(
      (sum: number, review: CourseReviewResponse) =>
        sum + review.rating,
      0
    );

    return total / items.length;
  }
}
