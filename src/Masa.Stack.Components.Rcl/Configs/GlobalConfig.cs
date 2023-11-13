namespace Masa.Stack.Components.Rcl.Configs;

public class GlobalConfig : IScopedDependency
{
    private const string DarkCookieKey = "GlobalConfig_IsDark";
    private const string MiniCookieKey = "GlobalConfig_NavigationMini";
    private const string FavoriteCookieKey = "GlobalConfig_Favorite";
    private const string LangCookieKey = "GlobalConfig_Lang";

    private readonly CookieStorage _cookieStorage;
    private readonly I18n _i18N;
    private bool _dark;
    private bool _mini;
    private string _favorite;
    private Guid _currentTeamId;

    public delegate void GlobalConfigChanged();

    public event GlobalConfigChanged? OnLanguageChanged;

    public delegate void CurrentTeamChanged(Guid teamId);

    public event CurrentTeamChanged? OnCurrentTeamChanged;

    public GlobalConfig(CookieStorage cookieStorage, I18n i18n)
    {
        _cookieStorage = cookieStorage;
        _i18N = i18n;
    }

    public CultureInfo Culture
    {
        get => _i18N.Culture;
        set
        {
            _i18N.SetCulture(value);
            _cookieStorage.SetAsync(LangCookieKey, value.Name);
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
            _cookieStorage.SetAsync(DarkCookieKey, value);
        }
    }

    public List<Nav> Menus { get; set; }

    public bool Mini
    {
        get => _mini;
        set
        {
            _mini = value;
            _cookieStorage?.SetAsync(MiniCookieKey, value);
        }
    }

    public string Favorite
    {
        get => _favorite;
        set
        {
            _favorite = value;
            _cookieStorage?.SetAsync(FavoriteCookieKey, value);
        }
    }

    public async void Initialization()
    {
        _dark = Convert.ToBoolean(await _cookieStorage.GetAsync(DarkCookieKey));
        bool.TryParse(await _cookieStorage.GetAsync(MiniCookieKey), out _mini);
        _favorite = await _cookieStorage.GetAsync(FavoriteCookieKey);

        var lang = await _cookieStorage.GetAsync(LangCookieKey);
        if (!string.IsNullOrWhiteSpace(lang))
        {
            _i18N.SetCulture(new CultureInfo(lang));
        }
    }
}
