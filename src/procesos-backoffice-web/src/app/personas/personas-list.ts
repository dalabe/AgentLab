import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { FormsModule } from '@angular/forms';
import { PersonasService } from '../core/personas.service';
import { PersonaListItem } from '../core/models';

@Component({
  selector: 'app-personas-list',
  imports: [
    RouterLink,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    FormsModule,
  ],
  templateUrl: './personas-list.html',
  styleUrl: './personas-list.css',
})
export class PersonasList {
  private readonly personasService = inject(PersonasService);
  private readonly router = inject(Router);

  readonly personas = signal<PersonaListItem[]>([]);
  readonly cargando = signal(true);
  readonly busqueda = signal('');
  readonly columnas = [
    'nombreCompleto',
    'documentoIdentidad',
    'email',
    'telefono',
    'cantidadProcesos',
    'acciones',
  ];

  constructor() {
    this.cargar();
  }

  async cargar(): Promise<void> {
    this.cargando.set(true);
    try {
      this.personas.set(await this.personasService.listar(this.busqueda() || undefined));
    } finally {
      this.cargando.set(false);
    }
  }

  abrir(persona: PersonaListItem): void {
    this.router.navigate(['/personas', persona.id]);
  }

  async eliminar(persona: PersonaListItem, event: Event): Promise<void> {
    event.stopPropagation();
    if (
      !confirm(
        `¿Eliminar a ${persona.nombreCompleto}? Esto también elimina sus procesos, documentos y eventos.`,
      )
    ) {
      return;
    }
    await this.personasService.eliminar(persona.id);
    await this.cargar();
  }
}
