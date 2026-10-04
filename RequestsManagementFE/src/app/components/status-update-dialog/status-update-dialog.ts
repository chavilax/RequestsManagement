import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import {
  MAT_DIALOG_DATA,
  MatDialogRef,
  MatDialogModule,
} from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';

import { RequestsService } from '../../services/requests.service';
import { RequestListItem, RequestStatus } from '../../models/request';

export interface StatusUpdateDialogData {
  request: RequestListItem;
}

@Component({
  selector: 'app-status-update-dialog',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatSelectModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatIconModule,
  ],
  templateUrl: './status-update-dialog.html',
  styleUrl: './status-update-dialog.scss',
})
export class StatusUpdateDialog {
  private requestsService = inject(RequestsService);
  private dialogRef = inject(MatDialogRef<StatusUpdateDialog>);
  data = inject<StatusUpdateDialogData>(MAT_DIALOG_DATA);

  saving = signal(false);
  conflict = signal(false);

  selectedStatus: RequestStatus;

  // מעברי סטטוס מותרים - תואם לחוקים ב-Backend (StatusTransitionRules)
  private allowedTransitions: Record<RequestStatus, RequestStatus[]> = {
    [RequestStatus.New]: [RequestStatus.InProgress, RequestStatus.Waiting],
    [RequestStatus.InProgress]: [RequestStatus.Waiting, RequestStatus.Completed],
    [RequestStatus.Waiting]: [RequestStatus.InProgress, RequestStatus.Completed],
    [RequestStatus.Completed]: [],
  };

  statusLabels: Record<RequestStatus, string> = {
    [RequestStatus.New]: 'חדשה',
    [RequestStatus.InProgress]: 'בטיפול',
    [RequestStatus.Waiting]: 'ממתינה',
    [RequestStatus.Completed]: 'הושלמה',
  };

  availableStatuses: { value: RequestStatus; label: string }[];

  constructor() {
    const current = this.data.request.status;
    this.availableStatuses = this.allowedTransitions[current].map((s) => ({
      value: s,
      label: this.statusLabels[s],
    }));
    this.selectedStatus = this.availableStatuses[0]?.value ?? current;
  }

  get currentStatusLabel(): string {
    return this.statusLabels[this.data.request.status];
  }

  get hasAvailableTransitions(): boolean {
    return this.availableStatuses.length > 0;
  }

  save(): void {
    this.saving.set(true);
    this.conflict.set(false);

    this.requestsService
      .updateStatus(this.data.request.id, {
        newStatus: this.selectedStatus,
        rowVersion: this.data.request.rowVersion,
        changedBy: 'משתמש מערכת',
      })
      .subscribe({
        next: (updated) => {
          this.saving.set(false);
          this.dialogRef.close(updated); // מחזיר את הפנייה המעודכנת
        },
        error: (err: HttpErrorResponse) => {
          this.saving.set(false);
          if (err.status === 409) {
            // Concurrency conflict - מישהו עדכן בינתיים
            this.conflict.set(true);
          }
          // שאר השגיאות מטופלות ב-interceptor הגלובלי
        },
      });
  }

  cancel(): void {
    this.dialogRef.close(null);
  }

  // סגירה עם סימון שצריך רענון (אחרי conflict)
  closeAndRefresh(): void {
    this.dialogRef.close('refresh');
  }
}
