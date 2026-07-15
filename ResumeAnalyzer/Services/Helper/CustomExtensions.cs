using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;

namespace ResumeAnalyzer.Services.Helper
{
	public static class CustomExtensions
	{
		#region Extract Methods
		public static string ExtractText(this Stream fileStream, string extension)
		{
			extension = extension.ToLower();
			if (extension == ".pdf")
				return ExtractPdfText(fileStream);
			if (extension == ".docx")
				return ExtractDocxText(fileStream);
			throw new InvalidOperationException("Unsupported file type");
		}

		public static string ExtractText(this byte[] bytes, string extension)
		{
			extension = extension.ToLower();
			return extension switch
			{
				".pdf" => ExtractPdfText(bytes),
				".docx" => ExtractDocxText(bytes),
				_ => throw new InvalidOperationException("Unsupported file type")
			};
		}

		private static string ExtractPdfText(this Stream stream)
		{
			using var reader = new iText.Kernel.Pdf.PdfReader(stream);
			using var pdf = new iText.Kernel.Pdf.PdfDocument(reader);
			var sb = new StringBuilder();
			for (int i = 1; i <= pdf.GetNumberOfPages(); i++)
			{
				var page = pdf.GetPage(i);
				var strategy = new iText.Kernel.Pdf.Canvas.Parser.Listener.SimpleTextExtractionStrategy();
				var text = iText.Kernel.Pdf.Canvas.Parser.PdfTextExtractor.GetTextFromPage(page, strategy);
				sb.AppendLine(text);
			}
			return sb.ToString();
		}

		private static string ExtractPdfText(byte[] bytes)
		{
			using var ms = new MemoryStream(bytes);
			return ms.ExtractPdfText();
		}

		private static string ExtractDocxText(Stream stream)
		{
			using var doc = WordprocessingDocument.Open(stream, false);
			return doc.MainDocumentPart.Document.Body.InnerText;
		}

		private static string ExtractDocxText(byte[] bytes)
		{
			using var ms = new MemoryStream(bytes);
			return ExtractDocxText(ms);
		}
		#endregion Extract Methods

		#region Santize Text
		public static string SanitizeText(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
				return string.Empty;
			text = text.Replace("\0", ""); // remove null chars
			text = Regex.Replace(text, @"<[^>]+>", ""); // remove HTML
			//text = Regex.Replace(text, @"\s+", " "); // normalize whitespace
			text = Regex.Replace(text, @"[\u0000-\u001F]", ""); // remove control chars
			text = Regex.Replace(text, @"[ \t]+", " ");
			text = Regex.Replace(text, @"\n{3,}", "\n\n");
			text = text.Trim();
			return text;
		}

		public static string SanitizeResumeText(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
				return string.Empty;
			text = text.Replace("\0", ""); // Remove null chars			
			text = Regex.Replace(text, "<.*?>", " "); // Remove HTML			
			text = Regex.Replace(text, @"https?:\/\/\S+", " "); // Remove URLs			
			text = Regex.Replace(text, @"\S+@\S+\.\S+", " "); // Remove emails			
			text = Regex.Replace(text, @"\+?\d[\d\s\-]{7,}\d", " "); // Remove phone numbers			
			text = Regex.Replace(text, @"[•●▪■□▶►]", " "); // Remove bullet symbols														   
			text = Regex.Replace(text, @"page\s+\d+(\s+of\s+\d+)?", " ", RegexOptions.IgnoreCase); // Remove page numbers																								   
			text = Regex.Replace(text, @"[\u0000-\u001F]", " "); // Remove control chars																 
			text = text.Replace("\u00A0", " ").Replace("\u00AD", ""); // Remove non-breaking spaces & soft hyphens
			text = Regex.Replace(text, @"\s{2,}", " ").Trim(); // Normalize whitespace
			return text;
		}

		public static string CleanBeforeChunking(string text)
		{
			if (string.IsNullOrWhiteSpace(text)) return string.Empty;
			text = text.Replace("\0", ""); // Remove null chars			
			text = Regex.Replace(text, "<.*?>", " "); // Remove HTML			
			text = Regex.Replace(text, @"https?:\/\/\S+", " "); // Remove URLs			
			text = Regex.Replace(text, @"\S+@\S+\.\S+", " "); // Remove emails			
			text = Regex.Replace(text, @"\+?\d[\d\s\-]{7,}\d", " "); // Remove phone numbers			
			text = Regex.Replace(text, @"[•●▪■□▶►]", " "); // Remove bullet symbols                                                           
			text = Regex.Replace(text, @"page\s+\d+(\s+of\s+\d+)?", " ", RegexOptions.IgnoreCase); // Remove page numbers
			// MODIFIED: Remove control characters EXCEPT newlines (\n) and carriage returns (\r)
			text = Regex.Replace(text, @"[\u0000-\u0009\u000B\u000C\u000E-\u001F]", " ");
			text = text.Replace("\u00A0", " ").Replace("\u00AD", ""); // Remove non-breaking spaces & soft hyphens
			// MODIFIED: Squash multiple spaces, but PRESERVE newlines
			text = Regex.Replace(text, @"[ \t]{2,}", " ");
			return text;
		}

