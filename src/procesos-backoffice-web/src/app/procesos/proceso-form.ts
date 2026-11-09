import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ProcesosService } from '../core/procesos.service';
import { PersonasService } from '../core/personas.service';
import { DocumentosService } from '../core/documentos.service';
import { EventosService } from '../core/eventos.service';
import {
  DocumentoListItem,
  EventoListItem,
  EstadoProceso,
  ESTADOS_PROCESO,
  PersonaListItem,
  TipoDocumento,
  TipoEvento,
  TIPOS_DOCUMENTO,
  TIPOS_EVENTO,
  TIPOS_PROCESO,
  TipoProceso,
} from '../core/models';

@Component({
  selector: 'app-proceso-form',
  imports: [
    RouterLink,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './proceso-form.html',
  styleUrl: './proceso-form.css',
})
export class ProcesoForm {
  private readonly procesosService = inject(ProcesosService);
  private readonly personasService = inject(PersonasService);
  private readonly documentosService = inject(DocumentosService);
  private readonly eventosService = inject(EventosService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly tipos = TIPOS_PROCESO;
  readonly estados = ESTADOS_PROCESO;
  readonly tiposDocumento = TIPOS_DOCUMENTO;
  readonly tiposEvento = TIPOS_EVENTO;

  readonly procesoId = signal<number | null>(null);
  readonly cargando = signal(true);
  readonly guardando = signal(false);
  readonly errorMensaje = signal<string | null>(null);
  readonly personas = signal<PersonaListItem[]>([]);
  readonly documentos = signal<DocumentoListItem[]>([]);
  readonly eventos = signal<EventoListItem[]>([]);

  readonly form = this.fb.nonNullable.group({
    numeroExpediente: ['', Validators.required],
    personaId: [0, Validators.required],
    tipo: ['Civil' as TipoProceso, Validators.required],
    estado: ['Activo' as EstadoProceso, Validators.required],
    entidadOJuzgado: ['', Validators.required],
    descripcion: ['', Validators.required],
    fechaInicio: ['', Validators.required],
    notas: [''],
  });

  readonly docForm = this.fb.nonNullable.group({
    nombre: ['', Validators.required],
    tipo: ['Otro' as TipoDocumento, Validators.required],
  });
  readonly archivoSeleccionado = signal<File | null>(null);
  readonly subiendoDocumento = signal(false);

  readonly eventoForm = this.fb.nonNullable.group({
    fecha: ['', Validators.required],
    tipo: ['Otro' as TipoEvento, Validators.required],
    descripcion: ['', Validators.required],
  });
  readonly guardandoEvento = signal(false);

  get esNuevo(): boolean {
    return this.procesoId() === null;
  }

  constructor() {
    this.personasService.listar().then((lista) => this.personas.set(lista));

    const idParam = this.route.snapshot.paramMap.get('id');
    if (!idParam || idParam === 'nuevo') {
      this.cargando.set(false);
      return;
    }

    this.procesoId.set(Number(idParam));
    this.cargarProceso(Number(idParam));
  }

  private async cargarProceso(id: number): Promise<void> {
    this.cargando.set(true);
    try {
      const proceso = await this.procesosService.obtener(id);
      this.form.patchValue({
        numeroExpediente: proceso.numeroExpediente,
        personaId: proceso.persona.id,
        tipo: proceso.tipo,
        estado: proceso.estado,
        entidadOJuzgado: proceso.entidadOJuzgado,
        descripcion: proceso.descripcion,
        fechaInicio: proceso.fechaInicio,
        notas: proceso.notas ?? '',
      });
      this.documentos.set(proceso.documentos);
      this.eventos.set(proceso.eventos);
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
    const dto = { ...valores, notas: valores.notas || null };

    try {
      if (this.esNuevo) {
        const creado = await this.procesosService.crear(dto);
        await this.router.navigate(['/procesos', creado.id]);
      } else {
        await this.procesosService.actualizar(this.procesoId()!, dto);
        await this.cargarProceso(this.procesoId()!);
      }
    } catch (err: unknown) {
      this.errorMensaje.set(this.extraerError(err));
    } finally {
      this.guardando.set(false);
    }
  }

  onArchivoSeleccionado(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.archivoSeleccionado.set(input.files?.[0] ?? null);
  }

  async subirDocumento(): Promise<void> {
    const archivo = this.archivoSeleccionado();
    if (!archivo || this.docForm.invalid || this.procesoId() === null) {
      return;
    }

    this.subiendoDocumento.set(true);
    try {
      const { nombre, tipo } = this.docForm.getRawValue();
      await this.documentosService.subir(this.procesoId()!, archivo, nombre, tipo);
      this.documentos.set(await this.documentosService.listarDeProceso(this.procesoId()!));
      this.docForm.reset({ nombre: '', tipo: 'Otro' });
      this.archivoSeleccionado.set(null);
    } finally {
      this.subiendoDocumento.set(false);
    }
  }

  descargarUrl(documentoId: number): string {
    return this.documentosService.urlContenido(documentoId);
  }

  async eliminarDocumento(documentoId: number): Promise<void> {
    if (!confirm('¿Eliminar este documento? Esto también borra el archivo físico.')) {
      return;
    }
    await this.documentosService.eliminar(documentoId);
    this.documentos.set(await this.documentosService.listarDeProceso(this.procesoId()!));
  }

  async agregarEvento(): Promise<void> {
    if (this.eventoForm.invalid || this.procesoId() === null) {
      this.eventoForm.markAllAsTouched();
      return;
    }

    this.guardandoEvento.set(true);
    try {
      await this.eventosService.crear(this.procesoId()!, this.eventoForm.getRawValue());
      this.eventos.set(await this.eventosService.listarDeProceso(this.procesoId()!));
      this.eventoForm.reset({ fecha: '', tipo: 'Otro', descripcion: '' });
    } finally {
      this.guardandoEvento.set(false);
    }
  }

  async eliminarEvento(eventoId: number): Promise<void> {
    if (!confirm('¿Eliminar este evento de la línea de tiempo?')) {
      return;
    }
    await this.eventosService.eliminar(eventoId);
    this.eventos.set(await this.eventosService.listarDeProceso(this.procesoId()!));
  }

  private extraerError(err: unknown): string {
    const httpErr = err as { error?: string };
    return httpErr?.error ?? 'Ocurrió un error al guardar. Verifique los datos e intente de nuevo.';
  }
}
