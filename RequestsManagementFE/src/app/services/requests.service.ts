import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { RequestListItem, RequestFilter, PagedResult } from '../models/request';
import { RequestStats } from '../models/stats';
import { StatusHistory } from '../models/status-history';
import { UpdateStatus, BulkUpdateStatus, BulkUpdateResult } from '../models/bulk-update';

@Injectable({ providedIn: 'root' })
export class RequestsService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}requests`;

  /** שליפת פניות עם סינון, חיפוש, מיון ודפדוף - הכל בצד השרת. */
  getRequests(filter: RequestFilter): Observable<PagedResult<RequestListItem>> {
    let params = new HttpParams()
      .set('page', filter.page)
      .set('pageSize', filter.pageSize);

    if (filter.status !== null && filter.status !== undefined) {
      params = params.set('status', filter.status);
    }
    if (filter.priority !== null && filter.priority !== undefined) {
      params = params.set('priority', filter.priority);
    }
    if (filter.organizationName) {
      params = params.set('organizationName', filter.organizationName);
    }
    if (filter.assignedTo) {
      params = params.set('assignedTo', filter.assignedTo);
    }
    if (filter.createdFrom) {
      params = params.set('createdFrom', filter.createdFrom);
    }
    if (filter.createdTo) {
      params = params.set('createdTo', filter.createdTo);
    }
    if (filter.search) {
      params = params.set('search', filter.search);
    }
    if (filter.sortBy) {
      params = params.set('sortBy', filter.sortBy);
      params = params.set('sortDescending', filter.sortDescending ?? false);
    }

    return this.http.get<PagedResult<RequestListItem>>(this.baseUrl, { params });
  }

  /** נתונים מסכמים (Aggregations). */
  getStats(): Observable<RequestStats> {
    return this.http.get<RequestStats>(`${this.baseUrl}/stats`);
  }

  /** היסטוריית שינויי הסטטוס של פנייה. */
  getHistory(id: number): Observable<StatusHistory[]> {
    return this.http.get<StatusHistory[]>(`${this.baseUrl}/${id}/history`);
  }

  /** עדכון סטטוס של פנייה בודדת (עם RowVersion ל-Concurrency). */
  updateStatus(id: number, dto: UpdateStatus): Observable<RequestListItem> {
    return this.http.patch<RequestListItem>(`${this.baseUrl}/${id}/status`, dto);
  }

  /** עדכון סטטוס מרובה (Bulk). */
  bulkUpdateStatus(dto: BulkUpdateStatus): Observable<BulkUpdateResult> {
    return this.http.post<BulkUpdateResult>(`${this.baseUrl}/bulk-status`, dto);
  }
}
