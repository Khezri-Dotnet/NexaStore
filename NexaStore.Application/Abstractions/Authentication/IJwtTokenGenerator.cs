namespace NexaStore.Application.Abstractions.Authentication;

public interface IJwtTokenGenerator
{
    string GenerateToken(Guid userId, string email, IList<string> roles);
}