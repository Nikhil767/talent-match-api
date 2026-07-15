# JobEndpoints.cs Analysis

## Existing APIs and Valid Use Cases

1. **`POST /api/jobs/ingest`**
   - **When to call**: When you need to populate or update your local job database from an external provider (like JSearch).
   - **Valid Use Case**: A background chron job or an admin action to pull new software engineering roles into the system so users can match against fresh data.

2. **`POST /api/jobs/match`**
   - **When to call**: When a user wants to find the best job opportunities tailored specifically to their uploaded resume.
   - **Valid Use Case**: The "Job Recommendations" page where a user clicks "Find Matches" for their resume, and the system performs a semantic search across jobs and provides an AI-generated explanation of why they are a good fit.

3. **`GET /api/jobs/search?q=keyword`**
   - **When to call**: When a user is manually searching for jobs using text keywords.
   - **Valid Use Case**: A standard search bar on a "Jobs" page where a user types "React Developer" to browse available roles.

4. **`GET /api/jobs/{id}`**
   - **When to call**: When a user clicks on a specific job to view its full details.
   - **Valid Use Case**: A "Job Details" page showing the full description, company name, and application link for a specific job ID.

## Should you save the data from `/match`?

**Yes, you should save it.** 
Currently, the `/match` endpoint calls `analysis.ExplainMatchAsync()` for *every* hit (up to 10 jobs) on the fly. This has severe implications:
- **Cost**: You are making up to 10 LLM calls per API request, which will quickly consume your token limits and API budget.
- **Latency**: Doing 10 sequential (or even parallel) LLM calls blocks the HTTP response for a long time, resulting in a poor user experience.

**Recommendation**:
You should create a `JobMatch` or `ResumeJobMatch` entity in your database to store:
- `ResumeId`
- `JobId`
- `VectorScore`
- `AiAnalysis` (the JSON explanation)

When `/match` is called, you should first check if a match analysis already exists in the database. If it does, return the cached analysis. If it doesn't, queue a background task to generate the AI explanation, or generate it once and save it immediately so subsequent requests are instant.
