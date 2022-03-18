using Microsoft.AspNetCore.Http;

namespace Masa.Stack.Components.Configs;

public class GlobalConfig
{
    private const string DarkCookieKey = "GlobalConfig_IsDark";
    private const string PageModeKey = "GlobalConfig_PageMode";
    private const string MiniCookieKey = "GlobalConfig_NavigationMini";
    private const string ExpandOnHoverCookieKey = "GlobalConfig_ExpandOnHover";
    private const string FavoriteCookieKey = "GlobalConfig_Favorite";

    private readonly CookieStorage? _cookieStorage;
    private readonly I18nConfig? _i18NConfig;
    private bool _dark;
    private string? _pageMode;
    private bool _mini;
    private bool _expandOnHover;
    private string _favorite;

    public delegate void GlobalConfigChanged();
    public event GlobalConfigChanged? OnPageModeChanged;
    public event GlobalConfigChanged? OnLanguageChanged;

    public GlobalConfig(CookieStorage cookieStorage, I18nConfig i18NConfig, IHttpContextAccessor httpContextAccessor)
    {
        _cookieStorage = cookieStorage;
        _i18NConfig = i18NConfig;

        if (httpContextAccessor.HttpContext is not null)
            Initialization(httpContextAccessor.HttpContext.Request.Cookies);
    }

    public string? Language
    {
        get => _i18NConfig?.Language;
        set
        {
            if (_i18NConfig is null)
            {
                return;
            }

            _i18NConfig.Language = value;
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

    public string PageMode
    {
        get => _pageMode ?? PageModes.PageTabs;
        set
        {
            _pageMode = value;
            _cookieStorage?.SetItemAsync(PageModeKey, value);
            OnPageModeChanged?.Invoke();
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
    
    public bool ExpandOnHover
    {
        get => _expandOnHover;
        set
        {
            _expandOnHover = value;
            _cookieStorage?.SetItemAsync(ExpandOnHoverCookieKey, value);
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
        _pageMode = cookies[PageModeKey];
        _mini = Convert.ToBoolean(cookies[MiniCookieKey]);
        _expandOnHover = Convert.ToBoolean(cookies[ExpandOnHoverCookieKey]);
        _favorite = cookies[FavoriteCookieKey];
    }
}