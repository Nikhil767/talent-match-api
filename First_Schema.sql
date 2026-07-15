-- Run these in Supabase SQL Editor

-- RESUMES
create table if not exists resumes (
  id 			   uuid primary key default gen_random_uuid(),
  user_id          uuid not null references auth.users(id) on delete cascade,
  file_name        text not null,
  file_hash        text not null,
  storage_path     text not null,
  mime_type        text,
  file_size_bytes  bigint,
  status           text not null default 'queued'
                   check (status in ('queued', 'processing', 'completed', 'failed', 'duplicate')),
  processing_step  text
                   check (processing_step in ('uploaded', 'extracted', 'scored', 'embedded', 'done', 'failed')),
  attempt_count    integer not null default 0,
  locked_at        timestamptz,
  processed_at     timestamptz,
  error_message    text,
  created_at       timestamptz not null default now(),
  updated_at       timestamptz not null default now(),
  unique (user_id, file_hash)
);

create index if not exists idx_resumes_user_id on resumes(user_id);
create index if not exists idx_resumes_status on resumes(status);
create index if not exists idx_resumes_file_hash on resumes(file_hash);
create index if not exists idx_resumes_locked_at on resumes(locked_at);

alter table resumes enable row level security;

-- RESUME ANALYSIS
create table if not exists resume_analysis (
  id                    uuid primary key default gen_random_uuid(),
  resume_id             uuid not null references resumes(id) on delete cascade,
  ats_score             numeric(5,2) not null default 0,
  ats_analysis_json     jsonb not null default '[]'::jsonb,
  skills_json   jsonb not null default '[]'::jsonb,
  extracted_summary     text,
  extracted_text_preview text,
  model_used            text,
  analyzed_at           timestamptz,
  created_at            timestamptz not null default now(),
  updated_at            timestamptz not null default now(),
  unique (resume_id)
);

create index if not exists idx_resume_analysis_resume_id on resume_analysis(resume_id);

alter table resume_analysis enable row level security;

-- JOBS
create table if not exists jobs (
  id		   uuid primary key default gen_random_uuid(),
  job_id       text unique,
  title        text,
  company      text,
  location     text,
  description  text,
  url          text unique,
  posted_at    text,
  source       text,
  created_at   timestamptz not null default now(),
  updated_at   timestamptz not null default now()
);

create index if not exists idx_jobs_title on jobs(title);
create index if not exists idx_jobs_company on jobs(company);

alter table jobs enable row level security;

-- RESUME JOB MATCHES
create table if not exists resume_job_matches (
  id                   uuid primary key default gen_random_uuid(),
  resume_id            uuid not null references resumes(id) on delete cascade,
  job_id               uuid not null references jobs(id) on delete cascade,
  match_score          numeric(5,2) not null default 0,
  matched_skills_json  jsonb not null default '[]'::jsonb,
  missing_skills_json  jsonb not null default '[]'::jsonb,
  model_used           text,
  matched_at           timestamptz,
  created_at           timestamptz not null default now(),
  updated_at           timestamptz not null default now(),
  unique (resume_id, job_id)
);
 
create index if not exists idx_rjm_resume_id on resume_job_matches(resume_id);
create index if not exists idx_rjm_job_id on resume_job_matches(job_id);
 
alter table resume_job_matches enable row level security;

---==================== STORAGE SCRIPTS 
-- 1. INSERT Policy (Allows uploading new resumes)
CREATE POLICY "Allow authenticated resume uploads" 
ON storage.objects 
FOR INSERT 
TO authenticated 
WITH CHECK (
    bucket_id = 'resumes' 
    AND storage.extension(name) = ANY (ARRAY['pdf', 'docx', 'doc', 'txt'])
);

-- 2. SELECT Policy (Allows viewing and downloading resumes)
CREATE POLICY "Allow authenticated resume selection" 
ON storage.objects 
FOR SELECT 
TO authenticated 
USING (
    bucket_id = 'resumes'
);

-- 3. UPDATE Policy (Allows replacing or updating existing resumes)
CREATE POLICY "Allow authenticated resume updates" 
ON storage.objects 
FOR UPDATE 
TO authenticated 
USING (
    bucket_id = 'resumes'
)
WITH CHECK (
    bucket_id = 'resumes' 
    AND storage.extension(name) = ANY (ARRAY['pdf', 'docx', 'doc', 'txt'])
);

-- 4. DELETE Policy (Allows deleting resumes)
CREATE POLICY "Allow authenticated resume deletions" 
ON storage.objects 
FOR DELETE 
TO authenticated 
USING (
    bucket_id = 'resumes'
);