import { Component, inject, OnInit, signal, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BehaviorSubject, Subject, merge } from 'rxjs';
import { debounceTime, distinctUntilChanged, map, switchMap, takeUntil, tap } from 'rxjs/operators';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatSnackBar } from '@angular/material/snack-bar';

import { RequestsService } from '../../services/requests.service';
import {
  RequestListItem,
  RequestFilter,
  RequestStatus,
  RequestPriority,
} from '../../models/request';
import { StatusUpdateDialog } from '../status-update-dialog/status-update-dialog';
import { HistoryDialog } from '../history-dialog/history-dialog';
import { BulkUpdateDialog } from '../bulk-update-dialog/bulk-update-dialog';

@Component({
  selector: 'app-requests-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatTableModule,
    MatPaginatorModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatChipsModule,
    MatDialogModule,
    MatCheckboxModule,
  ],
  templateUrl: './requests-list.html',
  styleUrl: './requests-list.scss',
})
export class RequestsList implements OnInit {
  private requestsService = inject(RequestsService);
  private dialog = inject(MatDialog);
  private snackBar = inject(MatSnackBar);
  private destroy$ = new Subject<void>();

  // משדר כשנתונים השתנו (עדכון סטטוס) - מאותת לפאנל ה-stats להתרענן
  dataChanged = output<void>();

  // סט המזהים הנבחרים לעדכון Bulk
  selectedIds = signal<Set<number>>(new Set());

  // State באמצעות signals
  items = signal<RequestListItem[]>([]);
  totalCount = signal(0);
  loading = signal(false);
  error = signal(false);

  // עמודות הטבלה
  displayedColumns = ['select', 'id', 'title', 'organizationName', 'status', 'priority', 'assignedTo', 'createdAt', 'actions'];

  // מגבלת ה-Bulk - תואם ל-Backend
  readonly maxBulkSize = 100;

  // ערכים ל-dropdowns
  statusOptions = [
    { value: RequestStatus.New, label: 'חדשה' },
    { value: RequestStatus.InProgress, label: 'בטיפול' },
    { value: RequestStatus.Waiting, label: 'ממתינה' },
    { value: RequestStatus.Completed, label: 'הושלמה' },
  ];

  priorityOptions = [
    { value: RequestPriority.Low, label: 'נמוכה' },
    { value: RequestPriority.Medium, label: 'בינונית' },
    { value: RequestPriority.High, label: 'גבוהה' },
  ];

  sortOptions = [
    { value: 'createdat', label: 'תאריך יצירה' },
    { value: 'updatedat', label: 'תאריך עדכון' },
    { value: 'title', label: 'כותרת' },
    { value: 'organizationname', label: 'ארגון' },
    { value: 'status', label: 'סטטוס' },
    { value: 'priority', label: 'עדיפות' },
  ];

  // ה-filter הנוכחי - כל שינוי נדחף דרך ה-Subject
  filter: RequestFilter = {
    page: 1,
    pageSize: 20,
    sortBy: 'createdat',
    sortDescending: true,
  };

  // ערוץ שינויי filter - עובר debounce + distinct (חיפוש/סינון/מיון/דפדוף)
  private filter$ = new BehaviorSubject<RequestFilter>(this.filter);
  // ערוץ רענון מאולץ - תמיד עובר, גם אם ה-filter זהה (אחרי עדכון סטטוס)
  private refresh$ = new Subject<void>();

  ngOnInit(): void {
    // ערוץ 1: שינויי filter עם debounce ומניעת כפילויות
    const filterChanges$ = this.filter$.pipe(
      debounceTime(300),
      distinctUntilChanged((a, b) => JSON.stringify(a) === JSON.stringify(b))
    );

    // ערוץ 2: רענון מאולץ - ממפה ל-filter הנוכחי בלי distinct
    const forcedRefresh$ = this.refresh$.pipe(map(() => this.filter));

    // איחוד שני הערוצים - כל אחד מהם מפעיל שליפה
    merge(filterChanges$, forcedRefresh$)
      .pipe(
        tap(() => {
          this.loading.set(true);
          this.error.set(false);
        }),
        // switchMap - מבטל בקשה קודמת שעדיין רצה כשמגיעה חדשה
        switchMap((filter) => this.requestsService.getRequests(filter)),
        takeUntil(this.destroy$)
      )
      .subscribe({
        next: (result) => {
          this.items.set(result.items);
          this.totalCount.set(result.totalCount);
          this.loading.set(false);
        },
        error: () => {
          this.loading.set(false);
          this.error.set(true);
          this.items.set([]);
        },
      });
  }

  /** חיפוש טקסטואלי - נקרא על כל הקלדה, ה-debounce מטפל בתזמון. */
  onSearchChange(value: string): void {
    this.filter = { ...this.filter, search: value, page: 1 };
    this.filter$.next(this.filter);
  }

