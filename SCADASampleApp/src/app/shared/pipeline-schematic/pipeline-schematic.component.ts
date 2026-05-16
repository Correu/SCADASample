import { CommonModule } from '@angular/common';
import { Component, computed, input } from '@angular/core';
import { ProcessGraph, ProcessLocation, ProcessTransfer } from '../../models/api.models';

const FLUID_COLORS: Record<string, string> = {
  DIESEL: '#ca8a04',
  OIL: '#b45309',
  WATER: '#0ea5e9',
  WTR: '#0ea5e9',
  MIX: '#64748b',
};

let markerSeq = 0;

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
    if (!g?.locations.length) {
      return '0 0 640 280';
    }
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

  locationById(id: number): ProcessLocation | undefined {
    return this.graph()?.locations.find((l) => l.processLocationId === id);
  }

  fillPercent(loc: ProcessLocation): number {
    if (loc.capacity <= 0) return 0;
    return Math.min(100, Math.max(0, (loc.currentVolume / loc.capacity) * 100));
  }

  midPoint(from: ProcessLocation, to: ProcessLocation): { x: number; y: number } {
    return {
      x: (from.layoutX + to.layoutX) / 2,
      y: (from.layoutY + to.layoutY) / 2,
    };
  }

  fluidColor(code: string): string {
    return FLUID_COLORS[code.toUpperCase()] ?? '#64748b';
  }

  edgeClass(t: ProcessTransfer): Record<string, boolean> {
    const flowing = t.isPumpRunning && t.valveOpen && t.currentFlowRate > 0;
    return {
      edge: true,
      'edge-on': flowing,
      'edge-flow': flowing,
    };
  }

  topFluids(loc: ProcessLocation, max = 3): ProcessLocation['fluids'] {
    return [...(loc.fluids ?? [])]
      .sort((a, b) => b.volume - a.volume)
      .slice(0, max);
  }
}
