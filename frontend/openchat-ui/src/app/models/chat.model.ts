import { ToolCallRecord } from './tool-event.model';

export interface ChatMessage {
  id?: string;
  userId: string;
  conversationId: string;
  role: 'user' | 'assistant' | 'system';
  content: string;
  timestamp: Date;
  stopped?: boolean;
  toolCallsUsed?: ToolCallRecord[];
}

export interface ChatRequest {
  userId: string;
  conversationId?: string;
  message: string;
  model?: string;
}

export interface ChatResponse {
  conversationId: string;
  conversationTitle: string;
  reply: string;
  timestamp: Date;
  tokensUsed: number;
}
