import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { environment } from '../../../environments/environment';
import { UserListItem } from '../../models/api.models';

const Roles = ['Admin', 'Operator', 'Viewer'] as const;

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-users.component.html',
  styleUrl: './admin-users.component.css',
})
export class AdminUsersComponent implements OnInit {
  private readonly http = inject(HttpClient);

  readonly users = signal<UserListItem[]>([]);
  readonly error = signal<string | null>(null);

  newEmail = '';
  newPassword = '';
  newRole: string = 'Viewer';

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.error.set(null);
    this.http.get<UserListItem[]>(`${environment.apiUrl}/api/admin/users`).subscribe({
      next: (u) => this.users.set(u),
      error: (e) => this.error.set(this.formatError(e)),
    });
  }

  create(): void {
    this.error.set(null);
    this.http
      .post<UserListItem>(`${environment.apiUrl}/api/admin/users`, {
        email: this.newEmail,
        password: this.newPassword,
        role: this.newRole,
      })
      .subscribe({
        next: () => {
          this.newEmail = '';
          this.newPassword = '';
          this.newRole = 'Viewer';
          this.load();
        },
        error: (e) => this.error.set(this.formatError(e)),
      });
  }

  deleteUser(id: string): void {
    this.error.set(null);
    this.http.delete(`${environment.apiUrl}/api/admin/users/${id}`).subscribe({
      next: () => this.load(),
      error: (e) => this.error.set(this.formatError(e)),
    });
  }

  setRoles(user: UserListItem, role: string, checked: boolean): void {
    const next = new Set(user.roles);
    if (checked) {
      next.add(role);
    } else {
      next.delete(role);
    }
    const body = { roles: Array.from(next) };
    this.http.put(`${environment.apiUrl}/api/admin/users/${user.id}/roles`, body).subscribe({
      next: () => this.load(),
      error: (e) => this.error.set(this.formatError(e)),
    });
  }

  hasRole(user: UserListItem, role: string): boolean {
    return user.roles.some((r) => r.toLowerCase() === role.toLowerCase());
  }

  rolesList(): readonly string[] {
    return Roles;
  }

  private formatError(e: unknown): string {
    if (e && typeof e === 'object' && 'error' in e) {
      const err = (e as { error: unknown }).error;
      if (typeof err === 'string') {
        return err;
      }
      if (Array.isArray(err)) {
        return err.join('; ');
      }
    }
    return 'Request failed.';
  }
}
