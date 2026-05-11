import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { SidebarComponent } from '../../components/sidebar/sidebar.component';
import { MermaidComponent } from '../../shared/components/mermaid/mermaid.component';
import { Conversation } from '../../models/conversation.model';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-docs',
  standalone: true,
  imports: [SidebarComponent, MermaidComponent],
  templateUrl: './docs.component.html'
})
export class DocsComponent {
  get userId(): string { return this.authService.userId; }

  constructor(private router: Router, private authService: AuthService) {}

  readonly systemContextDiagram = `
flowchart TB
    User(["User / Browser"])
    subgraph FE["Frontend  •  :4200"]
        Angular["Angular 21 SPA<br/>Tailwind CSS · ngx-markdown"]
    end
    subgraph BE["Backend  •  :5124"]
        API[".NET 9 Web API<br/>Controllers · Services · Repos"]
        DB[("MongoDB<br/>conversations · chatmessages<br/>logs · users · allowed_domains")]
    end
    subgraph AI["Local AI  •  :11434"]
        Ollama["Ollama<br/>qwen2.5:7b · llama3.1:8b"]
    end
    Web(["🌐 External Web<br/>angular.dev · react.dev · ..."])
    User <-->|HTTPS| FE
    FE <-->|REST / SSE| API
    API <-->|MongoDB Driver| DB
    API <-->|HTTP streaming| Ollama
    API -->|HTTPS fetch<br/>allowlist-validated| Web
  `;

  readonly backendArchDiagram = `
flowchart TB
    subgraph API["API Layer"]
        C1["ChatController"]
        C2["ConversationController"]
        C3["ModelsController"]
        C4["AllowlistController"]
        C5["AuthController"]
    end
    subgraph SVC["Service Layer"]
        S1["ChatService"]
        S2["AgenticChatService"]
        S3["OllamaService"]
        S4["ModelCatalogService"]
        S5["AllowlistService"]
        S6["WebFetcherService"]
        S7["ToolRegistry"]
        S8["AuthService"]
    end
    subgraph REPO["Repository Layer"]
        R1["ChatRepository"]
        R2["ConversationRepository"]
        R3["LogRepository"]
        R4["UserRepository"]
        R5["AllowedDomainRepository"]
    end
    subgraph DB["MongoDB Collections"]
        D1[("chatmessages")]
        D2[("conversations")]
        D3[("logs")]
        D4[("users")]
        D5[("allowed_domains")]
    end
    C1 --> S1
    C2 --> R2
    C3 --> S4
    C4 --> S5
    C5 --> S8
    S1 --> S2
    S1 --> S3
    S1 --> R1
    S1 --> R2
    S1 --> R3
    S2 --> S3
    S2 --> S7
    S7 --> S6
    S6 --> S5
    S5 --> R5
    S8 --> R4
    R1 --> D1
    R2 --> D2
    R3 --> D3
    R4 --> D4
    R5 --> D5
  `;

  readonly frontendArchDiagram = `
flowchart TB
    subgraph App["App Bootstrap"]
        Routes["app.routes.ts<br/>Standalone routing"]
        Config["app.config.ts<br/>HttpClient · Markdown · Router"]
    end
    subgraph Features["Features"]
        Chat["ChatComponent<br/>/ and /c/:id"]
        Docs["DocsComponent<br/>/docs"]
        Auth["AuthComponent<br/>/login · /signup"]
        Settings["SettingsComponent<br/>/settings · /settings/allowed-domains"]
    end
    subgraph Components["Shared & UI Components"]
        Sidebar["SidebarComponent"]
        ModelSel["ModelSelectorComponent"]
        ToolInd["ToolIndicatorComponent"]
        SrcFoot["SourcesFooterComponent"]
        Markdown["MarkdownMessageComponent"]
        Mermaid["MermaidComponent"]
    end
    subgraph Services["Services"]
        ChatSvc["ChatService<br/>SSE + tool events"]
        ConvSvc["ConversationService"]
        ModelSvc["ModelService"]
        AllowSvc["AllowlistService"]
        AuthSvc["AuthService"]
    end
    App --> Features
    Features --> Components
    Features --> Services
  `;

