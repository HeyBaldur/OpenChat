import {
  Component, OnInit, AfterViewChecked,
  ViewChild, ElementRef, signal, OnDestroy
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { ChatService } from '../../services/chat.service';
import { ConversationService } from '../../services/conversation.service';
import { AuthService } from '../../services/auth.service';
import { ModelService } from '../../services/model.service';
import { ChatMessage } from '../../models/chat.model';
import { InProgressToolCall, ToolCallRecord } from '../../models/tool-event.model';
import { Conversation } from '../../models/conversation.model';
import { OllamaModel } from '../../models/model.model';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { MarkdownMessageComponent } from '../markdown-message/markdown-message.component';
import { UserMessageComponent } from '../user-message/user-message.component';
import { ModelSelectorComponent } from '../model-selector/model-selector.component';
import { ToolIndicatorComponent } from '../tool-indicator/tool-indicator.component';
import { SourcesFooterComponent } from '../sources-footer/sources-footer.component';

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    SidebarComponent, MarkdownMessageComponent,
    UserMessageComponent, ModelSelectorComponent,
    ToolIndicatorComponent, SourcesFooterComponent
  ],
  templateUrl: './chat.component.html'
})
export class ChatComponent implements OnInit, AfterViewChecked, OnDestroy {
  @ViewChild('messagesEnd') private messagesEnd!: ElementRef;
  @ViewChild('scrollContainer') private scrollContainer!: ElementRef<HTMLElement>;
  @ViewChild('sidebar') sidebarRef!: SidebarComponent;
  @ViewChild('textareaEl') private textareaEl!: ElementRef<HTMLTextAreaElement>;

  get userId(): string { return this.authService.userId; }

  messages           = signal<ChatMessage[]>([]);
  streamingContent   = signal<string>('');
  isStreaming        = signal(false);
  pendingMessage     = signal<string>('');
  inputText          = '';
  activeConversation = signal<Conversation | null>(null);
  editingIndex       = signal<number | null>(null);
  selectedModel      = signal<string | null>(null);
  availableModels    = signal<OllamaModel[]>([]);
  modelMissingBanner = signal<string | null>(null);
  inProgressToolCalls = signal<InProgressToolCall[]>([]);
  streamToolCallsUsed = signal<ToolCallRecord[]>([]);

  private shouldScroll = false;
  userScrolledUp = signal(false);
  private abortController?: AbortController;
  private routeSub?: Subscription;
  private streamSubs  = new Subscription();
  private rawBuffer = '';
  private rafId?: number;

  readonly suggestions = [
    {
      label: 'Summarize',
      description: 'Summarize a text or article',
      prompt: 'Summarize the following text for me:',
      icon: `<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
          d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
      </svg>`
    },
    {
      label: 'Explain',
      description: 'Explain a concept simply',
      prompt: 'Explain this concept in simple terms:',
      icon: `<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
          d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m1.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z"/>
      </svg>`
    },
    {
      label: 'Write',
      description: 'Draft an email, message or text',
      prompt: 'Write a professional email about:',
      icon: `<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
          d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"/>
      </svg>`
    },
    {
      label: 'Brainstorm',
      description: 'Generate ideas on any topic',
      prompt: 'Help me brainstorm ideas for:',
      icon: `<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
          d="M13 10V3L4 14h7v7l9-11h-7z"/>
      </svg>`
    }
  ];

  constructor(
    private chatService: ChatService,
    private conversationService: ConversationService,
    private route: ActivatedRoute,
    private router: Router,
    private sanitizer: DomSanitizer,
    readonly authService: AuthService,
    private modelService: ModelService
  ) {}

  safeIcon(svg: string): SafeHtml {
    return this.sanitizer.bypassSecurityTrustHtml(svg);
  }

