import { CommonModule, isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, OnDestroy, PLATFORM_ID, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { forkJoin, of, Subscription } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ProcessHubService } from '../../core/process-hub.service';
import { PipelineSummary, ProcessGraph } from '../../models/api.models';
import { PipelineSchematicComponent } from '../../shared/pipeline-schematic/pipeline-schematic.component';

export interface PipelineSchematicCard {
  pipeline: PipelineSummary;
  graph: ProcessGraph | null;
  loadError: boolean;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, PipelineSchematicComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css',
})
export class DashboardComponent implements OnDestroy {
  private readonly http = inject(HttpClient);
  private readonly hub = inject(ProcessHubService);
  private readonly platformId = inject(PLATFORM_ID);

  readonly cards = signal<PipelineSchematicCard[]>([]);
  readonly loading = signal(true);

  private subs: Subscription[] = [];

  constructor() {
    // Auth token lives in sessionStorage — only available in the browser.
    // SSR must not call authenticated APIs (hydration would reuse empty/401 results).
    if (isPlatformBrowser(this.platformId)) {
      this.loadDashboard();
      void this.hub.ensureConnected();
      this.subs.push(
        this.hub.locationUpdates.subscribe((u) => {
          this.cards.update((list) =>
            list.map((c) => {
              if (c.pipeline.pipelineId !== u.pipelineId || !c.graph) return c;
              return {
                ...c,
                graph: {
                  ...c.graph,
                  locations: c.graph.locations.map((l) =>
                    l.processLocationId === u.processLocationId
                      ? {
                          ...l,
                          currentVolume: u.currentVolume,
                          capacity: u.capacity,
                          lastUpdatedUtc: u.lastUpdatedUtc,
                          fluids: u.fluids ?? l.fluids,
                        }
                      : l,
                  ),
                },
              };
            }),
          );
        }),
      );
      this.subs.push(
        this.hub.transferUpdates.subscribe((u) => {
          this.cards.update((list) =>
            list.map((c) => {
              if (c.pipeline.pipelineId !== u.pipelineId || !c.graph) return c;
              return {
                ...c,
                graph: {
                  ...c.graph,
                  transfers: c.graph.transfers.map((t) =>
                    t.processTransferId === u.processTransferId
                      ? {
                          ...t,
                          currentFlowRate: u.currentFlowRate,
                          isPumpRunning: u.isPumpRunning,
                          valveOpen: u.valveOpen,
                          lastUpdatedUtc: u.lastUpdatedUtc,
                          fluids: u.fluids ?? t.fluids,
                        }
                      : t,
                  ),
                },
              };
            }),
          );
        }),
      );
    } else {
      this.loading.set(true);
    }
  }

  ngOnDestroy(): void {
    this.subs.forEach((s) => s.unsubscribe());
  }

  private loadDashboard(): void {
    this.loading.set(true);
    this.http.get<PipelineSummary[]>(`${environment.apiUrl}/api/pipelines`).subscribe({
      next: (pipelines) => this.loadGraphs(pipelines),
      error: () => {
        this.cards.set([]);
        this.loading.set(false);
      },
    });
  }

  private loadGraphs(pipelines: PipelineSummary[]): void {
    if (pipelines.length === 0) {
      this.cards.set([]);
      this.loading.set(false);
      return;
    }

    forkJoin(
      pipelines.map((p) =>
        this.http
          .get<ProcessGraph>(`${environment.apiUrl}/api/pipelines/${p.pipelineId}/process-graph`)
          .pipe(catchError(() => of(null))),
      ),
    ).subscribe({
      next: (graphs) => {
        this.cards.set(
          pipelines.map((p, i) => ({
            pipeline: p,
            graph: graphs[i] ?? null,
            loadError: graphs[i] == null,
          })),
        );
        this.loading.set(false);
      },
      error: () => {
        this.cards.set(
          pipelines.map((p) => ({
            pipeline: p,
            graph: null,
            loadError: true,
          })),
        );
        this.loading.set(false);
      },
    });
  }
}
