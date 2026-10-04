import { RequestStatus, RequestPriority } from './request';

// נתונים מסכמים (RequestStatsDTO)
export interface RequestStats {
  totalCount: number;
  countByStatus: StatusCount[];
  countByPriority: PriorityCount[];
}

export interface StatusCount {
  status: RequestStatus;
  count: number;
}

export interface PriorityCount {
  priority: RequestPriority;
  count: number;
}
