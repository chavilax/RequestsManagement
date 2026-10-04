// Enums - תואמים לערכים המספריים ב-Backend
export enum RequestStatus {
  New = 0,
  InProgress = 1,
  Waiting = 2,
  Completed = 3,
}

export enum RequestPriority {
  Low = 0,
  Medium = 1,
  High = 2,
}

// שורת פנייה ברשימה (RequestListItemDTO)
export interface RequestListItem {
  id: number;
  title: string;
  organizationName: string;
  status: RequestStatus;
  priority: RequestPriority;
  assignedTo: string | null;
  createdAt: string;
  updatedAt: string;
  rowVersion: string;
}

// פרמטרי סינון/חיפוש/מיון/דפדוף (RequestFilterDTO)
export interface RequestFilter {
  status?: RequestStatus | null;
  priority?: RequestPriority | null;
  organizationName?: string | null;
  assignedTo?: string | null;
  createdFrom?: string | null;
  createdTo?: string | null;
  search?: string | null;
  sortBy?: string | null;
  sortDescending?: boolean;
  page: number;
  pageSize: number;
}

// תוצאה מדופדפת (PagedResultDTO<T>)
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}
