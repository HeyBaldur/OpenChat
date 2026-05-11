import { Injectable } from '@angular/core';
import * as XLSX from 'xlsx';

interface ParsedTable {
  headers: string[];
  rows: string[][];
}

@Injectable({ providedIn: 'root' })
export class ExcelService {

  hasTables(content: string): boolean {
    return /^\|.+\|/m.test(content);
  }

  download(markdownContent: string, filename = 'table-export'): void {
    const tables = this.parseTables(markdownContent);
    if (tables.length === 0) return;

    const wb = XLSX.utils.book_new();

    tables.forEach((table, i) => {
      const ws = XLSX.utils.aoa_to_sheet([table.headers, ...table.rows]);
      ws['!cols'] = table.headers.map(() => ({ wch: 22 }));
      const sheetName = tables.length === 1 ? 'Plan' : `Plan ${i + 1}`;
      XLSX.utils.book_append_sheet(wb, ws, sheetName);
    });

    const dateStr = new Date().toISOString().slice(0, 10);
    XLSX.writeFile(wb, `${filename}-${dateStr}.xlsx`);
  }

  private parseTables(content: string): ParsedTable[] {
    const tables: ParsedTable[] = [];
    const lines = content.split('\n');
    let i = 0;

    while (i < lines.length) {
      if (lines[i].trim().startsWith('|')) {
        const block: string[] = [];
        while (i < lines.length && lines[i].trim().startsWith('|')) {
          block.push(lines[i].trim());
          i++;
        }
        const parsed = this.parseTable(block);
        if (parsed) tables.push(parsed);
      } else {
        i++;
      }
    }
    return tables;
  }

  private parseTable(lines: string[]): ParsedTable | null {
    if (lines.length < 3) return null;
    if (!/^\|[-:| ]+\|$/.test(lines[1])) return null;

    const splitRow = (line: string) =>
      line.split('|').slice(1, -1).map(c => c.trim());

    return {
      headers: splitRow(lines[0]),
      rows: lines.slice(2).map(splitRow)
    };
  }
}
