import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

/**
 * HTTP interceptor that adds authentication headers from AuthService.
 * 
 * Adds the following headers to all outgoing requests:
 * - X-User-Id: The user ID from AuthService
 * - X-Is-Admin: Admin flag from AuthService
 * 
 * The AuthService allows users to toggle between different users and roles
 * for testing authorization behavior.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const context = authService.getCurrentContext();

  // Clone the request and add auth headers
  const authReq = req.clone({
    setHeaders: {
      'X-User-Id': context.userId.toString(),
      'X-Is-Admin': context.isAdmin.toString()
    }
  });

  return next(authReq);
};
