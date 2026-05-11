# PageUI — AI Inference Service

Production-grade multimodal AI inference system for UI understanding, structured UI extraction, and Prompt-to-UI intelligence.

---

# Overview

PageUI AI Service is the AI inference layer powering the Prompt-to-UI Design Platform. The system analyzes UI screenshots using a fine-tuned Vision-Language Model (VLM) and transforms visual interfaces into structured, machine-readable UI representations.

As the AI Developer on the Prompt-to-UI Design Platform project, I was responsible for the end-to-end design, engineering, training, and deployment of the AI pipeline enabling intelligent UI understanding and analysis. This encompassed the full ML lifecycle — from dataset engineering and model selection through fine-tuning, inference optimization, and production deployment.

---

# user guide
- import run_notebook to kaggle
- run kaggle note book
- take the kaggle api url put on .env
- run docker 


---

# AI System Architecture

```mermaid 
flowchart TD
    A[Backend] --> B[clinet_API]

    %% Left branch
    B -- prompt --> C[chat_rename]

    %% Main generation flow
    B -- prompt --> D[Ideation]
    D -- concept JSON --> E[HTML generator]
    E -- HTML --> F[css_generator]
    F -- styled_HTML --> G[JS_generator]
    G -- interactive_UI --> H[Critic]
    H -- reviewed_code --> I[fixer]
  
    create image looks like that for this maimed char
```

---

# Repository Structure

```text
ai-api/
├── dia/
│   └── schema.py
├── graphql_api/
│   ├── __init__.py
│   └── schema.py
├── graphql_client/
│   ├── __init__.py
│   ├── client.py
│   ├── enums.py
│   └── mutations.py
├── infrastructure/
│   ├── auth.py
│   ├── graphql_gateway.py
│   ├── render_gateway.py
│   ├── storage_gateway.py
│   └── urls.py
├── model/
│   ├── page-ui-train.ipynb
│   └── the-model.ipynb
├── ai_pipeline.py
├── Dockerfile
├── main.py
├── requirements.txt
├── .env
├── .gitignore
├── docker-compose.yml
└── README.md
```

---

# Core Features

- Prompt-to-UI generation pipeline for Web and Mobile apps
- Multi-agent orchestration architecture for autonomous UI synthesis
- AI-powered app ideation and concept generation
- Dynamic session title generation for conversations/projects
- Semantic HTML generation with accessibility-first structure
- Tailwind CSS token generation and responsive styling system
- Event-driven vanilla JavaScript generation
- Automated UI critique and quality-review agent
- Self-healing UI patching and refinement workflow
- Cached intermediate generation stages for faster regeneration
- Incremental HTML/CSS/JS reuse across iterations
- Full-stack UI composition from natural language prompts
- Platform-aware rendering for Web and Mobile targets
- Structured JSON communication between agents
- Modular agent-based architecture with stage isolation
- Intelligent prompt decomposition and task routing
- UI component hierarchy understanding and generation
- Design-token extraction and reusable styling pipelines
- Real-time HTML assembly and output merging
- Context-aware UI enhancement and repair system
- Automated consistency checking across layout and styles
- Accessibility-aware markup generation (ARIA, semantic tags)
- Responsive layout generation with adaptive breakpoints
- Deterministic multi-stage rendering workflow
- Scalable inference orchestration for parallel agents
- GPU-accelerated inference pipeline with CUDA support
- FastAPI-powered API serving and orchestration backend
- Dockerized production deployment architecture
- GraphQL-integrated backend communication layer
- Cache-aware regeneration and partial recomputation system
- Session persistence and generated UI storage pipeline
- Modular infrastructure gateways for rendering and storage
- Extensible adapter architecture for future UI platforms
- AI-driven frontend prototyping from plain English prompts
- End-to-end automated UI engineering workflow
- Production-ready AI UI generation system
- Agentic frontend generation with iterative refinement
- Automated merge-and-fix rendering pipeline
- Scalable microservice-ready architecture for UI generation
- Clean separation between ideation, rendering, validation, and repair stages

---



# Base Model Selection

## gemma-4-31B-it

Hugging Face:

https://huggingface.co/google/gemma-4-31B-it

---

# Inference Pipeline

```text
Client
   ↓
FLASK API
   ↓
gemma-4-31B-it
   ↓
the UI
   ↓
API Response
```

---


# flask Inference Service

## Base URL

```text
docker
```

---


# Source Code Compilation

## Prerequisites and Dependencies

### Programming Language

- Python 3.11+

### ML & AI Frameworks

- PyTorch 2.3.1
- Transformers 4.51.3
- PEFT 0.14.0
- Accelerate 0.34.2

### API & Backend

- Flask 
- Uvicorn 0.29.0
- graphql

### Required Software

- Docker
- NVIDIA CUDA 12.1
- NVIDIA Container Toolkit
- Git

### System Requirements

| Requirement | Minimum |
|---|---|
| GPU | NVIDIA T4 or equivalent |
| VRAM | 16 GB |
| RAM | 16 GB |
| Python | 3.11+ |

---

# Compilation Steps

## Build Docker Image

```bash
docker build -t ui-ai-service .
```

---

# Run Instructions

## Local Development

```bash
uvicorn app.main:app --host 0.0.0.0 --port 7860
```

---

# Performance Metrics

| Metric | Value |
|---|---|
| RAM Usage | 8–12 GB |
| VRAM Usage | 10–14 GB |
| GPU | NVIDIA T4 |

---