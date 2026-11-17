namespace NexaStore.Application.Features.Authentication.DTOs;

public sealed record LoginRequest(
    string Email,
    string Password);