import { Component, inject, OnInit, signal, input, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';

import { RequestsService } from '../../services/requests.service';
import { RequestStats } from '../../models/stats';
import { RequestStatus, RequestPriority } from '../../models/request';

@Component({
  selector: 'app-stats-panel',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatIconModule],
  templateUrl: './stats-panel.html',
  styleUrl: './stats-panel.scss',
})
export class StatsPanel implements OnInit {
  private requestsService = inject(RequestsService);

  // signal input - כשהערך משתנה (אחרי עדכון), הפאנל נטען מחדש
  refreshTrigger = input<number>(0);

  stats = signal<RequestStats | null>(null);
  loading = signal(false);

  private statusLabels: Record<RequestStatus, string> = {
    [RequestStatus.New]: 'חדשה',
    [RequestStatus.InProgress]: 'בטיפול',
    [RequestStatus.Waiting]: 'ממתינה',
    [RequestStatus.Completed]: 'הושלמה',
  };

  private priorityLabels: Record<RequestPriority, string> = {
    [RequestPriority.Low]: 'נמוכה',
    [RequestPriority.Medium]: 'בינונית',
    [RequestPriority.High]: 'גבוהה',
  };

  constructor() {
    // טעינה מחדש בכל שינוי של ה-trigger (אחרי עדכון סטטוס)
    effect(() => {
      this.refreshTrigger();
      this.loadStats();
    });
  }

  ngOnInit(): void {
    this.loadStats();
  }

  private loadStats(): void {
    this.loading.set(true);
    this.requestsService.getStats().subscribe({
      next: (data) => {
        this.stats.set(data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  statusLabel(status: RequestStatus): string {
    return this.statusLabels[status];
  }

  priorityLabel(priority: RequestPriority): string {
    return this.priorityLabels[priority];
  }

  statusClass(status: RequestStatus): string {
    return `status-${status}`;
  }
}
