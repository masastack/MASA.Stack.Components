using Masa.Blazor;
using Masa.Stack.Components.Configs;
using Masa.Stack.Components.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Xunit;

namespace Masa.Stack.Components.Tests;

public class SkinManagerTests
{
    // A catalog in the shape of Masa.Skins/wwwroot/skins.json, reduced to the roles these tests look at.
    private const string CatalogJson = """
    {
      "schema": 1,
      "default": "masa-design",
      "base": "base.css",
      "skins": [
        {
          "id": "masa-design", "name": "MasaDesign", "profile": "identity", "css": "skins/masa-design/tokens.css",
          "themes": {
            "light": { "roles": { "primary": "#4318FF", "on-primary": "#FFFFFF", "surface": "#F0F3FA" }, "opacities": { "hover-opacity": 0.04 } },
            "dark":  { "roles": { "primary": "#C5C0FF", "on-primary": "#2400A2", "surface": "#121212" }, "opacities": { "hover-opacity": 0.04 } }
          }
        },
        {
          "id": "test", "name": "Test", "profile": "themed", "css": "skins/test/tokens.css",
          "components": { "css": "skins/test/components.css", "bytes": 100 },
          "themes": {
            "light": { "roles": { "primary": "#1677FF", "on-primary": "#123456", "surface": "#FFFFFF", "future-role": "#000000" }, "opacities": { "hover-opacity": 0.08 } },
            "dark":  { "roles": { "primary": "#112233", "on-primary": "#ABCDEF", "surface": "#000000" }, "opacities": { "hover-opacity": 0.08 } }
          }
        },
        {
          "id": "other", "name": "Other", "profile": "themed", "css": "skins/other/tokens.css",
          "themes": {
            "light": { "roles": { "primary": "#52C41A", "on-primary": "#000000" } },
            "dark":  { "roles": { "primary": "#73D13D", "on-primary": "#000000" } }
          }
        }
      ]
    }
    """;

    [Fact]
    public async Task CatalogSkinsReplaceBothSchemesAndPrimarySurvivesSkinAndModeChanges()
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
        Assert.Equal("#000000", fixture.Masa.Theme.CurrentTheme.Surface);
        Assert.Equal(0.08f, fixture.Masa.Theme.CurrentTheme.Variables.HoverOpacity);

        await manager.SetDarkAsync(false);
        Assert.Equal("#1677FF", manager.EffectivePrimaryColor);
        Assert.Equal("#123456", fixture.Masa.Theme.CurrentTheme.OnPrimary);

