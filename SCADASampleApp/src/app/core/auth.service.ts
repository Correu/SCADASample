import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { inject, Injectable, PLATFORM_ID, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { LoginResponse } from '../models/api.models';

const StorageKey = 'scada_auth';

interface StoredAuth {
  token: string;
  email: string;
  roles: string[];
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly platformId = inject(PLATFORM_ID);

  readonly token = signal<string | null>(null);
  readonly email = signal<string | null>(null);
  readonly roles = signal<string[]>([]);

  constructor() {
    if (isPlatformBrowser(this.platformId)) {
      const raw = sessionStorage.getItem(StorageKey);
      if (raw) {
        try {
          const s: StoredAuth = JSON.parse(raw);
          this.token.set(s.token);
          this.email.set(s.email);
          this.roles.set(s.roles ?? []);
        } catch {
          sessionStorage.removeItem(StorageKey);
        }
      }
    }
  }

  login(userEmail: string, password: string): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${environment.apiUrl}/api/auth/login`, {
        email: userEmail,
        password,
      })
      .pipe(
        tap((res) => {
          this.persist(res.token, res.email, res.roles);
        }),
      );
  }

  logout(): void {
    this.token.set(null);
    this.email.set(null);
    this.roles.set([]);
    if (isPlatformBrowser(this.platformId)) {
      sessionStorage.removeItem(StorageKey);
    }
    void this.router.navigate(['/login']);
  }

  hasRole(role: string): boolean {
    return this.roles().some((r) => r.toLowerCase() === role.toLowerCase());
  }

  canAcknowledgeAlarms(): boolean {
    return this.hasRole('Admin') || this.hasRole('Operator');
  }

  private persist(token: string, userEmail: string, roles: string[]): void {
    this.token.set(token);
    this.email.set(userEmail);
    this.roles.set(roles);
    if (isPlatformBrowser(this.platformId)) {
      const payload: StoredAuth = { token, email: userEmail, roles };
      sessionStorage.setItem(StorageKey, JSON.stringify(payload));
    }
  }
}
