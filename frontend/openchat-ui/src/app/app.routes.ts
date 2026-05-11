import { Routes } from '@angular/router';
import { ChatComponent } from './components/chat/chat.component';
import { authGuard, guestGuard } from './guards/auth.guard';

export const routes: Routes = [
  { path: 'login',  loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent),   canActivate: [guestGuard] },
  { path: 'signup', loadComponent: () => import('./features/auth/signup/signup.component').then(m => m.SignupComponent), canActivate: [guestGuard] },
  { path: 'c/:id', component: ChatComponent, canActivate: [authGuard] },
  { path: 'docs',   loadComponent: () => import('./features/docs/docs.component').then(m => m.DocsComponent), canActivate: [authGuard] },
  {
    path: 'settings',
    loadComponent: () => import('./features/settings/settings.component').then(m => m.SettingsComponent),
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'allowed-domains', pathMatch: 'full' },
      {
        path: 'allowed-domains',
        loadComponent: () => import('./features/settings/allowed-domains/allowed-domains.component').then(m => m.AllowedDomainsComponent)
      }
    ]
  },
  { path: '', component: ChatComponent, canActivate: [authGuard] },
  { path: '**', redirectTo: '' }
];
