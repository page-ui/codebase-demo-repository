# Page.Ui Full Stack

Page.Ui is a Dockerized full-stack prompt-to-UI platform. The repository contains:

- `src/backend`: .NET backend, GraphQL/SignalR API, AI worker, Svelte render service, PostgreSQL/Redis/RabbitMQ/MinIO integration.
- `src/ai/main-ai-service`: Python AI orchestration API that talks to Kaggle/AI generation and backend render/upload endpoints.
- `src/ai/ui-ai-service`: Python FastAPI vision/UI analysis service using Qwen2.5-VL and optional LoRA adapters.
- `src/frontend/static-web`: static web frontend served by Nginx in the full stack.
- `src/frontend/flutter-app`: Flutter web frontend source project.

## Source Code Compilation

These instructions are written for a user starting from a clean machine.

### Prerequisites and Dependencies

Programming languages and versions:

- .NET SDK `10.0.x`
- Python `3.11+`
- Node.js `20.x` or newer with npm
- Flutter SDK `3.10.4+` and Dart SDK compatible with Flutter

Frameworks and libraries:

- ASP.NET Core / .NET 10
- Entity Framework Core
- HotChocolate GraphQL
- SignalR
- MassTransit
- FastAPI `0.111.0`
- Flask `3.0.3`
- Uvicorn `0.29.0`
- PyTorch-compatible GPU runtime for local AI inference
- Transformers `4.51.3`
- PEFT `0.14.0`
- Accelerate `0.34.2`
- Qwen-VL-Utils `0.0.8`
- Flutter packages listed in `src/frontend/flutter-app/pubspec.yaml`
- Svelte compiler and Tailwind CSS dependencies in `src/backend/Page.Ui.SvelteRender/NodeWorker/package.json`

Required software/tools:

- Git
- Docker Desktop or Docker Engine with Docker Compose v2
- PowerShell on Windows for `scripts/docker-up.ps1` or any shell that can run Docker Compose commands
- npm, bundled with Node.js
- pip, bundled with Python
- NVIDIA Container Toolkit if running CUDA/GPU-backed AI containers locally

System requirements:

- OS: Windows with Docker Desktop/WSL2, macOS with Docker Desktop, or Linux with Docker Engine
- RAM: 16 GB recommended for the full stack; 8 GB minimum for backend-only work
- Disk: at least 10 GB free for images, packages, model cache, database volumes, and generated artifacts
- GPU: NVIDIA T4 or equivalent recommended for local `ui-ai-service` inference
- VRAM: 16 GB recommended for local model inference

External services:

- Kaggle notebook/API URL for the main AI generation flow. This must be prepared first and placed in `KAGGLE_API_URL`.
- Hugging Face token in `HUGGING_FACE_HUB_TOKEN` when pulling gated model weights.
- SMTP credentials only if testing real email delivery flows.

Runtime services started by root Docker Compose:

- PostgreSQL `17`
- Redis `7`
- RabbitMQ `3` with management UI
- MinIO object storage
- Nginx gateway
- Backend API service
- Backend AI worker
- Svelte render service and isolated Node render sandbox
- Main AI API
- UI AI analysis service
- Frontend static web service

### Installation Steps

1. Clone the repository:

```bash
git clone <repository-url>
cd Page.Ui.Full
```

2. Configure the Kaggle API URL before starting the full AI flow:

Run the Kaggle notebook for the main AI service first, copy the generated Kaggle API base URL, then set it in the root `.env` file:

```text
KAGGLE_API_URL=<your-kaggle-api-base-url>
```

The root `.env` already contains the key; replace the empty value.

3. Install local dependencies if you want to compile components outside Docker:

```bash
cd src/backend
dotnet restore Page.Ui.sln
cd Page.Ui.SvelteRender/NodeWorker
npm install
cd ../../../..
```

```bash
cd src/ai/main-ai-service/ai-api
python -m pip install -r requirements.txt
cd ../../../..
```

```bash
cd src/ai/ui-ai-service
python -m pip install -r requirements.txt
cd ../../..
```

```bash
cd src/frontend/flutter-app
flutter pub get
cd ../../..
```

4. Configure the environment:

Use the root `.env` file for Docker Compose. Required values for the complete AI flow are:

```text
KAGGLE_API_URL=<your-kaggle-api-base-url>
HUGGING_FACE_HUB_TOKEN=<your-hugging-face-token-if-required>
```

The remaining local development defaults in `.env` are usable as-is for Docker startup.

### Compilation Steps

Recommended full-stack Docker build from the repository root:

```bash
docker compose build
```

Build and start in one command:

```bash
docker compose up --build
```

Windows PowerShell helper:

```powershell
.\scripts\docker-up.ps1
```

The helper runs `docker compose up --build -d` and waits for services to become healthy, running, or completed successfully.

Backend-only compilation:

```bash
cd src/backend
dotnet build Page.Ui.sln
```

Backend Docker images:

```bash
docker compose -f src/backend/docker-compose.yml build
```

Main AI API Docker image:

```bash
cd src/ai/main-ai-service/ai-api
docker build -t page-ui/ai-api:local .
```

UI AI service Docker image:

```bash
cd src/ai/ui-ai-service
docker build -t page-ui/ui-ai-service:local .
```

Flutter web source compilation:

```bash
cd src/frontend/flutter-app
flutter build web
```

### Run Instructions

Start the complete stack from the repository root:

```bash
docker compose up --build
```

Start the complete stack in the background:

```bash
docker compose up --build -d
```

Or on Windows:

```powershell
.\scripts\docker-up.ps1
```

Useful local URLs after startup:

