import { RequestStatus } from './request';

// רשומת היסטוריית שינוי סטטוס (StatusHistoryDTO)
export interface StatusHistory {
  id: number;
  requestId: number;
  previousStatus: RequestStatus | null;
  newStatus: RequestStatus;
  changedAt: string;
  changedBy: string;
}
