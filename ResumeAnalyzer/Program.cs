using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ResumeAnalyzer.Background;
using ResumeAnalyzer.Domain;
using ResumeAnalyzer.Domain.Repositories;
using ResumeAnalyzer.Endpoints;
using ResumeAnalyzer.Services;
using ResumeAnalyzer.Services.Facade;
using ResumeAnalyzer.Services.Polly;
using ResumeAnalyzer.Services.Providers;
using ResumeAnalyzer.Services.Sse;
using ResumeAnalyzer.Services.Strategy;
using System.Security.Cryptography;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

#if DEBUG
// Read the PORT environment variable provided by Render, defaulting to 8080
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
#endif

// 1 Configurations
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var url = builder.Configuration["Supabase:Url"];
var projectId = builder.Configuration["Supabase:ProjectId"];
var jwtSecret = builder.Configuration["Supabase:JwtSecret"];
var anonKey = builder.Configuration["Supabase:AnonKey"];
var qdrantHost = builder.Configuration["Qdrant:Host"];
var qdrantApiKey = builder.Configuration["Qdrant:ApiKey"];
var tokenLimit = builder.Configuration.GetValue<int>("TokenLimit");
tokenLimit = tokenLimit == 0 ? 20 : tokenLimit;
var tokensPerPeriod = builder.Configuration.GetValue<int>("TokensPerPeriod");
tokensPerPeriod = tokensPerPeriod == 0 ? 20 : tokensPerPeriod;
var retryAfter = builder.Configuration["RetryAfter"];
var maxRequestBodySize = builder.Configuration.GetValue<int>("MaxRequestBodySize");
maxRequestBodySize = maxRequestBodySize == 0 ? 5 : maxRequestBodySize;

// 1. Hook up Npgsql EF Core Engine Configuration
builder.Services.AddDbContext<AppDbContext>(options =>
	options.UseNpgsql(connectionString));

// Add generic injections if needed fallback strategy
//builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
// 2. Register Repositories Core Framework Infrastructure
builder.Services.AddScoped<IResumeRepository, ResumeRepository>();
builder.Services.AddScoped<IResumeAnalysisRepository, ResumeAnalysisRepository>();
builder.Services.AddScoped<IResumeJobMatchRepository, ResumeJobMatchRepository>();
builder.Services.AddScoped<IJobRepository, JobRepository>();
// 3. Register Core Background Workers & Custom Queues 
builder.Services.AddSingleton<AnalysisQueue>();
builder.Services.AddHostedService<ResumeProcessingWorker>();

// ---------------------------------------------------------
// 1. Add Services
// ---------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
	options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});
builder.Services.AddOpenApi();
builder.Services.AddRateLimiter(options =>
{
	options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
	options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
	{
		var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
		options.OnRejected = (context, token) =>
		{
			context.HttpContext.Response.Headers["Retry-After"] = retryAfter;
			return ValueTask.CompletedTask;
		};
		return RateLimitPartition.GetTokenBucketLimiter(
			partitionKey: ip,
			factory: _ => new TokenBucketRateLimiterOptions
			{
				TokenLimit = tokenLimit,                     // Max tokens
				TokensPerPeriod = tokensPerPeriod,           // Refill amount
				ReplenishmentPeriod = TimeSpan.FromMinutes(1),
				QueueLimit = 0,
				AutoReplenishment = true
			});
	});
});

// set request size at Kestrel level 
builder.WebHost.ConfigureKestrel(options =>
{
	options.Limits.MaxRequestBodySize = maxRequestBodySize * 1024 * 1024;
});

// Supabase JWT Auth (JWKS)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(o =>
	{
		o.RequireHttpsMetadata = true;
		// 1. Reconstruct the EC Public Key using the exact coordinates from your browser output
		var ecParameters = new ECParameters
		{
			Curve = ECCurve.NamedCurves.nistP256,
			Q = new ECPoint
			{
				X = Base64UrlEncoder.DecodeBytes("fquKHQ0kW5VPQUwv5i_qf3T30YkEC4fdHbWNkzqon4g"),
				Y = Base64UrlEncoder.DecodeBytes("Gwagm2fGccpilZUlBuBGr4exCuU7PbhrhsOryN-bGM0")
			}
		};
		var ecdsaKey = new ECDsaSecurityKey(ECDsa.Create(ecParameters))
		{
			KeyId = jwtSecret // Binds explicitly to your token's kid
		};
		o.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidIssuer = $"{url}/auth/v1",
			ValidateAudience = true,
			ValidAudience = "authenticated",
			ValidateIssuerSigningKey = true,
			IssuerSigningKey = ecdsaKey, // Feed the ECDsa cryptographic engine directly
			ClockSkew = TimeSpan.Zero
		};
		o.Events = new JwtBearerEvents
		{
			OnAuthenticationFailed = context =>
			{
				// Set a breakpoint here to see why it fails
				var errorMessage = context.Exception.Message;
				System.Diagnostics.Debug.WriteLine($"JWT Auth Failed: {errorMessage}");
				return Task.CompletedTask;
			}
		};
	});

// Authorization
builder.Services.AddAuthorization();
builder.Services.AddHealthChecks();

// ---------------------------------------------------------
// 2. Dependency Injection (your services)
// ---------------------------------------------------------

// Polly policies
builder.Services.AddSingleton<PollyPolicies>();

