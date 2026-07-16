namespace ResumeAnalyzer.Services
{
	public interface ISupabaseStorageRestService
	{
		Task<string> UploadAsync(string bucket, string objectKey, Stream fileStream, string contentType, string bearerToken);
		Task<byte[]> DownloadAsync(string bucket, string objectKey, string bearerToken);
		Task<bool> DeleteAsync(string bucket, string objectKey, string bearerToken);
	}
}
