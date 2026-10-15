using Masa.Blazor;
using Masa.Stack.Components.Configs;
using Masa.Stack.Components.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Xunit;

namespace Masa.Stack.Components.Tests;

public class SkinManagerTests
{
    [Fact]
    public async Task PrimarySurvivesSkinAndModeChangesAndRestoresEachOriginalPair()
    {
        await using var fixture = new Fixture();
        var manager = fixture.Manager;
        await manager.SetPrimaryColorAsync("#FAAD14");
        await manager.SelectAsync("test");
        await manager.SetDarkAsync(true);
        Assert.Equal("#FAAD14", manager.EffectivePrimaryColor);
        Assert.Equal("#000000", fixture.Masa.Theme.CurrentTheme.OnPrimary);

        await manager.SetPrimaryColorAsync(null);
        Assert.Equal("#112233", manager.EffectivePrimaryColor);
        Assert.Equal("#ABCDEF", fixture.Masa.Theme.CurrentTheme.OnPrimary);
        await manager.SetDarkAsync(false);
        Assert.Equal("#1677FF", manager.EffectivePrimaryColor);
        Assert.Equal("#123456", fixture.Masa.Theme.CurrentTheme.OnPrimary);
        await manager.SelectAsync("default");
        Assert.Equal("#4318FF", manager.EffectivePrimaryColor);
        await manager.SetDarkAsync(true);
        Assert.Equal("#CFBCFF", manager.EffectivePrimaryColor);
    }

    [Theory]
    [InlineData("#abc", "#AABBCC", "#000000")]
    [InlineData(" 1677ff ", "#1677FF", "#000000")]
    [InlineData("#000000", "#000000", "#FFFFFF")]
    [InlineData("#xyz", "#4318FF", "#FFFFFF")]
    [InlineData("##abc", "#4318FF", "#FFFFFF")]
    [InlineData("#12345678", "#4318FF", "#FFFFFF")]
    public async Task HexValidationKeepsLastValidColorAndChoosesHigherContrastText(string input, string expected, string foreground)
    {
        await using var fixture = new Fixture();
        await fixture.Manager.SetPrimaryColorAsync("#4318FF");
        await fixture.Manager.SetPrimaryColorAsync(input);
        Assert.Equal(expected, fixture.Manager.CustomPrimaryColor);
        Assert.Equal(foreground, fixture.Masa.Theme.CurrentTheme.OnPrimary);
    }

    [Fact]
    public async Task RapidChangesFinishWithLatestSelectionAndDoNotOverlapJsMutations()
    {
        await using var fixture = new Fixture();
        var manager = fixture.Manager;
        var changes = 0;
        manager.Changed += () => changes++;
        await Task.WhenAll(manager.InitializeAsync(), manager.InitializeAsync());
        await Task.WhenAll(manager.SelectAsync("test"), manager.SetDarkAsync(true),
            manager.SetPrimaryColorAsync("#000000"), manager.SelectAsync("default"),
            manager.SetDarkAsync(false), manager.SetPrimaryColorAsync("#FAAD14"));
        Assert.Equal(1, fixture.Js.Imports);
        Assert.Equal(1, fixture.Js.MaxConcurrent);
        Assert.Equal("default", manager.Current!.Id);
        Assert.False(manager.IsDark);
        Assert.Equal("#FAAD14", manager.EffectivePrimaryColor);
        Assert.Equal(7, changes);
    }

    [Fact]
    public async Task InitializationRestoresSavedAppearanceAndSupportsDefaultOnly()
    {
        await using var fixture = new Fixture(defaultOnly: true);
        fixture.Js.SavedSkin = "removed-skin";
        fixture.Js.SavedPrimary = "#abc";
        await fixture.Manager.InitializeAsync();
        Assert.Single(fixture.Manager.Skins);
        Assert.Equal("default", fixture.Manager.Current!.Id);
        Assert.Equal("#AABBCC", fixture.Manager.EffectivePrimaryColor);
        await fixture.Manager.SetPrimaryColorAsync(null);
        Assert.Equal("#4318FF", fixture.Manager.EffectivePrimaryColor);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly ServiceProvider _provider;
        public FakeJs Js { get; } = new();
        public MasaBlazor Masa { get; }
        public SkinManager Manager { get; }

        public Fixture(bool defaultOnly = false)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IJSRuntime>(Js);
            services.AddMasaBlazor(options => options.ConfigureTheme(theme =>
            {
                theme.Themes.Light.Primary = "#4318FF";
                theme.Themes.Light.OnPrimary = "#FFFFFF";
                theme.Themes.Dark.Primary = "#CFBCFF";
                theme.Themes.Add("test-light", false, t => { t.Primary = "#1677FF"; t.OnPrimary = "#123456"; });
                theme.Themes.Add("test-dark", true, t => { t.Primary = "#112233"; t.OnPrimary = "#ABCDEF"; });
            }));
            _provider = services.BuildServiceProvider();
            Masa = _provider.GetRequiredService<MasaBlazor>();
            var skins = defaultOnly ? Array.Empty<MasaStackSkin>() : new[]
            {
                new MasaStackSkin { Id = "test", DisplayName = "Test", LightTheme = "test-light", DarkTheme = "test-dark", CssHref = "test.css" }
            };
            Manager = new SkinManager(Masa, Js, new GlobalConfig(null!, new LocalStorage(Js)), skins, null);
        }

        public async ValueTask DisposeAsync()
        {
            await Manager.DisposeAsync();
            await _provider.DisposeAsync();
        }
    }

    private sealed class FakeJs : IJSRuntime, IJSObjectReference
    {
        public int Imports, MaxConcurrent;
        private int _active;
        public string? SavedSkin, SavedPrimary;

        public ValueTask<T> InvokeAsync<T>(string identifier, object?[]? args) => InvokeAsync<T>(identifier, default, args);

        public async ValueTask<T> InvokeAsync<T>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            if (identifier == "import") { Imports++; return (T)(object)this; }
            if (identifier == "getSavedSkin") return (T)(object?)SavedSkin!;
            if (identifier == "getSavedPrimaryColor") return (T)(object?)SavedPrimary!;
            if (identifier == "applySkin")
            {
                MaxConcurrent = Math.Max(MaxConcurrent, ++_active);
                await Task.Delay(5, cancellationToken);
                _active--;
            }
            return default!;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
