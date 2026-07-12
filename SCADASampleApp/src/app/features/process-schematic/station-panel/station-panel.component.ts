import { CommonModule } from '@angular/common';
import { Component, input } from '@angular/core';
import { Station, ProcessLocation } from '../../../models/api.models';
import { FLUID_COLORS } from '../../../shared/pipeline-schematic/pipeline-schematic.component';

@Component({
  selector: 'app-station-panel',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './station-panel.component.html',
  styleUrl: './station-panel.component.css',
})
export class StationPanelComponent {
  readonly stations = input<Station[]>([]);
  readonly locations = input<ProcessLocation[]>([]);

  /** Returns the live location from the flat locations array, kept in sync by SignalR. */
  liveLocation(stationLoc: ProcessLocation): ProcessLocation {
    return (
      this.locations().find((l) => l.processLocationId === stationLoc.processLocationId) ?? stationLoc
    );
  }

  fillPercent(loc: ProcessLocation): number {
    if (loc.capacity <= 0) return 0;
    return Math.min(100, Math.max(0, (loc.currentVolume / loc.capacity) * 100));
  }

  fluidColor(code: string): string {
    return FLUID_COLORS[code.toUpperCase()] ?? '#94a3b8';
  }

  fluidFillWidth(loc: ProcessLocation, fluidVolume: number): number {
    const total = loc.currentVolume || 1;
    return Math.max(0, Math.min(100, (fluidVolume / total) * this.fillPercent(loc)));
  }

  sortedFluids(loc: ProcessLocation): ProcessLocation['fluids'] {
    return [...(loc.fluids ?? [])].sort((a, b) => b.volume - a.volume);
  }
}
