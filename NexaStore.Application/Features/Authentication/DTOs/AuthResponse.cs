namespace NexaStore.Application.Features.Authentication.DTOs;

public sealed record AuthResponse(
    Guid UserId,
    string Email,
    string Token);