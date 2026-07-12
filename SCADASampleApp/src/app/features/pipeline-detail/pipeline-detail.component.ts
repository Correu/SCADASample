import { CommonModule, isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, OnDestroy, OnInit, PLATFORM_ID, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ProcessHubService } from '../../core/process-hub.service';
import { PipelineSummary, TagSnapshot, TagValueUpdate } from '../../models/api.models';

@Component({
  selector: 'app-pipeline-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './pipeline-detail.component.html',
  styleUrl: './pipeline-detail.component.css',
})
export class PipelineDetailComponent implements OnInit, OnDestroy {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  private readonly hub = inject(ProcessHubService);
  private readonly platformId = inject(PLATFORM_ID);

  readonly pipeline = signal<PipelineSummary | null>(null);
  readonly tags = signal<TagSnapshot[]>([]);
  readonly liveByTagId = signal<Record<number, TagValueUpdate>>({});

  private sub?: Subscription;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    const pipelineId = id ? Number(id) : NaN;
    if (Number.isNaN(pipelineId)) {
      return;
    }

    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    this.http
      .get<PipelineSummary>(`${environment.apiUrl}/api/pipelines/${pipelineId}`)
      .subscribe({
        next: (p) => this.pipeline.set(p),
        error: () => this.pipeline.set(null),
      });

    this.http
      .get<TagSnapshot[]>(`${environment.apiUrl}/api/pipelines/${pipelineId}/tags`)
      .subscribe((t) => this.tags.set(t));

    void this.hub.ensureConnected();
    this.sub = this.hub.tagUpdates.subscribe((u) => {
      if (u.pipelineId === pipelineId) {
        this.liveByTagId.update((m) => ({ ...m, [u.tagId]: u }));
      }
    });
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }

  liveValue(tagId: number): TagValueUpdate | undefined {
    return this.liveByTagId()[tagId];
  }
}