		public static string SanitizeJobDescription(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
				return string.Empty;
			text = text.Replace("\0", ""); // Remove null chars			
			text = Regex.Replace(text, "<.*?>", " "); // Remove HTML			
			text = Regex.Replace(text, @"https?:\/\/\S+", " "); // Remove URLs			
			text = Regex.Replace(text, @"\S+@\S+\.\S+", " "); // Remove emails			
			text = Regex.Replace(text, @"\+?\d[\d\s\-]{7,}\d", " "); // Remove phone numbers			
			text = Regex.Replace(text, @"[^\u0000-\u007F]+", " "); // Remove emojis / non-ASCII			
			text = Regex.Replace(text, @"[\u0000-\u001F]", " "); // Remove control chars			
			string[] noiseSections = {"about the company","about us", "how to apply",
			"equal opportunity employer", "benefits", "perks","apply now","company overview"};
			foreach (var section in noiseSections) // Remove boilerplate sections
			{
				var idx = text.IndexOf(section, StringComparison.OrdinalIgnoreCase);
				if (idx >= 0)
					text = text[..idx];
			}
			text = Regex.Replace(text, @"\s{2,}", " ").Trim(); // Normalize whitespace
			return text;
		}
		#endregion Santize Text

		public static bool IsNullOrEmpty<T>(this IEnumerable<T> source)
		{
			return source is null || !source.Any();
		}

		public static bool IsNotNullOrEmpty<T>(this IEnumerable<T> source)
		{
			return !source.IsNullOrEmpty();
		}

		/// <summary>
		/// Compute SHA-256 from Byte[] & return hex string (uppercase by default)
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public static string ComputeSha256Hex(this byte[] data)
		{
			if (data == null || data.Length == 0)
				return string.Empty;
			// Compute SHA-256
			using var sha = SHA256.Create();
			var hashBytes = sha.ComputeHash(data);
			// Convert to hex string (uppercase by default)
			var hex = Convert.ToHexString(hashBytes);
			return hex;
		}

		public static List<string> ChunkText(string text, int chunkSize = 2000)
		{
			if (string.IsNullOrWhiteSpace(text)) return [];
			var chunks = new List<string>();
			for (int i = 0; i < text.Length; i += chunkSize)
				chunks.Add(text.Substring(i, Math.Min(chunkSize, text.Length - i)));
			return chunks;
		}

		public static List<string> ChunkByParagraph(string text, int maxCharacters = 2000)
		{
			if (string.IsNullOrWhiteSpace(text)) return [];
			var chunks = new List<string>();
			// Split by common double-newline formats to find structural breaks
			var paragraphs = text.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
			var currentChunk = new StringBuilder();
			foreach (var paragraph in paragraphs)
			{
				var cleanParagraph = paragraph.Trim();
				if (string.IsNullOrEmpty(cleanParagraph)) continue;
				// If a single paragraph is larger than maxCharacters, dump the buffer and isolate it
				if (cleanParagraph.Length > maxCharacters)
				{
					if (currentChunk.Length > 0)
					{
						// Clean and save the existing buffer
						string bufferedText = Regex.Replace(currentChunk.ToString(), @"\s{2,}", " ").Trim();
						chunks.Add(bufferedText);
						currentChunk.Clear();
					}
					// Clean and save the oversized paragraph on its own
					string oversizedText = Regex.Replace(cleanParagraph, @"\s{2,}", " ").Trim();
					chunks.Add(oversizedText);
					continue;
				}
				// Check if adding this paragraph exceeds our chunk character limit
				if (currentChunk.Length + cleanParagraph.Length + 1 > maxCharacters)
				{
					// Clean and save the full chunk
					string fullChunkText = Regex.Replace(currentChunk.ToString(), @"\s{2,}", " ").Trim();
					chunks.Add(fullChunkText);
					currentChunk.Clear();
				}
				// Append paragraph with a single space divider inside the building block
				if (currentChunk.Length > 0)
					currentChunk.Append(' ');
				currentChunk.Append(cleanParagraph);
			}
			// Catch any remaining text in the buffer after loop finishes
			if (currentChunk.Length > 0)
			{
				string finalChunkText = Regex.Replace(currentChunk.ToString(), @"\s{2,}", " ").Trim();
				chunks.Add(finalChunkText);
			}
			return chunks;
		}
	}
}
