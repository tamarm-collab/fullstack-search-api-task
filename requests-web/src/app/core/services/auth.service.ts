import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

/**
 * User context for authentication.
 */
export interface UserContext {
  userId: number;
  isAdmin: boolean;
}

/**
 * Predefined personas for testing different user scenarios.
 */
export interface Persona {
  id: string;
  label: string;
  userId: number;
  isAdmin: boolean;
}

/**
 * Available test personas.
 */
export const PERSONAS: Persona[] = [
  { id: 'user1', label: 'משתמש 1 (רגיל)', userId: 1, isAdmin: false },
  { id: 'user5', label: 'משתמש 5 (רגיל)', userId: 5, isAdmin: false },
  { id: 'admin', label: 'מנהל (Admin)', userId: 1, isAdmin: true },
];

/**
 * Default user context values.
 * In production, these would come from an Identity Provider (OAuth/JWT).
 */
const DEFAULT_USER_ID = 1;
const DEFAULT_IS_ADMIN = false;

/**
 * Service for managing user authentication context.
 * 
 * For demo/testing purposes, user context can be set via:
 * 1. URL query parameters: ?userId=3&isAdmin=true
 * 2. Persona Switcher in the UI header
 * 
 * NOTE: In production, these values would come from a JWT token validated
 * by an API Gateway or Auth Middleware - NOT from client-controlled parameters.
 * This implementation is for demonstration purposes only.
 */
@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private userId: number = DEFAULT_USER_ID;
  private isAdmin: boolean = DEFAULT_IS_ADMIN;
  private initialized = false;

  private readonly contextSubject = new BehaviorSubject<UserContext>({
    userId: DEFAULT_USER_ID,
    isAdmin: DEFAULT_IS_ADMIN
  });

  /** Observable for reactive context updates */
  readonly context$: Observable<UserContext> = this.contextSubject.asObservable();

  constructor() {
    this.initializeFromUrl();
  }

  /**
   * Initialize auth context from URL query parameters.
   * Called automatically on service creation.
   */
  private initializeFromUrl(): void {
    if (this.initialized) {
      return;
    }

    const urlParams = new URLSearchParams(window.location.search);

    // Parse userId from query params
    const userIdParam = urlParams.get('userId');
    if (userIdParam) {
      const parsed = parseInt(userIdParam, 10);
      if (!isNaN(parsed) && parsed >= 1 && parsed <= 100) {
        this.userId = parsed;
      }
    }

    // Parse isAdmin from query params
    const isAdminParam = urlParams.get('isAdmin');
    if (isAdminParam) {
      this.isAdmin = isAdminParam.toLowerCase() === 'true';
    }

    this.initialized = true;
    this.emitContext();
    
    console.log(`[AuthService] User ID: ${this.userId}, Is Admin: ${this.isAdmin}`);
  }

  /**
   * Switch to a predefined persona.
   * Updates URL params and refreshes context.
   */
  switchPersona(persona: Persona): void {
    this.userId = persona.userId;
    this.isAdmin = persona.isAdmin;
    this.updateUrlParams();
    this.emitContext();
    console.log(`[AuthService] Switched to: ${persona.label}`);
  }

  /**
   * Get the currently active persona ID (if matching a predefined persona).
   */
  getCurrentPersonaId(): string | null {
    const match = PERSONAS.find(
      p => p.userId === this.userId && p.isAdmin === this.isAdmin
    );
    return match?.id ?? null;
  }

  /**
   * Update URL query parameters to reflect current context.
   * Enables sharing URLs with specific persona settings.
   */
  private updateUrlParams(): void {
    const url = new URL(window.location.href);
    
    if (this.userId !== DEFAULT_USER_ID) {
      url.searchParams.set('userId', String(this.userId));
    } else {
      url.searchParams.delete('userId');
    }
    
    if (this.isAdmin) {
      url.searchParams.set('isAdmin', 'true');
    } else {
      url.searchParams.delete('isAdmin');
    }

    window.history.replaceState({}, '', url.toString());
  }

  private emitContext(): void {
    this.contextSubject.next({
      userId: this.userId,
      isAdmin: this.isAdmin
    });
  }

  /**
   * Get current user context for HTTP headers.
   */
  getCurrentContext(): UserContext {
    return {
      userId: this.userId,
      isAdmin: this.isAdmin
    };
  }

  /**
   * Get current user ID.
   */
  getUserId(): number {
    return this.userId;
  }

  /**
   * Check if current user is admin.
   */
  getIsAdmin(): boolean {
    return this.isAdmin;
  }
}
