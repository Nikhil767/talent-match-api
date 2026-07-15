using System.Net.Http.Headers;
using System.Security.Claims;

namespace ResumeAnalyzer.Middleware
{
	public static class ClaimsHelper
	{
		/// <summary>Extract Supabase user id from JWT sub claim</summary>
		public static string GetUserId(this ClaimsPrincipal user) =>
			user.FindFirstValue(ClaimTypes.NameIdentifier)
			?? user.FindFirstValue("sub")
			?? throw new UnauthorizedAccessException("User ID not found in token");

		/// <summary>Extract Supabase user id from JWT sub claim</summary>
		public static Guid GetGuidUserId(this ClaimsPrincipal user)
		{
			var userId = GetUserId(user);
			if (Guid.TryParse(userId, out var guidUserId))
			{
				return guidUserId;
			}
			throw new FormatException("User ID is not a valid GUID");
		}

		/// <summary>
		/// Extracts the raw JWT Bearer token string from the Authorization header.
		/// </summary>
		/// <param name="context">The current HttpContext.</param>
		/// <returns>The raw token string if found; otherwise, null.</returns>
		public static string? GetBearerToken(this HttpContext context)
		{
			if (context == null) return null;
			// 1. Fetch the Authorization header value safely
			string? authHeader = context.Request.Headers.Authorization;
			if (string.IsNullOrWhiteSpace(authHeader)) return null;
			// 2. Use AuthenticationHeaderValue to cleanly parse the Scheme and Parameter
			if (AuthenticationHeaderValue.TryParse(authHeader, out var parsedHeader))
			{
				// Verify the scheme is indeed "Bearer"
				if (string.Equals(parsedHeader.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase))
				{
					return parsedHeader.Parameter; // Returns just the raw eyJ... token string
				}
			}
			return null;
		}
	}
}
