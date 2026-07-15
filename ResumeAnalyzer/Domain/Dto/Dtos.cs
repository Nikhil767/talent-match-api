namespace ResumeAnalyzer.Domain.Dto
{
	public record RegisterRequestDto(string Email, string Password);
	public record LoginRequestDto(string Email, string Password);


	// ── Analysis ────────────────────────────────────────────────────────────────
	public record AtsRequestDto(Guid ResumeId, string JobDescription);
	public record GapRequestDto(Guid ResumeId, string JobDescription);
	public record TailorRequestDto(Guid ResumeId, string JobDescription);

	public record ResumeExportRequest();

	// ── Jobs ────────────────────────────────────────────────────────────────
	public record MatchRequestDto(Guid ResumeId);
	public record JobSearchRequestDto(string Query = "Senior Software Engineer",
	string Location = "Dubai", // remote
	string Country = "UAE", // it is must
	string DatePosted = "week", // all, today, 3days, week, month
	string? JobRequirements = "", // under_3_years_experience, no_experience	
	string? EmploymentType = "FULLTIME", // FULLTIME, PARTTIME, CONTRACTOR, INTERN
	bool? RemoteJobsOnly = false, // true, false	
	int Page = 1);
}
