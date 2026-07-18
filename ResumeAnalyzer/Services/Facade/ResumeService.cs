using DocumentFormat.OpenXml.Packaging;
using iText.Kernel.Pdf;

namespace ResumeAnalyzer.Services.Facade
{
	public class ResumeService(ILogger<ResumeService> logger) : IResumeService
	{

		public async Task<ResumeExportResultDto> ExportAsync(ResumeExportRequestDto req)
		{
			return req.Format.ToLower() switch
			{
				"pdf" => await BuildPdfAsync(req),
				"docx" => await BuildDocxAsync(req),
				_ => throw new Exception("Invalid format. Use pdf or docx.")
			};
		}

		// ---------------------------------------------------------
		// PDF GENERATION
		// ---------------------------------------------------------
		private Task<ResumeExportResultDto> BuildPdfAsync(ResumeExportRequestDto req)
		{
			var fileName = $"{req.FullName.Replace(" ", "_")}_Resume.pdf";

			using var ms = new MemoryStream();
			var writer = new PdfWriter(ms);
			var pdf = new PdfDocument(writer);
			var doc = new iText.Layout.Document(pdf);

			return Task.FromResult(new ResumeExportResultDto
			{
				FileBytes = ms.ToArray(),
				FileName = fileName,
				FileType = "pdf"
			});
		}

		// ---------------------------------------------------------
		// DOCX GENERATION
		// ---------------------------------------------------------
		private Task<ResumeExportResultDto> BuildDocxAsync(ResumeExportRequestDto req)
		{
			var fileName = $"{req.FullName.Replace(" ", "_")}_Resume.docx";

			using var ms = new MemoryStream();
			using var doc = WordprocessingDocument.Create(ms, DocumentFormat.OpenXml.WordprocessingDocumentType.Document);

			return Task.FromResult(new ResumeExportResultDto
			{
				FileBytes = ms.ToArray(),
				FileName = fileName,
				FileType = "docx"
			});
		}
	}
}
