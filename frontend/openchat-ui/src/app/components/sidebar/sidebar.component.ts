import { Component, Input, Output, EventEmitter, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { Conversation } from '../../models/conversation.model';
import { ConversationService } from '../../services/conversation.service';
import { ConfirmService } from '../../services/confirm.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './sidebar.component.html'
})
export class SidebarComponent implements OnInit {
  @Input() userId!: string;
  @Input() activeConversationId: string | null = null;
  @Output() conversationSelected = new EventEmitter<Conversation>();
  @Output() newChat = new EventEmitter<void>();

  conversations = signal<Conversation[]>([]);

  constructor(
    private conversationService: ConversationService,
    private confirmService: ConfirmService,
    private router: Router,
    readonly authService: AuthService
  ) {}

  ngOnInit(): void {
    this.loadConversations();
  }

  loadConversations(): void {
    this.conversationService.getConversations(this.userId).subscribe({
      next: (data) => this.conversations.set(data),
      error: () => {}
    });
  }

  selectConversation(conv: Conversation): void {
    this.conversationSelected.emit(conv);
  }

  onNewChat(): void {
    this.newChat.emit();
  }

  async deleteConversation(event: Event, conv: Conversation): Promise<void> {
    event.stopPropagation();

    const confirmed = await this.confirmService.confirm({
      title: 'Delete conversation',
      message: 'This will permanently delete the conversation and all its messages. This action cannot be undone.',
      confirmLabel: 'Delete',
      cancelLabel: 'Cancel',
      danger: true
    });

    if (!confirmed) return;

    this.conversationService.deleteConversation(conv.id).subscribe({
      next: () => {
        this.conversations.update(list => list.filter(c => c.id !== conv.id));
        if (this.activeConversationId === conv.id) this.newChat.emit();
      }
    });
  }

  goToSettings(): void {
    this.router.navigate(['/settings']);
  }

  addConversation(conv: Conversation): void {
    this.conversations.update(list => [conv, ...list]);
  }

  updateConversation(updated: Conversation): void {
    this.conversations.update(list =>
      list.map(c => c.id === updated.id ? { ...c, ...updated } : c)
        .sort((a, b) => new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime())
    );
  }
}
