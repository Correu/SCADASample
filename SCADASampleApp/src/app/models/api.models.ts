export interface LoginResponse {
  token: string;
  email: string;
  roles: string[];
}

export interface PipelineSummary {
  pipelineId: number;
  name: string;
  code: string;
  isActive: boolean;
  tagCount: number;
}

export interface TagSnapshot {
  tagId: number;
  pipelineId: number;
  name: string;
  unit: string;
  minValue: number;
  maxValue: number;
  currentValue: number;
  lastUpdatedUtc: string;
}

export interface TagValueUpdate {
  pipelineId: number;
  tagId: number;
  tagName: string;
  value: number;
  unit: string;
  timestampUtc: string;
}

/** Managed fluid/product catalog entry. */
export interface Product {
  code: string;
  name: string;
  hexColor: string;
  productType: string;
  density: number;
}

/** Volume in m³; flow rates in m³/h. */
export interface ProcessLocationFluid {
  fluidCode: string;
  volume: number;
}

/** Per-fluid entry on a transfer leg. */
export interface TransferFluid {
  fluidCode: string;
  flowRateFraction: number;
  currentFlowRate: number;
}

/** Volume in m³; flow rates in m³/h. */
export interface ProcessLocation {
  processLocationId: number;
  pipelineId: number;
  stationId?: number | null;
  name: string;
  code: string;
  kind?: string | null;
  capacity: number;
  currentVolume: number;
  layoutX: number;
  layoutY: number;
  lastUpdatedUtc: string;
  fluids: ProcessLocationFluid[];
}

export interface ProcessTransfer {
  processTransferId: number;
  pipelineId: number;
  fromLocationId: number;
  toLocationId: number;
  isPumpRunning: boolean;
  valveOpen: boolean;
  maxFlowRate: number;
  currentFlowRate: number;
  lastUpdatedUtc: string;
  fluids: TransferFluid[];
}

export interface Station {
  stationId: number;
  pipelineId: number;
  name: string;
  code: string;
  layoutX: number;
  layoutY: number;
  locations: ProcessLocation[];
}

export interface ProcessGraph {
  locations: ProcessLocation[];
  transfers: ProcessTransfer[];
  stations: Station[];
}

export interface LocationUpdate {
  pipelineId: number;
  processLocationId: number;
  stationId?: number | null;
  currentVolume: number;
  capacity: number;
  lastUpdatedUtc: string;
  fluids: ProcessLocationFluid[];
}

export interface TransferUpdate {
  pipelineId: number;
  processTransferId: number;
  currentFlowRate: number;
  isPumpRunning: boolean;
  valveOpen: boolean;
  lastUpdatedUtc: string;
  fluids: TransferFluid[];
}

export interface Alarm {
  alarmId: number;
  pipelineId: number;
  tagId: number;
  alarmType: string;
  setPoint: number;
  severity: number;
  message: string;
  isEnabled: boolean;
  isActive: boolean;
  raisedAt?: string | null;
  acknowledgedAt?: string | null;
  acknowledgedByUserId?: string | null;
  tag?: { name: string };
  pipeline?: { name: string; code: string };
}

export interface UserListItem {
  id: string;
  email?: string | null;
  userName?: string | null;
  roles: string[];
}
