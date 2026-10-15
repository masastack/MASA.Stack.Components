namespace Masa.Stack.Components.Configs;

public sealed class SkinManager : IAsyncDisposable
{
    private readonly MasaBlazor _masa;
    private readonly IJSRuntime _js;
    private readonly GlobalConfig _config;
    private IJSObjectReference? _module;
    private readonly System.Threading.SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<string, (string Primary, string OnPrimary)> _originalColors = new();
    private bool _initialized;
    private ElementReference? _anchor;

    public IReadOnlyList<MasaStackSkin> Skins { get; }

    public MasaStackSkin? Current { get; private set; }

    public bool IsDark => _config.Dark;

    public string? CustomPrimaryColor { get; private set; }

    public string EffectivePrimaryColor => _masa.Theme.CurrentTheme.Primary;

    public event Action? Changed;

    public SkinManager(MasaBlazor masa, IJSRuntime js, GlobalConfig config,
        IEnumerable<MasaStackSkin> skins, string? defaultSkinId)
    {
        _masa = masa;
        _js = js;
        _config = config;
        Skins = skins.Prepend(MasaStackSkin.Default).DistinctBy(s => s.Id).ToArray();
        Current = Skins.FirstOrDefault(s => s.Id == defaultSkinId) ?? MasaStackSkin.Default;
    }

    public Task InitializeAsync() => ExecuteAsync(() => Task.CompletedTask);

    public Task InitializeAsync(ElementReference anchor) => ExecuteAsync(
        () => ApplyAsync(persist: false), anchor);

    private async Task InitializeCoreAsync()
    {
        if (_initialized) return;
        _module ??= await _js.InvokeAsync<IJSObjectReference>("import", "./_content/Masa.Stack.Components/js/skin.js");
        var saved = await _module.InvokeAsync<string?>("getSavedSkin");
        Current = Skins.FirstOrDefault(s => s.Id == saved) ?? Current;
        CustomPrimaryColor = NormalizeColor(await _module.InvokeAsync<string?>("getSavedPrimaryColor"));
        foreach (var name in Skins.SelectMany(s => new[] { s.LightTheme, s.DarkTheme }).Distinct())
        {
            var theme = _masa.Theme.Themes[name];
            _originalColors[name] = (theme.Primary, theme.OnPrimary);
        }
        await ApplyAsync(persist: false);
        _initialized = true;
    }

    public Task SelectAsync(string id) => ExecuteAsync(async () =>
    {
        var skin = Skins.FirstOrDefault(s => s.Id == id);
        if (skin is null || skin == Current) return;
        Current = skin;
        await ApplyAsync(persist: true);
    });

    public Task SetDarkAsync(bool dark) => ExecuteAsync(async () =>
    {
        await _config.SetDarkAsync(dark);
        await ApplyAsync(persist: true);
    });

    public Task SetPrimaryColorAsync(string? color) => ExecuteAsync(async () =>
    {
        var normalized = NormalizeColor(color);
        if (color is not null && normalized is null) return;
        CustomPrimaryColor = normalized;
        await ApplyAsync(persist: true);
    });

    private async Task ExecuteAsync(Func<Task> action, ElementReference? anchor = null)
    {
        await _gate.WaitAsync();
        try
        {
            if (anchor.HasValue) _anchor = anchor;
            await InitializeCoreAsync();
            await action();
        }
        finally { _gate.Release(); }
    }

    private async Task ApplyAsync(bool persist)
    {
        var skin = Current!;
        _masa.UpdateTheme(theme =>
        {
            foreach (var (name, original) in _originalColors)
            {
                theme.Themes[name].Primary = CustomPrimaryColor ?? original.Primary;
                theme.Themes[name].OnPrimary = CustomPrimaryColor is null
                    ? original.OnPrimary : GetOnPrimary(CustomPrimaryColor);
            }
        });
        _masa.SetTheme(IsDark ? skin.DarkTheme : skin.LightTheme);
        await _module!.InvokeVoidAsync("applySkin", skin.Id, skin.CssHref,
            Skins.Select(s => s.CssHref).ToArray(), IsDark, persist, CustomPrimaryColor,
            _masa.Theme.CurrentTheme.OnPrimary, _anchor);
        Changed?.Invoke();
    }

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
            await _module.InvokeVoidAsync("releaseApp", _anchor);
            await _module.DisposeAsync();
        }
    }
}
