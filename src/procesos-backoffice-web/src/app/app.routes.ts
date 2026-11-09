import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'personas', pathMatch: 'full' },
  {
    path: 'personas',
    loadComponent: () => import('./personas/personas-list').then((m) => m.PersonasList),
  },
  {
    path: 'personas/nuevo',
    loadComponent: () => import('./personas/persona-form').then((m) => m.PersonaForm),
  },
  {
    path: 'personas/:id',
    loadComponent: () => import('./personas/persona-form').then((m) => m.PersonaForm),
  },
  {
    path: 'procesos',
    loadComponent: () => import('./procesos/procesos-list').then((m) => m.ProcesosList),
  },
  {
    path: 'procesos/nuevo',
    loadComponent: () => import('./procesos/proceso-form').then((m) => m.ProcesoForm),
  },
  {
    path: 'procesos/:id',
    loadComponent: () => import('./procesos/proceso-form').then((m) => m.ProcesoForm),
  },
];
