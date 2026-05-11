import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, Subject } from 'rxjs';
import { ChatMessage, ChatRequest } from '../models/chat.model';
import { DoneEvent, ToolEndEvent, ToolStartEvent } from '../models/tool-event.model';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class ChatService {
  private readonly apiUrl = 'http://localhost:5124';

  readonly toolStart$ = new Subject<ToolStartEvent>();
  readonly toolEnd$   = new Subject<ToolEndEvent>();
  readonly done$      = new Subject<DoneEvent>();

  constructor(private http: HttpClient, private auth: AuthService) {}

  getMessages(conversationId: string): Observable<ChatMessage[]> {
    return this.http.get<ChatMessage[]>(`${this.apiUrl}/chat/conversation/${conversationId}/messages`);
  }

  streamMessage(
    request: ChatRequest,
    onToken: (token: string) => void,
    onError: (err: unknown) => void,
    onAbort: () => void,
    abortSignal: AbortSignal
  ): void {
    const headers: Record<string, string> = { 'Content-Type': 'application/json' };
    const token = this.auth.token;
    if (token) headers['Authorization'] = `Bearer ${token}`;

    fetch(`${this.apiUrl}/chat/stream`, {
      method: 'POST',
      headers,
      body: JSON.stringify(request),
      signal: abortSignal
    })
      .then(async (response) => {
        if (!response.ok) throw new Error(`HTTP ${response.status}`);

        const reader = response.body!.getReader();
        const decoder = new TextDecoder();
        let buffer = '';
        let currentEvent = '';
        let currentData = '';

        const dispatch = () => {
          if (!currentEvent || !currentData) return;
          try {
            switch (currentEvent) {
              case 'token':
                onToken(JSON.parse(currentData) as string);
                break;
              case 'tool_start':
                this.toolStart$.next(JSON.parse(currentData) as ToolStartEvent);
                break;
              case 'tool_end':
                this.toolEnd$.next(JSON.parse(currentData) as ToolEndEvent);
                break;
              case 'done':
                this.done$.next(JSON.parse(currentData) as DoneEvent);
                break;
              case 'error': {
                const errData = JSON.parse(currentData) as { message?: string };
                onError(new Error(errData.message ?? 'Stream error'));
                break;
              }
            }
          } catch { /* malformed JSON — skip */ }
          currentEvent = '';
          currentData = '';
        };

        while (true) {
          const { done, value } = await reader.read();
          if (done) break;

          buffer += decoder.decode(value, { stream: true });
          const lines = buffer.split('\n');
          buffer = lines.pop() ?? '';

          for (const line of lines) {
            if (line.startsWith('event: ')) {
              currentEvent = line.slice(7).trim();
            } else if (line.startsWith('data: ')) {
              currentData = line.slice(6);
            } else if (line === '') {
              dispatch();
            }
          }
        }
        dispatch();
      })
      .catch((err: unknown) => {
        if (err instanceof DOMException && err.name === 'AbortError') {
          onAbort();
        } else {
          onError(err);
        }
      });
  }
}
