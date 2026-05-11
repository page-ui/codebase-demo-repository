# PageUI — AI Inference Service

Production-grade multimodal AI inference system for UI understanding, structured UI extraction, and Prompt-to-UI intelligence.

---

# Overview

PageUI AI Service is the AI inference layer powering the Prompt-to-UI Design Platform. The system analyzes UI screenshots using a fine-tuned Vision-Language Model (VLM) and transforms visual interfaces into structured, machine-readable UI representations.

As the AI Developer on the Prompt-to-UI Design Platform project, I was responsible for the end-to-end design, engineering, training, and deployment of the AI pipeline enabling intelligent UI understanding and analysis. This encompassed the full ML lifecycle — from dataset engineering and model selection through fine-tuning, inference optimization, and production deployment.

---

# AI System Architecture

```text
Image URL
   ↓
Image Fetching & Validation
   ↓
Platform Detection
(Aspect Ratio + Resolution + Optional OCR Voting)
   ↓
Adapter Routing
(Mobile LoRA | Web LoRA)
   ↓
Qwen2.5-VL Multimodal Inference
   ↓
Structured JSON Extraction
   ↓
Schema Validation
   ↓
API Response
```

---

# Repository Structure

```text
ui-ai-service/
│
├── adapters/
│   ├── mobile/
│   └── web/
│
├── app/
│   ├── main.py
│   ├── model.py
│   ├── qwen2.5model.py
│   ├── platform_detector.py
│   ├── router.py
│   └── schemas.py
│
├── Dockerfile
├── docker-compose.yml
├── requirements.txt
└── README.md
```

---

# Core Features

- Multimodal UI understanding using Vision-Language Models
- Structured JSON extraction pipeline
- Platform-aware adapter routing
- Specialized LoRA adapters for mobile and web UIs
- FastAPI-based inference serving
- GPU-accelerated inference with CUDA
- Schema validation and JSON recovery
- Normalized bounding-box extraction
- Typography and layout understanding
- Production-ready Docker deployment

---

# Technology Stack

| Layer | Technology |
|---|---|
| Foundation Model | Qwen2.5-VL-3B-Instruct |
| Fine-Tuning | LoRA (PEFT) |
| Training Framework | LLaMA-Factory |
| ML Framework | PyTorch |
| Inference Framework | Transformers |
| API Framework | FastAPI |
| ASGI Server | Uvicorn |
| Containerization | Docker |
| GPU Runtime | NVIDIA CUDA 12.1 |
| Deployment | Hugging Face Spaces |
| Language | Python 3.11+ |

---

# Base Model Selection

## Qwen2.5-VL-3B-Instruct

Hugging Face:

https://huggingface.co/Qwen/Qwen2.5-VL-3B-Instruct

---

# Specialized LoRA Adapters

Two domain-specialized adapters were engineered independently:

| Adapter | Purpose |
|---|---|
| Mobile Adapter | Mobile UI analysis |
| Web Adapter | Desktop/Web UI analysis |

### Mobile Adapter Specialization

- Vertical layout understanding
- Compact spacing analysis
- Mobile navigation patterns
- Mobile typography extraction

### Web Adapter Specialization

- Multi-column layouts
- Sidebar navigation understanding
- Dense desktop component extraction
- Grid and table recognition

---

# Dataset Engineering

## Training Datasets

| Dataset | Purpose |
|---|---|
| iOS-1K-Mobile-UI-Dataset | Mobile layout understanding |
| mobile-ui-design | Typography and component understanding |
| ShowUI-web | Web UI understanding |

### Dataset Links

#### iOS-1K-Mobile-UI-Dataset
https://huggingface.co/datasets/atharparvezce/iOS-1K-Mobile-UI-Dataset

#### mobile-ui-design
https://huggingface.co/datasets/mrtoy/mobile-ui-design

#### ShowUI-web
https://huggingface.co/datasets/showlab/ShowUI-web

Approximate total dataset size:

- ~8,400 annotated UI screenshots

---

# Annotation Schema

```json
{
  "screen_id": "001",
  "elements": [
    {
      "id": "1",
      "type": "Button",
      "text": "Login",
      "bbox_norm": {
        "x_norm": 0.42,
        "y_norm": 0.71,
        "w_norm": 0.18,
        "h_norm": 0.06
      },
      "is_clickable": true,
      "font_weight": "semibold",
      "font_size_role": "body"
    }
  ]
}
```

---

# Data Preprocessing Pipeline

The preprocessing workflow includes:

1. Image validation and filtering
2. Resolution normalization
3. Bounding-box normalization
4. Annotation cleaning
5. JSON schema standardization
6. Instruction-format conversion
7. Dataset validation and splitting