- Backend gateway: `http://localhost`
- Frontend through gateway: `http://localhost`
- Main AI API health: `http://localhost:5000/health`
- UI AI readiness: `http://localhost:8000/ready`
- RabbitMQ management: `http://localhost:15672` (`guest` / `guest` by default)
- MinIO console: `http://localhost:9001`
- GraphQL endpoint: `http://localhost/graphql`
- Rendered previews: `http://localhost/runs/<publicRunToken>/preview.html`

Stop the stack:

```bash
docker compose down
```

Stop and remove local Docker volumes:

```bash
docker compose down -v
```

Run individual services for development:

```bash
cd src/backend
dotnet run --project Page.Ui.Presentation/Page.Ui.Presentation.csproj
```

```bash
cd src/ai/main-ai-service/ai-api
python main.py
```

```bash
cd src/ai/ui-ai-service
uvicorn app.main:app --host 0.0.0.0 --port 8000
```

```bash
cd src/frontend/flutter-app
flutter run -d chrome
```

### Environment Setup & Configuration

Root configuration lives in `.env`.

Important variables:

```text
COMPOSE_PROJECT_NAME             Docker Compose project name
BACKEND_HTTP_PORT                Gateway/backend host port
FRONTEND_HTTP_PORT               Legacy frontend host port setting
AI_API_PORT                      Main AI API host port
UI_AI_PORT                       UI AI service host port
POSTGRES_PORT                    PostgreSQL host port
REDIS_PORT                       Redis host port
RABBITMQ_PORT                    RabbitMQ AMQP host port
RABBITMQ_MANAGEMENT_PORT         RabbitMQ management UI host port
MINIO_API_PORT                   MinIO API host port
MINIO_CONSOLE_PORT               MinIO console host port
DB_PASSWORD                      PostgreSQL password
RABBITMQ_USER                    RabbitMQ username
RABBITMQ_PASSWORD                RabbitMQ password
MINIO_ACCESS_KEY                 MinIO access key
MINIO_SECRET_KEY                 MinIO secret key
JWT_ISSUER                       Backend JWT issuer
JWT_AUDIENCE                     Backend JWT audience
JWT_ACCESS_TOKEN_EXPIRATION_MINUTES
JWT_REFRESH_TOKEN_EXPIRATION_DAYS
SVELTE_RENDER_API_KEY            Internal worker-to-render API key
SMTP_HOST                        SMTP host for email flows
SMTP_PORT                        SMTP port
SMTP_USERNAME                    SMTP username
SMTP_PASSWORD                    SMTP password
SMTP_FROM                        SMTP sender address
AI_API_KEY                       Optional shared secret for AI API auth
AI_INTERNAL_JWT_ISSUER           Worker internal JWT issuer
AI_INTERNAL_JWT_AUDIENCE         Worker internal JWT audience
JWT_SECRET                       AI service JWT secret if enabled
JWT_ALGORITHM                    AI service JWT algorithm
KAGGLE_API_URL                   Kaggle API base URL from the initial Kaggle notebook step
UI_JOB_START_TIMEOUT_SECONDS     Main AI job startup timeout
UI_JOB_POLL_INTERVAL_SECONDS     Main AI job poll interval
UI_JOB_TOTAL_TIMEOUT_SECONDS     Main AI job total timeout
UI_JOB_TRANSIENT_POLL_FAILURE_LIMIT
HUGGING_FACE_HUB_TOKEN           Hugging Face token for model downloads
UI_AI_BASE_MODEL_ID              UI AI base model ID
UI_AI_SHM_SIZE                   Shared memory size for UI AI container
```

Database setup:

- The root Docker Compose file starts PostgreSQL and creates the `pageuidb` database.
- Backend services use `Database__ApplyMigrationsOnStartup=true` in the local Docker setup.
- No manual database creation is required for the default Docker workflow.

Object storage setup:

- MinIO starts automatically.
- The `create-bucket` service creates `chat-uploads` and `ai-runs`.
- Default local MinIO credentials are `minioadmin` / `minioadmin` unless changed in `.env`.

Message broker and cache setup:

- RabbitMQ and Redis start automatically in Docker Compose.
- RabbitMQ management is available at `http://localhost:15672`.

AI setup:

- Run the Kaggle notebook first and copy its API base URL into `KAGGLE_API_URL`.
- Set `HUGGING_FACE_HUB_TOKEN` when the UI AI service needs to download gated model weights.
- Place optional trained LoRA adapter files before building `ui-ai-service`:

```text
src/ai/ui-ai-service/adapters/
+-- mobile/
|   +-- adapter_config.json
|   +-- adapter_model.safetensors
+-- web/
    +-- adapter_config.json
    +-- adapter_model.safetensors
```

If these adapter directories are empty, the UI AI service falls back to the base model.

Frontend setup:

- The full Docker stack serves `src/frontend/static-web` through the `frontend` container and Nginx gateway.
- The Flutter source app is under `src/frontend/flutter-app`.
- For Flutter development, use Chrome as the target device:

```bash
cd src/frontend/flutter-app
flutter run -d chrome
```

### Verification Commands

Run backend build and tests:

```bash
cd src/backend
dotnet build Page.Ui.sln
dotnet test Page.Ui.sln
```

Run Flutter quality checks:

```bash
cd src/frontend/flutter-app
flutter analyze
flutter test
```

Check Docker service status:

```bash
docker compose ps
```

Check health endpoints:

```bash
curl http://localhost:5000/health
curl http://localhost:8000/ready
curl http://localhost/health
```
#   F u l l _ P r o j e c t  
 #   F u l l _ P r o j e c t  
 