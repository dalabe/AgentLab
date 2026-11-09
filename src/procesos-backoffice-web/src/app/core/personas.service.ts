import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { BACKOFFICE_API_BASE_URL } from './api.config';
import { PersonaDetail, PersonaListItem, PersonaUpsert } from './models';

@Injectable({ providedIn: 'root' })
export class PersonasService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${BACKOFFICE_API_BASE_URL}/api/personas`;

  listar(q?: string): Promise<PersonaListItem[]> {
    const url = q ? `${this.baseUrl}?q=${encodeURIComponent(q)}` : this.baseUrl;
    return firstValueFrom(this.http.get<PersonaListItem[]>(url));
  }

  obtener(id: number): Promise<PersonaDetail> {
    return firstValueFrom(this.http.get<PersonaDetail>(`${this.baseUrl}/${id}`));
  }

  crear(dto: PersonaUpsert): Promise<PersonaDetail> {
    return firstValueFrom(this.http.post<PersonaDetail>(this.baseUrl, dto));
  }

  actualizar(id: number, dto: PersonaUpsert): Promise<PersonaDetail> {
    return firstValueFrom(this.http.put<PersonaDetail>(`${this.baseUrl}/${id}`, dto));
  }

  eliminar(id: number): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${this.baseUrl}/${id}`));
  }
}
