import { Component } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { SidebarComponent } from '../../components/sidebar/sidebar.component';
import { AuthService } from '../../services/auth.service';
import { Conversation } from '../../models/conversation.model';

interface SettingsTab {
  label:   string;
  icon:    string;
  route:   string;
  enabled: boolean;
}

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [SidebarComponent, RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './settings.component.html'
})
export class SettingsComponent {
  tabs: SettingsTab[] = [
    { label: 'Allowed Domains',   icon: 'shield',   route: 'allowed-domains',   enabled: true  },
    { label: 'Model Parameters',  icon: 'sliders',  route: 'model-parameters',  enabled: false },
    { label: 'Appearance',        icon: 'palette',  route: 'appearance',        enabled: false },
    { label: 'Profile',           icon: 'user',     route: 'profile',           enabled: false },
    { label: 'Data & Privacy',    icon: 'lock',     route: 'data-privacy',      enabled: false },
  ];

  get userId(): string { return this.authService.userId; }

  constructor(private router: Router, private authService: AuthService) {}

  selectConversation(conv: Conversation): void {
    this.router.navigate(['/c', conv.id]);
  }

  startNewChat(): void {
    this.router.navigate(['/']);
  }
}
