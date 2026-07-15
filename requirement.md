# ResumeAnalyzer Project High level Requirement Analysis

1) User can register Using Supabase (Email & password)
2) User can Upload his/her resume (.pdf, .docx)
3) Once resume is process User will get notified by SSE
4) User can ATS score for uploaded resume
5) User can see skills
6) User can Delete the Resume
7) User can upload more than 1 Resumes
8) User can find a gaps with the Job description
9) User can tailor resume with the Job description
10) User can search for the Jobs using ingest
10) User can search for matching Jobs using match

# workflow
1) User will register using Supabase authentication
	User will be authenticated by Supabase authenticated & will get token
2) he will upload his resume
	File_hash will be generated for the uplaoded file to identify deduplication of file
	file will be uploaded to supabase storage
	then will be stored data into Resume entity & then return the resumeid to the User
3) User will call 1 Api (which will be SSE)
	which will give contineous update of processing resume Analysis
	picking the resume from queued state to processing
	then update to client through sse
	then downloading form storage & have a raw_text	
	should perform the guardrails (input sanitization) ?
	then update to client through sse
	then genearting the embeddings from raw_text
	then update to client through sse
	then generate ats score
	then update to client through sse
	then generate the skills
	then update to client through sse
	should response need to be sanitized ?
	then store the embeddings to vector db
	then update to client through sse

# New Changes	
01 Extract and sanitize resume text
(Remove control characters, normalize whitespace, strip artifacts, and ensure the text is clean before any processing.)
02 Chunk the sanitized text
(Split the resume into manageable 300–500 token chunks to avoid LLM token limits and improve accuracy.)
03 Generate embeddings for each chunk (only call the same embedding.GetResumeEmbeddingAsync() method for chunck)
04 Send chunks to LLM with strict JSON prompt
(already implemented just validate the prompts => Force the model to return structured JSON containing ATS score, skills, summary, and missing skills.)
05 Validate and sanitize LLM output
Check JSON validity, clamp ATS score, remove hallucinated skills, and ensure summary quality.
06 Store analysis and chunks in Supabase