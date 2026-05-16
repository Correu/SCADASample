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

/** Volume in m³; flow rates in m³/h (see README). */
export interface ProcessLocationFluid {
  fluidCode: string;
  volume: number;
}

/** Volume in m³; flow rates in m³/h (see README). */
export interface ProcessLocation {
  processLocationId: number;
  pipelineId: number;
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
  fluidCode: string;
  outflowWeight: number;
  maxFlowRate: number;
  currentFlowRate: number;
  lastUpdatedUtc: string;
}

export interface ProcessGraph {
  locations: ProcessLocation[];
  transfers: ProcessTransfer[];
}

export interface LocationUpdate {
  pipelineId: number;
  processLocationId: number;
  currentVolume: number;
  capacity: number;
  lastUpdatedUtc: string;
  fluids?: ProcessLocationFluid[];
}

export interface TransferUpdate {
  pipelineId: number;
  processTransferId: number;
  fluidCode?: string;
  currentFlowRate: number;
  isPumpRunning: boolean;
  valveOpen: boolean;
  lastUpdatedUtc: string;
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
