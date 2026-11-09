import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { BACKOFFICE_API_BASE_URL } from './api.config';
import { ProcesoDetail, ProcesoListItem, ProcesoUpsert } from './models';

export interface ProcesosFiltro {
  personaId?: number;
  tipo?: string;
  estado?: string;
  q?: string;
}

@Injectable({ providedIn: 'root' })
export class ProcesosService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${BACKOFFICE_API_BASE_URL}/api/procesos`;

  listar(filtro: ProcesosFiltro = {}): Promise<ProcesoListItem[]> {
    const params = new URLSearchParams();
    if (filtro.personaId != null) params.set('personaId', String(filtro.personaId));
    if (filtro.tipo) params.set('tipo', filtro.tipo);
    if (filtro.estado) params.set('estado', filtro.estado);
    if (filtro.q) params.set('q', filtro.q);

    const query = params.toString();
    const url = query ? `${this.baseUrl}?${query}` : this.baseUrl;
    return firstValueFrom(this.http.get<ProcesoListItem[]>(url));
  }

  obtener(id: number): Promise<ProcesoDetail> {
    return firstValueFrom(this.http.get<ProcesoDetail>(`${this.baseUrl}/${id}`));
  }

  crear(dto: ProcesoUpsert): Promise<ProcesoDetail> {
    return firstValueFrom(this.http.post<ProcesoDetail>(this.baseUrl, dto));
  }

  actualizar(id: number, dto: ProcesoUpsert): Promise<ProcesoDetail> {
    return firstValueFrom(this.http.put<ProcesoDetail>(`${this.baseUrl}/${id}`, dto));
  }

  eliminar(id: number): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${this.baseUrl}/${id}`));
  }
}
