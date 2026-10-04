import { RequestStatus } from './request';

// קלט עדכון Bulk (BulkUpdateStatusDTO)
export interface BulkUpdateStatus {
  requestIds: number[];
  newStatus: RequestStatus;
  changedBy?: string;
}

// תוצאת עדכון Bulk (BulkUpdateResultDTO)
export interface BulkUpdateResult {
  totalRequested: number;
  succeededCount: number;
  failedCount: number;
  succeeded: number[];
  failed: BulkFailure[];
}

export interface BulkFailure {
  requestId: number;
  reason: string;
}

// קלט עדכון סטטוס יחיד (UpdateStatusDTO)
export interface UpdateStatus {
  newStatus: RequestStatus;
  rowVersion: string;
  changedBy?: string;
}
