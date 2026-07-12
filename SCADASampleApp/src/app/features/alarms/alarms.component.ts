import { CommonModule, isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit, PLATFORM_ID, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { environment } from '../../../environments/environment';
import { AuthService } from '../../core/auth.service';
import { Alarm } from '../../models/api.models';

@Component({
  selector: 'app-alarms',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './alarms.component.html',
  styleUrl: './alarms.component.css',
})
export class AlarmsComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly platformId = inject(PLATFORM_ID);
  readonly auth = inject(AuthService);

  readonly alarms = signal<Alarm[]>([]);
  pipelineFilter: number | null = null;

  ngOnInit(): void {
    if (isPlatformBrowser(this.platformId)) {
      this.load();
    }
  }

  load(): void {
    const q =
      this.pipelineFilter != null && !Number.isNaN(this.pipelineFilter)
        ? `?pipelineId=${this.pipelineFilter}`
        : '';
    this.http.get<Alarm[]>(`${environment.apiUrl}/api/alarms${q}`).subscribe((a) => this.alarms.set(a));
  }

  acknowledge(id: number): void {
    this.http.post(`${environment.apiUrl}/api/alarms/${id}/acknowledge`, {}).subscribe({
      next: () => this.load(),
    });
  }
}
