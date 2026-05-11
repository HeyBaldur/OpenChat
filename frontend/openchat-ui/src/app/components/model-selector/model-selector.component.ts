import {
  Component, Input, Output, EventEmitter,
  signal, HostListener, ElementRef, OnInit
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { OllamaModel } from '../../models/model.model';
import { ModelService } from '../../services/model.service';

@Component({
  selector: 'app-model-selector',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './model-selector.component.html'
})
export class ModelSelectorComponent implements OnInit {
  @Input() selectedModel: string | null = null;
  @Input() disabled = false;
  @Output() modelChange = new EventEmitter<string>();

  models = signal<OllamaModel[]>([]);
  isOpen = signal(false);
  unavailable = signal(false);

  constructor(
    private modelService: ModelService,
    private el: ElementRef
  ) {}

  ngOnInit(): void {
    this.modelService.getAvailableModels().subscribe({
      next: (list) => {
        this.models.set(list);
        this.unavailable.set(list.length === 0 && this.modelService.modelsUnavailable());
      }
    });
  }

  // Resolves which model is actually active — uses selectedModel input first,
  // falls back to the service default when the parent hasn't propagated yet.
  get effectiveSelected(): string | null {
    if (this.selectedModel) return this.selectedModel;
    return this.modelService.resolveDefaultModel(this.models());
  }

  get selectedDisplay(): string {
    const name = this.effectiveSelected;
    if (!name) return 'Select model';
    return this.models().find(m => m.name === name)?.displayName ?? name;
  }

  toggle(): void {
    if (this.disabled || this.unavailable()) return;
    this.isOpen.update(v => !v);
  }

  select(model: OllamaModel): void {
    this.isOpen.set(false);
    if (model.name === this.effectiveSelected) return;
    this.modelChange.emit(model.name);
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.el.nativeElement.contains(event.target)) {
      this.isOpen.set(false);
    }
  }
}
