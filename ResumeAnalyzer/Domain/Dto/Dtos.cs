namespace ResumeAnalyzer.Domain.Dto
{
	public record RegisterRequestDto(string Email, string Password);
	public record LoginRequestDto(string Email, string Password);


	// ── Analysis ────────────────────────────────────────────────────────────────
	public record AtsRequestDto(Guid ResumeId, string JobDescription);
	public record GapRequestDto(Guid ResumeId, string JobDescription);
	public record TailorRequestDto(Guid ResumeId, string JobDescription);

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

public class ResumeExportRequestDto
{
	public string FullName { get; set; } = default!;
	public string Email { get; set; } = default!;
	public string Phone { get; set; } = default!;
	public string Summary { get; set; } = default!;
	public List<string> Skills { get; set; } = new();
	public List<ExperienceItem> Experience { get; set; } = new();
	public string Format { get; set; } = "pdf"; // "pdf" or "docx"
}

public class ExperienceItem
{
	public string Company { get; set; } = default!;
	public string Role { get; set; } = default!;
	public string Duration { get; set; } = default!;
	public string Description { get; set; } = default!;
}

public class ResumeExportResultDto
{
	public byte[] FileBytes { get; set; } = default!;
	public string FileName { get; set; } = default!;
	public string FileType { get; set; } = default!; // pdf or docx
}