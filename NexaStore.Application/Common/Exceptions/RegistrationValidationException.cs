namespace NexaStore.Application.Common.Exceptions;

public sealed class RegistrationValidationException : Exception
{
    public RegistrationValidationException()
        : base("User registration failed.")
    {
    }
}