namespace Masa.Stack.Components.Configs;

public class GlobalConfig : IScopedDependency
{
    private const string DarkCookieKey = "GlobalConfig_IsDark";
    private const string MiniCookieKey = "GlobalConfig_NavigationMini";
    private const string FavoriteCookieKey = "GlobalConfig_Favorite";
    private const string MenusKey = "GlobalConfig_Menus";

    private readonly CookieStorage? _cookieStorage;
    private readonly I18n? _i18N;
    private bool _dark;
    private bool _mini;
    private string _favorite;
    private List<Nav> _menus;
    private Guid _currentTeamId;

    public delegate void GlobalConfigChanged();

    public event GlobalConfigChanged? OnLanguageChanged;

    public delegate void CurrentTeamChanged(Guid teamId);

    public event CurrentTeamChanged? OnCurrentTeamChanged;

    public string Version { get; init; }

    public GlobalConfig(CookieStorage cookieStorage, I18n i18n, IHttpContextAccessor httpContextAccessor)
    {
        _cookieStorage = cookieStorage;
        _i18N = i18n;
        _menus = new();
        Version = "0.7.0";
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

    public List<Nav> Menus
    {
        get => _menus;
        set
        {
            _menus = value;
            _cookieStorage?.SetItemAsync(MenusKey, JsonSerializer.Serialize(value));
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
        if (cookies.TryGetValue(MenusKey, out string? value) && value != null)
        {
            _menus = JsonSerializer.Deserialize<List<Nav>>(value) ?? new();
        }
    }
}
