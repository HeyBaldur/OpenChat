export interface Conversation {
  id: string;
  userId: string;
  title: string;
  createdAt: Date;
  updatedAt: Date;
  totalTokens: number;
  model?: string;
  modelMissing?: boolean;
}
