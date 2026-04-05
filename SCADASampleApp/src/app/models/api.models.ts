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
