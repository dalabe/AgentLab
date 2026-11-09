namespace Procesos.ChatApi.Dtos;

public sealed record ChatRequestDto(string? SessionId, string Message);

public sealed record ToolCallDto(string Name, IDictionary<string, object?>? Arguments, string? Result);

public sealed record ChatResponseDto(string SessionId, string Answer, IReadOnlyList<ToolCallDto> ToolCalls);
