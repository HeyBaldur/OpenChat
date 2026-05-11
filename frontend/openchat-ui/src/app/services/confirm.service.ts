import { Injectable, signal } from '@angular/core';

export interface ConfirmOptions {
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  danger?: boolean;
}

interface ConfirmState extends ConfirmOptions {
  visible: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

@Injectable({ providedIn: 'root' })
export class ConfirmService {
  readonly dialog = signal<ConfirmState | null>(null);

  confirm(options: ConfirmOptions): Promise<boolean> {
    return new Promise(resolve => {
      this.dialog.set({
        ...options,
        confirmLabel: options.confirmLabel ?? 'Confirm',
        cancelLabel: options.cancelLabel ?? 'Cancel',
        danger: options.danger ?? false,
        visible: false,
        onConfirm: () => { this.close(); resolve(true); },
        onCancel:  () => { this.close(); resolve(false); }
      });
      // Next tick — let the DOM render before triggering the enter transition
      setTimeout(() => {
        this.dialog.update(d => d ? { ...d, visible: true } : null);
      });
    });
  }

  private close(): void {
    this.dialog.update(d => d ? { ...d, visible: false } : null);
    setTimeout(() => this.dialog.set(null), 250);
  }
}
