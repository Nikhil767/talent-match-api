using System.Net.ServerSentEvents;

namespace ResumeAnalyzer.Services.Sse;

public interface ISseBroker
{
    // The fully generic method your background workers will call
    void Publish<TData>(Guid userId, string eventType, string message, string flag, TData data);

    // Used exclusively by the Minimal API route
    IAsyncEnumerable<SseItem<object>> Subscribe(Guid userId, CancellationToken ct);

    // NEW: Forcefully kill a connection from anywhere in the backend
    bool Disconnect(Guid userId);

    // NEW: Debugging tool to check who is currently connected
    IEnumerable<Guid> GetActiveUserIds();
}
