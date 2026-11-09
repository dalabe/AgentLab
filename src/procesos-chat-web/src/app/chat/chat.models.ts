export interface ToolCallDto {
  name: string;
  arguments: Record<string, unknown> | null;
  result: string | null;
}

export interface ChatRequestDto {
  sessionId: string | null;
  message: string;
}

export interface ChatResponseDto {
  sessionId: string;
  answer: string;
  toolCalls: ToolCallDto[];
}

export interface ChatMessageVm {
  role: 'user' | 'assistant';
  text: string;
  toolCalls?: ToolCallDto[];
  isError?: boolean;
}
