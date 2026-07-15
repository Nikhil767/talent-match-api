# Sprint Tracking Ledger

## Completed Tasks
- `[x]` Set up .NET 10 Minimal API project structure
- `[x]` Configure Supabase PostgreSQL and EF Core
- `[x]` Implement Supabase JWT Authentication with ECDsa
- `[x]` Build document upload, validation, and Supabase Storage integration
- `[x]` Integrate Qdrant vector storage
- `[x]` Implement background processing queue (`AnalysisQueue`) and worker
- `[x]` Integrate LLM providers (Gemini, Groq) and Polly policies
- `[x]` Implement SSE (Server-Sent Events) for real-time notifications

## In Progress
- `[/]` Refine `ResumePipelineService` logic for storing LLM responses

## Open Tasks
- `[ ]` Fix unused variables in `ResumePipelineService` (`jobEmbedding`, `gap`, `match`, `tailored`)
- `[ ]` Resolve Vector Database Overlap (decide between Qdrant vs pgvector)
- `[ ]` Clean up unused entity properties (e.g., `R2Url` naming inconsistency)
- `[ ]` Build frontend UI and connect to SSE streams
- `[ ]` Implement robust unit and integration tests
- `[ ]` Enhance resume chunking logic and embeddings optimization
