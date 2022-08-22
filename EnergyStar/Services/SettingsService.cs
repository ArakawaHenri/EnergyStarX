using EnergyStar.Contracts.Services;

namespace EnergyStar.Services;

class SettingsService : ISettingsService
{
    private readonly ILocalSettingsService _localSettingsService;

    public SettingsService(ILocalSettingsService localSettingsService)
    {
        _localSettingsService = localSettingsService;
    }

    public async Task InitializeAsync()
    {
        EnergyManager.EnergyManager.AlwaysThrottle = await _localSettingsService.ReadSettingAsync<string>("AlwaysThrottle") == "true";
        await Task.CompletedTask;
    }

    public async Task SaveSettingsAsync(string SettingsKey, string Value)
    {
        await _localSettingsService.SaveSettingAsync(SettingsKey, Value);
    }

    public async Task<string?> LoadFromSettingsAsync(string SettingsKey)
    {
        return await _localSettingsService.ReadSettingAsync<string>(SettingsKey);
    }
}
