import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  MAT_DIALOG_DATA,
  MatDialogRef,
  MatDialogModule,
} from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { RequestsService } from '../../services/requests.service';
import { RequestStatus } from '../../models/request';
import { BulkUpdateResult } from '../../models/bulk-update';

export interface BulkUpdateDialogData {
  requestIds: number[];
}

@Component({
  selector: 'app-bulk-update-dialog',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './bulk-update-dialog.html',
  styleUrl: './bulk-update-dialog.scss',
})
export class BulkUpdateDialog {
  private requestsService = inject(RequestsService);
  private dialogRef = inject(MatDialogRef<BulkUpdateDialog>);
  data = inject<BulkUpdateDialogData>(MAT_DIALOG_DATA);

  saving = signal(false);
  result = signal<BulkUpdateResult | null>(null);

  selectedStatus: RequestStatus = RequestStatus.InProgress;

  statusOptions = [
    { value: RequestStatus.New, label: 'חדשה' },
    { value: RequestStatus.InProgress, label: 'בטיפול' },
    { value: RequestStatus.Waiting, label: 'ממתינה' },
    { value: RequestStatus.Completed, label: 'הושלמה' },
  ];

  get count(): number {
    return this.data.requestIds.length;
  }

  save(): void {
    this.saving.set(true);
    this.requestsService
      .bulkUpdateStatus({
        requestIds: this.data.requestIds,
        newStatus: this.selectedStatus,
        changedBy: 'משתמש מערכת',
      })
      .subscribe({
        next: (res) => {
          this.saving.set(false);
          // אם היו כשלונות - מציגים סיכום לפני סגירה. אחרת סוגרים ישר.
          if (res.failedCount > 0) {
            this.result.set(res);
          } else {
            this.dialogRef.close(res);
          }
        },
        error: () => {
          this.saving.set(false);
        },
      });
  }

  close(): void {
    this.dialogRef.close(this.result());
  }

  cancel(): void {
    this.dialogRef.close(null);
  }

  /** תרגום סיבת כישלון לעברית. */
  reasonLabel(reason: string): string {
    switch (reason) {
      case 'NotFound':
        return 'הפנייה לא נמצאה';
      case 'InvalidTransition':
        return 'מעבר סטטוס לא חוקי';
      case 'ConcurrencyConflict':
        return 'עודכנה על ידי משתמש אחר';
      default:
        return reason;
    }
  }
}
