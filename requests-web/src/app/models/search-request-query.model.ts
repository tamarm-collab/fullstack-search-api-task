import { RequestStatus } from './request-status.enum';
import { RequestType } from './request-type.enum';

export interface SearchRequestQuery {
  requestNumber?: string;
  statuses?: RequestStatus[];
  requestType?: RequestType;
  dateFrom?: string;  // ISO 8601 format
  dateTo?: string;    // ISO 8601 format
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
  pageNumber: number;
  pageSize: number;
}
