import {
  Component, Input, ViewChild, ElementRef,
  AfterViewInit, ChangeDetectionStrategy, ChangeDetectorRef
} from '@angular/core';

let counter = 0;

@Component({
  selector: 'app-mermaid',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (loading) {
      <div class="flex items-center gap-2 text-neutral-500 text-sm py-10 justify-center">
        <span class="w-4 h-4 border-2 border-purple-500 border-t-transparent rounded-full animate-spin"></span>
        Rendering diagram...
      </div>
    }
    <div #container [class.hidden]="loading" class="overflow-x-auto flex justify-center py-2"></div>
  `
})
export class MermaidComponent implements AfterViewInit {
  @Input() diagram!: string;
  @ViewChild('container') containerRef!: ElementRef<HTMLDivElement>;

  loading = true;

  constructor(private cdr: ChangeDetectorRef) {}

  async ngAfterViewInit(): Promise<void> {
    const { default: mermaid } = await import('mermaid');

    mermaid.initialize({
      startOnLoad: false,
      theme: 'dark',
      themeVariables: {
        darkMode: true,
        background: '#0a0a0f',
        primaryColor: '#3b0764',
        primaryTextColor: '#f9fafb',
        primaryBorderColor: '#7c3aed',
        lineColor: '#6b7280',
        secondaryColor: '#1e1b4b',
        tertiaryColor: '#0a0a0f',
        edgeLabelBackground: '#1e1b4b',
        fontFamily: '"Inter", ui-sans-serif, system-ui, sans-serif',
        fontSize: '13px'
      }
    });

    const id = `mermaid-${++counter}`;
    try {
      const { svg } = await mermaid.render(id, this.diagram.trim());
      this.containerRef.nativeElement.innerHTML = svg;
    } catch (e) {
      this.containerRef.nativeElement.innerHTML =
        `<p class="text-red-400 text-xs p-4">Error rendering diagram</p>`;
    } finally {
      this.loading = false;
      this.cdr.markForCheck();
    }
  }
}
