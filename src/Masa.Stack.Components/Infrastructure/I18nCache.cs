namespace Masa.Stack.Components.Infrastructure;

public class I18nCache : IScopedDependency
{
    public Dictionary<string, string> Section { get; private set; } = new();
    public Dictionary<string, string> SappSection { get; private set; } = new();
    private readonly I18n _i18N;
    private readonly IDccClient _dccClient;
    private readonly ISappClient _sappClient;
    private readonly Extensions.OpenIdConnect.MasaOpenIdConnectOptions? _openIdOptions;
    private int _initializationVersion;

    public bool UseSappNav { get; set; }

    public event Action? OnSectionUpdated;

    public I18nCache(
        IClientScopeServiceProviderAccessor serviceProviderAccessor, IDccClient dccClient)
    {
        _i18N = serviceProviderAccessor.ServiceProvider.GetRequiredService<I18n>();
        _dccClient = dccClient;
        _sappClient = serviceProviderAccessor.ServiceProvider.GetRequiredService<ISappClient>();
        _openIdOptions = serviceProviderAccessor.ServiceProvider.GetService<Extensions.OpenIdConnect.MasaOpenIdConnectOptions>();

        if (_i18N != null)
        {
            _i18N.CultureChanged += async (object? sender, EventArgs arg) =>
            {
                await InitializeAsync();
            };
        }
    }

    public virtual async Task InitializeAsync()
    {
        var initializationVersion = Interlocked.Increment(ref _initializationVersion);
        var culture = _i18N.Culture.Name;
        Dictionary<string, string> section;
        Dictionary<string, string> sappSection;

        try
        {
            section = await _dccClient.OpenApiService.GetI18NConfigAsync(culture);
        }
        catch (Exception)
        {
            section = new();
        }

        if (UseSappNav)
        {
            try
            {
                var clientId = _openIdOptions?.ClientId ?? string.Empty;
                sappSection = string.IsNullOrWhiteSpace(clientId)
                    ? new()
                    : await _sappClient.GlobalNavService.GetI18NConfigByClientIdAsync(clientId, culture);
            }
            catch (Exception)
            {
                sappSection = new();
            }
        }
        else
        {
            sappSection = new();
        }

        if (initializationVersion != Volatile.Read(ref _initializationVersion))
        {
            return;
        }

        Section = section;
        SappSection = sappSection;
        OnSectionUpdated?.Invoke();
    }
}

