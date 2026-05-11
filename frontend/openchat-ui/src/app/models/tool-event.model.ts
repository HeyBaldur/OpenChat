export interface ToolStartEvent {
  tool: string;
  args: Record<string, unknown>;
}

export interface ToolEndEvent {
  tool: string;
  ok: boolean;
  sourceUrl: string | null;
  errorReason: string;
  preview: string;
}

export interface ToolCallRecord {
  tool: string;
  arguments: string;
  success: boolean;
  sourceUrl: string | null;
}

export interface DoneEvent {
  conversationId: string;
  conversationTitle: string;
  promptTokens: number;
  completionTokens: number;
  tokensUsed: number;
  toolCallsUsed: ToolCallRecord[];
  model?: string;
}

export interface InProgressToolCall {
  tool: string;
  args: Record<string, unknown>;
  status: 'running' | 'done' | 'failed';
  sourceUrl: string | null;
  errorReason: string;
  startedAt: number;
}
