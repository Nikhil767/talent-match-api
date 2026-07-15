using Microsoft.AspNetCore.Identity.Data;
using ResumeAnalyzer.Services;

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
					: Results.Ok(new { message = "registered", token = Token, refreshToken = RefreshToken, userId = UserId });
			}).AllowAnonymous();

			group.MapPost("/login", async (LoginRequest req, SupabaseService auth) =>
			{
				//var result = await auth.LoginAsync(req.Email, req.Password);
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

			//group.MapPost("/logout", async (SupabaseService auth) =>
			//{
			//	await auth.LogoutAsync();
			//	return Results.Ok();
			//}).RequireAuthorization();
		}
	}
}