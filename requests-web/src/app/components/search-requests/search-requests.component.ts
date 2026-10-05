import { Component, ChangeDetectionStrategy, OnInit, inject, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup } from '@angular/forms';
import { BehaviorSubject } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { RequestSearchService } from '../../services/request-search.service';
import { 
  PagedResult, 
  RequestDto, 
  RequestStatus, 
  RequestType, 
  SearchRequestQuery 
} from '../../models';
import { RequestsTableComponent } from '../requests-table';

/**
 * Search and filter options for status dropdown.
 */
interface StatusOption {
  value: RequestStatus;
  label: string;
}

/**
 * Search and filter options for request type dropdown.
 */
interface TypeOption {
  value: RequestType;
  label: string;
}

/**
 * Search and filter options for sort field dropdown.
 */
interface SortFieldOption {
  value: string;
  label: string;
}

/**
 * Component for searching and filtering requests.
 * Provides a form with filter controls and displays search results.
 */
@Component({
  selector: 'app-search-requests',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RequestsTableComponent],
  templateUrl: './search-requests.component.html',
  styleUrls: ['./search-requests.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SearchRequestsComponent implements OnInit {
  private readonly searchService = inject(RequestSearchService);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  /** Reactive form for search filters */
  searchForm!: FormGroup;

  /** Results from the search API */
  readonly results$ = new BehaviorSubject<PagedResult<RequestDto> | null>(null);
  
  /** Loading state indicator */
  readonly loading$ = new BehaviorSubject<boolean>(false);
  
  /** Error message from failed searches */
  readonly error$ = new BehaviorSubject<string | null>(null);

  /** Status options with Hebrew labels */
  readonly statusOptions: StatusOption[] = [
    { value: RequestStatus.New, label: 'חדש' },
    { value: RequestStatus.InProgress, label: 'בטיפול' },
    { value: RequestStatus.Completed, label: 'הושלם' },
    { value: RequestStatus.Cancelled, label: 'בוטל' }
  ];

  /** Request type options with Hebrew labels */
  readonly typeOptions: TypeOption[] = [
    { value: RequestType.General, label: 'כללי' },
    { value: RequestType.Legal, label: 'משפטי' },
    { value: RequestType.Payment, label: 'תשלום' },
    { value: RequestType.Appeal, label: 'ערעור' }
  ];

  /** Sort field options */
  readonly sortFieldOptions: SortFieldOption[] = [
    { value: 'Id', label: 'מזהה' },
    { value: 'RequestNumber', label: 'מספר בקשה' },
    { value: 'Status', label: 'סטטוס' },
    { value: 'RequestType', label: 'סוג בקשה' },
    { value: 'CreatedAt', label: 'תאריך יצירה' },
    { value: 'CustomerId', label: 'מזהה לקוח' },
    { value: 'OwnerId', label: 'מזהה בעלים' }
  ];

  /** Sort direction options */
  readonly sortDirectionOptions = [
    { value: 'asc', label: 'עולה' },
    { value: 'desc', label: 'יורד' }
  ];

  ngOnInit(): void {
    this.initForm();
  }

  /**
   * Initialize the search form with default values.
   */
  private initForm(): void {
    this.searchForm = this.fb.group({
      requestNumber: [''],
      statuses: [[]],  // Multi-select - array of statuses
      requestType: [null],
      dateFrom: [''],
      dateTo: [''],
      sortBy: ['CreatedAt'],
      sortDirection: ['desc'],
      pageNumber: [1]
    });
  }

  /**
   * Execute search with current form values.
   * Resets page number to 1 when starting a new search.
   */
  onSearch(): void {
    if (this.loading$.value) {
      return;
    }

    // Reset to page 1 when performing a new search (user clicked search button)
    this.searchForm.patchValue({ pageNumber: 1 }, { emitEvent: false });

    this.executeSearch();
  }

  /**
   * Execute the search without resetting page number.
   * Used internally by onSearch() and onPageChange().
   */
  private executeSearch(): void {
    this.loading$.next(true);
    this.error$.next(null);

    const query = this.buildQueryFromForm();

    this.searchService.search(query)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.results$.next(result);
          this.loading$.next(false);
        },
        error: (err) => {
          this.error$.next(this.extractErrorMessage(err));
          this.results$.next(null); // Clear previous results on error
          this.loading$.next(false);
        }
      });
  }

  /**
   * Reset form to default values and clear results.
   */
  onClear(): void {
    this.searchForm.reset({
      requestNumber: '',
      statuses: [],  // Multi-select - empty array
      requestType: null,
      dateFrom: '',
      dateTo: '',
      sortBy: 'CreatedAt',
      sortDirection: 'desc',
      pageNumber: 1
    });
    this.results$.next(null);
    this.error$.next(null);
  }

  /**
   * Navigate to a specific page and execute search.
   * Does not reset page number (unlike onSearch which resets to page 1).
   * @param pageNumber The page number to navigate to
   */
  onPageChange(pageNumber: number): void {
    this.searchForm.patchValue({ pageNumber });
    this.executeSearch();
  }

  /**
   * Toggle a status in the multi-select array.
   * @param status The status to toggle
   * @param event The checkbox change event
   */
  onStatusToggle(status: RequestStatus, event: Event): void {
    const checkbox = event.target as HTMLInputElement;
    const currentStatuses: RequestStatus[] = this.searchForm.get('statuses')?.value || [];
    
    if (checkbox.checked) {
      // Add status if not already present
      if (!currentStatuses.includes(status)) {
        this.searchForm.patchValue({ statuses: [...currentStatuses, status] });
      }
    } else {
      // Remove status
      this.searchForm.patchValue({ 
        statuses: currentStatuses.filter(s => s !== status) 
      });
    }
  }

  /**
   * Check if a status is currently selected.
   * @param status The status to check
   * @returns True if the status is selected
   */
  isStatusSelected(status: RequestStatus): boolean {
    const currentStatuses: RequestStatus[] = this.searchForm.get('statuses')?.value || [];
    return currentStatuses.includes(status);
  }

  /**
   * Get Hebrew label for a status value.
   * @param status The status enum value
   * @returns The Hebrew label
   */
  getStatusLabel(status: RequestStatus): string {
    const option = this.statusOptions.find(o => o.value === status);
    return option?.label ?? String(status);
  }

  /**
   * Get Hebrew label for a request type value.
   * @param type The request type enum value
   * @returns The Hebrew label
   */
  getTypeLabel(type: RequestType): string {
    const option = this.typeOptions.find(o => o.value === type);
    return option?.label ?? String(type);
  }

  /**
   * Build search query from form values.
   * With [ngValue], status and requestType are already numbers (not strings).
   * @returns The search query object
   */
  private buildQueryFromForm(): SearchRequestQuery {
    const formValue = this.searchForm.value;

    // Default page size is 20 (as per requirements)
    const query: SearchRequestQuery = {
      pageNumber: formValue.pageNumber ?? 1,
      pageSize: 20
    };

    if (formValue.requestNumber?.trim()) {
      query.requestNumber = formValue.requestNumber.trim();
    }

    // Multi-status selection - pass array directly if not empty
    if (formValue.statuses?.length) {
      query.statuses = formValue.statuses;
    }

    if (formValue.requestType !== null) {
      query.requestType = formValue.requestType;
    }

    if (formValue.dateFrom) {
      query.dateFrom = formValue.dateFrom;
    }

    if (formValue.dateTo) {
      query.dateTo = formValue.dateTo;
    }

    if (formValue.sortBy) {
      query.sortBy = formValue.sortBy;
    }

    if (formValue.sortDirection) {
      query.sortDirection = formValue.sortDirection;
    }

    return query;
  }

  /**
   * Extract error message from HTTP error response.
   * Relies on errorInterceptor which already translates messages to Hebrew.
   * @param error The error object
   * @returns User-friendly error message in Hebrew
   */
  private extractErrorMessage(error: unknown): string {
    if (error && typeof error === 'object') {
      const err = error as { 
        error?: { message?: string }; 
        message?: string; 
        status?: number 
      };
      
      // The errorInterceptor already prepares Hebrew messages in error.error.message
      if (err.error?.message) {
        return err.error.message;
      }
      
      if (err.message) {
        return err.message;
      }
      
      if (err.status === 0) {
        return 'שגיאת חיבור לשרת. אנא בדוק את החיבור לאינטרנט.';
      }
    }
    
    return 'אירעה שגיאה בחיפוש. אנא נסה שנית.';
  }
}
