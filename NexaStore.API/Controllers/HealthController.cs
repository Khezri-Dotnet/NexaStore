
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using NexaStore.Infrastructure.Persistence;

namespace NexaStore.API.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    private readonly NexaStoreDbContext _db;

    public HealthController(NexaStoreDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult GetHealth()
    {
        return Ok(new
        {
            Status = "Healthy",
            Application = "NexaStore"
        });
    }

    [HttpGet("database")]
    public async Task<IActionResult> GetDatabaseHealth(
        CancellationToken cancellationToken)
    {
        var canConnect = await _db.Database.CanConnectAsync(
            cancellationToken);

        if (!canConnect)
        {
            return Problem(
                title: "Database connection failed",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        return Ok(new
        {
            Status = "Healthy",
            Database = "Connected",
            Provider = "PostgreSQL"
        });
    }
}
