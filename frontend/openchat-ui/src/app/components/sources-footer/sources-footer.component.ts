import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToolCallRecord } from '../../models/tool-event.model';

@Component({
  selector: 'app-sources-footer',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './sources-footer.component.html'
})
export class SourcesFooterComponent {
  @Input({ required: true }) toolCalls: ToolCallRecord[] = [];

  get successfulCalls(): ToolCallRecord[] {
    return this.toolCalls.filter(c => c.success && c.sourceUrl);
  }

  chipLabel(sourceUrl: string): string {
    try {
      const u = new URL(sourceUrl);
      const raw = u.hostname + u.pathname;
      if (raw.length <= 50) return raw;
      return raw.slice(0, 22) + '…' + raw.slice(raw.length - 25);
    } catch {
      return sourceUrl;
    }
  }

  open(sourceUrl: string): void {
    window.open(sourceUrl, '_blank', 'noopener,noreferrer');
  }
}
