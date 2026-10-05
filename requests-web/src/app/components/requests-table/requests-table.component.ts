import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';

import { PagedResult, RequestDto, RequestStatus, RequestType } from '../../models';

/**
 * Status label map - Hebrew labels for request statuses.
 */
const STATUS_LABELS: Record<RequestStatus, string> = {
  [RequestStatus.New]: 'חדש',
  [RequestStatus.InProgress]: 'בטיפול',
  [RequestStatus.Completed]: 'הושלם',
  [RequestStatus.Cancelled]: 'בוטל'
};

/**
 * Request type label map - Hebrew labels for request types.
 */
const TYPE_LABELS: Record<RequestType, string> = {
  [RequestType.General]: 'כללי',
  [RequestType.Legal]: 'משפטי',
  [RequestType.Payment]: 'תשלום',
  [RequestType.Appeal]: 'ערעור'
};

/**
 * Standalone table component for displaying request search results.
 * Features RTL layout, pagination, and Hebrew labels.
 */
@Component({
  selector: 'app-requests-table',
  standalone: true,
  imports: [CommonModule, DatePipe],
  templateUrl: './requests-table.component.html',
  styleUrls: ['./requests-table.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RequestsTableComponent {
  /** Paged results to display in the table */
  readonly results = input<PagedResult<RequestDto> | null>(null);

  /** Emits when page changes */
  readonly pageChange = output<number>();

  /**
   * Get Hebrew label for a status value.
   * @param status The status enum value
   * @returns The Hebrew label
   */
  getStatusLabel(status: RequestStatus): string {
    return STATUS_LABELS[status] ?? String(status);
  }

  /**
   * Get Hebrew label for a request type value.
   * @param type The request type enum value
   * @returns The Hebrew label
   */
  getTypeLabel(type: RequestType): string {
    return TYPE_LABELS[type] ?? String(type);
  }

  /**
   * Handle page click and emit page change event.
   * @param pageNumber The page number to navigate to
   */
  onPageClick(pageNumber: number): void {
    this.pageChange.emit(pageNumber);
  }
}
