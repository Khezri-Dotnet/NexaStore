namespace NexaStore.Application.Features.Authentication.DTOs;

public sealed record RegisterRequest(
    string Email,
    string Password);