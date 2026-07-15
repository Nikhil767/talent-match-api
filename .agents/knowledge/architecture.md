# Architecture & Integrations

## Database Entities (PostgreSQL via EF Core)
- `Resume`: Tracks uploaded documents, file hashes, storage paths, and processing states.
- `ResumeAnalysis`: Stores extracted skills, ATS scores, missing skills, and tailored text.
- `ResumeChunk`: Used for local pgvector text embeddings.
- `Job`: Tracks job postings for matching.

## Service Boundaries
- **SupabaseStorageRestService**: Handles direct file uploads/downloads to Supabase buckets.
- **LLM Services**: 
  - `GeminiService`, `GroqService`: For skills extraction, gap analysis, and resume tailoring.
  - `EmbeddingService`: Converts text chunks into vector embeddings via HuggingFace/local models.
- **VectorService**: Manages communication with Qdrant for semantic search and `UpsertAsync` operations.
- **SSE Broker (`ISseBroker`)**: Provides real-time notifications to clients regarding background pipeline progress.
- **Pipeline (`ResumePipelineService`)**: Orchestrates document parsing, chunking, AI analysis, and database state synchronization.

## Queues & Background Workers
- **AnalysisQueue**: A thread-safe queue holding resume IDs for asynchronous processing.
- **ResumeProcessingWorker**: Hosted service that picks up queued items and executes the heavy ML/LLM pipeline in the background.
