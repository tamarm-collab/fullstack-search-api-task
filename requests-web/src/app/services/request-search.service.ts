import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { SearchRequestQuery, PagedResult, RequestDto } from '../models';

/**
 * Service for searching and filtering requests via the backend API.
 * Uses HttpClient with inject() function following Angular 18+ patterns.
 */
@Injectable({ providedIn: 'root' })
export class RequestSearchService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = '/api/requests/search';

  /**
   * Search requests with filtering, sorting, and pagination.
   * @param query The search query parameters
   * @returns Observable of paged results containing request DTOs
   */
  search(query: SearchRequestQuery): Observable<PagedResult<RequestDto>> {
    const params = this.buildHttpParams(query);
    return this.http.get<PagedResult<RequestDto>>(this.apiUrl, { params });
  }

  /**
   * Builds HttpParams from the search query object.
   * Handles multiple statuses as repeated query parameters.
   * @param query The search query to convert to HTTP params
   * @returns HttpParams object for the HTTP request
   */
  private buildHttpParams(query: SearchRequestQuery): HttpParams {
    let params = new HttpParams()
      .set('pageNumber', query.pageNumber.toString())
      .set('pageSize', query.pageSize.toString());

    if (query.requestNumber) {
      params = params.set('requestNumber', query.requestNumber);
    }

    // Handle multiple statuses as repeated query params (e.g., statuses=1&statuses=2)
    if (query.statuses?.length) {
      query.statuses.forEach(status => {
        params = params.append('statuses', status.toString());
      });
    }

    if (query.requestType !== undefined && query.requestType !== null) {
      params = params.set('requestType', query.requestType.toString());
    }

    if (query.dateFrom) {
      params = params.set('dateFrom', query.dateFrom);
    }

    if (query.dateTo) {
      params = params.set('dateTo', query.dateTo);
    }

    if (query.sortBy) {
      params = params.set('sortBy', query.sortBy);
    }

    if (query.sortDirection) {
      params = params.set('sortDirection', query.sortDirection);
    }

    return params;
  }
}
