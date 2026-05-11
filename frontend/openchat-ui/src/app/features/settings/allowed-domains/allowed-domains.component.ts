import {
  Component, OnInit, OnDestroy, signal, computed
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, debounceTime, takeUntil } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';

import {
  AllowedDomain, DomainCategory, AllowedDomainRequest,
  CATEGORY_LABELS, CATEGORY_COLORS, ALL_CATEGORIES
} from '../../../models/allowed-domain.model';
import { AllowlistService } from '../../../services/allowlist.service';
import { ConfirmService } from '../../../services/confirm.service';
import { DomainFormModalComponent } from './domain-form-modal.component';

@Component({
  selector: 'app-allowed-domains',
  standalone: true,
  imports: [CommonModule, FormsModule, DomainFormModalComponent],
  templateUrl: './allowed-domains.component.html',
  styleUrl: './allowed-domains.component.scss'
})
export class AllowedDomainsComponent implements OnInit, OnDestroy {
  private readonly destroy$ = new Subject<void>();
  private readonly searchSubject = new Subject<string>();

  searchQuery      = signal('');
  activeCategory   = signal<DomainCategory | 'all'>('all');
  loadError        = signal(false);
  modalOpen        = signal(false);
  editingDomain    = signal<AllowedDomain | null>(null);
  togglingIds      = signal<Set<string>>(new Set());
  modalServerError = signal('');

  readonly categories    = ALL_CATEGORIES;
  readonly categoryLabel = CATEGORY_LABELS;
  readonly categoryColor = CATEGORY_COLORS;

  filteredDomains = computed(() => {
    const query = this.searchQuery().toLowerCase();
    const cat   = this.activeCategory();
    return this.allowlistService.domains().filter(d => {
      const matchesCat   = cat === 'all' || d.category === cat;
      const matchesQuery = !query ||
        d.domain.includes(query) ||
        d.description.toLowerCase().includes(query);
      return matchesCat && matchesQuery;
    });
  });

  get loading() { return this.allowlistService.loading; }

  constructor(
    readonly allowlistService: AllowlistService,
    private confirmService: ConfirmService
  ) {}

  ngOnInit(): void {
    this.searchSubject.pipe(debounceTime(200), takeUntil(this.destroy$))
      .subscribe(q => this.searchQuery.set(q));

    this.load();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  load(): void {
    this.loadError.set(false);
    this.allowlistService.getAll().subscribe({
      error: () => this.loadError.set(true)
    });
  }

  onSearch(value: string): void {
    this.searchSubject.next(value);
  }

  openAdd(): void {
    this.editingDomain.set(null);
    this.modalOpen.set(true);
  }

  openEdit(domain: AllowedDomain): void {
    this.editingDomain.set(domain);
    this.modalOpen.set(true);
  }

  closeModal(): void {
    this.modalOpen.set(false);
    this.editingDomain.set(null);
    this.modalServerError.set('');
  }

  onSave(request: AllowedDomainRequest): void {
    this.modalServerError.set('');
    const editing = this.editingDomain();
    const op = editing
      ? this.allowlistService.update(editing.id, request)
      : this.allowlistService.create(request);

    op.subscribe({
      next: () => this.closeModal(),
      error: (err: HttpErrorResponse) => {
        const msg = err.status === 409
          ? 'Domain already exists in the allowlist.'
          : (err.error?.message ?? 'An error occurred. Please try again.');
        this.modalServerError.set(msg);
      }
    });
  }

  toggle(domain: AllowedDomain): void {
    const ids = new Set(this.togglingIds());
    ids.add(domain.id);
    this.togglingIds.set(ids);

    this.allowlistService.toggle(domain.id).subscribe({
      next: () => {
        const updated = new Set(this.togglingIds());
        updated.delete(domain.id);
        this.togglingIds.set(updated);
      },
      error: () => {
        const updated = new Set(this.togglingIds());
        updated.delete(domain.id);
        this.togglingIds.set(updated);
      }
    });
  }

  async deleteDomain(domain: AllowedDomain): Promise<void> {
    if (domain.isSystemDefault) return;

    const confirmed = await this.confirmService.confirm({
      title:        'Delete domain',
      message:      `Remove "${domain.domain}" from the allowlist? This cannot be undone.`,
      confirmLabel: 'Delete',
      cancelLabel:  'Cancel',
      danger:       true
    });

    if (!confirmed) return;

    this.allowlistService.delete(domain.id).subscribe({
      error: (err: HttpErrorResponse) => {
        if (err.status === 403) {
          this.confirmService.confirm({
            title:        'Cannot delete',
            message:      'System defaults can be disabled but not deleted.',
            confirmLabel: 'OK',
            cancelLabel:  '',
            danger:       false
          });
        }
      }
    });
  }

  isToggling(id: string): boolean {
    return this.togglingIds().has(id);
  }
}
