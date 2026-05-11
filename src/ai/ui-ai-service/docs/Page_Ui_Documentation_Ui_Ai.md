# 1\. Introduction

## 1.1 Role Overview

As the AI Developer on the Prompt-to-UI Design Platform project, I was responsible for the end-to-end design, engineering, training, and deployment of the AI pipeline enabling intelligent UI understanding and analysis. This encompassed the full ML lifecycle - from dataset engineering and model selection through fine-tuning, inference optimization, and production deployment.

The AI layer serves as the analytical core of the platform, transforming raw UI screenshots into structured, machine-readable design tokens and component hierarchies that downstream rendering and generation systems consume.

## 1.2 Responsibilities

**Model Engineering**

- Selection and evaluation of foundation vision-language models
- Design of LoRA adapter architecture for parameter-efficient fine-tuning
- Instruction-tuning dataset construction and formatting
- Supervised fine-tuning (SFT) workflow orchestration via LLaMA-Factory

**Inference Systems**

- Adaptive platform detection with weighted voting logic
- Adapter selection routing (mobile vs. web)
- Structured JSON extraction and schema validation
- Prompt engineering for reliable structured output generation

**Data Engineering**

- Multimodal dataset collection and curation across three specialized datasets
- Bounding box normalization and coordinating system design
- Custom JSON schema definition for unified UI representation
- Annotation quality validation and filtering pipelines

**Deployment & Serving**

- FastAPI inference service architecture
- Dockerized GPU-accelerated deployment
- Hugging Face Spaces hosting configuration
- Runtime memory optimization and latency profiling

## 1.3 AI System Capabilities

The trained AI system analyzes UI reference images and produces structured representations across the following analytical dimensions:

| **Capability**              | **Description**                                                         |
| --------------------------- | ----------------------------------------------------------------------- |
| Element Detection           | Identifies and localizes buttons, inputs, navbars, cards, icons, labels |
| Layout Understanding        | Infers compositional hierarchy, grid structure, column layout           |
| Typography Extraction       | Classifies font weight roles, size hierarchies, text semantics          |
| Spacing Analysis            | Detects padding, margin, and density relationships                      |
| Color System Understanding  | Identifies background, foreground, and accent color regions             |
| Clickable Element Detection | Flags interactive elements with is_clickable metadata                   |

## 1.4 Current AI Stack

| **Component**         | **Technology**                |
| --------------------- | ----------------------------- |
| Foundation Model      | Qwen2.5-VL-3B-Instruct        |
| Mobile Adapter        | LoRA fine-tuned adapter (r=8) |
| Web Adapter           | LoRA fine-tuned adapter (r=8) |
| Inference Framework   | Transformers + PEFT           |
| API Layer             | FastAPI + Uvicorn             |
| Containerization      | Docker + CUDA 12.1            |
| Training Orchestrator | LLaMA-Factory                 |
| Training Hardware     | NVIDIA T4 2× CUDA GPUs        |
| Inference Hardware    | NVIDIA T4 GPU                 |

## 1.5 AI Goals

The platform's AI system was designed around four primary goals:

- UI Understanding - Extract semantic meaning from visual UI screenshots
- UI Generation Support - Provide structured tokens for downstream generative modules
- Design Assistance - Surface design system properties (typography, spacing, color)
- Structured UI Extraction - Produce validated, machine-readable JSON for rendering pipelines

# 2\. User Guide - AI Inference Service

## 2.1 Inference Service Overview

The AI inference service exposes a single REST endpoint that accepts a UI image URL, performs platform-aware adapter selection, runs multimodal inference, and returns a structured JSON representation of the UI.

