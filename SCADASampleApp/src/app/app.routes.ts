import { Routes } from '@angular/router';
import { adminGuard } from './core/admin.guard';
import { authGuard } from './core/auth.guard';

export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./features/login/login.component').then((m) => m.LoginComponent) },
  {
    path: '',
    loadComponent: () => import('./shell/main-layout.component').then((m) => m.MainLayoutComponent),
    canActivate: [authGuard],
    children: [
      { path: '', loadComponent: () => import('./features/dashboard/dashboard.component').then((m) => m.DashboardComponent) },
      {
        path: 'pipelines/:id',
        loadComponent: () =>
          import('./features/pipeline-detail/pipeline-detail.component').then((m) => m.PipelineDetailComponent),
      },
      {
        path: 'pipelines/:id/process',
        loadComponent: () =>
          import('./features/process-schematic/process-schematic.component').then((m) => m.ProcessSchematicComponent),
      },
      { path: 'alarms', loadComponent: () => import('./features/alarms/alarms.component').then((m) => m.AlarmsComponent) },
      {
        path: 'admin/users',
        canActivate: [adminGuard],
        loadComponent: () => import('./features/admin-users/admin-users.component').then((m) => m.AdminUsersComponent),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
