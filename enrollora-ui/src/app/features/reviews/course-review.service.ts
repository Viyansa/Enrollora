
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

import {
  CourseReviewResponse,
  CourseReviewSummaryResponse,
  SaveCourseReviewRequest
} from './course-review.models';

@Injectable({
  providedIn: 'root'
})
export class CourseReviewService {

  private http = inject(HttpClient);

  private apiUrl = `${environment.apiUrl}/reviews`;

  getForCourse(courseId: number): Observable<CourseReviewSummaryResponse> {
    return this.http.get<CourseReviewSummaryResponse>(
      `${this.apiUrl}/course/${courseId}`
    );
  }

  getMineForCourse(courseId: number): Observable<CourseReviewResponse | null> {
    return this.http.get<CourseReviewResponse | null>(
      `${this.apiUrl}/my/course/${courseId}`
    );
  }

  getAll(): Observable<CourseReviewResponse[]> {
    return this.http.get<CourseReviewResponse[]>(
      `${this.apiUrl}/admin`
    );
  }

  create(courseId: number, request: SaveCourseReviewRequest): Observable<CourseReviewResponse> {
    return this.http.post<CourseReviewResponse>(
      `${this.apiUrl}/course/${courseId}`,
      request
    );
  }

  update(reviewId: number, request: SaveCourseReviewRequest): Observable<CourseReviewResponse> {
    return this.http.put<CourseReviewResponse>(
      `${this.apiUrl}/${reviewId}`,
      request
    );
  }

  delete(reviewId: number): Observable<void> {
    return this.http.delete<void>(
      `${this.apiUrl}/${reviewId}`
    );
  }
}
