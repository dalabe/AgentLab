import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ChatService } from './chat.service';
import { ChatMessageVm } from './chat.models';

const EJEMPLOS_DE_PREGUNTAS = [
  '¿Cuál es el estado del proceso EXP-2024-00031?',
  '¿Qué procesos tiene Juan Carlos Pérez Londoño?',
  '¿Qué documentos tiene el proceso EXP-2023-00450?',
];

@Component({
  selector: 'app-chat-page',
  imports: [FormsModule],
  templateUrl: './chat-page.html',
  styleUrl: './chat-page.css',
})
export class ChatPage {
  private readonly chatService = inject(ChatService);

  readonly ejemplos = EJEMPLOS_DE_PREGUNTAS;
  readonly messages = signal<ChatMessageVm[]>([]);
  readonly draft = signal('');
  readonly sending = signal(false);

  usarEjemplo(texto: string): void {
    this.draft.set(texto);
  }

  async enviar(): Promise<void> {
    const texto = this.draft().trim();
    if (!texto || this.sending()) {
      return;
    }

    this.messages.update((lista) => [...lista, { role: 'user', text: texto }]);
    this.draft.set('');
    this.sending.set(true);

    try {
      const respuesta = await this.chatService.sendMessage(texto);
      this.messages.update((lista) => [
        ...lista,
        { role: 'assistant', text: respuesta.answer, toolCalls: respuesta.toolCalls },
      ]);
    } catch {
      this.messages.update((lista) => [
        ...lista,
        {
          role: 'assistant',
          text: 'No pude conectarme con el servicio de chat (Procesos.ChatApi). Verifica que esté corriendo en http://localhost:5090.',
          isError: true,
        },
      ]);
    } finally {
      this.sending.set(false);
    }
  }

  nuevaConversacion(): void {
    this.chatService.resetSession();
    this.messages.set([]);
  }

  formatearArgumentos(args: Record<string, unknown> | null): string {
    if (!args || Object.keys(args).length === 0) {
      return '(sin argumentos)';
    }
    return JSON.stringify(args);
  }
}
