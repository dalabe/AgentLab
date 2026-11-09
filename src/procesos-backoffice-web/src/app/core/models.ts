export type TipoProceso =
  'Civil' | 'Penal' | 'Laboral' | 'Administrativo' | 'Comercial' | 'Familia';
export type EstadoProceso = 'Activo' | 'EnRevision' | 'Suspendido' | 'Cerrado' | 'Archivado';
export type TipoDocumento =
  | 'Demanda'
  | 'Contestacion'
  | 'Sentencia'
  | 'Contrato'
  | 'Prueba'
  | 'Resolucion'
  | 'Notificacion'
  | 'Otro';
export type TipoEvento =
  'Presentacion' | 'Audiencia' | 'Notificacion' | 'Resolucion' | 'Recurso' | 'Pago' | 'Otro';

export const TIPOS_PROCESO: TipoProceso[] = [
  'Civil',
  'Penal',
  'Laboral',
  'Administrativo',
  'Comercial',
  'Familia',
];
export const ESTADOS_PROCESO: EstadoProceso[] = [
  'Activo',
  'EnRevision',
  'Suspendido',
  'Cerrado',
  'Archivado',
];
export const TIPOS_DOCUMENTO: TipoDocumento[] = [
  'Demanda',
  'Contestacion',
  'Sentencia',
  'Contrato',
  'Prueba',
  'Resolucion',
  'Notificacion',
  'Otro',
];
export const TIPOS_EVENTO: TipoEvento[] = [
  'Presentacion',
  'Audiencia',
  'Notificacion',
  'Resolucion',
  'Recurso',
  'Pago',
  'Otro',
];

export interface PersonaListItem {
  id: number;
  nombreCompleto: string;
  documentoIdentidad: string;
  email: string | null;
  telefono: string | null;
  cantidadProcesos: number;
}

export interface PersonaDetail {
  id: number;
  nombres: string;
  apellidos: string;
  documentoIdentidad: string;
  email: string | null;
  telefono: string | null;
  fechaNacimiento: string | null;
  createdAtUtc: string;
  procesos: ProcesoListItem[];
}

export interface PersonaUpsert {
  documentoIdentidad: string;
  nombres: string;
  apellidos: string;
  email: string | null;
  telefono: string | null;
  fechaNacimiento: string | null;
}

export interface ProcesoListItem {
  id: number;
  numeroExpediente: string;
  tipo: TipoProceso;
  estado: EstadoProceso;
  entidadOJuzgado: string;
  fechaInicio: string;
  fechaUltimaActualizacionUtc: string;
  personaId: number;
  personaNombreCompleto: string;
}

export interface ProcesoDetail {
  id: number;
  numeroExpediente: string;
  tipo: TipoProceso;
  estado: EstadoProceso;
  entidadOJuzgado: string;
  descripcion: string;
  fechaInicio: string;
  fechaUltimaActualizacionUtc: string;
  notas: string | null;
  persona: PersonaListItem;
  documentos: DocumentoListItem[];
  eventos: EventoListItem[];
}

export interface ProcesoUpsert {
  numeroExpediente: string;
  personaId: number;
  tipo: TipoProceso;
  estado: EstadoProceso;
  entidadOJuzgado: string;
  descripcion: string;
  fechaInicio: string;
  notas: string | null;
}

export interface DocumentoListItem {
  id: number;
  procesoId: number;
  nombre: string;
  tipo: TipoDocumento;
  contentType: string;
  fechaCargaUtc: string;
  tamanoBytes: number;
}

export interface EventoListItem {
  id: number;
  procesoId: number;
  fecha: string;
  tipo: TipoEvento;
  descripcion: string;
}

export interface EventoUpsert {
  fecha: string;
  tipo: TipoEvento;
  descripcion: string;
}
