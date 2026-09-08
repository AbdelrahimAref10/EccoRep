import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '../../pipes/translate.pipe';

export type PaginationPageItem = number | 'ellipsis';

@Component({
  selector: 'app-pagination',
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  templateUrl: './pagination.component.html',
  styleUrl: './pagination.component.css'
})
export class PaginationComponent {
  @Input() currentPage = 1;
  @Input() totalPages = 1;
  @Input() totalCount = 0;
  @Input() pageSize = 12;

  @Output() pageChange = new EventEmitter<number>();

  get pages(): PaginationPageItem[] {
    const total = Math.max(this.totalPages, 1);
    const current = Math.min(Math.max(this.currentPage, 1), total);
    const items: PaginationPageItem[] = [];

    if (total <= 7) {
      for (let i = 1; i <= total; i++) {
        items.push(i);
      }
      return items;
    }

    items.push(1);

    if (current > 3) {
      items.push('ellipsis');
    }

    const start = Math.max(2, current - 1);
    const end = Math.min(total - 1, current + 1);

    for (let i = start; i <= end; i++) {
      items.push(i);
    }

    if (current < total - 2) {
      items.push('ellipsis');
    }

    items.push(total);
    return items;
  }

  get canGoPrev(): boolean {
    return this.currentPage > 1;
  }

  get canGoNext(): boolean {
    return this.currentPage < this.totalPages;
  }

  goTo(page: number): void {
    if (page < 1 || page > this.totalPages || page === this.currentPage) {
      return;
    }
    this.pageChange.emit(page);
  }

  goPrev(): void {
    this.goTo(this.currentPage - 1);
  }

  goNext(): void {
    this.goTo(this.currentPage + 1);
  }

  isNumber(item: PaginationPageItem): item is number {
    return typeof item === 'number';
  }
}
