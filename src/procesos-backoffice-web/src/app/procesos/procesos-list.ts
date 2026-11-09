import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { ProcesosService } from '../core/procesos.service';
import { ProcesoListItem, ESTADOS_PROCESO, TIPOS_PROCESO } from '../core/models';

@Component({
  selector: 'app-procesos-list',
  imports: [
    RouterLink,
    FormsModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
  ],
  templateUrl: './procesos-list.html',
  styleUrl: './procesos-list.css',
})
export class ProcesosList {
  private readonly procesosService = inject(ProcesosService);
  private readonly router = inject(Router);

  readonly tipos = TIPOS_PROCESO;
  readonly estados = ESTADOS_PROCESO;

  readonly procesos = signal<ProcesoListItem[]>([]);
  readonly cargando = signal(true);
  readonly busqueda = signal('');
  readonly tipoFiltro = signal<string | null>(null);
  readonly estadoFiltro = signal<string | null>(null);

  readonly columnas = [
    'numeroExpediente',
    'tipo',
    'estado',
    'personaNombreCompleto',
    'entidadOJuzgado',
    'acciones',
  ];

  constructor() {
    this.cargar();
  }

  async cargar(): Promise<void> {
    this.cargando.set(true);
    try {
      this.procesos.set(
        await this.procesosService.listar({
          q: this.busqueda() || undefined,
          tipo: this.tipoFiltro() ?? undefined,
          estado: this.estadoFiltro() ?? undefined,
        }),
      );
    } finally {
      this.cargando.set(false);
    }
  }

  abrir(proceso: ProcesoListItem): void {
    this.router.navigate(['/procesos', proceso.id]);
  }

  async eliminar(proceso: ProcesoListItem, event: Event): Promise<void> {
    event.stopPropagation();
    if (
      !confirm(
        `¿Eliminar el proceso ${proceso.numeroExpediente}? Esto también elimina sus documentos y eventos.`,
      )
    ) {
      return;
    }
    await this.procesosService.eliminar(proceso.id);
    await this.cargar();
  }
}
