using ResumeAnalyzer.Domain.Dto;
using System.Collections.Concurrent;
using System.Net.ServerSentEvents;
using System.Threading.Channels;

namespace ResumeAnalyzer.Services.Sse;

public class SseBroker : ISseBroker
{
    private readonly ConcurrentDictionary<Guid, Channel<SseItem<object>>> _connections = new();

    public IAsyncEnumerable<SseItem<object>> Subscribe(Guid userId, CancellationToken ct)
    {
        var channel = Channel.CreateBounded<SseItem<object>>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });
        _connections[userId] = channel;
        ct.Register(() =>
        {
            _connections.TryRemove(userId, out _);
            channel.Writer.TryComplete();
        });
        return channel.Reader.ReadAllAsync(ct);
    }

    public void Publish<TData>(Guid userId, string eventType, string message, string flag, TData data)
    {
        if (_connections.TryGetValue(userId, out var channel))
        {
            var envelope = new SsePayload<TData>(message, flag, data);
            var sseItem = new SseItem<object>(envelope, eventType)
            {
                EventId = Guid.NewGuid().ToString()
            };
            channel.Writer.TryWrite(sseItem);
        }
    }

    // IMPLEMENTATION: Closes the stream channel completely
    public bool Disconnect(Guid userId)
    {
        if (_connections.TryRemove(userId, out var channel))
        {
            // Telling the writer it is complete forces the IAsyncEnumerable stream to finish.
            // This cleanly breaks the HTTP connection on the server end.
            channel.Writer.TryComplete(); 
            return true;
        }
        return false;
    }

    public IEnumerable<Guid> GetActiveUserIds()
    {
        return _connections.Keys;
    }
}
