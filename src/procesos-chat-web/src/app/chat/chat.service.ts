import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { CHAT_API_BASE_URL } from './chat-api.config';
import { ChatRequestDto, ChatResponseDto } from './chat.models';

/**
 * Cliente HTTP de Procesos.ChatApi. Mantiene el sessionId devuelto por el backend (que a su vez
 * mantiene el historial de la conversación en memoria) para que las siguientes preguntas tengan
 * contexto de las anteriores dentro de la misma sesión de chat.
 */
@Injectable({ providedIn: 'root' })
export class ChatService {
  private readonly http = inject(HttpClient);
  private sessionId: string | null = null;

  async sendMessage(message: string): Promise<ChatResponseDto> {
    const body: ChatRequestDto = { sessionId: this.sessionId, message };

    const response = await firstValueFrom(
      this.http.post<ChatResponseDto>(`${CHAT_API_BASE_URL}/api/chat/messages`, body),
    );

    this.sessionId = response.sessionId;
    return response;
  }

  resetSession(): void {
    this.sessionId = null;
  }
}
