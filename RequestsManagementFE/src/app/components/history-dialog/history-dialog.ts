import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  MAT_DIALOG_DATA,
  MatDialogModule,
} from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { RequestsService } from '../../services/requests.service';
import { StatusHistory } from '../../models/status-history';
import { RequestStatus } from '../../models/request';

export interface HistoryDialogData {
  requestId: number;
}

@Component({
  selector: 'app-history-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './history-dialog.html',
  styleUrl: './history-dialog.scss',
})
export class HistoryDialog implements OnInit {
  private requestsService = inject(RequestsService);
  data = inject<HistoryDialogData>(MAT_DIALOG_DATA);

  history = signal<StatusHistory[]>([]);
  loading = signal(false);
  error = signal(false);

  private statusLabels: Record<RequestStatus, string> = {
    [RequestStatus.New]: 'חדשה',
    [RequestStatus.InProgress]: 'בטיפול',
    [RequestStatus.Waiting]: 'ממתינה',
    [RequestStatus.Completed]: 'הושלמה',
  };

  ngOnInit(): void {
    this.loading.set(true);
    this.requestsService.getHistory(this.data.requestId).subscribe({
      next: (items) => {
        this.history.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set(true);
      },
    });
  }

  statusLabel(status: RequestStatus | null): string {
    return status === null ? 'יצירה' : this.statusLabels[status];
  }
}
