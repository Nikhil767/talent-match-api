using Microsoft.AspNetCore.Authorization;
using StackExchange.Redis;
using System.Security.Claims;

namespace ResumeAnalyzer.Middleware
{
	public class TokenBlacklistMiddleware
	{
		private readonly RequestDelegate _next;
		private readonly IDatabase _redisDb;

		public TokenBlacklistMiddleware(RequestDelegate next, IConnectionMultiplexer redis)
		{
			_next = next;
			_redisDb = redis.GetDatabase();
		}

		public async Task InvokeAsync(HttpContext context)
		{
			var endpoint = context.GetEndpoint();
			if (endpoint == null)
			{
				await _next(context);
				return;
			}
			// 2. CHECK: Does this endpoint require Authorization? 
			// Handles both [Authorize] attributes and .RequireAuthorization() Minimal APIs
			var requiresAuth = endpoint.Metadata.GetMetadata<IAuthorizeData>() != null;
			if (!requiresAuth)
			{
				await _next(context);
				return;
			}
			var sessionId = context.User?.FindFirst("session_id")?.Value;
			var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(sessionId))
			{
				bool isBlacklisted = await _redisDb.KeyExistsAsync($"blacklist:{userId}:{sessionId}");
				if (isBlacklisted)
				{
					context.Response.StatusCode = StatusCodes.Status401Unauthorized;
					context.Response.ContentType = "application/json";
					await context.Response.WriteAsJsonAsync(new { error = "Token has been revoked/logged out." });
					return; // Short-circuits the request pipeline!
				}
			}
			await _next(context);
		}
	}
}
