import { Component, signal } from '@angular/core';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { RequestsList } from './components/requests-list/requests-list';
import { StatsPanel } from './components/stats-panel/stats-panel';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [MatToolbarModule, MatIconModule, RequestsList, StatsPanel],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  protected readonly title = signal('ניהול פניות');

  // מונה שמשתנה בכל עדכון סטטוס - מאותת לפאנל ה-stats להתרענן
  protected readonly statsRefresh = signal(0);

  onDataChanged(): void {
    this.statsRefresh.update((v) => v + 1);
  }
}
