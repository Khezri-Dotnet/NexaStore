
using System.ComponentModel.DataAnnotations;

namespace NexaStore.Application.Features.Authentication.DTOs;

public sealed record RefreshTokenRequest(
    [param: Required]
    [param: MinLength(32)]
    [param: MaxLength(512)]
    string RefreshToken);
