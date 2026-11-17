using Microsoft.AspNetCore.Mvc;
using NexaStore.Application.Features.Authentication.Register;
using NexaStore.Application.Features.Authentication.Login;
using NexaStore.Application.Features.Authentication.DTOs;

namespace NexaStore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthenticationController : ControllerBase
{
    private readonly RegisterHandler _registerHandler;
    private readonly LoginHandler _loginHandler;

    public AuthenticationController(
        RegisterHandler registerHandler,
        LoginHandler loginHandler)
    {
        _registerHandler = registerHandler;
        _loginHandler = loginHandler;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var userId = await _registerHandler.Handle(
            new RegisterCommand(request.Email, request.Password),
            cancellationToken);

        return Ok(new { UserId = userId });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _loginHandler.Handle(
            new LoginCommand(request.Email, request.Password),
            cancellationToken);

        return Ok(response);
    }
}