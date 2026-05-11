import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { AllowedDomain, AllowedDomainRequest } from '../models/allowed-domain.model';

@Injectable({ providedIn: 'root' })
export class AllowlistService {
  private readonly apiUrl = 'http://localhost:5124/api/allowlist';

  domains = signal<AllowedDomain[]>([]);
  loading = signal(false);

  constructor(private http: HttpClient) {}

  getAll(): Observable<AllowedDomain[]> {
    this.loading.set(true);
    return this.http.get<AllowedDomain[]>(this.apiUrl).pipe(
      tap({
        next: (list) => { this.domains.set(list); this.loading.set(false); },
        error: ()   => { this.loading.set(false); }
      })
    );
  }

  getById(id: string): Observable<AllowedDomain> {
    return this.http.get<AllowedDomain>(`${this.apiUrl}/${id}`);
  }

  create(request: AllowedDomainRequest): Observable<AllowedDomain> {
    return this.http.post<AllowedDomain>(this.apiUrl, request).pipe(
      tap(created => this.domains.update(list => [created, ...list]))
    );
  }

  update(id: string, request: AllowedDomainRequest): Observable<AllowedDomain> {
    return this.http.put<AllowedDomain>(`${this.apiUrl}/${id}`, request).pipe(
      tap(updated => this.domains.update(list => list.map(d => d.id === id ? updated : d)))
    );
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`).pipe(
      tap(() => this.domains.update(list => list.filter(d => d.id !== id)))
    );
  }

  toggle(id: string): Observable<AllowedDomain> {
    return this.http.patch<AllowedDomain>(`${this.apiUrl}/${id}/toggle`, {}).pipe(
      tap(updated => this.domains.update(list => list.map(d => d.id === id ? updated : d)))
    );
  }
}
