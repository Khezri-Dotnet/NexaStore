
using System.ComponentModel.DataAnnotations;

namespace NexaStore.Application.Features.Authentication.DTOs;

public sealed record RegisterRequest(
    [param: Required]
    [param: EmailAddress]
    [param: MaxLength(254)]
    string Email,

    [param: Required]
    [param: MinLength(12)]
    [param: MaxLength(128)]
    string Password);
