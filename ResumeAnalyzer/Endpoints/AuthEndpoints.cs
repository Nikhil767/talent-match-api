using Microsoft.AspNetCore.Identity.Data;
using ResumeAnalyzer.Services;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ResumeAnalyzer.Endpoints
{
	public static class AuthEndpoints
	{
		public static void MapAuthEndpoints(this WebApplication app)
		{
			var group = app.MapGroup("/api/auth").WithTags("Auth");
			group.MapPost("/register", async (RegisterRequest req, SupabaseService auth) =>
			{
				//var result = await auth.RegisterAsync(req.Email, req.Password);
				(bool Success, string? Token, string? RefreshToken, string? UserId, string? Error) =
				await auth.RegisterAsync(req.Email, req.Password);
				return (Success is false || !string.IsNullOrWhiteSpace(Error))
					? Results.BadRequest($"Error: {Error}")
					: Results.Ok(new
					{
						status = "registered",
						token = Token,
						refreshToken = RefreshToken,
						userId = UserId,
						message = "Registration successfull. Please check your email inbox to verify your account."
					});
			}).AllowAnonymous();

			group.MapPost("/login", async (LoginRequest req, SupabaseService auth) =>
			{
				(bool Success, string? Token, string? RefreshToken, string? UserId, string? Error)
				= await auth.LoginAsync(req.Email, req.Password);
				return Success != true
					? Results.Unauthorized()
					: Results.Ok(new
					{
						token = Token,
						refreshToken = RefreshToken,
						userId = UserId
					});
			}).AllowAnonymous();

			group.MapPost("/logout", async (HttpContext context, IConnectionMultiplexer redis) =>
			{
				var authHeader = context.Request.Headers["Authorization"].ToString();
				if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
					return Results.BadRequest(new { message = "Missing or invalid Authorization header" });
				var expClaim = context.User?.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;
				if (string.IsNullOrEmpty(expClaim))
					return Results.BadRequest(new { message = "Invalid token claims (Exp)." });
				var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
				if (string.IsNullOrEmpty(userId))
					return Results.BadRequest(new { message = "Invalid token claims (Sub)." });
				var sessionId = context.User?.FindFirst("session_id")?.Value;
				if (string.IsNullOrEmpty(sessionId))
					return Results.BadRequest(new { message = "Invalid token claims (session_id)." });

				var expirationTimeUnix = long.Parse(expClaim);
				var expirationDateTime = DateTimeOffset.FromUnixTimeSeconds(expirationTimeUnix);
				var remainingTime = expirationDateTime - DateTimeOffset.UtcNow;
				if (remainingTime > TimeSpan.Zero)
				{
					var db = redis.GetDatabase();
					// Save to Upstash with the exact expiration remaining
					await db.StringSetAsync(
						key: $"bl:{userId}:{sessionId}",
						value: "revoked",
						expiry: remainingTime
					);
				}
				return Results.Ok(new { message = "Logged out successfully" });
			}).RequireAuthorization();
		}
	}
}