namespace EnergyStar.Contracts.Services;

public interface ISettingsService
{
    Task InitializeAsync();

    Task SaveSettingsAsync(string SettingsKey, string Value);
}
