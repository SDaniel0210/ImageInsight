# ImageInsight

> AI-powered desktop application for automatic image captioning and tagging.

---

##  Overview

ImageInsight is a desktop application built with **C# WPF** and a **Python AI backend** that automatically analyzes images and generates:

- Natural language descriptions (captions)
- Structured tags for search and filtering

The goal is to simplify image organization and enable intelligent indexing using modern vision-language models.

---

## Architecture

```
WPF (C# UI)
    ↓
HTTP API
    ↓
Python Backend (FastAPI)
    ↓
Florence-2 Vision Model
```

---

## AI Model

The project uses:

- **Florence-2-large** (Microsoft Vision-Language Model)
- Hosted locally via Hugging Face Transformers

Capabilities:
- Image captioning
- Scene understanding
- Object-level semantic tagging

---

## Features

- Image analysis from local files
- AI-generated captions
- Automatic tag extraction
- Local inference (no API required)
- Modular backend (easy to extend)

---

## Tech Stack

### Frontend
- C# WPF (.NET)

### Backend
- Python
- FastAPI

### AI / ML
- PyTorch
- Hugging Face Transformers
- Florence-2

---

## Future Improvements

- Tag normalization system
- Confidence scoring
- Batch processing
- Local database integration

---

## Project Goal

To create a fast, local, and intelligent image analysis tool that can be integrated into real-world workflows such as:

- Asset management
- Dataset labeling
- Content organization

---

## License

MIT License