// Named HttpClients
builder.Services.AddHttpClient("supabase-storage", client =>
{
	client.BaseAddress = new Uri(url!);
	client.DefaultRequestHeaders.Add("apikey", anonKey);
	client.DefaultRequestHeaders.Add("Authorization", $"Bearer {builder.Configuration["Supabase:service_role"]}");
})
.AddPolicyHandler((sp, req) => sp.GetRequiredService<PollyPolicies>().RetryPolicy);

builder.Services.AddHttpClient("huggingface", client =>
{
	client.BaseAddress = new Uri(builder.Configuration["HuggingFace:Url"]!);
	client.DefaultRequestHeaders.Add("Authorization", $"Bearer {builder.Configuration["HuggingFace:ApiKey"]!}");
})
.AddPolicyHandler((sp, req) => sp.GetRequiredService<PollyPolicies>().RetryPolicy);

builder.Services.AddHttpClient("groq", client =>
{
	client.BaseAddress = new Uri(builder.Configuration["Groq:Url"]!);
	client.DefaultRequestHeaders.Add("Authorization", $"Bearer {builder.Configuration["Groq:ApiKey"]!}");
})
.AddPolicyHandler((sp, req) => sp.GetRequiredService<PollyPolicies>().RetryPolicy);

builder.Services.AddHttpClient("gemini", client =>
{
	client.BaseAddress = new Uri(builder.Configuration["Gemini:Url"]!);
	client.DefaultRequestHeaders.Add("x-goog-api-key", builder.Configuration["Gemini:ApiKey"]!);
})
.AddPolicyHandler((sp, req) => sp.GetRequiredService<PollyPolicies>().RetryPolicy);

builder.Services.AddHttpClient("job-search", client =>
{
	client.BaseAddress = new Uri(builder.Configuration["JSearch:Url"]!);
	client.DefaultRequestHeaders.Add("X-RapidAPI-Key", builder.Configuration["JSearch:ApiKey"]!);
	client.DefaultRequestHeaders.Add("X-RapidAPI-Host", builder.Configuration["JSearch:Host"]!);
});

//builder.Services.AddHttpClient("openai", client =>
//{
//	client.BaseAddress = new Uri(builder.Configuration["OpenAI:Url"]!);
//})
//.AddPolicyHandler((sp, req) => sp.GetRequiredService<PollyPolicies>().RetryPolicy);

// Providers
builder.Services.AddSingleton<EmbeddingService>();
builder.Services.AddSingleton<GroqService>();
builder.Services.AddSingleton<GeminiService>();
//builder.Services.AddSingleton<OpenAiMiniService>();
builder.Services.AddSingleton<SupabaseStorageRestService>();

// Strategies
builder.Services.AddSingleton<IEmbeddingStrategy, EmbeddingStrategy>();
builder.Services.AddSingleton<IAnalysisStrategy, AnalysisStrategy>();
builder.Services.AddSingleton<ITailorStrategy, TailorStrategy>();

// Register Strategy & Pipeline Facades
builder.Services.AddScoped<ResumePipelineService>();
builder.Services.AddSingleton<VectorService>();
builder.Services.AddSingleton<JobIngestionService>();
builder.Services.AddHttpClient<SupabaseService>();
builder.Services.AddSingleton<ISseBroker, SseBroker>();
//builder.Services.AddScoped<CloudflareR2Service>();

// ---------------------------------------------------------
// 3. Build App
// ---------------------------------------------------------
var app = builder.Build();
// ---------------------------------------------------------
// 4. Middleware - Configure the HTTP request pipeline.
// ---------------------------------------------------------

// 1. Global exception handler
app.UseExceptionHandler(errorApp =>
{
	errorApp.Run(async context =>
	{
		context.Response.ContentType = "application/json";
		context.Response.StatusCode = 500;
		var problem = new
		{
			error = "ServerError",
			message = "An unexpected error occurred."
		};
		await context.Response.WriteAsJsonAsync(problem);
	});
});

// --- STARTUP AUTOMATIC DATABASE CREATION LOGIC ---
using (var scope = app.Services.CreateScope())
{
	var services = scope.ServiceProvider;
	try
	{
		var context = services.GetRequiredService<AppDbContext>();
		await context.Database.EnsureCreatedAsync();
		var vectoreService = services.GetRequiredService<VectorService>();
		await vectoreService.EnsureCollectionAsync(builder.Configuration["Qdrant:QdrantCollections:Resumes"]!);
		await vectoreService.EnsureCollectionAsync(builder.Configuration["Qdrant:QdrantCollections:Jobs"]!);
	}
	catch (Exception ex)
	{
		var logger = services.GetRequiredService<ILogger<Program>>();
		logger.LogError(ex, "An error occurred creating or migrating the PostgreSQL database schema structural layout.");
	}
}
// -------------------------------------------------

// 2. Redirect HTTP → HTTPS
app.UseHttpsRedirection();
// 3. CORS before auth
app.UseCors();
// 4. Rate limiting before auth
app.UseRateLimiter();
// 5. Auth before authorization
app.UseAuthentication();
// 6. Authorization
app.UseAuthorization();
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
	app.MapOpenApi();
}

// ---------------------------------------------------------
// 5. Map Endpoints
// ---------------------------------------------------------

// MAP LIVENESS ENDPOINT (/alive)
// Returns instantly with HTTP 200 "Healthy" if the app isn't deadlocked.
app.MapHealthChecks("/alive", new HealthCheckOptions
{
	Predicate = check => check.Tags.Contains("live")
});

app.MapAuthEndpoints();
app.MapResumeEndpoints();
app.MapAnalysisEndpoints();
app.MapJobEndpoints();
app.MapNotificationEndpoints();

app.Run();