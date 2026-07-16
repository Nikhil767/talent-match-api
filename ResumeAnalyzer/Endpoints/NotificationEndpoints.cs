using ResumeAnalyzer.Services.Sse;

namespace ResumeAnalyzer.Endpoints
{
	public static class NotificationEndpoints
	{
		public static void MapNotificationEndpoints(this WebApplication app)
		{
			var adminGroup = app.MapGroup("/api/admin").WithTags("AdminNotifications").RequireAuthorization();
			// 1. GET active connections list
			adminGroup.MapGet("/sse/connections", (ISseBroker broker) =>
			{
				// Returns a list of all Guids currently holding open channels
				return Results.Ok(broker.GetActiveUserIds());
			});

			// 2. DELETE an active connection forcefully via API
			adminGroup.MapDelete("/sse/connections/{userId:guid}", (Guid userId, ISseBroker broker) =>
			{
				return DisconnectSSE(userId, broker);
			});


			var group = app.MapGroup("/api/notifications").WithTags("Notifications").RequireAuthorization();
			// POST /notifications/userId
			group.MapGet("/{userId:guid}", async (Guid userId, ISseBroker broker, CancellationToken ct) =>
			{
				return TypedResults.ServerSentEvents(broker.Subscribe(userId, ct));
			});

			// CLIENT DISCONNECT ENDPOINT
			// Clients hit this to manually drop their own real-time subscription stream
			group.MapDelete("/cancel/{userId:guid}", (Guid userId, ISseBroker broker) =>
			{
				return DisconnectSSE(userId, broker);
			});
		}

		private static IResult DisconnectSSE(Guid userId, ISseBroker broker)
		{
			// Call our in-memory broker to destroy the channel
			bool isClosed = broker.Disconnect(userId);
			if (isClosed)
			{
				return Results.Ok(new
				{
					Status = "DISCONNECTED",
					Message = $"Successfully dropped connection for user {userId}"
				});
			}
			return Results.Ok(new
			{
				Status = "NO_ACTIVE_STREAM",
				Message = "No active connection was found for your user ID."
			});
		}
	}
}
