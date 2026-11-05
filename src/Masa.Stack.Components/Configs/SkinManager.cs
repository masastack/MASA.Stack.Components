namespace Masa.Stack.Components.Configs;

/// <summary>
/// Applies a skin from the <c>Masa.Stack.Skins</c> catalog. It is the only writer of the appearance channel
/// <c>&lt;html data-skin data-theme data-skin-profile&gt;</c> and the only place that registers skin colours with MASA Blazor,
/// so host projects never write either.
/// </summary>
/// <remarks>
/// Without <see cref="MasaStackComponentOptions.UseSkinCatalog"/> only the default skin exists: no catalog is read and
/// only the dark mode and the custom primary colour are handled.
/// </remarks>
public sealed class SkinManager : IAsyncDisposable
{
    private const string LegacyDefaultId = "default";
    private const string LightTheme = "light";
    private const string DarkTheme = "dark";

    private readonly MasaBlazor _masa;
    private readonly IJSRuntime _js;
    private readonly GlobalConfig _config;
    private readonly SkinCatalogOptions? _options;
    private readonly System.Threading.SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<string, (string? Primary, string? OnPrimary)> _originalPrimaries = new();
    private Dictionary<string, SkinCatalogEntry> _entries = new();
    private IJSObjectReference? _module;
    private SkinCatalog? _catalog;
    private string _assetDirectory = string.Empty;
    private bool _initialized;

    public IReadOnlyList<MasaStackSkin> Skins { get; private set; } = new[] { MasaStackSkin.Default };

    public MasaStackSkin? Current { get; private set; } = MasaStackSkin.Default;

    public bool IsDark => _config.Dark;

    public string? CustomPrimaryColor { get; private set; }

    /// <summary>
    /// Whether the optional component layer is wanted (opt-in; <see cref="SkinCatalogOptions.ComponentLayer"/> is the initial value, the
    /// saved choice wins). It only has an effect for skins with <see cref="MasaStackSkin.HasComponentLayer"/>.
    /// </summary>
    public bool ComponentLayerEnabled { get; private set; }

    /// <summary>The component layer is loaded and switched on for the current skin.</summary>
    public bool ComponentLayerActive => ComponentLayerEnabled && Current is { HasComponentLayer: true };

    public string EffectivePrimaryColor => _masa.Theme.CurrentTheme.Primary ?? MasaStackSkin.Default.PrimaryColor;

    /// <summary>Why the catalog could not be used (the default skin stays active); <c>null</c> when there is no problem.</summary>
    public string? CatalogError { get; private set; }

    public event Action? Changed;

    public SkinManager(MasaBlazor masa, IJSRuntime js, GlobalConfig config, SkinCatalogOptions? options = null)
    {
        _masa = masa;
        _js = js;
        _config = config;
        _options = options;
    }

    public Task InitializeAsync() => ExecuteAsync(() => Task.CompletedTask);

    public Task SelectAsync(string id) => ExecuteAsync(async () =>
    {
        var skin = Skins.FirstOrDefault(s => s.Id == NormalizeId(id));
        if (skin is null || skin == Current) return;
        Current = skin;
        await ApplyAsync(persist: true);
    });

    public Task SetDarkAsync(bool dark) => ExecuteAsync(async () =>
    {
        await _config.SetDarkAsync(dark);
        await ApplyAsync(persist: true);
    });

    /// <summary>Opts in or out of the optional component layer of the current (and every later) skin.</summary>
    public Task SetComponentLayerAsync(bool enabled) => ExecuteAsync(async () =>
    {
        if (ComponentLayerEnabled == enabled) return;
        ComponentLayerEnabled = enabled;
        await ApplyAsync(persist: true);
    });

    public Task SetPrimaryColorAsync(string? color) => ExecuteAsync(async () =>
    {
        var normalized = NormalizeColor(color);
        if (color is not null && normalized is null) return;
        CustomPrimaryColor = normalized;
        await ApplyAsync(persist: true);
    });

    private async Task ExecuteAsync(Func<Task> action)
    {
        await _gate.WaitAsync();
        try
        {
            await InitializeCoreAsync();
            await action();
        }
        finally { _gate.Release(); }
    }

    private async Task InitializeCoreAsync()
    {
        if (_initialized) return;
        _module ??= await _js.InvokeAsync<IJSObjectReference>("import", "./_content/Masa.Stack.Components/js/skin.js");
        var saved = NormalizeId(await _module.InvokeAsync<string?>("getSavedSkin"));
        CustomPrimaryColor = NormalizeColor(await _module.InvokeAsync<string?>("getSavedPrimaryColor"));
        ComponentLayerEnabled = await _module.InvokeAsync<string?>("getSavedComponentLayer") switch
        {
            "1" => true,
            "0" => false,
            _ => _options?.ComponentLayer ?? false,
        };

        foreach (var name in new[] { LightTheme, DarkTheme })
        {
            var theme = _masa.Theme.Themes[name];
            _originalPrimaries[name] = (theme.Primary, theme.OnPrimary);
        }

        if (_options is not null) await LoadCatalogAsync();

        var defaultId = _catalog is null ? MasaStackSkin.DefaultId : DefaultSkinId(_catalog, _options!);
        Current = Skins.FirstOrDefault(s => s.Id == saved)
                  ?? Skins.FirstOrDefault(s => s.Id == defaultId)
                  ?? MasaStackSkin.Default;
        await ApplyAsync(persist: false);
        _initialized = true;
    }

