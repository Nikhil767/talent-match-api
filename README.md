# 🎯 TalentMatch API

**AI-powered resume parsing and semantic job matching engine.** TalentMatch ingests resumes in any common format, converts them into 768-dimensional vector embeddings, and uses hybrid semantic search + LLM reasoning to rank candidates against job descriptions with human-readable matchmaking summaries.

![.NET](https://img.shields.io/badge/.NET-10-512BD4?style=for-the-badge&logo=dotnet)
![Supabase](https://img.shields.io/badge/Supabase-PostgreSQL-3ECF8E?style=for-the-badge&logo=supabase)
![Supabase Storage](https://img.shields.io/badge/Supabase-Storage-3ECF8E?style=for-the-badge&logo=supabase&logoColor=white)
[![Supabase Auth](https://img.shields.io/badge/Supabase-Auth-3ECF8E?style=for-the-badge&logo=supabase&logoColor=white)](https://supabase.com/auth)
![Qdrant](https://img.shields.io/badge/Qdrant-VectorDB-DC244C?style=for-the-badge&logo=qdrant)
![Docker](https://img.shields.io/badge/Docker-Multi--Stage-2496ED?style=for-the-badge&logo=docker)
![Render](https://img.shields.io/badge/Deployed%20on-Render-46E3B7?style=for-the-badge&logo=render)

---

## ✨ Key Features

- **Dynamic Resume Ingestion** — Upload PDF, DOCX, DOC, or TXT files with strict MIME-type and size validation before processing.
- **Semantic Vector Search** — Resume and job description text is embedded into 768-dim vectors and matched via Qdrant's approximate nearest-neighbor search, not keyword matching.
- **LLM-Powered Matchmaking** — Groq and Gemini generate contextual candidate-job fit summaries, strengths, gaps, and confidence scores.
- **Live Job Sourcing** — JSearch API integration pulls real-world job postings to match resumes against, beyond internal job listings.
- **Secure Document Storage** — Original resume files persist in Supabase Storage buckets with signed URL access.
- **Clean Architecture** — Repository Pattern with clear separation between controllers, services, repositories, and external clients for testability and maintainability.

---

## 🏗️ System Architecture

```
                         ┌───────────────────────┐
                         │   Client / Frontend   │
                         └───────────┬───────────┘
                                     │ POST /api/resumes/upload
                                     ▼
                     ┌───────────────────────────────┐
                     │ ASP.NET Core Web API (.NET 10)│
                     │      Controller → Service     │
                     └───────────────┬───────────────┘
                                     │
              ┌──────────────────────┼──────────────────────┐
              ▼                      ▼                      ▼
   ┌────────────────────┐  ┌────────────────────┐ ┌────────────────────┐
   │  Supabase Storage  │  │  Document Parser   │ │  Supabase Postgres │
   │  (raw PDF/DOCX/TXT)│  │ (text extraction)  │ │ (metadata/identity)│
   └────────────────────┘  └──────────┬─────────┘ └────────────────────┘
                                      │
                                      ▼
                     ┌─────────────────────────────────┐
                     │  Embedding Service              │
                     │ (Hugging Face API / local model)│
                     │  → 768-dim vector               │
                     └──────────────┬──────────────────┘
                                    ▼
                     ┌─────────────────────────────────┐
                     │        Qdrant Vector DB         │
                     │  Stores & indexes resume vectors│
                     └──────────────┬──────────────────┘
                                    │ semantic similarity query
                                    ▼
                     ┌─────────────────────────────────┐
                     │  Matching Engine                │
                     │  Cosine similarity ranking      │
                     └──────────────┬──────────────────┘
                                    ▼
                     ┌─────────────────────────────────┐
                     │   LLM Layer (Groq / Gemini)     │
                     │  → Matchmaking summary, gap     │
                     │    analysis, fit score          │
                     └──────────────┬──────────────────┘
                                    ▼
                         ┌───────────────────────┐
                         │  JSON Response to User│
                         └───────────────────────┘
```

---

## ⚙️ Configuration & Setup

### 1. Prerequisites
- .NET 10 SDK
- Supabase project (PostgreSQL + Storage)
- Qdrant instance (cloud or self-hosted)
- API keys: Gemini, Groq, OpenAI, Hugging Face, JSearch

### 2. Configure User Secrets (Local Development)

Never commit secrets to `appsettings.json`. Use .NET User-Secrets instead:

```bash
cd TalentMatch.Api
dotnet user-secrets init

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "your-db-connection-string"
dotnet user-secrets set "Supabase:Url" "https://xxxx.supabase.co"
dotnet user-secrets set "Supabase:ApiKey" "your-supabase-service-role-key"
dotnet user-secrets set "Supabase:BucketName" "resumes"

dotnet user-secrets set "Supabase:ProjectId" "your-project-id"
dotnet user-secrets set "Supabase:AnonKey" "your-annon-key"
dotnet user-secrets set "Supabase:service_role" "your-supabase-service-role-key"
dotnet user-secrets set "Supabase:JwtSecret" "your-annon-key"

dotnet user-secrets set "Qdrant:Host" "your-qdrant-host"
dotnet user-secrets set "Qdrant:ApiKey" "your-qdrant-api-key"

dotnet user-secrets set "AI:Gemini:ApiKey" "your-gemini-key"
dotnet user-secrets set "AI:Groq:ApiKey" "your-groq-key"
dotnet user-secrets set "AI:OpenAI:ApiKey" "your-openai-key"
dotnet user-secrets set "HuggingFace:ApiKey" "your-hf-key"
dotnet user-secrets set "JSearch:ApiKey" "your-jsearch-key"
```

### 3. `appsettings.json` Structure Reference

```json
"ConnectionStrings": {
  "DefaultConnection": "your-db-connection-string"
},
"Files": {
  "AllowedExtensions": [ ".pdf", ".docx", ".doc", ".txt" ],
  "AllowedMimeTypes": [
    "application/pdf",
    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
    "application/msword",
    "text/plain"
  ],
  "MinSizeKb": 1,
  "MaxSizeMb": 1,
  "MaxFileNameLength": 100
},
"JobDescription": {
  "MinLength": 100,
  "MaxLength": 5000
},
"Supabase": {
  "ProjectId": "your-project-id",
  "JwtSecret": "your-jwt-secret",
  "Url": "https://xxxx.supabase.co",
  "AnonKey": "your-annon-key",
  "service_role": "your-service-role",
  "StorageBucket": "resumes"
},
"HuggingFace": {
  "Url": "https://router.huggingface.co/hf-inference/",
  "ApiKey": "your-hf-key"
},
"Groq": {
  "Url": "https://api.groq.com/openai/v1/",
  "ApiKey": "your-groq-key"
},
"Gemini": {
  "Url": "https://generativelanguage.googleapis.com/v1beta/",
  "ApiKey": "your-gemini-key"
},
"OpenAI": {
  "Url": "https://api.openai.com/v1/",
  "ApiKey": "your-openai-key"
},
"Qdrant": {
  "Host": "your-qdrant-host",
  "ApiKey": "your-qdrant-api-key",
  "Port": 0,
  "VectorSize": 768,
  "QdrantCollections": {
    "Resumes": "resumes",
    "Jobs": "jobs"
  }
},
"JSearch": {
  "Url": "https://jsearch.p.rapidapi.com/",
  "ApiKey": "your-jsearch-key",
  "Host": "jsearch.p.rapidapi.com"
}
```

---

## 🐳 Docker Deployment

Multi-stage build keeps the runtime image lean and production-ready.

```dockerfile
# ---- Build Stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src
COPY . .
RUN dotnet restore "TalentMatch.Api.csproj"
RUN dotnet publish "TalentMatch.Api.csproj" -c Release -o /app/publish

# ---- Runtime Stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "TalentMatch.Api.dll"]
```

**Build & Run Locally:**
```bash
docker build -t talent-match-api .
docker run -p 8080:8080 --env-file .env talent-match-api
```

**Deployed on [Render](https://render.com)** — auto-deploys on push to `main`, using the alpine ASP.NET runtime image for a minimal container footprint and faster cold starts.

---

## 📡 API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET`  | `/api/resume` | Retrieve All resumes list for current User |
| `POST` | `/api/resume/upload` | Upload & validate a resume (PDF/DOCX/DOC/TXT), extract text, store in Supabase, generate embedding |
| `GET`  | `/api/resume/{id}` | Retrieve parsed resume metadata |
| `GET`  | `/api/resume/{id}/skills` | Retrieve parsed resume skills |
| `POST` | `/api/resume/{id}/build` | Build Resume |
| `POST` | `/api/resume/{id}/export` | Export Resume |
| `DELETE` | `/api/resume/{id}` | Remove resume from storage, database, and vector index |
| `POST` | `/api/analysis/ats` | Run semantic match between a resume and a job description, returns fit score + LLM summary |
| `POST` | `/api/analysis/gaps` | Run semantic match between a resume and a job description, returns Missing gaps |
| `POST` | `/api/analysis/tailor` | Run semantic match between a resume and a job description, returns resume bullets to a specific job description |
| `GET`  | `/api/jobs/search` | Search live external job postings via JSearch API |
| `POST` | `/api/jobs/match` | Matches job description with Existing  jobs with its own embedding |
| `GET`  | `/api/jobs/{id}` | Get job details by JobId |
| `GET`  | `/api/notifications/{userId}` | Get sse events for the resume upload process |
| `DELETE`| `/api/notifications/cancel/{userId}` | Disconnect the sse connection manually if required |
| `GET`  | `/api/admin/sse/connections` | Get All sse connection list |
| `DELETE` | `/api/admin/sse/connections/{userId}` | Disconnect the sse connection manually if required |

---

## 🤝 Contributing

Contributions are welcome. Fork the repo, create a feature branch, and submit a PR with a clear description of your change.

```bash
git checkout -b feature/your-feature-name
git commit -m "Add: your feature description"
git push origin feature/your-feature-name
```

---

## 📄 License

This project is licensed under the **MIT License**.

```
MIT License

Copyright (c) 2025 Nikhil

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```