---

# Platform Detection System

The platform detector automatically classifies screenshots into:

- Mobile UI
- Web UI

### Detection Signals

| Signal | Weight |
|---|---|
| Aspect Ratio | 3 |
| Optional OCR Keyword Scoring | 2 |
| Resolution Heuristics | 1 |

---

# Inference Pipeline

```text
Client
   ↓
FastAPI API
   ↓
Image Validation
   ↓
Platform Detector
   ↓
Adapter Router
   ↓
Qwen2.5-VL Inference
   ↓
Structured JSON Extraction
   ↓
Schema Validation
   ↓
API Response
```

---

# Structured JSON Extraction 

The AI service extracts:

| Dimension | Description |
|---|---|
| Element Classification | Button, Card, InputField, Navbar, etc. |
| Typography Metadata | Weight, role, semantic hierarchy |
| Spatial Coordinates | Normalized bounding boxes |
| Interactivity | Clickable element detection |
| Layout Structure | Hierarchical UI organization |
| Text Content | Visible UI text extraction |

### Extraction Features

- Normalized coordinates
- Deterministic JSON formatting
- Schema validation
- JSON recovery heuristics
- Structured UI representation

---

# Prompt Engineering

The production prompt enforces:

- JSON-only responses
- Structured schema compliance
- Normalized coordinate formatting
- Controlled component vocabulary
- Deterministic output generation

The prompt design underwent multiple iterations to improve reliability on dense UI layouts and reduce malformed JSON outputs.

---

# FastAPI Inference Service

## Base URL

```text
https://zeyad-alaa-pageui-ai-analysis.hf.space
```

---

## Swagger Documentation

```text
https://zeyad-alaa-pageui-ai-analysis.hf.space/docs
```

---

## Main Endpoint

```http
POST /ai/analyze-ui
```

The main AI worker calls this endpoint when its `/api/generate` request contains `triggerMessageAttachmentUrl`. The worker sends the attachment URL as `imageUrl`, then forwards the response to the UI generator as `ui_analysis` together with the original `attachmentUrl`.

### Request Example

```json
{
  "imageUrl": "https://example.com/ui-screenshot.jpg"
}
```

### Response Example

```json
{
  "screen_id": "001",
  "elements": [
    {
      "id": "1",
      "type": "Button",
      "text": "Login",
      "bbox_norm": {
        "x_norm": 0.42,
        "y_norm": 0.71,
        "w_norm": 0.18,
        "h_norm": 0.06
      },
      "is_clickable": true,
      "font_weight": "semibold",
      "font_size_role": "body"
    }
  ]
}
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
- Qwen-VL-Utils 0.0.8

### API & Backend

- FastAPI 0.111.0
- Uvicorn 0.29.0

### Required Software

- Docker
- NVIDIA CUDA 12.1
- NVIDIA Container Toolkit
- Git

### System Requirements

| Requirement | Minimum |
|---|---|
| OS | Ubuntu 22.04 / Windows WSL2 |
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

## Docker Runtime

```bash
docker run --gpus all -p 7860:7860 ui-ai-service
```

---

# Docker Deployment

## Docker Base Image

```dockerfile
FROM nvidia/cuda:12.1.1-cudnn8-runtime-ubuntu22.04
```

### Deployment Features

- CUDA-enabled inference
- GPU acceleration
- Containerized FastAPI service
- Runtime health checks
- Production-ready deployment pipeline

---

# Performance Metrics

| Metric | Value |
|---|---|
| Inference Latency | 7–15 seconds |
| RAM Usage | 8–12 GB |
| VRAM Usage | 10–14 GB |
| GPU | NVIDIA T4 |

---

# Kaggle Training Infrastructure

## Mobile Adapter Notebook

https://www.kaggle.com/code/zeyadpop/qwen25vl-mobileui-lora

---

## Web Adapter Notebook

https://www.kaggle.com/code/zeyadpop/qwen25vl-webui-lora/notebook

---

# Future Improvements

Planned enhancements include:

- OCR-assisted typography extraction
- Quantized deployment
- TensorRT acceleration
- Streaming inference
- Kubernetes autoscaling
- Design-token generation
- UI-to-code synthesis

---

# Screenshots

---

## Swagger API

<img width="1776" height="742" alt="3" src="https://github.com/user-attachments/assets/7e227060-8f69-4a58-b39a-dcbb35526218" />


---

## Hugging Face Deployment

<img width="1920" height="879" alt="4" src="https://github.com/user-attachments/assets/ac94f9b9-fa98-4924-8d41-4de4a04cff7d" />

---