        await manager.SelectAsync("masa-design");
        Assert.Equal("#4318FF", manager.EffectivePrimaryColor);
        // Nothing of the previous skin is left behind, including the opacities.
        Assert.Equal(0.04f, fixture.Masa.Theme.CurrentTheme.Variables.HoverOpacity);
        await manager.SetDarkAsync(true);
        Assert.Equal("#C5C0FF", manager.EffectivePrimaryColor);
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
            manager.SetPrimaryColorAsync("#000000"), manager.SelectAsync("masa-design"),
            manager.SetDarkAsync(false), manager.SetPrimaryColorAsync("#FAAD14"));
        Assert.Equal(1, fixture.Js.Imports);
        Assert.Equal(1, fixture.Js.CatalogLoads);
        Assert.Equal(1, fixture.Js.MaxConcurrent);
        Assert.Equal("masa-design", manager.Current!.Id);
        Assert.False(manager.IsDark);
        Assert.Equal("#FAAD14", manager.EffectivePrimaryColor);
        Assert.Equal(7, changes);
    }

    [Fact]
    public async Task SkinChannelArgumentsDescribeTheSkinAndItsStylesheets()
    {
        await using var fixture = new Fixture();
        await fixture.Manager.SelectAsync("test");
        var args = fixture.Js.LastApply!;
        Assert.Equal("test", args[0]);
        Assert.Equal("themed", args[1]);
        Assert.Equal("_content/Masa.Skins/skins/test/tokens.css", args[2]);
        Assert.Equal("_content/Masa.Skins/base.css", args[3]);
        Assert.Equal(false, args[4]);
        Assert.Equal(true, args[5]);
        Assert.Null(args[6]);

        await fixture.Manager.SelectAsync("masa-design");
        Assert.Equal("identity", fixture.Js.LastApply![1]);
        Assert.Equal("test", fixture.Manager.Skins.Single(s => s.Id == "test").Id);
        Assert.Equal("#1677FF", fixture.Manager.Skins.Single(s => s.Id == "test").PrimaryColor);
    }

    [Fact]
    public async Task ComponentLayerIsOffByDefaultAndNothingIsRequestedForIt()
    {
        await using var fixture = new Fixture();
        await fixture.Manager.SelectAsync("test");
        Assert.False(fixture.Manager.ComponentLayerEnabled);
        Assert.False(fixture.Manager.ComponentLayerActive);
        Assert.Equal(string.Empty, fixture.Js.LastPrepare![2]);
        Assert.Equal(string.Empty, fixture.Js.LastApply![8]);
        Assert.Equal(false, fixture.Js.LastApply![9]);
        // The catalog tells which skins have a layer; the others never do.
        Assert.True(fixture.Manager.Skins.Single(s => s.Id == "test").HasComponentLayer);
        Assert.False(fixture.Manager.Skins.Single(s => s.Id == "other").HasComponentLayer);
        Assert.False(fixture.Manager.Skins.Single(s => s.Id == "masa-design").HasComponentLayer);
    }

    [Fact]
    public async Task OptingInLoadsTheLayerOnlyForSkinsThatHaveOneAndIsRemembered()
    {
        await using var fixture = new Fixture();
        var manager = fixture.Manager;
        await manager.SelectAsync("test");
        await manager.SetComponentLayerAsync(true);
        Assert.True(manager.ComponentLayerActive);
        Assert.Equal("_content/Masa.Skins/skins/test/components.css", fixture.Js.LastPrepare![2]);
        Assert.Equal("_content/Masa.Skins/skins/test/components.css", fixture.Js.LastApply![8]);
        Assert.Equal(true, fixture.Js.LastApply![9]);
        Assert.True(fixture.Js.LastApplyPersisted);

        // A skin without a layer: the opt-in stays, nothing is requested.
        await manager.SelectAsync("other");
        Assert.True(manager.ComponentLayerEnabled);
        Assert.False(manager.ComponentLayerActive);
        Assert.Equal(string.Empty, fixture.Js.LastApply![8]);

        await manager.SelectAsync("test");
        Assert.True(manager.ComponentLayerActive);
        await manager.SetComponentLayerAsync(false);
        Assert.False(manager.ComponentLayerActive);
        Assert.Equal(string.Empty, fixture.Js.LastApply![8]);
        Assert.Equal(false, fixture.Js.LastApply![9]);
    }

    [Theory]
    [InlineData(null, false, false)]
    [InlineData(null, true, true)]
    [InlineData("1", false, true)]
    [InlineData("0", true, false)]
    public async Task SavedChoiceOfTheComponentLayerBeatsTheHostDefault(string? saved, bool hostDefault, bool expected)
    {
        await using var fixture = new Fixture(configure: o => o.ComponentLayer = hostDefault);
        fixture.Js.SavedComponentLayer = saved;
        fixture.Js.SavedSkin = "test";
        await fixture.Manager.InitializeAsync();
        Assert.Equal(expected, fixture.Manager.ComponentLayerEnabled);
        Assert.Equal(expected, fixture.Manager.ComponentLayerActive);
        Assert.False(fixture.Js.LastApplyPersisted);
    }

    [Fact]
    public async Task InitializationRestoresSavedSkinAndMapsLegacyDefaultId()
    {
        await using (var fixture = new Fixture())
        {
            fixture.Js.SavedSkin = "test";
            fixture.Js.SavedPrimary = "#abc";
            await fixture.Manager.InitializeAsync();
            Assert.Equal("test", fixture.Manager.Current!.Id);
            Assert.Equal("#AABBCC", fixture.Manager.EffectivePrimaryColor);
            Assert.False(fixture.Js.LastApplyPersisted);
        }

        await using (var fixture = new Fixture())
        {
            fixture.Js.SavedSkin = "default";
            await fixture.Manager.InitializeAsync();
            Assert.Equal("masa-design", fixture.Manager.Current!.Id);
        }

        await using (var fixture = new Fixture(configure: o => o.DefaultSkinId = "other"))
        {
            await fixture.Manager.InitializeAsync();
            Assert.Equal("other", fixture.Manager.Current!.Id);
        }

        await using (var fixture = new Fixture(configure: o => o.DefaultSkinId = "gone"))
        {
            fixture.Js.SavedSkin = "gone-too";
            await fixture.Manager.InitializeAsync();
            Assert.Equal("masa-design", fixture.Manager.Current!.Id);
        }
    }

    [Fact]
    public async Task IncludeLimitsTheOfferedSkinsButKeepsTheDefault()
    {
        await using var fixture = new Fixture(configure: o => o.Include = new HashSet<string> { "other" });
        await fixture.Manager.InitializeAsync();
        Assert.Equal(new[] { "masa-design", "other" }, fixture.Manager.Skins.Select(s => s.Id).ToArray());
        await fixture.Manager.SelectAsync("test");
        Assert.Equal("masa-design", fixture.Manager.Current!.Id);
    }

    [Fact]
    public async Task WithoutCatalogOnlyDefaultSkinExistsAndNothingIsFetched()
    {
        await using var fixture = new Fixture(catalog: false);
        fixture.Js.SavedSkin = "removed-skin";
        fixture.Js.SavedPrimary = "#abc";
        await fixture.Manager.InitializeAsync();
        Assert.Single(fixture.Manager.Skins);
        Assert.Equal("masa-design", fixture.Manager.Current!.Id);
        Assert.Equal("#AABBCC", fixture.Manager.EffectivePrimaryColor);
        Assert.Equal(0, fixture.Js.CatalogLoads);
        Assert.Equal(string.Empty, fixture.Js.LastApply![2]);

        await fixture.Manager.SetPrimaryColorAsync(null);
        Assert.Equal("#4318FF", fixture.Manager.EffectivePrimaryColor);
        await fixture.Manager.SetDarkAsync(true);
        Assert.Equal("#CFBCFF", fixture.Manager.EffectivePrimaryColor);
    }

    [Fact]
    public async Task BrokenCatalogFallsBackToTheDefaultSkinAndReportsWhy()
    {
        await using var fixture = new Fixture { };
        fixture.Js.CatalogJson = """{ "schema": 2, "default": "masa-design", "skins": [] }""";
        await fixture.Manager.InitializeAsync();
        Assert.Single(fixture.Manager.Skins);
        Assert.NotNull(fixture.Manager.CatalogError);
        Assert.Contains("schema", fixture.Manager.CatalogError);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly ServiceProvider _provider;
        public FakeJs Js { get; } = new();
        public MasaBlazor Masa { get; }
        public SkinManager Manager { get; }

        public Fixture(bool catalog = true, Action<SkinCatalogOptions>? configure = null)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IJSRuntime>(Js);
            services.AddMasaBlazor(options => options.ConfigureTheme(theme =>
            {
                theme.Themes.Light.Primary = "#4318FF";
                theme.Themes.Light.OnPrimary = "#FFFFFF";
                theme.Themes.Dark.Primary = "#CFBCFF";
            }));
            _provider = services.BuildServiceProvider();
            Masa = _provider.GetRequiredService<MasaBlazor>();
            Js.CatalogJson = CatalogJson;

            SkinCatalogOptions? options = null;
            if (catalog)
            {
                var holder = new MasaStackComponentOptions(null!);
                holder.UseSkinCatalog(configure);
                options = holder.SkinCatalog;
            }

            Manager = new SkinManager(Masa, Js, new GlobalConfig(null!, new LocalStorage(Js)), options);
        }

        public async ValueTask DisposeAsync()
        {
            await Manager.DisposeAsync();
            await _provider.DisposeAsync();
        }
    }

    private sealed class FakeJs : IJSRuntime, IJSObjectReference
    {
        public int Imports, CatalogLoads, MaxConcurrent;
        private int _active;
        public string? SavedSkin, SavedPrimary, SavedComponentLayer, CatalogJson;
        public object?[]? LastApply, LastPrepare;
        public bool LastApplyPersisted;

        public ValueTask<T> InvokeAsync<T>(string identifier, object?[]? args) => InvokeAsync<T>(identifier, default, args);

        public async ValueTask<T> InvokeAsync<T>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            if (identifier == "import") { Imports++; return (T)(object)this; }
            if (identifier == "getSavedSkin") return (T)(object?)SavedSkin!;
            if (identifier == "getSavedPrimaryColor") return (T)(object?)SavedPrimary!;
            if (identifier == "getSavedComponentLayer") return (T)(object?)SavedComponentLayer!;
            if (identifier == "prepareSkin") LastPrepare = args;
            if (identifier == "loadCatalog") { CatalogLoads++; return (T)(object)CatalogJson!; }
            if (identifier == "applySkin")
            {
                MaxConcurrent = Math.Max(MaxConcurrent, ++_active);
                LastApply = args;
                LastApplyPersisted = (bool)args![5]!;
                await Task.Delay(5, cancellationToken);
                _active--;
            }
            return default!;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
