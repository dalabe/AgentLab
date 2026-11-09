import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { BACKOFFICE_API_BASE_URL } from './api.config';
import { DocumentoListItem, TipoDocumento } from './models';

@Injectable({ providedIn: 'root' })
export class DocumentosService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = BACKOFFICE_API_BASE_URL;

  listarDeProceso(procesoId: number): Promise<DocumentoListItem[]> {
    return firstValueFrom(
      this.http.get<DocumentoListItem[]>(`${this.baseUrl}/api/procesos/${procesoId}/documentos`),
    );
  }

  subir(
    procesoId: number,
    archivo: File,
    nombre: string,
    tipo: TipoDocumento,
  ): Promise<DocumentoListItem> {
    const form = new FormData();
    form.append('archivo', archivo);
    form.append('nombre', nombre);
    form.append('tipo', tipo);

    return firstValueFrom(
      this.http.post<DocumentoListItem>(
        `${this.baseUrl}/api/procesos/${procesoId}/documentos`,
        form,
      ),
    );
  }

  urlContenido(documentoId: number): string {
    return `${this.baseUrl}/api/documentos/${documentoId}/contenido`;
  }

  eliminar(documentoId: number): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${this.baseUrl}/api/documentos/${documentoId}`));
  }
}
