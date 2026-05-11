import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ConfirmDialogComponent } from './components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, ConfirmDialogComponent],
  template: `
    <div class="fixed inset-0 pointer-events-none overflow-hidden z-0">
      <div class="absolute -top-40 -left-40 w-[600px] h-[600px] rounded-full bg-purple-600 opacity-[0.12] blur-3xl"></div>
      <div class="absolute top-1/3 -right-40 w-[500px] h-[500px] rounded-full bg-blue-500 opacity-[0.10] blur-3xl"></div>
      <div class="absolute -bottom-32 left-1/3 w-[450px] h-[450px] rounded-full bg-cyan-400 opacity-[0.08] blur-3xl"></div>
    </div>
    <div class="relative z-10 h-full">
      <router-outlet />
      <app-confirm-dialog />
    </div>
  `
})
export class App {}
