import { CommonModule, isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, OnDestroy, PLATFORM_ID, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { AuthService } from '../../core/auth.service';
import { ProcessHubService } from '../../core/process-hub.service';
import { ProcessGraph, ProcessLocation, ProcessTransfer, Station } from '../../models/api.models';
import { PipelineSchematicComponent } from '../../shared/pipeline-schematic/pipeline-schematic.component';
import { StationPanelComponent } from './station-panel/station-panel.component';

@Component({
  selector: 'app-process-schematic',
  standalone: true,
  imports: [CommonModule, RouterLink, PipelineSchematicComponent, StationPanelComponent],
  templateUrl: './process-schematic.component.html',
  styleUrl: './process-schematic.component.css',
})
export class ProcessSchematicComponent implements OnDestroy {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  private readonly hub = inject(ProcessHubService);
  private readonly platformId = inject(PLATFORM_ID);
  readonly auth = inject(AuthService);

  readonly pipelineId = signal<number>(NaN);
  readonly pipelineName = signal<string>('');
  readonly pipelineCode = signal<string>('');
  readonly graph = signal<ProcessGraph | null>(null);
  readonly loadError = signal(false);
  readonly pumpBusy = signal<Record<number, boolean>>({});
  readonly valveBusy = signal<Record<number, boolean>>({});
  readonly activeTab = signal<'controls' | 'stations'>('controls');

  private subs: Subscription[] = [];

  constructor() {
    const id = this.route.snapshot.paramMap.get('id');
    const pid = id ? Number(id) : NaN;
    if (Number.isNaN(pid)) return;
    this.pipelineId.set(pid);

    // Auth token is browser-only; skip SSR fetches so hydration does not freeze empty graphs.
    if (!isPlatformBrowser(this.platformId)) return;

    this.http.get<{ name: string; code: string }>(`${environment.apiUrl}/api/pipelines/${pid}`).subscribe({
      next: (p) => { this.pipelineName.set(p.name); this.pipelineCode.set(p.code); },
      error: () => { this.pipelineName.set(''); this.pipelineCode.set(''); },
    });

    this.http.get<ProcessGraph>(`${environment.apiUrl}/api/pipelines/${pid}/process-graph`).subscribe({
      next: (g) => { this.graph.set(g); this.loadError.set(false); },
      error: () => { this.graph.set(null); this.loadError.set(true); },
    });

    void this.hub.ensureConnected();

    this.subs.push(
      this.hub.locationUpdates.subscribe((u) => {
        if (u.pipelineId !== pid) return;
        this.graph.update((g) => {
          if (!g) return g;
          const updatedLocations = g.locations.map((l) =>
            l.processLocationId === u.processLocationId
              ? { ...l, currentVolume: u.currentVolume, capacity: u.capacity, lastUpdatedUtc: u.lastUpdatedUtc, fluids: u.fluids ?? l.fluids }
              : l
          );
          const updatedStations = (g.stations ?? []).map((s) => ({
            ...s,
            locations: (s.locations ?? []).map((sl) =>
              sl.processLocationId === u.processLocationId
                ? { ...sl, currentVolume: u.currentVolume, capacity: u.capacity, lastUpdatedUtc: u.lastUpdatedUtc, fluids: u.fluids ?? sl.fluids }
                : sl
            ),
          }));
          return { ...g, locations: updatedLocations, stations: updatedStations };
        });
      }),
    );

    this.subs.push(
      this.hub.transferUpdates.subscribe((u) => {
        if (u.pipelineId !== pid) return;
        this.graph.update((g) => {
          if (!g) return g;
          return {
            ...g,
            transfers: g.transfers.map((t) =>
              t.processTransferId === u.processTransferId
                ? { ...t, currentFlowRate: u.currentFlowRate, isPumpRunning: u.isPumpRunning, valveOpen: u.valveOpen, lastUpdatedUtc: u.lastUpdatedUtc, fluids: u.fluids ?? t.fluids }
                : t
            ),
          };
        });
      }),
    );
  }

  ngOnDestroy(): void {
    this.subs.forEach((s) => s.unsubscribe());
  }

  locationById(id: number): ProcessLocation | undefined {
    return this.graph()?.locations.find((l) => l.processLocationId === id);
  }

  stations(): Station[] {
    return this.graph()?.stations ?? [];
  }

  locations(): ProcessLocation[] {
    return this.graph()?.locations ?? [];
  }

  setPumpRunning(t: ProcessTransfer, running: boolean): void {
    if (!this.auth.canOperateProcess()) return;
    const pid = this.pipelineId();
    this.pumpBusy.update((m) => ({ ...m, [t.processTransferId]: true }));
    this.http
      .post(`${environment.apiUrl}/api/pipelines/${pid}/transfers/${t.processTransferId}/pump`, { running })
      .pipe(finalize(() => this.pumpBusy.update((m) => { const { [t.processTransferId]: _, ...rest } = m; return rest; })))
      .subscribe({
        next: () => {
          this.graph.update((g) => {
            if (!g) return g;
            return { ...g, transfers: g.transfers.map((x) => x.processTransferId === t.processTransferId ? { ...x, isPumpRunning: running } : x) };
          });
        },
      });
  }

  setValveOpen(t: ProcessTransfer, open: boolean): void {
    if (!this.auth.canOperateProcess()) return;
    const pid = this.pipelineId();
    this.valveBusy.update((m) => ({ ...m, [t.processTransferId]: true }));
    this.http
      .post(`${environment.apiUrl}/api/pipelines/${pid}/transfers/${t.processTransferId}/valve`, { open })
      .pipe(finalize(() => this.valveBusy.update((m) => { const { [t.processTransferId]: _, ...rest } = m; return rest; })))
      .subscribe({
        next: () => {
          this.graph.update((g) => {
            if (!g) return g;
            return { ...g, transfers: g.transfers.map((x) => x.processTransferId === t.processTransferId ? { ...x, valveOpen: open } : x) };
          });
        },
      });
  }
}
