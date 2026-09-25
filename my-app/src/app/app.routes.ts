import { Routes } from '@angular/router';
import { Dashboard } from './pages/dashboard/dashboard';

export const routes: Routes = [
  { path: '', component: Dashboard },
  {
    path: 'meeting-rooms',
    loadComponent: () => import('./pages/meeting-rooms/meeting-rooms').then(m => m.MeetingRooms),
  },
  { path: '**', redirectTo: '' },
];
