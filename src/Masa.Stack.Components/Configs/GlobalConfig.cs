using System.Globalization;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace Masa.Stack.Components.Configs;

public class GlobalConfig
{
    private const string DarkCookieKey = "GlobalConfig_IsDark";
    private const string MiniCookieKey = "GlobalConfig_NavigationMini";
    private const string FavoriteCookieKey = "GlobalConfig_Favorite";

    private readonly CookieStorage? _cookieStorage;
    private readonly I18n? _i18N;
    private bool _dark;
    private bool _mini;
    private string _favorite;

    public delegate void GlobalConfigChanged();

    public event GlobalConfigChanged? OnLanguageChanged;

    public GlobalConfig(CookieStorage cookieStorage, I18n i18n, IHttpContextAccessor httpContextAccessor)
    {
        _cookieStorage = cookieStorage;
        _i18N = i18n;

        SetCultureInfo(i18n.Culture);

        if (httpContextAccessor.HttpContext is not null)
            Initialization(httpContextAccessor.HttpContext.Request.Cookies);
    }

    public CultureInfo? Culture
    {
        get => _i18N?.Culture;
        set
        {
            if (_i18N is null)
            {
                return;
            }

            _i18N.SetCulture(value);
            SetCultureInfo(value);

            OnLanguageChanged?.Invoke();
        }
    }

    public bool Dark
    {
        get => _dark;
        set
        {
            _dark = value;
            _cookieStorage?.SetItemAsync(DarkCookieKey, value);
        }
    }

    public bool Mini
    {
        get => _mini;
        set
        {
            _mini = value;
            _cookieStorage?.SetItemAsync(MiniCookieKey, value);
        }
    }

    public string Favorite
    {
        get => _favorite;
        set
        {
            _favorite = value;
            _cookieStorage?.SetItemAsync(FavoriteCookieKey, value);
        }
    }

    private void Initialization(IRequestCookieCollection cookies)
    {
        _dark = Convert.ToBoolean(cookies[DarkCookieKey]);
        _mini = !cookies.ContainsKey(MiniCookieKey) || Convert.ToBoolean(cookies[MiniCookieKey]);
        _favorite = cookies[FavoriteCookieKey];
    }

    private void SetCultureInfo(CultureInfo culture)
    {
        ValidatorOptions.Global.LanguageManager.Culture = culture;
    }
}
