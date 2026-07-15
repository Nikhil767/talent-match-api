using ResumeAnalyzer.Domain.Dto;
using ResumeAnalyzer.Domain.Entities;
using System.Text;
using System.Text.Json;

namespace ResumeAnalyzer.Services
{
	public class JobIngestionService(IHttpClientFactory httpFactory, ILogger<JobIngestionService> logger)
	{
		public async Task<List<Job>> FetchJobsByGetAsync(JobSearchRequestDto reqDto, CancellationToken ct = default)
		{
			try
			{
				var client = httpFactory.CreateClient("job-search");
				var builder = new StringBuilder("search-v2?");
				builder.Append($"query={Uri.EscapeDataString(reqDto.Query)}+in+{Uri.EscapeDataString(reqDto.Location)}");
				builder.Append($"&page={reqDto.Page}&num_pages=1");
				if (!string.IsNullOrEmpty(reqDto.EmploymentType))
					builder.Append($"&employment_types={reqDto.EmploymentType}");
				if (!string.IsNullOrEmpty(reqDto.DatePosted))
					builder.Append($"&date_posted={reqDto.DatePosted}");
				if (!string.IsNullOrEmpty(reqDto.JobRequirements))
					builder.Append($"&job_requirements={reqDto.JobRequirements}");
				if (reqDto.RemoteJobsOnly.HasValue && reqDto.RemoteJobsOnly == true)
					builder.Append("&remote_jobs_only=true");

				var url = builder.Append($"&country={Uri.EscapeDataString(reqDto.Country)}&language=en").ToString();
				var res = await client.GetAsync(url, ct);
				if (!res.IsSuccessStatusCode)
				{
					var body = await res.Content.ReadAsStringAsync(ct);
					logger.LogWarning("JobIngestionService FetchJobsAsync failed {Status}: {Body}", res.StatusCode, body);
					res.EnsureSuccessStatusCode();
				}
				var json = await res.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
				var jobsJson = json.GetProperty("data").GetProperty("jobs");
				var jobs = new List<Job>();
				foreach (var item in jobsJson.EnumerateArray())
				{
					jobs.Add(new Job
					{
						JobId = item.GetProperty("job_id").GetString() ?? "",
						Title = item.GetProperty("job_title").GetString() ?? "",
						Company = item.GetProperty("employer_name").GetString() ?? "",
						Location = item.TryGetProperty("job_location", out var location)
							? location.GetString() ?? reqDto.Location
							: reqDto.Location,
						Description = item.TryGetProperty("job_description", out var desc)
							? desc.GetString() ?? ""
							: "",
						Url = item.TryGetProperty("job_apply_link", out var applyLink)
							? applyLink.GetString() ?? ""
							: "",
						PostedAt = item.TryGetProperty("job_posted_at_datetime_utc", out var dt)
							? dt.GetString() ?? ""
							: ""
					});
				}
				return jobs;
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "JobIngestionService FetchJobsAsync exception");
				throw;
			}
		}

		public async Task<List<Job>> FetchJobsByPostAsync(JobSearchRequestDto reqDto, CancellationToken ct = default)
		{
			try
			{
				var client = httpFactory.CreateClient("job-search");
				var requestBody = new
				{
					api_type = "fetch_jobs",
					search_terms = reqDto.Query,
					location = reqDto.Location,
					page = reqDto.Page.ToString(),
					employment_types = reqDto.EmploymentType,
					date_posted = reqDto.DatePosted,
					job_requirements = reqDto.JobRequirements,
					remote_jobs_only = reqDto.RemoteJobsOnly == true ? "true" : "false"
				};
				var req = new HttpRequestMessage(HttpMethod.Post, "")
				{
					Content = JsonContent.Create(requestBody)
				};
				var res = await client.SendAsync(req, ct);
				if (!res.IsSuccessStatusCode)
				{
					var body = await res.Content.ReadAsStringAsync(ct);
					logger.LogWarning("JobIngestionService FetchJobsAsync failed {Status}: {Body}", res.StatusCode, body);
				}
				res.EnsureSuccessStatusCode();
				var json = await res.Content.ReadFromJsonAsync<JsonElement>(ct);
				var jobs = new List<Job>();
				foreach (var item in json.EnumerateArray())
				{
					jobs.Add(new Job
					{
						JobId = item.GetProperty("job_id").GetString()!,
						Title = item.GetProperty("job_title").GetString() ?? "",
						Company = item.GetProperty("company_name").GetString() ?? "",
						Location = item.TryGetProperty("location", out var city) ? city.GetString() ?? reqDto.Location : reqDto.Location,
						Description = item.GetProperty("linkedin_company_profile_url").GetString() ?? "",
						Url = item.GetProperty("job_url").GetString() ?? "",
						PostedAt = item.TryGetProperty("posted_date", out var dt) ? dt.GetString() ?? "" : ""
					});
				}
				return jobs;
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "JobIngestionService FetchJobsAsync exception");
				throw;
			}
		}
	}
}
