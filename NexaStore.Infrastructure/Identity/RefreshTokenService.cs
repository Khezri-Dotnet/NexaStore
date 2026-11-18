
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NexaStore.Application.Abstractions.Authentication;
using NexaStore.Infrastructure.Persistence;

namespace NexaStore.Infrastructure.Identity;

public sealed class RefreshTokenService : IRefreshTokenService
{
    private readonly NexaStoreDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public RefreshTokenService(
        NexaStoreDbContext dbContext,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _configuration = configuration;
    }

    public async Task<string> CreateAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(64);
        var token = Convert.ToBase64String(tokenBytes);
        var tokenHash = HashToken(token);

        var expirationDays = int.TryParse(
            _configuration["Jwt:RefreshTokenExpirationDays"],
            out var days) ? days : 7;

        if (expirationDays <= 0)
            throw new InvalidOperationException(
                "Refresh token expiration must be positive.");

        var refreshToken = new RefreshToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(expirationDays)
        };

        _dbContext.RefreshTokens.Add(refreshToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return token;
    }

    public async Task<Guid?> ValidateAsync(
        string token,
        CancellationToken cancellationToken)
    {
        var tokenHash = HashToken(token);

        var refreshToken = await _dbContext.RefreshTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.TokenHash == tokenHash,
                cancellationToken);

        if (refreshToken is null ||
            refreshToken.RevokedAt is not null ||
            refreshToken.ExpiresAt <= DateTime.UtcNow)
        {
            return null;
        }

        return refreshToken.UserId;
    }

    public async Task RevokeAsync(
        string token,
        CancellationToken cancellationToken)
    {
        var tokenHash = HashToken(token);

        var refreshToken = await _dbContext.RefreshTokens
            .SingleOrDefaultAsync(
                x => x.TokenHash == tokenHash,
                cancellationToken);

        if (refreshToken is null || refreshToken.RevokedAt is not null)
            return;

        refreshToken.RevokedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string HashToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash);
    }


    public async Task<(Guid UserId, string RefreshToken)?> RotateAsync(
        string token,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var tokenHash = HashToken(token);

        var refreshToken = await _dbContext.RefreshTokens
            .FromSqlInterpolated(
                $"SELECT * FROM \"RefreshTokens\" WHERE \"TokenHash\" = {tokenHash} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

        if (refreshToken is null ||
            refreshToken.RevokedAt is not null ||
            refreshToken.ExpiresAt <= DateTime.UtcNow)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        var newTokenBytes = RandomNumberGenerator.GetBytes(64);
        var newToken = Convert.ToBase64String(newTokenBytes);
        var newTokenHash = HashToken(newToken);

        var expirationDays = int.TryParse(
            _configuration["Jwt:RefreshTokenExpirationDays"],
            out var days) ? days : 7;

        if (expirationDays <= 0)
            throw new InvalidOperationException(
                "Refresh token expiration must be positive.");

        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.ReplacedByTokenHash = newTokenHash;

        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = refreshToken.UserId,
            TokenHash = newTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(expirationDays)
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return (refreshToken.UserId, newToken);
    }

}