  readonly agenticFlowDiagram = `
sequenceDiagram
    actor User
    participant UI as Angular UI
    participant API as .NET 9 API
    participant DB as MongoDB
    participant LLM as Ollama
    participant Web as External Web

    User->>UI: Type message + Enter
    UI->>API: POST /chat/stream
    API->>DB: Save ChatMessage role=user
    API->>DB: Resolve or create Conversation
    API->>DB: Load last 20 messages
    API->>API: Check model.supportsToolCalling

    loop Agentic loop (max 3 tool calls)
        API->>LLM: messages + tools list (stream=false)
        LLM-->>API: tool_calls OR final content
        API-->>UI: SSE event tool_start
        API->>API: Validate URL via AllowlistService
        API->>Web: HTTPS fetch (10s timeout, 2MB cap)
        Web-->>API: HTML response
        API->>API: Extract markdown via AngleSharp+ReverseMarkdown
        API->>API: Truncate to 8000 chars + prompt hardening
        API-->>UI: SSE event tool_end + sourceUrl
        API->>LLM: Append hardened tool result
    end

    loop SSE token stream
        LLM-->>API: JSON chunk token
        API-->>UI: SSE event token
        UI-->>User: Render live
    end

    API->>DB: Save ChatMessage role=assistant + toolCallsUsed
    API->>DB: Update Conversation tokens and title
    API->>DB: Save Log token usage entry
    API-->>UI: SSE event done + metadata
    UI->>UI: Commit message + tool records to signal
  `;

  readonly dataModelDiagram = `
erDiagram
    User {
        ObjectId Id PK
        string Username
        string Email
        string PasswordHash
        string Role
        datetime CreatedAt
    }
    Conversation {
        ObjectId Id PK
        string UserId FK
        string Title
        string Model
        datetime CreatedAt
        datetime UpdatedAt
        int TotalTokens
    }
    ChatMessage {
        ObjectId Id PK
        ObjectId ConversationId FK
        string UserId
        string Role
        string Content
        array ToolCallsUsed
        datetime Timestamp
        bool Stopped
    }
    Log {
        ObjectId Id PK
        ObjectId ConversationId FK
        string UserId
        string Model
        int PromptTokens
        int CompletionTokens
        int TotalTokens
        datetime Timestamp
    }
    AllowedDomain {
        ObjectId Id PK
        string UserId FK
        string Domain
        bool Enabled
        string Category
        string Description
        bool AllowSubdomains
        string AddedBy
        datetime AddedAt
        datetime UpdatedAt
    }
    User ||--o{ Conversation : owns
    User ||--o{ AllowedDomain : configures
    Conversation ||--o{ ChatMessage : contains
    Conversation ||--o{ Log : tracks
  `;

  readonly toolCallingLoopDiagram = `
flowchart TD
    A["User message received"] --> B["Build messages list\nwith system prompt + history"]
    B --> C{"Model supports\ntool calling?"}
    C -->|No| G["Stream tokens directly\nvia OllamaService"]
    C -->|Yes| D["ChatWithToolsAsync\nOllama + tools list"]
    D --> E{"Response has\ntool_calls?"}
    E -->|No — final content| G
    E -->|Yes| F{"Tool call count\nless than 3?"}
    F -->|No| H["Inject limit notice"] --> G
    F -->|Yes| I["Emit SSE tool_start to UI"]
    I --> J["Execute fetch_url\nvalidate → fetch → extract"]
    J --> K["Wrap with BuildHardenedToolMessage\nEmit SSE tool_end (sourceUrl + preview)"]
    K --> L["Increment counter\nAppend to messages"] --> D
    G --> M["Emit SSE done\nwith toolCallsUsed metadata"]
  `;

  selectConversation(conv: Conversation): void {
    this.router.navigate(['/c', conv.id]);
  }

  startNewChat(): void {
    this.router.navigate(['/']);
  }
}
