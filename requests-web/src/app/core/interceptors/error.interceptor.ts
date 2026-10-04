import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';

/**
 * Interface for backend validation error response (ValidationProblemDetails).
 */
interface ValidationErrorResponse {
  type?: string;
  title?: string;
  status?: number;
  errors?: Record<string, string[]>;
}

/**
 * Interface for FluentValidation error response (array format).
 */
interface FluentValidationError {
  propertyName: string;
  errorMessage: string;
}

/**
 * Hebrew error messages for different HTTP status codes.
 */
const ERROR_MESSAGES = {
  CONNECTION_ERROR: 'שגיאת חיבור לשרת. אנא בדוק את החיבור לאינטרנט.',
  VALIDATION_ERROR_PREFIX: 'נתוני הבקשה אינם תקינים',
  UNAUTHORIZED: 'אין הרשאה לבצע פעולה זו',
  FORBIDDEN: 'אין הרשאה לגשת למשאב זה',
  NOT_FOUND: 'המשאב המבוקש לא נמצא',
  SERVER_ERROR: 'שגיאת שרת. אנא נסה שנית מאוחר יותר'
} as const;

/**
 * Map of English validation messages to Hebrew.
 */
const ERROR_TRANSLATIONS: Record<string, string> = {
  'DateFrom must be less than or equal to DateTo.': 'תאריך ההתחלה חייב להיות לפני או שווה לתאריך הסיום.',
  'Page number must be at least 1.': 'מספר העמוד חייב להיות לפחות 1.',
  'Page size must be between 1 and 100.': 'גודל העמוד חייב להיות בין 1 ל-100.',
  'Invalid status value provided.': 'ערך סטטוס לא תקין.',
  'Invalid request type value provided.': 'סוג בקשה לא תקין.'
};

/**
 * Translates an English error message to Hebrew if translation exists.
 */
function translateError(englishMessage: string): string {
  return ERROR_TRANSLATIONS[englishMessage] ?? englishMessage;
}

/**
 * Extracts validation errors from a 400 Bad Request response.
 * Handles both FluentValidation array format and ValidationProblemDetails format.
 * 
 * @param errorBody The error response body from the backend
 * @returns A formatted error message in Hebrew
 */
function extractValidationErrors(errorBody: unknown): string {
  // Handle FluentValidation array format: [{ propertyName, errorMessage }]
  if (Array.isArray(errorBody)) {
    const fluentErrors = errorBody as FluentValidationError[];
    if (fluentErrors.length > 0 && fluentErrors[0].errorMessage) {
      return translateError(fluentErrors[0].errorMessage);
    }
  }
  
  // Handle ValidationProblemDetails format: { errors: { field: ["message"] } }
  if (errorBody && typeof errorBody === 'object') {
    const validationResponse = errorBody as ValidationErrorResponse;
    
    if (validationResponse.errors && typeof validationResponse.errors === 'object') {
      const errorMessages: string[] = [];
      
      for (const [_field, messages] of Object.entries(validationResponse.errors)) {
        if (Array.isArray(messages)) {
          messages.forEach(msg => {
            errorMessages.push(translateError(msg));
          });
        }
      }

      if (errorMessages.length > 0) {
        return errorMessages.join(', ');
      }
    }
  }

  return ERROR_MESSAGES.VALIDATION_ERROR_PREFIX;
}

/**
 * Maps HTTP status code to appropriate Hebrew error message.
 * 
 * @param error The HTTP error response
 * @returns User-friendly error message in Hebrew
 */
function getErrorMessage(error: HttpErrorResponse): string {
  switch (error.status) {
    case 0:
      return ERROR_MESSAGES.CONNECTION_ERROR;
    
    case 400:
      return extractValidationErrors(error.error);
    
    case 401:
      return ERROR_MESSAGES.UNAUTHORIZED;
    
    case 403:
      return ERROR_MESSAGES.FORBIDDEN;
    
    case 404:
      return ERROR_MESSAGES.NOT_FOUND;
    
    default:
      if (error.status >= 500) {
        return ERROR_MESSAGES.SERVER_ERROR;
      }
      return error.message || ERROR_MESSAGES.SERVER_ERROR;
  }
}

/**
 * HTTP error interceptor for handling API errors.
 * 
 * Handles different HTTP error codes and provides Hebrew error messages:
 * - 0: Connection error
 * - 400: Bad Request with validation error extraction
 * - 401: Unauthorized
 * - 403: Forbidden
 * - 404: Not Found
 * - 500+: Server error
 * 
 * Logs errors to console for debugging and re-throws with enhanced error message.
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      // Log error to console for debugging
      console.error('HTTP Error:', {
        url: error.url,
        status: error.status,
        statusText: error.statusText,
        message: error.message,
        error: error.error
      });

      // Create enhanced error with Hebrew message
      const enhancedMessage = getErrorMessage(error);
      
      // Create a new error with the enhanced message
      const enhancedError = new HttpErrorResponse({
        error: {
          ...error.error,
          message: enhancedMessage,
          originalError: error.error
        },
        headers: error.headers,
        status: error.status,
        statusText: error.statusText,
        url: error.url ?? undefined
      });

      return throwError(() => enhancedError);
    })
  );
};
