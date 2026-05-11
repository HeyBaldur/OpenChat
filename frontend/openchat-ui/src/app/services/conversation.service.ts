import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Conversation } from '../models/conversation.model';

@Injectable({ providedIn: 'root' })
export class ConversationService {
  private readonly apiUrl = 'http://localhost:5124';

  constructor(private http: HttpClient) {}

  getConversations(userId: string): Observable<Conversation[]> {
    return this.http.get<Conversation[]>(`${this.apiUrl}/conversation/${userId}`);
  }

  deleteConversation(conversationId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/conversation/${conversationId}`);
  }

  updateModel(conversationId: string, model: string): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/conversation/${conversationId}/model`, { model });
  }
}
