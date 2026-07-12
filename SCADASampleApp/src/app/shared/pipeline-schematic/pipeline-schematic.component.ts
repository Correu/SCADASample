import { CommonModule } from '@angular/common';
import { Component, computed, input } from '@angular/core';
import { ProcessGraph, ProcessLocation, ProcessTransfer, Station, TransferFluid } from '../../models/api.models';

export const FLUID_COLORS: Record<string, string> = {
  DIESEL:   '#f59e0b',
  GASOLINE: '#eab308',
  NATGAS:   '#86efac',
  OIL:      '#92400e',
  WATER:    '#38bdf8',
  WTR:      '#7dd3fc',
  MIX:      '#94a3b8',
};

let markerSeq = 0;

export interface GradientStop {
  color: string;
  offset: number;
}

export interface TransferEdge {
  transfer: ProcessTransfer;
  from: ProcessLocation;
  to: ProcessLocation;
  gradientId: string;
  gradientStops: GradientStop[];
  flowing: boolean;
  isMultiFluid: boolean;
  labelParts: string[];
  totalFlowRate: number;
}

@Component({
  selector: 'app-pipeline-schematic',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './pipeline-schematic.component.html',
  styleUrl: './pipeline-schematic.component.css',
})
export class PipelineSchematicComponent {
  readonly graph = input<ProcessGraph | null>(null);
  readonly compact = input(false);
  readonly markerId = `arrowhead-${++markerSeq}`;

  readonly viewBox = computed(() => {
    const g = this.graph();
    if (!g?.locations.length) return '0 0 640 280';
    const xs = g.locations.map((l) => l.layoutX);
    const ys = g.locations.map((l) => l.layoutY);
    const padX = 90;
    const padY = 100;
    const minX = Math.min(...xs) - padX;
    const maxX = Math.max(...xs) + padX;
    const minY = Math.min(...ys) - padY;
    const maxY = Math.max(...ys) + padY;
    return `${minX} ${minY} ${maxX - minX} ${maxY - minY}`;
  });

  readonly edges = computed<TransferEdge[]>(() => {
    const g = this.graph();
    if (!g) return [];
    const edges: TransferEdge[] = [];
    let gIdx = 0;
    for (const t of g.transfers) {
      const from = g.locations.find((l) => l.processLocationId === t.fromLocationId);
      const to = g.locations.find((l) => l.processLocationId === t.toLocationId);
      if (!from || !to) continue;

      const flowing = t.isPumpRunning && t.valveOpen && t.currentFlowRate > 0;
      const fluids = t.fluids ?? [];
      const isMultiFluid = fluids.length > 1;

      const gradientId = `grad-${this.markerId}-${gIdx++}`;
      const gradientStops = this.buildGradientStops(fluids);

      const labelParts = fluids.map((f) => {
        const rate = Number.isFinite(f.currentFlowRate) ? f.currentFlowRate : 0;
        return `${f.fluidCode} ${rate.toFixed(1)}`;
      });

      edges.push({
        transfer: t,
        from,
        to,
        gradientId,
        gradientStops,
        flowing,
        isMultiFluid,
        labelParts,
        totalFlowRate: t.currentFlowRate,
      });
    }
    return edges;
  });

  /** Unique fluid codes present in any transfer on this graph, for the legend. */
  readonly uniqueFluids = computed<{ code: string; color: string }[]>(() => {
    const g = this.graph();
    if (!g) return [];
    const seen = new Set<string>();
    const result: { code: string; color: string }[] = [];
    for (const t of g.transfers) {
      for (const f of t.fluids ?? []) {
        if (!seen.has(f.fluidCode)) {
          seen.add(f.fluidCode);
          result.push({ code: f.fluidCode, color: this.fluidColor(f.fluidCode) });
        }
      }
    }
    return result;
  });

  readonly stationBounds = computed(() => {
    const g = this.graph();
    if (!g?.stations?.length) return [];
    return g.stations
      .map((s) => this.computeStationBounds(s, g.locations ?? []))
      .filter((b): b is NonNullable<typeof b> => b !== null);
  });

  locationById(id: number): ProcessLocation | undefined {
    return this.graph()?.locations.find((l) => l.processLocationId === id);
  }

  fillPercent(loc: ProcessLocation): number {
    if (loc.capacity <= 0) return 0;
    return Math.min(100, Math.max(0, (loc.currentVolume / loc.capacity) * 100));
  }

  midPoint(from: ProcessLocation, to: ProcessLocation): { x: number; y: number } {
    return { x: (from.layoutX + to.layoutX) / 2, y: (from.layoutY + to.layoutY) / 2 };
  }

  fluidColor(code: string): string {
    return FLUID_COLORS[code.toUpperCase()] ?? '#64748b';
  }

  dominantFluidColor(fluids: TransferFluid[]): string {
    if (!fluids?.length) return '#64748b';
    const top = [...fluids].sort((a, b) => b.flowRateFraction - a.flowRateFraction)[0];
    return this.fluidColor(top.fluidCode);
  }

  topFluids(loc: ProcessLocation, max = 3): ProcessLocation['fluids'] {
    return [...(loc.fluids ?? [])]
      .sort((a, b) => b.volume - a.volume)
      .slice(0, max);
  }

  /** Builds linear gradient stops for a multi-fluid pipe segment. */
  private buildGradientStops(fluids: TransferFluid[]): GradientStop[] {
    if (!fluids.length) return [{ color: '#64748b', offset: 100 }];
    if (fluids.length === 1) return [{ color: this.fluidColor(fluids[0].fluidCode), offset: 100 }];

    const total = fluids.reduce((s, f) => s + f.flowRateFraction, 0) || 1;
    const stops: GradientStop[] = [];
    let cursor = 0;
    for (const f of fluids) {
      const pct = (f.flowRateFraction / total) * 100;
      const color = this.fluidColor(f.fluidCode);
      if (cursor > 0) stops.push({ color, offset: Math.round(cursor) });
      stops.push({ color, offset: Math.round(cursor + pct) });
      cursor += pct;
    }
    return stops;
  }

  /** Computes a bounding rect for a station based on its member locations. */
  private computeStationBounds(
    station: Station,
    allLocations: ProcessLocation[]
  ): { station: Station; x: number; y: number; w: number; h: number } | null {
    const memberIds = new Set((station.locations ?? []).map((l) => l.processLocationId));
    const members = allLocations.filter((l) => memberIds.has(l.processLocationId));
    if (!members.length) return null;

    const pad = 55;
    const xs = members.map((l) => l.layoutX);
    const ys = members.map((l) => l.layoutY);
    const minX = Math.min(...xs) - pad;
    const minY = Math.min(...ys) - pad;
    const maxX = Math.max(...xs) + pad;
    const maxY = Math.max(...ys) + pad;
    return { station, x: minX, y: minY, w: maxX - minX, h: maxY - minY };
  }

  /** Gradient angle in SVG userSpaceOnUse based on the direction of the pipe. */
  gradientVector(from: ProcessLocation, to: ProcessLocation): { x1: string; y1: string; x2: string; y2: string } {
    return {
      x1: `${from.layoutX}`,
      y1: `${from.layoutY}`,
      x2: `${to.layoutX}`,
      y2: `${to.layoutY}`,
    };
  }
}
