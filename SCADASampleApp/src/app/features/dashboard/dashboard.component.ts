import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ProcessHubService } from '../../core/process-hub.service';
import { PipelineSummary, TagSnapshot, TagValueUpdate } from '../../models/api.models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css',
})
export class DashboardComponent implements OnInit, OnDestroy {
  private readonly http = inject(HttpClient);
  private readonly hub = inject(ProcessHubService);

  readonly pipelines = signal<PipelineSummary[]>([]);
  readonly liveByTagId = signal<Record<number, TagValueUpdate>>({});
  readonly sampleTags = signal<TagSnapshot[]>([]);

  private sub?: Subscription;

  ngOnInit(): void {
    this.http.get<PipelineSummary[]>(`${environment.apiUrl}/api/pipelines`).subscribe((p) => {
      this.pipelines.set(p);
      const firstId = p[0]?.pipelineId;
      if (firstId != null) {
        this.http
          .get<TagSnapshot[]>(`${environment.apiUrl}/api/pipelines/${firstId}/tags`)
          .subscribe({
            next: (t) => this.sampleTags.set(t),
            error: () => this.sampleTags.set([]),
          });
      }
    });

    void this.hub.ensureConnected();
    this.sub = this.hub.tagUpdates.subscribe((u) => {
      this.liveByTagId.update((m) => ({ ...m, [u.tagId]: u }));
    });
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }

  liveValue(tagId: number): TagValueUpdate | undefined {
    return this.liveByTagId()[tagId];
  }
}
