import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, shareReplay, catchError, of } from 'rxjs';
import { OllamaModel } from '../models/model.model';

const PREFERRED_MODEL_KEY = 'openchat-preferred-model';

@Injectable({ providedIn: 'root' })
export class ModelService {
  private readonly apiUrl = 'http://localhost:5124';

  readonly availableModels = signal<OllamaModel[]>([]);
  readonly modelsUnavailable = signal(false);

  private models$?: Observable<OllamaModel[]>;

  constructor(private http: HttpClient) {}

  getAvailableModels(): Observable<OllamaModel[]> {
    if (!this.models$) {
      this.models$ = this.http.get<OllamaModel[]>(`${this.apiUrl}/models`).pipe(
        shareReplay(1),
        catchError(() => {
          this.modelsUnavailable.set(true);
          return of([]);
        })
      );
    }
    return this.models$;
  }

  getPreferredModel(): string | null {
    return localStorage.getItem(PREFERRED_MODEL_KEY);
  }

  savePreferredModel(name: string): void {
    localStorage.setItem(PREFERRED_MODEL_KEY, name);
  }

  resolveDefaultModel(models: OllamaModel[]): string | null {
    if (models.length === 0) return null;
    const preferred = this.getPreferredModel();
    if (preferred && models.some(m => m.name === preferred)) return preferred;
    return models.find(m => m.supportsToolCalling)?.name ?? models[0].name;
  }
}