  ngOnInit(): void {
    this.modelService.getAvailableModels().subscribe(models => {
      this.availableModels.set(models);
      if (!this.activeConversation() && !this.selectedModel()) {
        this.selectedModel.set(this.modelService.resolveDefaultModel(models));
      }
    });

    this.routeSub = this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.loadConversation(id);
      } else {
        this.activeConversation.set(null);
        this.messages.set([]);
        this.streamingContent.set('');
        this.modelMissingBanner.set(null);
        const preferred = this.modelService.resolveDefaultModel(this.availableModels());
        this.selectedModel.set(preferred);
      }
    });
  }

  ngOnDestroy(): void {
    this.routeSub?.unsubscribe();
    this.streamSubs.unsubscribe();
    if (this.rafId) cancelAnimationFrame(this.rafId);
  }

  ngAfterViewChecked(): void {
    if (this.shouldScroll) {
      this.scrollToBottom();
      this.shouldScroll = false;
    }
  }

  private loadConversation(id: string): void {
    this.messages.set([]);
    this.streamingContent.set('');
    this.modelMissingBanner.set(null);

    this.chatService.getMessages(id).subscribe({
      next: (msgs) => {
        this.messages.set(msgs);
        this.shouldScroll = true;
      }
    });

    this.conversationService.getConversations(this.userId).subscribe({
      next: (convs) => {
        const found = convs.find(c => c.id === id);
        if (!found) return;
        this.activeConversation.set(found);

        if (found.model) {
          this.selectedModel.set(found.model);
          const models = this.availableModels();
          if (models.length > 0 && !models.some(m => m.name === found.model)) {
            const defaultModel = this.modelService.resolveDefaultModel(models);
            this.modelMissingBanner.set(found.model);
            if (defaultModel) this.selectedModel.set(defaultModel);
          }
        } else {
          this.selectedModel.set(
            this.modelService.resolveDefaultModel(this.availableModels())
          );
        }
      }
    });
  }

  onModelChange(modelName: string): void {
    const prev = this.selectedModel();
    this.selectedModel.set(modelName);
    this.modelService.savePreferredModel(modelName);
    this.modelMissingBanner.set(null);

    const conv = this.activeConversation();
    if (conv) {
      this.conversationService.updateModel(conv.id, modelName).subscribe();
      if (prev && prev !== modelName && this.messages().length > 0) {
        const model = this.availableModels().find(m => m.name === modelName);
        this.messages.update(list => [...list, {
          userId: this.userId,
          conversationId: conv.id,
          role: 'system',
          content: `Switched to ${model?.displayName ?? modelName}`,
          timestamp: new Date()
        }]);
        this.shouldScroll = true;
      }
    }
  }

  selectConversation(conv: Conversation): void {
    this.router.navigate(['/c', conv.id]);
  }

  startNewChat(): void {
    this.router.navigate(['/']);
  }

  sendMessage(text?: string): void {
    const msg = (text ?? this.inputText).trim();
    if (!msg) return;

    if (this.isStreaming()) {
      this.pendingMessage.set(msg);
      this.inputText = '';
      if (this.textareaEl) this.textareaEl.nativeElement.style.height = 'auto';
      return;
    }

    this.messages.update(list => [...list, {
      userId: this.userId,
      conversationId: this.activeConversation()?.id ?? '',
      role: 'user',
      content: msg,
      timestamp: new Date()
    }]);

    this.inputText = '';
    if (this.textareaEl) this.textareaEl.nativeElement.style.height = 'auto';
    this.streamResponse(msg);
  }

  private flushPending(): void {
    const pending = this.pendingMessage();
    if (pending) {
      this.pendingMessage.set('');
      this.sendMessage(pending);
    }
  }

  private resetStreamState(): void {
    if (this.rafId) { cancelAnimationFrame(this.rafId); this.rafId = undefined; }
    this.isStreaming.set(false);
    this.streamingContent.set('');
    this.inProgressToolCalls.set([]);
  }

  private streamResponse(msg: string): void {
    if (this.isStreaming()) return;

    this.userScrolledUp.set(false);
    this.isStreaming.set(true);
    this.streamingContent.set('');
    this.inProgressToolCalls.set([]);
    this.streamToolCallsUsed.set([]);
    this.rawBuffer = '';
    this.shouldScroll = true;
    this.abortController = new AbortController();

    this.streamSubs.unsubscribe();
    this.streamSubs = new Subscription();

    this.streamSubs.add(this.chatService.toolStart$.subscribe(evt => {
      this.inProgressToolCalls.update(list => [...list, {
        tool:        evt.tool,
        args:        evt.args,
        status:      'running',
        sourceUrl:   null,
        errorReason: '',
        startedAt:   Date.now()
      }]);
      this.shouldScroll = true;
    }));

    this.streamSubs.add(this.chatService.toolEnd$.subscribe(evt => {
      this.inProgressToolCalls.update(list => {
        const reversed = [...list].reverse();
        const revIdx = reversed.findIndex(c => c.tool === evt.tool && c.status === 'running');
        if (revIdx === -1) return list;
        const realIdx = list.length - 1 - revIdx;
        return list.map((c, i) => i === realIdx ? {
          ...c,
          status:      evt.ok ? 'done' : 'failed',
          sourceUrl:   evt.sourceUrl ?? null,
          errorReason: evt.errorReason ?? ''
        } : c);
      });
      this.shouldScroll = true;
    }));

    this.streamSubs.add(this.chatService.done$.subscribe(doneEvt => {
      this.streamSubs.unsubscribe();

      const finalContent = this.rawBuffer.trim();
      this.resetStreamState();

      this.messages.update(list => [...list, {
        userId:         this.userId,
        conversationId: doneEvt.conversationId ?? this.activeConversation()?.id ?? '',
        role:           'assistant',
        content:        finalContent,
        timestamp:      new Date(),
        toolCallsUsed:  doneEvt.toolCallsUsed?.length ? doneEvt.toolCallsUsed : undefined
      }]);

      if (!this.activeConversation() && doneEvt.conversationId) {
        const newConv: Conversation = {
          id:          doneEvt.conversationId,
          userId:      this.userId,
          title:       doneEvt.conversationTitle ?? 'New Chat',
          createdAt:   new Date(),
          updatedAt:   new Date(),
          totalTokens: doneEvt.tokensUsed ?? 0,
          model:       doneEvt.model ?? this.selectedModel() ?? undefined
        };
        this.activeConversation.set(newConv);
        this.sidebarRef?.addConversation(newConv);
        this.router.navigate(['/c', doneEvt.conversationId], { replaceUrl: true });
      } else if (this.activeConversation()) {
        const updated = {
          ...this.activeConversation()!,
          totalTokens: (this.activeConversation()!.totalTokens ?? 0) + (doneEvt.tokensUsed ?? 0),
          updatedAt:   new Date()
        };
        this.activeConversation.set(updated);
        this.sidebarRef?.updateConversation(updated);
      }

      this.shouldScroll = true;
      this.flushPending();
    }));

    this.chatService.streamMessage(
      {
        userId:         this.userId,
        conversationId: this.activeConversation()?.id,
        message:        msg,
        model:          this.selectedModel() ?? undefined
      },
      (token) => {
        this.rawBuffer += token;
        if (!this.rafId) {
          this.rafId = requestAnimationFrame(() => {
            this.streamingContent.set(this.rawBuffer);
            this.shouldScroll = true;
            this.rafId = undefined;
          });
        }
      },
      () => {
        this.streamSubs.unsubscribe();
        this.resetStreamState();
        this.rawBuffer = '';
        this.messages.update(list => [...list, {
          userId:         this.userId,
          conversationId: this.activeConversation()?.id ?? '',
          role:           'assistant',
          content:        'Connection error. Make sure the backend and Ollama are running.',
          timestamp:      new Date()
        }]);
        this.shouldScroll = true;
        this.flushPending();
      },
      () => {
        this.streamSubs.unsubscribe();
        const partial = this.rawBuffer.trim();
        this.resetStreamState();
        this.messages.update(list => [...list, {
          userId:         this.userId,
          conversationId: this.activeConversation()?.id ?? '',
          role:           'assistant',
          content:        partial,
          timestamp:      new Date(),
          stopped:        true
        }]);
        this.rawBuffer = '';
        this.shouldScroll = true;
        this.pendingMessage.set('');
      },
      this.abortController.signal
    );
  }

  stopStreaming(): void {
    this.abortController?.abort();
  }

  editMessage(index: number, newContent: string): void {
    this.editingIndex.set(null);
    this.messages.update(list => list.slice(0, index));
    this.sendMessage(newContent);
  }

  regenerateResponse(): void {
    const list = this.messages();
    const lastAssistantIdx = list.map(m => m.role).lastIndexOf('assistant');
    if (lastAssistantIdx === -1) return;
    const userMsg = list[lastAssistantIdx - 1];
    if (!userMsg || userMsg.role !== 'user') return;
    this.messages.update(l => l.slice(0, lastAssistantIdx));
    this.streamResponse(userMsg.content);
  }

  autoResize(): void {
    const el = this.textareaEl?.nativeElement;
    if (!el) return;
    el.style.height = 'auto';
    el.style.height = el.scrollHeight + 'px';
  }

  onKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }

  onScroll(): void {
    const el = this.scrollContainer?.nativeElement;
    if (!el) return;
    const distanceFromBottom = el.scrollHeight - el.scrollTop - el.clientHeight;
    this.userScrolledUp.set(distanceFromBottom > 80);
  }

  scrollToBottomNow(): void {
    this.userScrolledUp.set(false);
    try { this.messagesEnd.nativeElement.scrollIntoView({ behavior: 'smooth' }); } catch {}
  }

  private scrollToBottom(): void {
    if (this.userScrolledUp()) return;
    try { this.messagesEnd.nativeElement.scrollIntoView({ behavior: 'smooth' }); } catch {}
  }
}
