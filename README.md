# OpenChatAi

A full-stack local AI chat application. Chat in real time with a locally running LLM through a streaming .NET 9 API and an Angular 21 frontend. All conversations stay on your machine — no data reaches any external AI service.

## Features

- **Real-time streaming** — Token-by-token rendering via Server-Sent Events with requestAnimationFrame batching at 60 fps
- **Agentic tool use** — Models with tool-calling support (qwen2.5, llama3.1) can browse the web to answer questions using real content
- **Domain allowlist** — Per-user control over which external domains the AI can fetch. 23 defaults pre-seeded (MDN, Angular, React, Stack Overflow, GitHub, Wikipedia, etc.)
- **Per-conversation model selection** — Switch Ollama models per chat session without restarting the API
- **JWT authentication** — Signup/login with bcrypt-hashed passwords
- **Conversation history** — Persistent sessions with full message history in MongoDB
- **SSRF protection** — All outbound web fetches go through a multi-stage security validation pipeline (scheme check → private-IP rejection → allowlist → content-type → size cap)
- **Internal documentation** — Built-in `/docs` page with live Mermaid architecture diagrams

## Screenshots

## Screenshots

| | |
|:---:|:---:|
| <img src="https://github.com/user-attachments/assets/7f847eeb-1331-44d6-b2fc-c78bdd0ffe2e" alt="Welcome screen" /> | <img src="https://github.com/user-attachments/assets/682edae5-7c37-48a2-9b99-023167c10828" alt="Chat with tool calling and source citation" /> |
| **Welcome screen** — Clean entry point with starter suggestions | **Chat with tool calling** — Live answers backed by source citations |
| <img src="https://github.com/user-attachments/assets/89479fc5-382e-4aae-abc5-d973d5af5f2d" alt="Settings — Allowed Domains management" /> | <img src="https://github.com/user-attachments/assets/d37823a3-44f8-477d-aebb-65517ce6dda4" alt="Architecture documentation page" /> |
| **Allowed Domains** — Per-user allowlist with categories and toggles | **Architecture Docs** — Technical reference embedded in the app |

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Frontend | Angular 21 (standalone components + signals) |
| Backend | .NET 9 Web API (C#, Clean Architecture) |
| Database | MongoDB 7+ |
| AI Engine | Ollama — qwen2.5:7b (default) |
| Auth | JWT Bearer tokens |
| Styling | Tailwind CSS v4 |

## Prerequisites

- [**.NET 9 SDK**](https://dotnet.microsoft.com/download)
- [**Node.js 20+**](https://nodejs.org) and npm 10+
- [**MongoDB Community Server**](https://www.mongodb.com/try/download/community) — running on `localhost:27017`
- [**Ollama**](https://ollama.com) — with at least one model pulled

```bash
# Recommended — supports tool calling (web fetch)
ollama pull qwen2.5:7b

# Alternative with tool calling support
ollama pull llama3.1:8b
```

## Quick Start

A PowerShell script at the project root starts the API and UI together:

```powershell
.\dev.ps1
```

| Key | Action |
|-----|--------|
| `R` | Restart API |
| `U` | Restart UI |
| `Q` | Quit all |

## Manual Setup

### 1. Start MongoDB

```powershell
# Windows — run as a service after install
net start MongoDB

# Or run directly
mongod --dbpath C:\data\db
```

### 2. Backend

```bash
cd backend/OpenChat.API
dotnet run --launch-profile http
```

Runs on `http://localhost:5124` — Swagger UI available at `/swagger` in Development mode.

### 3. Frontend

```bash
cd frontend/openchat-ui
npm install        # first time only
npm start
```

Runs on `http://localhost:4200`.

## Configuration

`backend/OpenChat.API/appsettings.json`:

```json
{
  "MongoDb": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "OpenChatAi"
  },
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "Model": "qwen2.5:7b"
  },
  "Jwt": {
    "Secret": "ch4ng3-th1s-t0-a-str0ng-r4nd0m-64-char-s3cr3t-k3y-b3f0r3-g01ng-pr0d!!",
    "ExpiryDays": "7"
  }
}
```

> **Security:** `Jwt.Secret` is a placeholder. Replace it with a strong random 64-character key before any non-local deployment. For machine-specific overrides, create `appsettings.local.json` (gitignored).

## API Reference

### Authentication

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/auth/signup` | Register a new user |
| `POST` | `/auth/login` | Authenticate and receive a JWT |

### Chat

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/chat/stream` | Stream response via Server-Sent Events |
| `POST` | `/chat` | Single-shot response (no streaming) |
| `GET` | `/chat/conversation/:id/messages` | Load last 100 messages |

### Conversations

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/conversation/:userId` | List all conversations for a user |
| `PUT` | `/conversation/:id/model` | Update the model for a conversation |
| `DELETE` | `/conversation/:id` | Delete conversation and all its messages |

### Models

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/models` | List available Ollama models |

### Domain Allowlist

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/allowlist` | Get all allowed domains |
| `POST` | `/api/allowlist` | Add a domain |
| `PUT` | `/api/allowlist/:id` | Update a domain |
| `DELETE` | `/api/allowlist/:id` | Remove a domain |
| `PATCH` | `/api/allowlist/:id/toggle` | Enable or disable a domain |
| `GET` | `/api/allowlist/test?url=` | Test whether a URL passes the allowlist |

## SSE Event Types

The `/chat/stream` endpoint emits Server-Sent Events:

| Event | Payload | Description |
|-------|---------|-------------|
| `token` | `string` | A single text token |
| `tool_start` | `{ tool, args }` | Tool call started |
| `tool_end` | `{ tool, ok, sourceUrl, preview }` | Tool call completed |
| `done` | `{ conversationId, tokensUsed, toolCallsUsed }` | Response complete |
| `error` | `{ message }` | Fatal error |

## Agentic Tool Use

Models that support tool calling can invoke `fetch_url` to retrieve web content. Each request is validated through:

1. Scheme check — only `http://` and `https://` allowed
2. SSRF protection — private IP ranges and reserved hostnames rejected
3. Domain allowlist — per-user, memory-cached
4. 10 s timeout / 2 MB response cap
5. HTML → Markdown extraction via AngleSharp + ReverseMarkdown
6. Truncation to 8 000 characters

The model is limited to **3 tool calls per response turn**.

## MongoDB Collections

| Collection | Index | Description |
|-----------|-------|-------------|
| `users` | — | Accounts with bcrypt-hashed passwords |
| `conversations` | `(userId ASC, updatedAt DESC)` | Chat sessions with model and token count |
| `chatmessages` | `(conversationId ASC, timestamp ASC)` | Messages including tool call records |
| `logs` | — | Token usage per assistant response |
| `allowed_domains` | — | Per-user domain allowlist |

## Project Structure

```
OpenChatAi/
├── backend/
│   └── OpenChat.API/
│       ├── Controllers/      # HTTP entry points (Chat, Auth, Conversation, Models, Allowlist)
│       ├── Services/         # Business logic (ChatService, AgenticChatService, OllamaService, ...)
│       ├── Repositories/     # MongoDB access (Chat, Conversation, Log, User, AllowedDomain)
│       ├── Tools/            # Tool definitions (fetch_url, WebFetcherService, ToolRegistry)
│       ├── Models/           # Domain entities and DTOs
│       └── appsettings.json
├── frontend/
│   └── openchat-ui/
│       └── src/app/
│           ├── features/     # auth/, docs/, settings/ (with allowed-domains/)
│           ├── components/   # chat, sidebar, model-selector, tool-indicator, sources-footer, ...
│           ├── services/     # chat, conversation, model, allowlist, auth, excel, confirm
│           └── models/       # TypeScript interfaces
├── dev.ps1                   # Quick-start script (starts API + UI)
└── README.md
```

## Troubleshooting

| Problem | Fix |
|---------|-----|
| AI not responding | Run `ollama serve` and confirm `ollama list` shows your model |
| No web tool use | Model must support tool calling — use `qwen2.5:7b` or `llama3.1:8b` |
| MongoDB error | Ensure `mongod` is running on port 27017 |
| CORS error | Ensure backend is on port 5124 |
| Slow responses | Normal for local LLMs on CPU — GPU strongly recommended for 7B+ models |

## Internal Documentation

Open `/docs` in the running app for interactive architecture diagrams (Mermaid), data model ER diagram, agentic flow sequence, and full API reference.
