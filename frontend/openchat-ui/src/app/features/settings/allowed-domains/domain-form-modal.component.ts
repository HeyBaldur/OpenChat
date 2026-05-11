import {
  Component, Input, Output, EventEmitter, OnInit, OnChanges, signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  AllowedDomain, AllowedDomainRequest,
  DomainCategory, CATEGORY_LABELS, ALL_CATEGORIES
} from '../../../models/allowed-domain.model';

@Component({
  selector: 'app-domain-form-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './domain-form-modal.component.html',
  styleUrl: './domain-form-modal.component.scss'
})
export class DomainFormModalComponent implements OnInit, OnChanges {
  @Input() domain:        AllowedDomain | null = null;
  @Input() externalError: string = '';
  @Output() saved  = new EventEmitter<AllowedDomainRequest>();
  @Output() cancel = new EventEmitter<void>();

  form = {
    domain:          '',
    category:        'custom' as DomainCategory,
    description:     '',
    allowSubdomains: false,
    enabled:         true,
  };

  domainError = signal('');
  saving      = signal(false);

  ngOnChanges(): void {
    if (this.externalError) this.saving.set(false);
  }

  readonly categories    = ALL_CATEGORIES;
  readonly categoryLabel = CATEGORY_LABELS;

  get isEdit() { return this.domain !== null; }

  ngOnInit(): void {
    if (this.domain) {
      this.form = {
        domain:          this.domain.domain,
        category:        this.domain.category,
        description:     this.domain.description,
        allowSubdomains: this.domain.allowSubdomains,
        enabled:         this.domain.enabled,
      };
    }
  }

  submit(): void {
    this.domainError.set('');

    const rawDomain = this.form.domain.trim();
    if (!rawDomain) {
      this.domainError.set('Domain is required.');
      return;
    }
    if (/\s/.test(rawDomain)) {
      this.domainError.set('Domain must not contain spaces.');
      return;
    }
    if (/^https?:\/\//i.test(rawDomain)) {
      this.domainError.set('Remove the protocol (http:// or https://).');
      return;
    }
    if (rawDomain.includes('/')) {
      this.domainError.set('Domain must not include a path.');
      return;
    }

    this.saving.set(true);
    this.saved.emit({ ...this.form, domain: rawDomain });
  }
}
