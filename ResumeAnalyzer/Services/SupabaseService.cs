using System.Text;
using System.Text.Json;

namespace ResumeAnalyzer.Services
{
	public class SupabaseService(IConfiguration config, HttpClient http)
	{
		private readonly string _url = config["Supabase:Url"]!;
		private readonly string _key = config["Supabase:AnonKey"]!;

		private HttpRequestMessage BuildAuthRequest(string path, object body)
		{
			var req = new HttpRequestMessage(HttpMethod.Post, $"{_url}/auth/v1/{path}");
			req.Headers.Add("apikey", _key);
			req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
			return req;
		}

		public async Task<(bool Success, string? Token, string? RefreshToken, string? UserId, string? Error)>
		RegisterAsync(string email, string password)
		{
			using var res = await http.SendAsync(BuildAuthRequest("signup", new { email, password }));
			var json = await res.Content.ReadFromJsonAsync<JsonElement>();

			if (!res.IsSuccessStatusCode)
			{
				//return (false, null, null, null, json.GetProperty("msg").GetString());
				json.TryGetProperty("msg", out JsonElement msgValue);
				return (false, null, null, null, msgValue.ToString());
			}

			// Supabase returns session on signup when email confirm is OFF
			if (json.TryGetProperty("access_token", out var token))
				return (true,
					token.GetString(),
					json.GetProperty("refresh_token").GetString(),
					json.GetProperty("user").GetProperty("id").GetString(),
					null);

			return (true, null, null,
				json.GetProperty("id").GetString(),  // email confirm required — no token yet
				null);
		}

		public async Task<(bool Success, string? Token, string? RefreshToken, string? UserId, string? Error)>
			LoginAsync(string email, string password)
		{
			using var res = await http.SendAsync(BuildAuthRequest("token?grant_type=password", new { email, password }));
			var json = await res.Content.ReadFromJsonAsync<JsonElement>();

			if (!res.IsSuccessStatusCode)
				return (false, null, null, null, json.GetProperty("error_description").GetString());

			return (true,
				json.GetProperty("access_token").GetString(),
				json.GetProperty("refresh_token").GetString(),
				json.GetProperty("user").GetProperty("id").GetString(),
				null);
		}

		public async Task<(bool Success, string? Token, string? Error)> RefreshAsync(string refreshToken)
		{
			var res = await http.SendAsync(BuildAuthRequest("token?grant_type=refresh_token", new { refresh_token = refreshToken }));
			var json = await res.Content.ReadFromJsonAsync<JsonElement>();

			if (!res.IsSuccessStatusCode)
				return (false, null, json.GetProperty("error_description").GetString());

			return (true, json.GetProperty("access_token").GetString(), null);
		}

		//private readonly Supabase.Client _client = new(
		//	config["Supabase:Url"]!,
		//	config["Supabase:AnonKey"]!,
		//	new SupabaseOptions { AutoRefreshToken = false }
		//);

		//public async Task<Session?> RegisterAsync(string email, string password)
		//	=> await _client.Auth.SignUp(email, password);

		//public async Task<Session?> LoginAsync(string email, string password)
		//	=> await _client.Auth.SignIn(email, password);

		//public async Task LogoutAsync()
		//	=> await _client.Auth.SignOut();
	}
}