  /** שינוי סטטוס - מקבל את הערך ישירות מהאירוע (ללא race עם ngModel). */
  onStatusChange(value: RequestStatus | null): void {
    this.filter = { ...this.filter, status: value, page: 1 };
    this.filter$.next(this.filter);
  }

  onPriorityChange(value: RequestPriority | null): void {
    this.filter = { ...this.filter, priority: value, page: 1 };
    this.filter$.next(this.filter);
  }

  onOrganizationChange(value: string): void {
    this.filter = { ...this.filter, organizationName: value, page: 1 };
    this.filter$.next(this.filter);
  }

  onAssignedToChange(value: string): void {
    this.filter = { ...this.filter, assignedTo: value, page: 1 };
    this.filter$.next(this.filter);
  }

  /** שינוי מיון. */
  onSortChange(value: string): void {
    this.filter = { ...this.filter, sortBy: value, page: 1 };
    this.filter$.next(this.filter);
  }

  toggleSortDirection(): void {
    this.filter = { ...this.filter, sortDescending: !this.filter.sortDescending, page: 1 };
    this.filter$.next(this.filter);
  }

  /** דפדוף - מתרגם את אירוע ה-paginator ל-filter. */
  onPageChange(event: PageEvent): void {
    this.filter = {
      ...this.filter,
      page: event.pageIndex + 1,
      pageSize: event.pageSize,
    };
    this.filter$.next(this.filter);
  }

  /** רענון מאולץ - שולף מחדש גם אם ה-filter לא השתנה (אחרי עדכון/שגיאה). */
  retry(): void {
    this.refresh$.next();
  }

  /** איפוס כל הסינונים. */
  resetFilters(): void {
    this.filter = {
      page: 1,
      pageSize: this.filter.pageSize,
      sortBy: 'createdat',
      sortDescending: true,
    };
    this.filter$.next(this.filter);
  }

  /** פתיחת dialog עדכון סטטוס. מרענן את הרשימה אם היה עדכון או conflict. */
  openStatusDialog(request: RequestListItem): void {
    const ref = this.dialog.open(StatusUpdateDialog, {
      data: { request },
      width: '420px',
      direction: 'rtl',
    });

    ref.afterClosed().subscribe((result) => {
      // result: הפנייה המעודכנת / 'refresh' (אחרי conflict) / null (ביטול)
      if (result) {
        this.retry();
        // אות לפאנל ה-stats שהנתונים השתנו (הספירות עודכנו)
        if (result !== 'refresh') {
          this.dataChanged.emit();
        }
      }
    });
  }

  /** פתיחת dialog היסטוריית שינויים. */
  openHistoryDialog(request: RequestListItem): void {
    this.dialog.open(HistoryDialog, {
      data: { requestId: request.id },
      width: '520px',
      direction: 'rtl',
    });
  }

  // --- בחירה מרובה (Bulk) ---

  isSelected(id: number): boolean {
    return this.selectedIds().has(id);
  }

  toggleSelection(id: number): void {
    const set = new Set(this.selectedIds());
    if (set.has(id)) {
      set.delete(id);
    } else {
      set.add(id);
    }
    this.selectedIds.set(set);
  }

  /** האם כל הפניות בעמוד הנוכחי נבחרו. */
  allSelected(): boolean {
    const items = this.items();
    return items.length > 0 && items.every((r) => this.selectedIds().has(r.id));
  }

  someSelected(): boolean {
    return this.selectedIds().size > 0 && !this.allSelected();
  }

  /** בחירה/ביטול של כל הפניות בעמוד הנוכחי. */
  toggleSelectAll(): void {
    const set = new Set(this.selectedIds());
    const items = this.items();
    if (this.allSelected()) {
      items.forEach((r) => set.delete(r.id));
    } else {
      items.forEach((r) => set.add(r.id));
    }
    this.selectedIds.set(set);
  }

  clearSelection(): void {
    this.selectedIds.set(new Set());
  }

  /** פתיחת dialog עדכון Bulk לפניות הנבחרות. */
  openBulkDialog(): void {
    const ids = Array.from(this.selectedIds());
    if (ids.length === 0) {
      return;
    }

    const ref = this.dialog.open(BulkUpdateDialog, {
      data: { requestIds: ids },
      width: '460px',
      direction: 'rtl',
    });

    ref.afterClosed().subscribe((result) => {
      if (result) {
        // result = BulkUpdateResult. מציגים סיכום ומרעננים.
        this.snackBar.open(
          `עודכנו ${result.succeededCount} פניות, ${result.failedCount} נכשלו`,
          'סגור',
          { duration: 5000, direction: 'rtl' }
        );
        this.clearSelection();
        this.retry();
        this.dataChanged.emit();
      }
    });
  }

  statusLabel(status: RequestStatus): string {
    return this.statusOptions.find((o) => o.value === status)?.label ?? '';
  }

  priorityLabel(priority: RequestPriority): string {
    return this.priorityOptions.find((o) => o.value === priority)?.label ?? '';
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
