import {
  Component, Input, Output, EventEmitter, ViewChild,
  ElementRef, signal, ChangeDetectionStrategy, OnChanges
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-user-message',
  standalone: true,
  imports: [CommonModule, FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './user-message.component.html'
})
export class UserMessageComponent implements OnChanges {
  @Input() content = '';
  @Input() editing = false;
  @Input() disableEdit = false;

  @Output() editStarted   = new EventEmitter<void>();
  @Output() editCancelled = new EventEmitter<void>();
  @Output() editSubmit    = new EventEmitter<string>();

  @ViewChild('editArea') private editArea?: ElementRef<HTMLTextAreaElement>;

  editText = '';
  copied = signal(false);

  ngOnChanges(): void {
    if (this.editing) {
      this.editText = this.content;
      setTimeout(() => this.focusAndResize());
    }
  }

  startEdit(): void {
    this.editStarted.emit();
  }

  cancelEdit(): void {
    this.editCancelled.emit();
  }

  confirmEdit(): void {
    const text = this.editText.trim();
    if (!text || text === this.content) { this.cancelEdit(); return; }
    this.editSubmit.emit(text);
  }

  onKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.confirmEdit();
    }
    if (event.key === 'Escape') {
      this.cancelEdit();
    }
  }

  autoResize(): void {
    const el = this.editArea?.nativeElement;
    if (!el) return;
    el.style.height = 'auto';
    el.style.height = el.scrollHeight + 'px';
  }

  copyMessage(): void {
    navigator.clipboard.writeText(this.content).then(() => {
      this.copied.set(true);
      setTimeout(() => this.copied.set(false), 2000);
    });
  }

  private focusAndResize(): void {
    const el = this.editArea?.nativeElement;
    if (!el) return;
    el.style.height = 'auto';
    el.style.height = el.scrollHeight + 'px';
    el.focus();
    el.setSelectionRange(el.value.length, el.value.length);
  }
}
