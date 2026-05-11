# Page.Ui Backend

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-Backend-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://learn.microsoft.com/aspnet/core/)
[![SignalR](https://img.shields.io/badge/SignalR-Realtime-111111?style=for-the-badge&logo=dotnet&logoColor=white)](https://learn.microsoft.com/aspnet/core/signalr/introduction)
[![GraphQL](https://img.shields.io/badge/GraphQL-API-E10098?style=for-the-badge&logo=graphql&logoColor=white)](https://graphql.org/)
[![HotChocolate](https://img.shields.io/badge/HotChocolate-GraphQL-111111?style=for-the-badge)](https://chillicream.com/docs/hotchocolate)
[![WebSockets](https://img.shields.io/badge/WebSockets-Enabled-00C853?style=for-the-badge)](#)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-17-316192?style=for-the-badge&logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Redis](https://img.shields.io/badge/Redis-7-DC382D?style=for-the-badge&logo=redis&logoColor=white)](https://redis.io/)
[![RabbitMQ](https://img.shields.io/badge/RabbitMQ-3-FF6600?style=for-the-badge&logo=rabbitmq&logoColor=white)](https://www.rabbitmq.com/)
[![MinIO](https://img.shields.io/badge/MinIO-Object_Storage-C72E49?style=for-the-badge&logo=minio&logoColor=white)](https://min.io/)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?style=for-the-badge&logo=docker&logoColor=white)](https://www.docker.com/)
[![Nginx](https://img.shields.io/badge/Nginx-Edge_Routing-009639?style=for-the-badge&logo=nginx&logoColor=white)](https://nginx.org/)
[![JWT](https://img.shields.io/badge/JWT-Auth-000000?style=for-the-badge)](#)
[![Rate Limiting](https://img.shields.io/badge/Rate_Limiting-Enabled-00C853?style=for-the-badge)](#)
[![Architecture](https://img.shields.io/badge/Architecture-Microservices-111111?style=for-the-badge)](#)
[![Security](https://img.shields.io/badge/Security-Hardened-00C853?style=for-the-badge)](#)

## Repository Structure

Required submission shape:

```text
/src   -> Source code
/exe   -> Executable files, if applicable
README.md
```

Current project source is included in this repository as .NET solution/project directories plus Docker runtime files:

```text
Page.Ui.sln
Page.Ui.Domain/
Page.Ui.Application/
Page.Ui.Infrastructure/
Page.Ui.Presentation/
Page.Ui.Worker.Ai/
Page.Ui.SvelteRender/
docker-compose.yml
nginx.conf
docs/
README.md
```

No pre-built executable is required to run this web application. The supported setup path is source code compilation and Docker Compose deployment.

## Source Code Compilation

This section is the required setup path for running the project from source.

### Prerequisites and Dependencies

Programming languages and runtimes:

- .NET SDK `10.0.x`
- Node.js `20.x` or newer for the Svelte render worker dependencies
- npm, bundled with Node.js

Frameworks and libraries:

- ASP.NET Core / .NET
- Entity Framework Core
- HotChocolate GraphQL
- SignalR
- MassTransit
- Svelte compiler inside the render sandbox
- Tailwind CSS local compiler inside the render worker

Required tools:

- Docker Desktop or Docker Engine with Docker Compose v2
- Git
- PowerShell, Bash, or another shell capable of running Docker and .NET commands

Runtime services started by Docker Compose:

- PostgreSQL `17`
- Redis
- RabbitMQ with management UI
- MinIO
- Nginx
- Page.Ui API service
- Page.Ui AI worker
- Page.Ui Svelte render service
- Page.Ui isolated Node render sandbox

System requirements:

- Windows, macOS, or Linux with Docker support
- Recommended RAM: 8 GB minimum, 16 GB preferred
- Free disk space: at least 5 GB for containers, packages, generated artifacts, and database volumes

External services:

- External AI API is optional for local startup.
- If `AI_MODEL_API_BASE_URL` is empty, the worker uses the configured failure/fallback behavior instead of a real external AI generation service.
- SMTP is only needed for real email delivery flows.

### Installation Steps

1. Clone the repository:

```bash
git clone <repository-url>
cd Page.Ui
```

2. Restore .NET dependencies:

```bash
dotnet restore Page.Ui.sln
```

3. Install Node render worker dependencies:

```bash
cd Page.Ui.SvelteRender/NodeWorker
npm install
cd ../..
```

4. Configure environment values.

For local development, Docker Compose supplies defaults for the internal services. Create or update `.env` only when you need to override defaults such as database passwords, API keys, SMTP, or the external AI API URL.

### Compilation Steps

Build the full .NET solution:

```bash
dotnet build Page.Ui.sln
```

Build containers:

```bash
docker compose build
```

### Run Instructions

Start the full local stack:

```bash
docker compose up --build
```

Start in the background:

```bash
docker compose up --build -d
```

Useful local URLs:

- API / GraphQL: `http://localhost/graphql`
- RabbitMQ management: `http://localhost:15672` (`guest` / `guest`)
- MinIO console: `http://localhost:9001`
- Rendered preview links: `http://localhost/runs/<publicRunToken>/preview.html`

Stop the stack:

```bash
docker compose down
```

### Environment Setup and Configuration

Important environment variables:

```text
DB_PASSWORD                    PostgreSQL password used by compose
MINIO_ACCESS_KEY               MinIO access key, defaults to minioadmin
MINIO_SECRET_KEY               MinIO secret key, defaults to minioadmin
SVELTE_RENDER_API_KEY          internal API key for worker -> renderer
AI_MODEL_API_BASE_URL          optional external AI API base URL
AI_MODEL_API_KEY               optional shared secret sent to the external AI API
AI_INTERNAL_JWT_ISSUER         internal JWT issuer, defaults to Page.Ui.Worker.Ai
AI_INTERNAL_JWT_AUDIENCE       internal JWT audience, defaults to AiModelApi
PAGE_UI_ALLOWED_STYLESHEET_HOSTS optional comma-separated external stylesheet host allowlist
PAGE_UI_TAILWIND_MAX_CSS_BYTES optional generated Tailwind CSS size limit
```

Database setup:

- PostgreSQL is started by Docker Compose.
- Application services are configured to apply EF Core migrations on startup in the local compose setup.
- No separate manual database creation is required for the default local Docker workflow.

Storage setup:

- MinIO is started by Docker Compose.
- Required buckets are created by the compose bucket initialization job and by services when needed.

AI API setup:

- To run with a real external AI API, set `AI_MODEL_API_BASE_URL`.
- The AI API must accept `POST /api/generate`, verify the worker bearer token, upload generated files through `/api/ai-dev/upload/presign`, then call `/api/ai-dev/render-trigger`.
- Generated multi-page HTML may link to local generated CSS/JS files; the renderer resolves those links from the submitted source bundle.
- Tailwind CDN references are stripped and Tailwind CSS is compiled locally by the renderer.

## Pre-Built Executable Setup

No pre-built executable package is currently provided.

This is a Dockerized web application. Use the source-code compilation and Docker Compose instructions above.

If executable artifacts are added later, place them under `/exe` and document:

- download or artifact location;
- installation steps;
- required runtime prerequisites;
- launch command;
- configuration requirements.

## Common Tools and Deployment Platforms

Recommended deployment/build tooling:

- Docker / Docker Compose for local and containerized deployment
- GitHub Actions or GitLab CI for CI/CD
- Container registry for built service images
- Managed PostgreSQL, Redis, RabbitMQ, and S3-compatible object storage for production

Vercel or Netlify are suitable only for separate static/frontend hosting. This backend stack requires containerized services and databases, so the core backend should be deployed on infrastructure that supports long-running containers.

## Implementation Status Snapshot

### Implemented
- AI/chat pipeline now uses:
  - `USER_MESSAGE`
  - `AI_MESSAGE`
  - `THINKING`
  - `AI_RUN`
- message metadata foundation:
  - `Title`
  - `ClientRequestId`
- canonical chat UI versioning:
  - `AiRun`
  - `AiRunFile`
  - current/superseded version state
- MinIO-backed AI source storage in `ai-runs`
- renderer metadata indexing:
  - Postgres source of truth
  - Redis cache/index acceleration
  - raw `input/` files stored with each run
- opaque public run URLs:
  - `/runs/{publicRunToken}/preview.html`
- worker-side signed internal JWT generation for worker -> AI API calls
- AI API integration surface:
  - worker -> AI API `POST /api/generate`
  - `/api/ai-dev/upload/presign`
  - `/api/ai-dev/render-trigger`
- renderer support for multi-page HTML/CSS/JS source bundles with local link resolution
- Tailwind CDN stripping with local Tailwind compilation inside the render sandbox

### Partially implemented / still remaining
- end-to-end validation against a real external AI API that verifies the signed internal JWT
- historical render-run backfill / reconciliation tooling
- broader automated tests for the full worker/storage/renderer flow

## Architecture Diagrams

### 1) Runtime / Container Topology (docker compose)

```mermaid
graph TD
U[Browser] --> N[Nginx :80]

N --> API[auth-service<br/>Page.Ui.Presentation :8080]
N --> R[svelte-render<br/>Page.Ui.SvelteRender :8080<br/>via /runs/*]
N --> M[minio :9000<br/>via /minio/*]

API --> PG[(PostgreSQL 17)]
API --> RD[(Redis)]
API --> MQ[(RabbitMQ)]
API --> M
API --> DP[(RSA/DataProtection keys volumes)]

W[worker-ai<br/>Page.Ui.Worker.Ai] --> PG
W --> RD
W --> MQ
W --> R

MQ --> W
MQ --> API
R --> SB[svelte-render-sandbox<br/>Node.js :4000]
R --> RUNS[(render runs/result cache)]
CB[create-bucket init job] --> M
```

### 2) Edge Routing + Protocols (Nginx)

```mermaid
sequenceDiagram
participant U as Browser
participant N as Nginx (:80)
participant API as auth-service (:8080)
participant R as svelte-render (:8080)
participant M as minio (:9000)

U->>N: POST /graphql/ (queries + mutations)
N->>API: proxy_pass
API-->>U: JSON

U->>N: WS /graphql (subscriptions)
N->>API: proxy_pass + Upgrade
API-->>U: subscription events

U->>N: WS /hubs/chat (SignalR)
N->>API: proxy_pass + Upgrade
API-->>U: hub events

U->>N: GET /api/upload/presign?fileName=...
N->>API: proxy_pass
API-->>U: uploadUrl + downloadUrl (presigned)

U->>N: PUT /minio/chat-uploads/userId/objectKey?X-Amz-...
N->>M: proxy_pass /minio/*
M-->>U: 200/204

U->>N: GET /runs/publicRunToken/preview.html
N->>R: proxy_pass /runs/*
R-->>U: static HTML/CSS/JS
```

### 3) auth-service (Page.Ui.Presentation) Components

```mermaid
graph TB
subgraph API[auth-service]
GQL[HotChocolate GraphQL\nqueries/mutations/subscriptions]
WS[GraphQL WS auth\nChatSocketSessionInterceptor]
HUB[SignalR ChatHub\n/hubs/chat]
UP[UploadController\n/api/upload/presign]
AUTH[Identity + JWT auth]
CHAT[ChatService\nrate limits + createChat lock]
AUTHSVC[AuthService]
MAIL[AuthEmailRequested consumer]
RTC[Realtime consumers\nChatMessageCreated + AiResponseMessageGenerated]
OUTBOX[EF bus outbox]
end

subgraph State
PG[(PostgreSQL)]
RD[(Redis)]
MQ[(RabbitMQ)]
M[(MinIO chat-uploads)]
end

GQL --> CHAT
GQL --> AUTHSVC
GQL --> AUTH
WS --> AUTH
UP --> RD
UP --> M
CHAT --> PG
CHAT --> RD
CHAT --> M
CHAT --> OUTBOX
AUTH --> PG
AUTHSVC --> PG
MAIL --> MQ
HUB <--> RD
GQL <--> RD
OUTBOX --> PG
RTC --> PG
RTC <--> MQ
RTC --> HUB
RTC --> GQL
```

### 4) Chat + AI Render Flow (createChat/createMessage + AI callback)

```mermaid
sequenceDiagram
participant U as User
participant API as auth-service
participant RD as Redis
participant DB as PostgreSQL
participant MQ as RabbitMQ
participant RT as Realtime consumers
participant W as worker-ai
participant R as svelte-render

U->>API: createChat(input + initialUserMessage)
API->>RD: write-rate limit + distributed createChat lock
API->>DB: Begin transaction
API->>DB: Insert Chat with OwnerUserId
API->>DB: Insert initial Message + outbox rows
DB-->>MQ: Deliver ChatMessageCreated after commit
MQ->>RT: Broadcast initial message
MQ->>W: Consume ChatMessageCreated
W->>RD: short AI rate limit
W->>AI: POST /api/generate with signed internal JWT
AI->>API: GET /api/ai-dev/upload/presign
AI->>M: PUT generated source files to ai-runs
AI->>API: POST /api/ai-dev/render-trigger
API->>MQ: Publish TriggerAiRunRender
MQ->>W: Consume TriggerAiRunRender
W->>R: POST /api/render-objects
R-->>W: opaque public preview link
W-->>MQ: Publish AiResponseMessageGenerated
MQ->>API: Persist + rebroadcast AI_RUN
API-->>U: createChat payload (new chatId + chatKey each call)

U->>API: createMessage(chatKey, content, attachmentUrl?)
API->>RD: write-rate limit
API->>DB: Insert Message + outbox rows
DB-->>MQ: Deliver ChatMessageCreated after commit
MQ->>RT: Broadcast message
MQ->>W: Consume ChatMessageCreated
W->>RD: short AI rate limit
W->>AI: POST /api/generate with signed internal JWT
AI->>API: GET /api/ai-dev/upload/presign
AI->>M: PUT generated files to ai-runs via presigned URLs
AI->>API: POST /api/ai-dev/render-trigger
API->>MQ: Publish TriggerAiRunRender
MQ->>W: Consume TriggerAiRunRender
W->>R: POST /api/render-objects
R-->>W: opaque public preview link
W-->>MQ: Publish AiResponseMessageGenerated
MQ->>API: Persist + rebroadcast AI_RUN
```

### 5) Auth Email Delivery Flow

```mermaid
sequenceDiagram
participant U as User
participant API as auth-service
participant DB as PostgreSQL
participant MQ as RabbitMQ
participant MAIL as auth email consumer
participant SMTP as SMTP provider

U->>API: register / forgotPassword / resendVerification
API->>API: Validate input + apply Redis rate limits
API->>DB: Persist auth state + outbox rows
DB-->>MQ: Deliver AuthEmailRequested after commit
API-->>U: success response
MQ->>MAIL: Consume AuthEmailRequested
MAIL->>SMTP: Connect + authenticate + send
SMTP-->>MAIL: delivery result / retry
```

### 6) Attachment Upload + Message Validation Flow

```mermaid
sequenceDiagram
participant U as User
participant API as auth-service
participant M as MinIO
participant DB as PostgreSQL
participant MQ as RabbitMQ
participant RT as Realtime consumers

U->>API: GET /api/upload/presign?fileName=photo.png
API->>API: Validate filename + rate limit
API->>M: Ensure bucket exists on first use
API-->>U: uploadUrl + downloadUrl (signed for user folder)

U->>M: PUT uploadUrl (image bytes)
M-->>U: 200/204

U->>API: createMessage(..., attachmentUrl=downloadUrl)
API->>API: Validate attachment URL format
API->>M: StatObject(chat-uploads/userId/objectKey)
API->>DB: Save message (stores stable path without query)
API->>MQ: Publish ChatMessageCreated
MQ->>RT: Broadcast message with attachmentUrl
```

### 7) Database Schema

Current chat-related tables after the ownership reshape:

- `AspNetUsers`
- `Chats`
- `Messages`

Chat shape:

- `Chats.OwnerUserId` points to `AspNetUsers.Id`
- `Messages.ChatId` points to `Chats.Id`
- `Messages.SenderId` points to `AspNetUsers.Id`
- `ChatParticipants` is no longer part of the target schema

Other persisted runtime state still includes:

- ASP.NET Identity tables
- refresh/auth tables
- MassTransit outbox/inbox tables

### 8) Chat API Surface

Current chat operations:

- GraphQL queries:
  - `chats`
  - `searchChats`
  - `chat(chatKey)`
  - `messages(chatKey)`
  - `searchMessages(query, chatKey?)`
- GraphQL mutations:
  - `createChat`
  - `createMessage`
  - `renameChat`
  - `deleteChat`
- GraphQL subscription:
  - `onMessageCreated(chatKey)`
- SignalR hub:
  - `JoinChat(chatKey)`
  - `LeaveChat(chatKey)`
- REST:
  - `GET /api/upload/presign`
  - `GET /api/ai-dev/upload/presign`
  - `POST /api/ai-dev/render-trigger`

Pagination and filtered-list guidance:

- chat and message connections use explicit server paging defaults
  - default page size: `20`
  - max page size: `50`
- GraphQL cost analysis is enabled on the API
- for filtered chat lists, prefer `searchChats` over generic `chats(where, order)` unless richer filter composition is specifically required

### 9) Render Service Internals (Page.Ui.SvelteRender)

```mermaid
graph TB
subgraph R[svelte-render]
AuthMW[API key middleware\nX-Render-Api-Key]
RC[RenderController\nPOST /api/render-objects]
RF[RenderController\nPOST /api/render-form]
Hash[SHA-256 input hash -> runId]
Cache[result.json cache by runId]
RS[SandboxRenderService]
Static[Static files\nGET /runs/*]
Runs[Runs directory\nNodeWorker/runs/runId]
end

subgraph Sandbox[Isolated Node Sandbox]
Worker[runner.js TCP Server :4000]
Svelte[Pre-warmed Svelte Compiler/SSR]
Temp[tmpfs /render_jobs]
end

AuthMW --> RC
AuthMW --> RF
RC --> Hash --> Cache
RF --> Hash
RC --> RS
RF --> RS
RS -- TCP Protocol --> Worker
Worker --> Svelte
Worker --> Temp
RS -- Copy Artifacts --> Runs
Static --> Runs
```

### 10) Project Layers / Dependencies

```mermaid
graph BT
Domain[Page.Ui.Domain]
App[Page.Ui.Application] --> Domain
Infra[Page.Ui.Infrastructure] --> App
Infra --> Domain
Pres[Page.Ui.Presentation] --> App
Pres --> Infra
Worker[Page.Ui.Worker.Ai] --> App
Worker --> Infra
Render[Page.Ui.SvelteRender]
```

## Current Behavior

- Each `createChat` call now creates a new chat with a unique internal `chatId` and a public `chatKey`.
- Each chat belongs to exactly one owner user through `OwnerUserId`.
- `createChat` requires `initialUserMessage`; empty chats are not supported.
- The initial message from `createChat` is saved and published into the same AI response pipeline.
- Chat access checks for GraphQL, subscriptions, and SignalR are owner-based instead of participant-based.
- Chat timestamps are stored in UTC and converted to the configured client-facing timezone at the API/realtime boundary.
- Default chat display timezone is `Africa/Cairo` via `Chat:DisplayTimeZone`.
- Auth email flows (`register`, `forgotPasswordRequest`, `resendVerification`) now enqueue delivery through MassTransit/outbox instead of waiting on SMTP before responding.
- `createMessage` supports `attachmentUrl` and now validates that:
  - URL is absolute
  - URL targets `/minio/chat-uploads/<object>`
  - object exists in MinIO before message persistence
- Stored attachment links are normalized to a stable path (queryless URL).
- `chat-uploads` bucket is private; all uploads and downloads require valid presigned signatures.
- Assets are isolated in user-specific folders within the bucket (e.g., `chat-uploads/{userId}/...`).
- Upload presign requests warm up bucket existence once per service lifetime, then reuse cached confirmation.
- Chat name search now uses PostgreSQL `ILIKE` with a trigram-backed migration instead of `ToLower().Contains(...)`.
- GraphQL subscription access is cached briefly in-process to reduce repeated DB reads on hot subscription streams.
- Users can rename and delete their own chat rooms through GraphQL.
- AI text replies are persisted as `AI_MESSAGE`.
- Final rendered artifact links are persisted as `AI_RUN`.
- Legacy `Text` / `System` types are no longer used in the active AI/chat pipeline.
- Renderer metadata is indexed in Postgres (`RenderRuns`) and cached in Redis.
- Raw renderer inputs are stored with each run under `input/`.
- Public run links now use an opaque token instead of exposing internal storage path segments.
- Worker-generated AI source files are stored in `ai-runs` using versioned source storage; one version is marked current per chat.
- Worker -> AI API calls now prepare a signed internal JWT bearer token for service-to-service authentication.
- External AI APIs receive `POST /api/generate`, upload generated files through `/api/ai-dev/upload/presign`, then call `/api/ai-dev/render-trigger`.
- AI APIs do not need to call GraphQL `createMessage` or `renameChat` for the normal render path.
- Multi-page generated HTML can link to local generated CSS/JS files; the renderer resolves those links from the submitted source bundle.
- Tailwind CDN references are removed and Tailwind CSS is compiled locally when Tailwind classes/directives are detected.
- When the external AI API is unavailable, the worker emits a failure `AI_MESSAGE` and renders the `Model_Error` fallback page through the normal `AI_RUN` pipeline.
- Read-state tracking via `markMessagesRead` is no longer implemented.

## Svelte Rendering Sandbox

The rendering system uses a persistent, isolated sandbox for performance and security.

### 1. Architectural Design
- **Persistent Sandbox Service**: A long-running Node.js service (`svelte-render-sandbox`) handles compilation and SSR.
- **TCP Communication**: The backend communicates with the sandbox over an internal network using TCP port 4000.
- **Protocol**: A binary length-prefixed JSON protocol is used for high-speed data exchange.

### 2. Security & Hardening
- **Stateless Operation**: Uses `tmpfs` for all temporary files; no persistent storage.
- **Filesystem Isolation**: Container runs with `read_only: true`.
- **Privilege Reduction**: Runs as a non-root user with `cap_drop: ALL`.
- **Network Isolation**: Accessible only on the internal `render-internal` bridge.

## Key Runtime Contracts

- Worker -> Render endpoint: `POST /api/render-objects`
- Required worker header: `X-Render-Api-Key`
- Worker config:
  - `SvelteRender__BaseUrl=http://svelte-render:8080`
  - `SvelteRender__ApiKey=<key>`
- Render config:
  - `ServiceAuth__ApiKey=<same key>`
  - `Sandbox__Endpoint=http://svelte-render-sandbox:4000`

## Getting Started

### 1) Start services

```bash
docker compose up --build
```

### 2) Core endpoints

- GraphQL: `http://localhost/graphql`
- RabbitMQ: `http://localhost:15672` (`guest/guest`)
- MinIO Console: `http://localhost:9001`
- Render artifacts: `http://localhost/runs/<publicRunToken>/preview.html`

### 3) Quick behavior check

1. Open GraphQL playground.
2. Run `createChat` with `initialUserMessage.content` and note returned `chat.chatKey` for follow-up calls.
3. Send `createMessage(chatKey: ...)` and confirm broadcast.
4. For images: `GET /api/upload/presign` -> `PUT uploadUrl` -> `createMessage(attachmentUrl=downloadUrl)`.
5. For AI generation, call `createMessage` and confirm:
   - the worker dispatches the prompt to the external AI API with an internal signed JWT
   - the AI API uses `/api/ai-dev/upload/presign` and `/api/ai-dev/render-trigger`
   - `AI_MESSAGE` and `AI_RUN` are later persisted and rebroadcast
