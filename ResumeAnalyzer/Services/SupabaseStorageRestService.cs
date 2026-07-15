namespace ResumeAnalyzer.Services
{
	public class SupabaseStorageRestService(IConfiguration config, IHttpClientFactory httpFactory, 
	ILogger<SupabaseStorageRestService> logger)
	{
		private readonly string _projectUrl = config["Supabase:Url"]!;
		public async Task<string> UploadAsync(string bucket, string objectKey, Stream fileStream, string contentType, string bearerToken)
		{
			try
			{
				var client = httpFactory.CreateClient("supabase-storage");
				var url = $"{_projectUrl}/storage/v1/object/{bucket}/{objectKey}";
				fileStream.Position = 0;
				var content = new StreamContent(fileStream);
				content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
				//if (!string.IsNullOrWhiteSpace(bearerToken))
				//	client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);
				//System.Net.Http.Headers.AuthenticationHeaderValue.Parse(bearerToken);
				var response = await client.PostAsync(url, content);
				if (!response.IsSuccessStatusCode)
				{
					var body = await response.Content.ReadAsStringAsync();
					logger.LogWarning("SupabaseStorageRestService UploadAsync failed {Status}: {Body}", response.StatusCode, body);
				}
				response.EnsureSuccessStatusCode();
				return $"{_projectUrl}/storage/v1/object/{bucket}/{objectKey}";
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "SupabaseStorageRestService UploadAsync exception");
				throw;
			}
		}
		public async Task<byte[]> DownloadAsync(string bucket, string objectKey, string bearerToken)
		{
			try
			{
				var client = httpFactory.CreateClient("supabase-storage");
				var url = objectKey;// $"{_projectUrl}/storage/v1/object/{bucket}/{objectKey}";
				//if (!string.IsNullOrWhiteSpace(bearerToken))
				//	client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);

				var response = await client.GetAsync(url);
				if (!response.IsSuccessStatusCode)
				{
					var body = await response.Content.ReadAsStringAsync();
					logger.LogWarning("SupabaseStorageRestService DownloadAsync failed {Status}: {Body}", response.StatusCode, body);
				}
				response.EnsureSuccessStatusCode();
				return await response.Content.ReadAsByteArrayAsync();
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "SupabaseStorageRestService DownloadAsync exception");
				throw;
			}
		}
		public async Task<bool> DeleteAsync(string bucket, string objectKey, string bearerToken)
		{
			try
			{
				var client = httpFactory.CreateClient("supabase-storage");
				var url = objectKey;// $"{_projectUrl}/storage/v1/object/{bucket}/{objectKey}";
				if (!string.IsNullOrWhiteSpace(bearerToken))
					client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);
				var response = await client.DeleteAsync(url);
				if (!response.IsSuccessStatusCode)
				{
					var body = await response.Content.ReadAsStringAsync();
					logger.LogWarning("SupabaseStorageRestService DeleteAsync failed {Status}: {Body}", response.StatusCode, body);
				}
				//response.EnsureSuccessStatusCode();
				return true;
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "SupabaseStorageRestService DeleteAsync exception");
				throw;
			}
		}

	}
}
