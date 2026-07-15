namespace ResumeAnalyzer.Domain.Dto;

// A structured, uniform envelope for all real-time events
public record SsePayload<TData>(
    string Message,
    string Flag, // e.g., "PROCESSING", "SUCCESS", "FAILED"
    TData Data
);
