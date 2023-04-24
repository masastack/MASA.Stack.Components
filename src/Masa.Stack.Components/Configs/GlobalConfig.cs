namespace Masa.Stack.Components.Configs;

public class GlobalConfig : IScopedDependency
{
    private const string DarkCookieKey = "GlobalConfig_IsDark";
    private const string MiniCookieKey = "GlobalConfig_NavigationMini";
    private const string FavoriteCookieKey = "GlobalConfig_Favorite";

    private readonly CookieStorage? _cookieStorage;
    private readonly I18n? _i18N;
    private bool _dark;
    private bool _mini;
    private string _favorite;
    private Guid _currentTeamId;

    public delegate void GlobalConfigChanged();

    public event GlobalConfigChanged? OnLanguageChanged;

    public delegate void CurrentTeamChanged(Guid teamId);

    public event CurrentTeamChanged? OnCurrentTeamChanged;

    public GlobalConfig(CookieStorage cookieStorage, I18n i18n, IHttpContextAccessor httpContextAccessor)
    {
        _cookieStorage = cookieStorage;
        _i18N = i18n;
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

            OnLanguageChanged?.Invoke();
        }
    }

    public Guid CurrentTeamId
    {
        get
        {
            return _currentTeamId;
        }
        set
        {
            if (_currentTeamId != value)
            {
                _currentTeamId = value;
                OnCurrentTeamChanged?.Invoke(value);
            }
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

    public List<Nav> Menus { get; set; }

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
}
