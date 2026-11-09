import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { PersonasService } from '../core/personas.service';
import { ProcesoListItem } from '../core/models';

@Component({
  selector: 'app-persona-form',
  imports: [
    RouterLink,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatListModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './persona-form.html',
  styleUrl: './persona-form.css',
})
export class PersonaForm {
  private readonly personasService = inject(PersonasService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly personaId = signal<number | null>(null);
  readonly cargando = signal(true);
  readonly guardando = signal(false);
  readonly procesos = signal<ProcesoListItem[]>([]);
  readonly errorMensaje = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    documentoIdentidad: ['', Validators.required],
    nombres: ['', Validators.required],
    apellidos: ['', Validators.required],
    email: [''],
    telefono: [''],
    fechaNacimiento: [''],
  });

  get esNuevo(): boolean {
    return this.personaId() === null;
  }

  constructor() {
    const idParam = this.route.snapshot.paramMap.get('id');

    if (!idParam || idParam === 'nuevo') {
      this.cargando.set(false);
      return;
    }

    this.personaId.set(Number(idParam));
    this.cargarPersona(Number(idParam));
  }

  private async cargarPersona(id: number): Promise<void> {
    this.cargando.set(true);
    try {
      const persona = await this.personasService.obtener(id);
      this.form.patchValue({
        documentoIdentidad: persona.documentoIdentidad,
        nombres: persona.nombres,
        apellidos: persona.apellidos,
        email: persona.email ?? '',
        telefono: persona.telefono ?? '',
        fechaNacimiento: persona.fechaNacimiento ?? '',
      });
      this.procesos.set(persona.procesos);
    } finally {
      this.cargando.set(false);
    }
  }

  async guardar(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.guardando.set(true);
    this.errorMensaje.set(null);

    const valores = this.form.getRawValue();
    const dto = {
      documentoIdentidad: valores.documentoIdentidad,
      nombres: valores.nombres,
      apellidos: valores.apellidos,
      email: valores.email || null,
      telefono: valores.telefono || null,
      fechaNacimiento: valores.fechaNacimiento || null,
    };

    try {
      if (this.esNuevo) {
        const creada = await this.personasService.crear(dto);
        await this.router.navigate(['/personas', creada.id]);
      } else {
        await this.personasService.actualizar(this.personaId()!, dto);
        await this.cargarPersona(this.personaId()!);
      }
    } catch (err: unknown) {
      this.errorMensaje.set(this.extraerError(err));
    } finally {
      this.guardando.set(false);
    }
  }

  private extraerError(err: unknown): string {
    const httpErr = err as { error?: string; status?: number };
    return httpErr?.error ?? 'Ocurrió un error al guardar. Verifique los datos e intente de nuevo.';
  }
}
