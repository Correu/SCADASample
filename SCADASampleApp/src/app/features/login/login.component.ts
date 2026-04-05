import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css',
})
export class LoginComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly adminHint = 'admin@example.com';

  email = '';
  password = '';
  readonly error = signal<string | null>(null);
  readonly busy = signal(false);

  submit(): void {
    this.error.set(null);
    this.busy.set(true);
    this.auth.login(this.email, this.password).subscribe({
      next: () => void this.router.navigate(['/']),
      error: () => {
        this.error.set('Invalid email or password.');
        this.busy.set(false);
      },
    });
  }
}
