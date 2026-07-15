# Coding Patterns & Architectural Rules

## API Design
- **Minimal APIs**: Use `MapGroup` for endpoint routing (e.g., `app.MapGroup("/api/resume")`).
- **Route Prefixes**: All primary endpoints should reside under `/api/` prefixes.
- **Documentation**: Endpoints should use tags (`.WithTags()`) and summaries (`.WithSummary()`).

## Memory & Performance
- **Streaming**: Handle file uploads efficiently using `MemoryStream` and avoid excessive allocations (single-pass execution).
- **Validation**: Perform strict file validation (MIME type, size, extension) before processing payloads.

## Error Handling & Resilience
- **Polly**: Wrap all external HTTP client calls (Supabase, Qdrant, LLM providers) with Polly retry policies.
- **Exception Handling**: Use global exception handler for consistent 500 responses.

## Authentication
- **Supabase JWT**: Validate tokens against Supabase's JWKS using ECDsa security keys.
- **Security**: Apply `RequireAuthorization()` on sensitive `MapGroup` blocks.
