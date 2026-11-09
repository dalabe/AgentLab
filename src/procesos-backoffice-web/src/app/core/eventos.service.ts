import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { BACKOFFICE_API_BASE_URL } from './api.config';
import { EventoListItem, EventoUpsert } from './models';

@Injectable({ providedIn: 'root' })
export class EventosService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = BACKOFFICE_API_BASE_URL;

  listarDeProceso(procesoId: number): Promise<EventoListItem[]> {
    return firstValueFrom(
      this.http.get<EventoListItem[]>(`${this.baseUrl}/api/procesos/${procesoId}/eventos`),
    );
  }

  crear(procesoId: number, dto: EventoUpsert): Promise<EventoListItem> {
    return firstValueFrom(
      this.http.post<EventoListItem>(`${this.baseUrl}/api/procesos/${procesoId}/eventos`, dto),
    );
  }

  eliminar(eventoId: number): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${this.baseUrl}/api/eventos/${eventoId}`));
  }
}
