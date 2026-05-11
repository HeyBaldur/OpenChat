import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { InProgressToolCall } from '../../models/tool-event.model';

@Component({
  selector: 'app-tool-indicator',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './tool-indicator.component.html'
})
export class ToolIndicatorComponent {
  @Input({ required: true }) call!: InProgressToolCall;

  get label(): string {
    const url = this.call.sourceUrl;
    const args = this.call.args;
    const raw = url
      ? (() => { try { const u = new URL(url); return u.hostname + u.pathname; } catch { return url; } })()
      : String(args['url'] ?? args['path'] ?? '');
    return this.shorten(raw);
  }

  get errorTooltip(): string {
    const map: Record<string, string> = {
      domain_blocked:    'Domain not in your allowed list',
      internal_address:  'Internal address blocked',
      timeout:           'Source took too long to respond',
      http_404:          'Page not found',
      invalid_path:      'Invalid path'
    };
    return map[this.call.errorReason] ?? this.call.errorReason;
  }

  private shorten(s: string): string {
    if (!s) return 'unknown source';
    if (s.length <= 60) return s;
    return s.slice(0, 28) + '…' + s.slice(s.length - 29);
  }
}
