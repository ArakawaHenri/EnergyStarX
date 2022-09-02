namespace EnergyStar.Contracts.Services;

public interface IActivationService
{
    Task ActivateAsync(object activationArgs);
    Task SilentActivateAsync(object activationArgs);
}
