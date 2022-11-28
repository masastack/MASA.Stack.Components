namespace Masa.Stack.Components.Infrastructure;

public class DynamicTranslateProvider : IScopedDependency
{
    public const string I18N_KEY = "$public.i18n.";

    readonly IMemoryCache _memoryCache;
    readonly IMasaConfiguration _masaConfiguration;
    readonly I18n _i18N;

    public DynamicTranslateProvider(IMemoryCache memoryCache, IMasaConfiguration masaConfiguration, I18n i18N)
    {
        _memoryCache = memoryCache;
        _masaConfiguration = masaConfiguration;
        _i18N = i18N;
    }

    public string DT(string key)
    {
        var culture = _i18N.Culture.Name;
        var i18n_key = $"{I18N_KEY}{culture}";
        var section = _memoryCache.GetOrCreate(i18n_key, (entry) =>
        {
            entry.SlidingExpiration = TimeSpan.FromSeconds(10);
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
            return _masaConfiguration.ConfigurationApi.GetPublic().GetSection(i18n_key);
        });
        var value = section.GetValue<string>(key);
        return value ?? key;
    }
}