Base URL: [https://zeyad-alaa-pageui-ai-analysis.hf.space/ai/analyze-ui](mailto:https://zeyad-alaa-pageui-ai-analysis.hf.space/ai/analyze-ui):7860

Endpoint: POST /ai/analyze-ui

## 2.2 Step-by-Step Workflow

### Step 1 - Provide a UI Image URL

The service accepts a publicly accessible image URL. The image should be a screenshot of a mobile or web UI. Supported formats: JPEG, PNG,…etc

POST /ai/analyze-ui

Content-Type: application/json

{

"imageUrl": "<https://example.com/ui-screenshot.jpg>"

}

### Step 2 - Platform Detection

Upon receiving the request, the service fetches and analyzes the image to determine its platform type (Mobile or Web). This detection uses a weighted voting algorithm combining aspect ratio analysis, resolution heuristics, and OCR keyword scoring. The detected platform governs which LoRA adapter is loaded for inference.

### Step 3 - Automatic Adapter Selection

| **Detected Platform** | **Adapter Loaded**                |
| --------------------- | --------------------------------- |
| Mobile                | Mobile LoRA Adapter               |
| Web                   | Web LoRA Adapter                  |
| Ambiguous (tie)       | Mobile Adapter (fallback default) |

### Step 4 - AI Inference Execution

The selected adapter is applied over the Qwen2.5-VL-3B-Instruct base model. The image is encoded by the vision encoder and processed alongside a structured system prompt that instructs the model to extract UI elements in a defined JSON schema. Decoding uses greedy sampling with constrained output formatting.

### Step 5 - Structured JSON Extraction

The model's raw text output undergoes post-processing: preamble stripping, JSON block extraction, json.loads() parsing, and heuristic recovery if malformed. Failed recovery returns a structured error response.

### Step 6 - API Response

{

"screen_id": "001",

"elements": \[

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

\]

}

## 2.3 Response Field Reference

| **Field**                   | **Type** | **Description**                                                        |
| --------------------------- | -------- | ---------------------------------------------------------------------- |
| screen_id                   | string   | Unique identifier for the analyzed screen                              |
| elements                    | array    | Array of detected UI component objects                                 |
| elements\[\].id             | string   | Sequential element identifier                                          |
| elements\[\].type           | string   | Component class (Button, InputField, Navbar, Card, Icon, Label, Image) |
| elements\[\].text           | string   | Visible text content of the element (if any)                           |
| elements\[\].bbox_norm      | object   | Normalized bounding box (values 0.0-1.0)                               |
| bbox_norm.x_norm            | float    | Normalized left edge x-coordinate                                      |
| bbox_norm.y_norm            | float    | Normalized top edge y-coordinate                                       |
| bbox_norm.w_norm            | float    | Normalized element width                                               |
| bbox_norm.h_norm            | float    | Normalized element height                                              |
| elements\[\].is_clickable   | boolean  | Whether the element is interactive                                     |
| elements\[\].font_weight    | string   | regular / medium / semibold / bold …etc                                |
| elements\[\].font_size_role | string   | display / heading / subheading / body / caption…etc                    |

## 2.4 Swagger UI Testing

The inference service integrates FastAPI's built-in OpenAPI documentation. Access the interactive Swagger UI at:

<https://zeyad-alaa-pageui-ai-analysis.hf.space/docs>

# 3\. System Design and Architecture

_AI pipeline: Image → Platform Detector → Adapter Router → VLM Inference → JSON Extractor → Response_

## 3.1 Base Model Selection

### Model: Qwen/Qwen2.5-VL-3B-Instruct

HuggingFace: <https://huggingface.co/Qwen/Qwen2.5-VL-3B-Instruct>

**Why Vision-Language Models are Required**

UI screenshots are fundamentally multimodal artifacts. They encode meaning through both visual structure (spatial layout, color, size relationships, iconography) and textual content (labels, placeholders, navigation items). A text-only language model cannot perceive visual attributes - it has no mechanism to identify that a blue rounded rectangle at coordinates (0.42, 0.71) is a Login button with a semibold font.

Vision-Language Models (VLMs) process image tokens and text tokens jointly within a shared attention space, enabling the model to ground textual descriptions to specific visual regions. This capability is a prerequisite for any system that must extract structured metadata from UI screenshots.

**Why Qwen2.5-VL-3B-Instruct Was Selected**

| **Selection Criterion** | **Justification**                                                                |
| ----------------------- | -------------------------------------------------------------------------------- |
| Multimodal Reasoning    | Native vision encoder with high-resolution patch understanding                   |
| JSON Generation         | Demonstrated ability to produce valid, schema-compliant JSON from visual prompts |
| Parameter Efficiency    | 3B parameters - deployable on a single T4 GPU with 10-14 GB VRAM                 |
| LoRA Compatibility      | Standard transformer architecture compatible with PEFT LoRA injection            |
| Open Weights            | Fully accessible for fine-tuning without API dependency                          |
| Throughput              | Achieves 7-15 second inference latency on T4 hardware                            |

**Multimodal Architecture**

Qwen2.5-VL operates with three primary components:

- Vision Encoder - Processes image patches into visual token embeddings using a ViT-based encoder with dynamic resolution support
- Cross-Modal Projector - Projects visual embeddings into the language model's token space
- Language Model Decoder - Qwen2.5 transformer decoder that attends jointly to visual and text tokens to generate structured responses

_Qwen2.5-VL architecture: Vision Encoder → Cross-Modal Projector → LLM Decoder with image + text token fusion_

## 3.2 Fine-Tuning Strategy

The base Qwen2.5-VL-3B-Instruct model was fine-tuned using Supervised Fine-Tuning (SFT) with Low-Rank Adaptation (LoRA) a parameter-efficient fine-tuning technique that injects trainable rank-decomposition matrices into frozen base model layers, enabling task-specific adaptation without full parameter updates.

**LoRA Architecture**

LoRA decomposes weight updates into two low-rank matrices:

Only matrices A and B are trained. Base model weights remain frozen.

At inference:

This approach reduces trainable parameters by ~99% compared to full fine-tuning while achieving comparable task performance.

**Training Configuration**

| **Parameter**               | **Value**                    |
| --------------------------- | ---------------------------- |
| Method                      | Supervised Fine-Tuning (SFT) |
| LoRA Rank (r)               | 8                            |
| LoRA Alpha (α)              | 16                           |
| LoRA Dropout                | 0.05                         |
| Target Modules              | q_proj, v_proj               |
| Epochs                      | 2                            |
| Batch Size                  | 1                            |
| Gradient Accumulation Steps | 4                            |
| Effective Batch Size        | 4                            |
| Learning Rate               | 5e-5                         |
| LR Scheduler                | Cosine decay                 |
| Optimizer                   | AdamW (Torch Fused)          |
| Precision                   | FP16 Mixed Precision         |
| Max Sequence Length         | 1536 tokens                  |
| Hardware                    | 2× CUDA GPUs                 |
| Training Framework          | LLaMA-Factory                |

**Training Results**

| **Metric**           | **Value**    |
| -------------------- | ------------ |
| Final Training Loss  | 0.2145       |
| Total Training Steps | 1,250        |
| Total Runtime        | ~6.4 hours   |
| Hardware             | 2× CUDA GPUs |

**Engineering Decisions**

LoRA Target Modules - q_proj and v_proj: Attention query and value projections were selected as LoRA targets based on empirical evidence that adapting these modules is sufficient for instruction-following and structured output tasks, while minimizing parameter overhead.

Gradient Accumulation (steps=4): With a per-device batch size of 1 (imposed by VRAM constraints with the vision encoder active), gradient accumulation over 4 steps provides an effective batch size of 4, stabilizing gradient estimates and improving convergence.

FP16 Mixed Precision: Mixed precision training halves memory consumption relative to FP32 training with negligible precision loss on this task, while the fused optimizer reduces CUDA kernel launch overhead.

**Training Libraries**

| **Library**   | **Role**                                                    |
| ------------- | ----------------------------------------------------------- |
| transformers  | Model loading, tokenization, inference                      |
| peft          | LoRA adapter injection and management                       |
| torch         | Tensor operations, autograd, CUDA backend                   |
| datasets      | Dataset loading and streaming                               |
| tokenizers    | Fast tokenization pipeline                                  |
| LLaMA-Factory | Training orchestration, SFT pipeline, checkpoint management |

## 3.3 Specialized LoRA Adapters

Rather than training a single general-purpose adapter, two domain-specialized LoRA adapters were engineered - one for mobile UI analysis and one for web UI analysis. This specialization strategy is grounded in the fundamental structural differences between mobile and web interfaces.

**Why Two Adapters?**

| **Dimension**        | **Mobile UI**                       | **Web UI**                        |
| -------------------- | ----------------------------------- | --------------------------------- |
| Aspect Ratio         | Portrait (tall, narrow)             | Landscape (wide, short)           |
| Layout Structure     | Vertical, single column             | Multi-column, sidebar, grid       |
| Navigation Pattern   | Bottom tabs, hamburger menus        | Top nav, side rails               |
| Interaction Model    | Touch targets (min 44px)            | Cursor-based (smaller hit areas)  |
| Typography Scale     | Larger base font, limited hierarchy | Smaller base font, rich hierarchy |
| Spacing Density      | Generous touch spacing              | Compact, information-dense        |
| Component Vocabulary | Cards, FABs, bottom sheets          | Tables, sidebars, dashboards      |

**Mobile Adapter Specialization**

- Vertical layout understanding (portrait-oriented grids)
- Compact element spacing recognition
- Mobile navigation patterns (tab bars, hamburger icons, bottom sheets)
- Touch-optimized element sizing heuristics
- Mobile typography extraction (larger base sizes, limited weight variation)
- Detection: FABs, swipe cards, status bars

**Web Adapter Specialization**

- Dashboard and data visualization layout analysis
- Sidebar and rail navigation recognition
- Multi-column grid and flex layout understanding
- Table and data grid element extraction
- Desktop spacing density (tighter padding, denser hierarchy)
- Desktop components: breadcrumbs, dropdown menus, tabs, modals, tooltips

## 3.4 Dataset Engineering

**Dataset Overview**

| **Dataset**                                                                                        | **Purpose**                                    | **Size**      | **Adapter Target** |
| -------------------------------------------------------------------------------------------------- | ---------------------------------------------- | ------------- | ------------------ |
| [iOS-1K-Mobile-UI-Dataset](https://huggingface.co/datasets/atharparvezce/iOS-1K-Mobile-UI-Dataset) | Mobile UI layout understanding                 | ~1,000 images | Mobile             |
| [mobile-ui-design](https://huggingface.co/datasets/mrtoy/mobile-ui-design)                         | Typography extraction, component understanding | ~5,000 images | Mobile             |
| [ShowUI-web](https://huggingface.co/datasets/showlab/ShowUI-web)                                   | Web UI layout understanding                    | ~2,400 images | Web                |

Total Training Corpus: ~8,400 annotated UI images

**Annotation Schema**

{

"screen_id": "&lt;unique_identifier&gt;",

"elements": \[

{

"id": "&lt;element_index&gt;",

"type": "&lt;component_class&gt;",

"text": "&lt;visible_text_content&gt;",

"bbox_norm": {

"x_norm": 0.0,

"y_norm": 0.0,

"w_norm": 0.0,

"h_norm": 0.0

},

"is_clickable": true,

"font_weight": "&lt;weight_class&gt;",

"font_size_role": "&lt;semantic_role&gt;"

}

\]

}

**Schema Design Decisions**

Bounding Boxes: Bounding boxes encode spatial location and dimensions of each UI element. The four values - x, y, width, height - define a rectangle in image space, sufficient for element localization and downstream rendering.

Normalized Coordinates: All coordinates are normalized to \[0.0, 1.0\] relative to image dimensions. This design enables the schema to remain invariant to screen resolution, making annotations valid across device classes.

x_norm = bbox_x / image_width

y_norm = bbox_y / image_height

w_norm = bbox_width / image_width

h_norm = bbox_height / image_height

Semantic Labels: Component type labels classify each element into a fixed vocabulary: Button, InputField, Navbar, Card, Icon, Label, Image, Checkbox, Toggle, Divider. This controlled vocabulary ensures consistent downstream rendering.

Typography Metadata: font_weight and font_size_role capture design system typography using semantic roles (heading, body, caption) rather than raw pixel sizes, making the schema portable across design systems.

**Instruction-Tuning Conversation Format**

{

"conversations": \[

{

"role": "user",

"content": \[

{ "type": "image", "image": "&lt;base64_or_path&gt;" },

{ "type": "text", "text": " You are an expert mobile UI analyst. When given a mobile app screenshot, you extract detailed structured information about every visible UI element - including its type, purpose, position, interactivity, and typography (font size role, weight, style, alignment). Always respond with a single valid JSON object and nothing else.

..." }

\]

},

{

"role": "assistant",

"content": "&lt;structured_json_annotation&gt;"

}

\]

}

## 3.5 Data Preprocessing Pipeline

All three datasets underwent a standardized preprocessing pipeline before being consumed by the training framework.

**Stage 1 - Image Validation**

Each image was validated for: file integrity, minimum resolution (64×64px minimum), maximum file size limits, and format compatibility (JPEG, PNG only).

**Stage 2 - Image Processing (Qwen2.5-VL Pipeline)**

Images were processed through Qwen2.5-VL's native preprocessing pipeline: dynamic resizing to the model's supported resolution range, patch extraction for the vision encoder, pixel normalization to the model's expected statistics, and aspect ratio preservation with padding where required.

**Stage 3 - Annotation Cleaning**

Raw dataset annotations were cleaned via removal of samples with missing or null bounding box fields, removal of samples where coordinates fall outside \[0, 1\] after normalization, deduplication of element entries, and text field normalization.

**Stage 4 - Coordinate Normalization**

Original datasets provided bounding boxes in pixel-absolute coordinates. These were converted to normalized coordinates using image dimensions.

**Stage 5 - Instruction Format Conversion**

Cleaned annotations were serialized into the instruction-tuning conversation format and written to JSONL files, one sample per line, compatible with LLaMA-Factory's dataset loader.

**Stage 6 - Split and Validation**

Final datasets were split into train/validation sets. A validation pass confirmed all JSON strings are valid, all bounding box values are within \[0.0, 1.0\], no empty element arrays exist, and token lengths are within the 1536-token cutoff limit.

## 3.6 Platform Detection System

The platform detection system is a lightweight, rule-based classifier that determines whether an input UI screenshot depicts a mobile or web interface. Its output governs adapter routing - the most consequential inference decision in the pipeline.

**Detection Algorithm - Weighted Voting**

| **Signal**            | **Weight** | **Basis**                                                      |
| --------------------- | ---------- | -------------------------------------------------------------- |
| Aspect Ratio          | 3 votes    | Strongest discriminator - mobile is portrait, web is landscape |
| OCR / Keyword Scoring | 2 votes    | Mobile keywords vs. web UI text patterns                       |
| Resolution            | 1 vote     | Width heuristic - mobile screenshots typically <900px wide     |

**Decision Logic**

\# Aspect ratio (height / width)

if aspect_ratio > 1.3: vote_aspect = "mobile"

elif aspect_ratio < 0.8: vote_aspect = "web"

else: vote_aspect = None # abstain

\# Resolution

if image_width < 900: vote_resolution = "mobile"

else: vote_resolution = "web"

\# Weighted score aggregation

mobile_score = (3 if vote_aspect == "mobile" else 0)

\+ (2 if vote_ocr == "mobile" else 0)

\+ (1 if vote_res == "mobile" else 0)

platform = "mobile" if mobile_score >= web_score else "web"

\# Tie-breaking defaults to "mobile" (conservative fallback)

Maximum possible score: 6 votes per platform. Tie-breaking defaults to mobile - the mobile adapter produces more coherent outputs on ambiguous inputs than the web adapter.

## 3.7 Inference Pipeline

**End-to-End Workflow**

Image URL Received

↓

Image Fetched & Decoded

↓

Platform Detection (Aspect Ratio + Resolution + OCR Voting)

↓

Adapter Selection (Mobile LoRA | Web LoRA)

↓

Qwen2.5-VL Multimodal Inference (Vision Encoder + Language Decoder)

↓

Raw Text Output

↓

JSON Extraction & Parsing

↓

Schema Validation

↓

Normalized JSON Response

**Prompt Engineering**

The system prompt instructs the model to produce structured JSON in a specific schema. Prompt engineering was critical for reliable structured output - the prompt explicitly defines required output format (JSON only, no preamble), schema field definitions, normalized coordinate requirements, and component type vocabulary.

System Prompt:

"You are a UI analysis expert. Analyze the provided UI screenshot and extract

all visible UI elements as a structured JSON object. Return ONLY valid JSON.

Do not include any explanation or preamble. Use normalized coordinates in

range \[0.0, 1.0\]. Element types must be from: Button, InputField, Navbar,

Card, Icon, Label, Image, Checkbox, Toggle, Divider."

_Inference pipeline sequence diagram: Client → FastAPI → Detector → Adapter Router → VLM → JSON Parser → Validator → Response_

## 3.8 Structured UI Extraction Engine

**Extracted Dimensions**

| **Dimension**          | **JSON Fields**          | **Description**                                       |
| ---------------------- | ------------------------ | ----------------------------------------------------- |
| Element Classification | type                     | Component category (Button, Navbar, InputField, etc.) |
| Text Content           | text                     | Visible label or placeholder text                     |
| Spatial Location       | bbox_norm.x_norm, y_norm | Normalized top-left corner position                   |
| Spatial Extent         | bbox_norm.w_norm, h_norm | Normalized element dimensions                         |
| Interactivity          | is_clickable             | Whether the element responds to user interaction      |
| Typography Weight      | font_weight              | regular / medium / semibold / bold                    |
| Typography Role        | font_size_role           | display / heading / subheading / body / caption       |
| Element Identity       | id                       | Unique sequential identifier within the screen        |

**Extraction Example - Mobile Login Screen**

{

"screen_id": "106",

"elements": \[

{

"id": "1",

"type": "Label",

"text": "Welcome Back, Rahul",

"bbox_norm": {

"x_norm": 0.285,

"y_norm": 0.4379,

"w_norm": 0.432,

"h_norm": 0.04

},

"is_clickable": false,

"font_weight": "semibold",

"font_size_role": "body"

},

{

"id": "2",

"type": "InputField",

"text": "Email address",

"bbox_norm": {

"x_norm": 0.147,

"y_norm": 0.518,

"w_norm": 0.706,

"h_norm": 0.059

:

:

},

## 3.9 AI Deployment & Infrastructure

_Deployment architecture: Docker container with FastAPI + Uvicorn + PyTorch + CUDA → NVIDIA T4 GPU → External clients_

**Docker Configuration**

FROM nvidia/cuda:12.1.1-cudnn8-runtime-ubuntu22.04

\# Build the inference service image

docker build -t ui-ai-service.

\# Run with full GPU access on port 8000

docker run --gpus all -p 8000:8000 ui-ai-service

**Deployment Stack**

| **Component**     | **Technology**      | **Version** |
| ----------------- | ------------------- | ----------- |
| Container Runtime | Docker              | Latest      |
| CUDA Toolkit      | NVIDIA CUDA         | 12.1.1      |
| cuDNN             | NVIDIA cuDNN        | 8           |
| ML Framework      | PyTorch             | 2.3.1       |
| API Framework     | FastAPI             | Latest      |
| ASGI Server       | Uvicorn             | Latest      |
| Hosting Platform  | Hugging Face Spaces | -           |
| OS                | Ubuntu              | 22.04 LTS   |

**Inference Performance**

| **Metric**                   | **Observed Value**       |
| ---------------------------- | ------------------------ |
| End-to-end inference latency | 5-15 seconds per request |
| RAM usage                    | 8-12 GB                  |
| VRAM usage                   | 10-14 GB                 |
| GPU                          | NVIDIA T4 (16 GB VRAM)   |

**Runtime Optimization Techniques**

- Model loaded once at startup - eliminates per-request model loading overhead
- FP16 inference - reduces VRAM consumption and improves throughput on Turing GPUs
- Adapter hot-swap - both LoRA adapters pre-loaded; switching requires only weight merge
- Async FastAPI - maximizes throughput under sequential inference constraints

# 4\. Implementation Overview

## 4.1 Implementation Phases

### Phase 1 - Model Selection and Evaluation

Multiple vision-language models were evaluated against structured JSON generation reliability, inference latency on T4 hardware, LoRA compatibility, and open-weight availability. Qwen2.5-VL-3B-Instruct was selected after empirical evaluation demonstrated superior structured output compliance in the 3B-7B parameter range.

### Phase 2 - Dataset Engineering

Three datasets were sourced from Hugging Face covering mobile iOS UIs, mobile design systems, and web UIs. Each dataset required custom preprocessing: coordinating normalization, annotation cleaning, and conversion to the unified JSON schema. The pipeline was implemented in Python using datasets, PIL, and json libraries.

### Phase 3 - Fine-Tuning Workflow

Fine-tuning was orchestrated via LLaMA-Factory on Kaggle's dual-GPU environment. Two separate training runs were conducted - one per adapter - each consuming a domain-specific dataset subset.

- Mobile Adapter Notebook: <https://www.kaggle.com/code/zeyadpop/qwen25vl-mobileui-lora>
- Web Adapter Notebook: <https://www.kaggle.com/code/zeyadpop/qwen25vl-webui-lora/notebook>

### Phase 4 - Inference System Development

The inference service was built in FastAPI with three primary components: the Platform Detector (weighted voting classifier), Adapter Router (selects and applies LoRA adapter), and JSON Extractor (post-processes model output into validated structured JSON). Prompt engineering underwent approximately 30 iterations of manual testing on held-out UI screenshots before converging on the production system prompt.

### Phase 5 - Deployment

The service was containerized using the nvidia/cuda:12.1.1-cudnn8-runtime-ubuntu22.04 base image and deployed to Hugging Face Spaces, with CUDA runtime configuration, PyTorch GPU support, and model weights storage managed within the container environment.

## 4.2 Technical Challenges and Solutions

### Challenge 1 - JSON Output Inconsistency

_Problem: During early inference testing, the model frequently produced malformed JSON - truncated outputs, unescaped characters, or mixed prose/JSON responses on complex UI screenshots._

Solution: Three-layer mitigation was applied:

- Refined system prompt to explicitly prohibit prose output and define schema fields
- Increased max_new_tokens budget to prevent truncation on dense UIs
- Implemented JSON recovery post-processor with heuristic repairs before rejection

### Challenge 2 - Dataset Annotation Quality

_Problem: Source datasets contained annotation inconsistencies - misaligned bounding boxes, incorrect coordinate systems, and missing fields._

Solution: A multi-stage validation pipeline detected and filtered invalid samples. Coordinate systems were normalized to \[0,1\] with explicit per-dataset conversion logic. Samples failing validation were excluded rather than heuristically corrected to avoid introducing noisy training signals.

### Challenge 3 - VRAM Constraints During Training

_Problem: The vision encoder's image processing pipeline caused OOM errors on single-GPU configurations._

Solution: Gradient accumulation over 4 steps with batch size 1 simulates a larger effective batch while maintaining acceptable peak VRAM. FP16 mixed precision further halved the memory footprint. Multi-GPU training across 2 GPUs provided additional headroom.

### Challenge 4 - Inference Latency

_Problem: Initial inference latency of 20-30 seconds per request was prohibitive for interactive use._

Solution: Several optimizations reduced latency to 7-15 seconds: model pre-loading at service startup, FP16 inference mode, reduced max_new_tokens to the minimum sufficient for full-screen JSON, and adapter pre-loading with hot-swap.

### Challenge 5 - Deployment on T4 GPU

_Problem: The T4's 16 GB VRAM is tight for Qwen2.5-VL-3B-Instruct with both LoRA adapters loaded simultaneously._

Solution: Both adapters are stored as delta weights and merged into the base model on demand. Only the active adapter's merged weights occupy VRAM at any time, keeping peak VRAM within the 14 GB observed limit.

## 4.3 Implementation Summary

| **Phase**           | **Key Deliverable**                              | **Technologies**               |
| ------------------- | ------------------------------------------------ | ------------------------------ |
| Model Selection     | Qwen2.5-VL-3B-Instruct selected and evaluated    | Transformers, PEFT             |
| Dataset Engineering | 3 datasets preprocessed, unified schema designed | Python, datasets, PIL          |
| Mobile Fine-Tuning  | Mobile LoRA adapter (final loss: 0.2145)         | LLaMA-Factory, PyTorch, PEFT   |
| Web Fine-Tuning     | Web LoRA adapter trained and validated           | LLaMA-Factory, PyTorch, PEFT   |
| Platform Detection  | Weighted voting classifier implemented           | Python, PIL                    |
| Inference Service   | FastAPI REST API with adapter routing            | FastAPI, Uvicorn, Transformers |
| Deployment          | Dockerized GPU service on HuggingFace Spaces     | Docker, CUDA 12.1, HF Spaces   |