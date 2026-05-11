import {
  Component, Input, Output, EventEmitter, ViewChild, ElementRef,
  signal, ChangeDetectionStrategy
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { MarkdownComponent } from 'ngx-markdown';
import hljs from 'highlight.js';
import { ExcelService } from '../../services/excel.service';

@Component({
  selector: 'app-markdown-message',
  standalone: true,
  imports: [CommonModule, MarkdownComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './markdown-message.component.html'
})
export class MarkdownMessageComponent {
  @Input() content = '';
  @Input() streaming = false;
  @Input() showRegenerate = false;
  @Output() regenerate = new EventEmitter<void>();
  @ViewChild('container') container!: ElementRef<HTMLElement>;

  responseCopied = signal(false);

  constructor(private excelService: ExcelService) {}

  get hasTable(): boolean {
    return !this.streaming && this.excelService.hasTables(this.content);
  }

  downloadExcel(): void {
    this.excelService.download(this.content);
  }

  onMarkdownReady(): void {
    if (!this.container) return;
    const el = this.container.nativeElement;

    // Always apply syntax highlighting
    el.querySelectorAll<HTMLElement>('pre code').forEach((block) => {
      hljs.highlightElement(block);
    });

    // Copy buttons only after streaming is complete — avoids blinking on each rAF update
    if (this.streaming) return;

    el.querySelectorAll<HTMLElement>('pre').forEach((pre) => {
      if (pre.classList.contains('has-copy-btn')) return;
      pre.classList.add('has-copy-btn', 'code-block-wrapper');

      const code = pre.querySelector('code');
      const lang = [...(code?.classList ?? [])]
        .find(c => c.startsWith('language-'))?.replace('language-', '') ?? '';

      const btn = document.createElement('button');
      btn.className = 'copy-code-btn';
      btn.innerHTML = `
        <svg width="11" height="11" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <rect x="9" y="9" width="13" height="13" rx="2" stroke-width="2"/>
          <path d="M5 15H4a2 2 0 01-2-2V4a2 2 0 012-2h9a2 2 0 012 2v1" stroke-width="2"/>
        </svg>
        ${lang ? `<span style="opacity:0.6">${lang}</span>` : ''}
        <span class="btn-label">Copy</span>
      `;

      btn.addEventListener('click', () => {
        navigator.clipboard.writeText(code?.innerText ?? '').then(() => {
          btn.classList.add('copied');
          const label = btn.querySelector<HTMLElement>('.btn-label');
          if (label) label.textContent = 'Copied!';
          setTimeout(() => {
            btn.classList.remove('copied');
            if (label) label.textContent = 'Copy';
          }, 2000);
        });
      });

      pre.appendChild(btn);
    });
  }

  copyResponse(): void {
    navigator.clipboard.writeText(this.content).then(() => {
      this.responseCopied.set(true);
      setTimeout(() => this.responseCopied.set(false), 2000);
    });
  }
}
