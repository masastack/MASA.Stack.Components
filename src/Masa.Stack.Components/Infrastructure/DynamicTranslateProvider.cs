namespace Masa.Stack.Components.Infrastructure;

public class DynamicTranslateProvider : IScopedDependency
{
    public const string I18N_KEY = "$public.i18n.";
    protected I18nCache I18nCache { get; }

    public DynamicTranslateProvider(IClientScopeServiceProviderAccessor clientScopeServiceProviderAccessor)
    {
        I18nCache = clientScopeServiceProviderAccessor.ServiceProvider.GetRequiredService<I18nCache>();
    }

    public string DT(string key)
    {
        string? value = null;
        if (I18nCache.UseSappNav)
        {
            value = I18nCache.SappSection?.GetValueOrDefault(key);
        }

        value ??= I18nCache.Section?.GetValueOrDefault(key);
        return value ?? key;
    }
}