    private async Task LoadCatalogAsync()
    {
        try
        {
            var json = await _module!.InvokeAsync<string>("loadCatalog", _options!.CatalogPath);
            var catalog = SkinCatalog.Parse(json);
            _assetDirectory = _options.CatalogPath[..(_options.CatalogPath.LastIndexOf('/') + 1)];
            _entries = catalog.Skins.ToDictionary(s => s.Id);
            Skins = catalog.Skins
                .Where(s => s.Id == catalog.Default || _options.Include is null || _options.Include.Contains(s.Id))
                .Select(s => ToSkin(s, _assetDirectory))
                .ToArray();
            _catalog = catalog;
            CatalogError = null;
        }
        catch (Exception ex) when (ex is JSException or JsonException or InvalidOperationException)
        {
            // The host keeps working with the Stack default look; the cause stays inspectable.
            _catalog = null;
            CatalogError = ex.Message;
        }
    }

    private async Task ApplyAsync(bool persist)
    {
        var skin = Current!;
        var entry = _catalog is not null && _entries.TryGetValue(skin.Id, out var e) ? e : null;
        var tokens = entry is null || string.IsNullOrEmpty(entry.Css) ? string.Empty : _assetDirectory + entry.Css;
        var baseCss = entry is null || string.IsNullOrEmpty(_catalog!.Base) ? string.Empty : _assetDirectory + _catalog.Base;

        // The optional component layer (opt-in) is a third lazy stylesheet; it is only requested while it is wanted and exists.
        var componentsCss = ComponentLayerEnabled && entry?.Components is { Css.Length: > 0 } layer ? _assetDirectory + layer.Css : string.Empty;

        // Wait for the stylesheets first; this changes nothing on screen. Everything below runs in one task, so the roles
        // MASA writes and the appearance channel the skin css keys on become visible in the same frame.
        await _module!.InvokeVoidAsync("prepareSkin", tokens, baseCss, componentsCss);

        _masa.UpdateTheme(theme =>
        {
            if (entry is not null)
            {
                // The skin owns all 27 roles and the opacities of both schemes; nothing of a previous skin is left behind.
                if (entry.Themes.TryGetValue(LightTheme, out var light)) SkinThemeMapper.Apply(theme.Themes.Light, light);
                if (entry.Themes.TryGetValue(DarkTheme, out var dark)) SkinThemeMapper.Apply(theme.Themes.Dark, dark);
            }
            else
            {
                foreach (var (name, original) in _originalPrimaries)
                {
                    theme.Themes[name].Primary = original.Primary;
                    theme.Themes[name].OnPrimary = original.OnPrimary;
                }
            }

            if (CustomPrimaryColor is { } custom)
            {
                var onPrimary = GetOnPrimary(custom);
                foreach (var scheme in new[] { theme.Themes.Light, theme.Themes.Dark })
                {
                    scheme.Primary = custom;
                    scheme.OnPrimary = onPrimary;
                }
            }
        });
        _masa.SetTheme(IsDark ? DarkTheme : LightTheme);

        await _module!.InvokeVoidAsync("applySkin", skin.Id, ProfileName(skin.Profile), tokens, baseCss, IsDark, persist,
            CustomPrimaryColor, _masa.Theme.CurrentTheme.OnPrimary, componentsCss, ComponentLayerEnabled);
        Changed?.Invoke();
    }

    private static string DefaultSkinId(SkinCatalog catalog, SkinCatalogOptions options)
    {
        var id = NormalizeId(options.DefaultSkinId);
        return id is not null && catalog.Skins.Any(s => s.Id == id) ? id : catalog.Default;
    }

    /// <summary>Maps empty and the pre-catalog id <c>default</c> to the catalog default; other ids pass through.</summary>
    private static string? NormalizeId(string? id) =>
        string.IsNullOrWhiteSpace(id) ? null : id == LegacyDefaultId ? MasaStackSkin.DefaultId : id;

    private static MasaStackSkin ToSkin(SkinCatalogEntry entry, string assetDirectory) => new()
    {
        Id = entry.Id,
        DisplayName = entry.Name,
        Profile = string.Equals(entry.Profile, "identity", StringComparison.OrdinalIgnoreCase)
            ? SkinProfile.Identity
            : SkinProfile.Themed,
        PrimaryColor = entry.Themes.TryGetValue(LightTheme, out var light) && light.Roles.TryGetValue("primary", out var p)
            ? p
            : MasaStackSkin.Default.PrimaryColor,
        CssHref = string.IsNullOrEmpty(entry.Css) ? string.Empty : assetDirectory + entry.Css,
        HasComponentLayer = entry.Components is { Css.Length: > 0 },
    };

    private static string ProfileName(SkinProfile profile) => profile == SkinProfile.Identity ? "identity" : "themed";

    private static string? NormalizeColor(string? color)
    {
        if (color is null) return null;
        var hex = color.Trim();
        if (hex.StartsWith('#')) hex = hex[1..];
        if ((hex.Length != 3 && hex.Length != 6) || hex.Any(c => !Uri.IsHexDigit(c))) return null;
        if (hex.Length == 3) hex = string.Concat(hex.Select(c => new string(c, 2)));
        return "#" + hex.ToUpperInvariant();
    }

    private static string GetOnPrimary(string color)
    {
        var rgb = Convert.FromHexString(color[1..]);
        static double Linear(byte value)
        {
            var channel = value / 255d;
            return channel <= .04045 ? channel / 12.92 : Math.Pow((channel + .055) / 1.055, 2.4);
        }
        var luminance = .2126 * Linear(rgb[0]) + .7152 * Linear(rgb[1]) + .0722 * Linear(rgb[2]);
        return (luminance + .05) / .05 >= 1.05 / (luminance + .05) ? "#000000" : "#FFFFFF";
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            await _module.DisposeAsync();
        }
    }
}
