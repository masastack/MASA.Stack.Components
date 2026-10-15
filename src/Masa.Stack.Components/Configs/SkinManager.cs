namespace Masa.Stack.Components.Configs;

public sealed class SkinManager : IAsyncDisposable
{
    private readonly MasaBlazor _masa;
    private readonly IJSRuntime _js;
    private readonly GlobalConfig _config;
    private IJSObjectReference? _module;
    private bool _initialized;

    public IReadOnlyList<MasaStackSkin> Skins { get; }

    public MasaStackSkin? Current { get; private set; }

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

    public async Task InitializeAsync()
    {
        if (_initialized || Current is null) return;

        _module ??= await _js.InvokeAsync<IJSObjectReference>("import", "./_content/Masa.Stack.Components/js/skin.js");
        var saved = await _module.InvokeAsync<string?>("getSavedSkin");
        var skin = Skins.FirstOrDefault(s => s.Id == saved) ?? Current;
        await ApplyAsync(skin, persist: false);
        _initialized = true;
    }

    public async Task SelectAsync(string id)
    {
        if (!_initialized) await InitializeAsync();
        var skin = Skins.FirstOrDefault(s => s.Id == id);
        if (skin is null || skin == Current) return;

        await ApplyAsync(skin, persist: true);
    }

    private async Task ApplyAsync(MasaStackSkin skin, bool persist)
    {
        _masa.SetTheme(_config.Dark ? skin.DarkTheme : skin.LightTheme);
        await _module!.InvokeVoidAsync("applySkin", skin.Id, skin.CssHref,
            Skins.Select(s => s.CssHref).ToArray(), _config.Dark, persist);
        Current = skin;
        Changed?.Invoke();
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null) await _module.DisposeAsync();
    }
}
