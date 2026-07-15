namespace ResumeAnalyzer.Domain.Constants
{
	public static class ResumeStatusValues
	{
		public const string Queued = "queued";
		public const string Processing = "processing";
		public const string Completed = "completed";
		public const string Failed = "failed";
		public const string Duplicate = "duplicate";
	}
	public static class ProcessingStepValues
	{
		public const string Uploaded = "uploaded";
		public const string Extracted = "extracted";
		public const string Sanitize = "sanitize";
		public const string Embedded = "embedded";
		public const string Chunked = "chunked";
		public const string Scored = "scored";
		public const string Done = "done";
		public const string Failed = "failed";
	}

	public static class CustomConstant
	{
		public static string ResumeProgress = "ResumeProgress";
		public static string[] EMPLOYEETYPES = ["FULLTIME", "PARTTIME", "CONTRACTOR", "INTERN"];
		public static string[] DATETYPES = ["all", "today", "3days", "week", "month"];
	}
}
