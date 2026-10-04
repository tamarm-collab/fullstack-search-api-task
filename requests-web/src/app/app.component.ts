import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject, takeUntil } from 'rxjs';
import { SearchRequestsComponent } from './components/search-requests/search-requests.component';
import { AuthService, PERSONAS, Persona } from './core';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, SearchRequestsComponent],
  template: `
    <div class="app-container">
      <header class="app-header">
        <div class="header-content">
          <h1>מערכת ניהול בקשות</h1>
          
          <!-- Persona Switcher -->
          <div class="persona-switcher">
            <div class="persona-select-wrapper">
              <span class="user-icon">👤</span>
              <select 
                id="persona-select"
                [value]="currentPersonaId"
                (change)="onPersonaChange($event)"
                class="persona-select"
                aria-label="בחירת פרסונה"
              >
                @for (persona of personas; track persona.id) {
                  <option [value]="persona.id" [selected]="persona.id === currentPersonaId">
                    {{ persona.label }}
                  </option>
                }
              </select>
            </div>
            <span class="user-badge" [class.admin]="isAdmin">
              {{ isAdmin ? '🔓' : '🔒' }} #{{ userId }}
            </span>
          </div>
        </div>
      </header>
      <main class="app-main">
        <app-search-requests></app-search-requests>
      </main>
    </div>
  `,
  styles: [`
    :host {
      display: block;
    }
    .app-container {
      min-height: 100vh;
      background: #f0f2f5;
      display: flex;
      flex-direction: column;
    }
    .app-header {
      background: linear-gradient(135deg, #0066cc 0%, #004499 100%);
      color: white;
      padding: 16px 20px;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
    }
    .header-content {
      max-width: 1200px;
      margin: 0 auto;
      display: flex;
      justify-content: space-between;
      align-items: center;
    }
    .app-header h1 {
      margin: 0;
      font-size: 20px;
      font-weight: 500;
    }
    
    /* Persona Switcher Styles */
    .persona-switcher {
      display: flex;
      align-items: center;
      gap: 12px;
    }
    .persona-select-wrapper {
      position: relative;
      display: flex;
      align-items: center;
    }
    .user-icon {
      position: absolute;
      right: 10px;
      font-size: 14px;
      pointer-events: none;
      z-index: 1;
    }
    .persona-select {
      background: rgba(255, 255, 255, 0.95);
      color: #333;
      border: none;
      border-radius: 6px;
      padding: 8px 36px 8px 12px;
      font-size: 13px;
      cursor: pointer;
      min-width: 180px;
      direction: rtl;
      appearance: none;
      -webkit-appearance: none;
      -moz-appearance: none;
    }
    .persona-select:focus {
      outline: 2px solid rgba(255, 255, 255, 0.5);
      outline-offset: 2px;
    }
    .persona-select option {
      direction: rtl;
    }
    
    .user-badge {
      background: rgba(255, 255, 255, 0.2);
      padding: 6px 12px;
      border-radius: 16px;
      font-size: 13px;
      font-weight: 500;
    }
    .user-badge.admin {
      background: rgba(40, 167, 69, 0.3);
    }
    .app-main {
      padding: 20px;
      flex: 1;
    }

    /* Responsive */
    @media (max-width: 768px) {
      .header-content {
        flex-direction: column;
        gap: 12px;
        text-align: center;
      }
      .persona-switcher {
        flex-wrap: wrap;
        justify-content: center;
      }
    }
  `]
})
export class AppComponent implements OnInit, OnDestroy {
  private readonly authService = inject(AuthService);
  private readonly destroy$ = new Subject<void>();

  title = 'requests-web';
  userId = 1;
  isAdmin = false;
  currentPersonaId: string | null = null;
  
  readonly personas: Persona[] = PERSONAS;

  ngOnInit(): void {
    // Subscribe to auth context changes
    this.authService.context$
      .pipe(takeUntil(this.destroy$))
      .subscribe(context => {
        this.userId = context.userId;
        this.isAdmin = context.isAdmin;
        this.currentPersonaId = this.authService.getCurrentPersonaId();
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onPersonaChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    const personaId = select.value;
    const persona = this.personas.find(p => p.id === personaId);
    
    if (persona) {
      this.authService.switchPersona(persona);
      // Reload the page to ensure all components get the new context
      window.location.reload();
    }
  }
}
