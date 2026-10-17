using Masa.Blazor;
using Microsoft.Extensions.DependencyInjection;

namespace MasaStack.Blazor;

public static class MasaStackBlazorExtensions
{
    public const string SkinId = "masa-stack";
    public const string CssHref = "_content/MasaStack.Blazor/masa-stack-skin.css";
    public const string LightThemeName = "masa-stack-light";
    public const string DarkThemeName = "masa-stack-dark";
    public const string DefaultPrimaryColor = "#4318FF";

    public static IMasaBlazorBuilder AddMasaStack(
        this IServiceCollection services,
        Action<MasaBlazorOptions>? configure = null)
        => services.AddMasaBlazor(options =>
        {
            configure?.Invoke(options);
            options.UseMasaStack();
        });

    public static MasaBlazorOptions UseMasaStack(this MasaBlazorOptions options)
    {
        options.ConfigureTheme(theme =>
        {
            theme.Themes.Add(LightThemeName, false, ApplyLightTheme);
            theme.Themes.Add(DarkThemeName, true, ApplyDarkTheme);
        });

        return options;
    }

    private static void ApplyLightTheme(ThemeOptions theme)
    {
        theme.Primary = DefaultPrimaryColor;
        theme.OnPrimary = "#FFFFFF";
        theme.Secondary = "#485585";
        theme.OnSecondary = "#FFFFFF";
        theme.Accent = DefaultPrimaryColor;
        theme.OnAccent = "#FFFFFF";
        theme.Error = "#FF5252";
        theme.OnError = "#1B2559";
        theme.Info = "#37A7FF";
        theme.OnInfo = "#1B2559";
        theme.Success = "#00B42A";
        theme.OnSuccess = "#1B2559";
        theme.Warning = "#FF7D00";
        theme.OnWarning = "#1B2559";
        theme.Surface = "#F0F3FA";
        theme.SurfaceDim = "#E9EDF7";
        theme.SurfaceBright = "#FFFFFF";
        theme.SurfaceContainerLowest = "#FFFFFF";
        theme.SurfaceContainerLow = "#F6F8FD";
        theme.SurfaceContainer = "#F0F3FA";
        theme.SurfaceContainerHigh = "#E9EDF7";
        theme.SurfaceContainerHighest = "#E2E7F4";
        theme.OnSurface = "#1B2559";
        theme.InverseSurface = "#1B2559";
        theme.InverseOnSurface = "#FFFFFF";
        theme.InversePrimary = "#6956FF";
    }

    private static void ApplyDarkTheme(ThemeOptions theme)
    {
        theme.Primary = "#6956FF";
        theme.OnPrimary = "#FFFFFF";
        theme.Secondary = "#7681AB";
        theme.OnSecondary = "#141726";
        theme.Accent = "#6956FF";
        theme.OnAccent = "#FFFFFF";
        theme.Error = "#FF8A80";
        theme.OnError = "#141726";
        theme.Info = "#5FB9FF";
        theme.OnInfo = "#141726";
        theme.Success = "#23C343";
        theme.OnSuccess = "#141726";
        theme.Warning = "#FF9A2E";
        theme.OnWarning = "#1B2559";
        theme.Surface = "#141726";
        theme.SurfaceDim = "#0A0F1A";
        theme.SurfaceBright = "#7681AB";
        theme.SurfaceContainerLowest = "#141726";
        theme.SurfaceContainerLow = "#1B2559";
        theme.SurfaceContainer = "#323D6F";
        theme.SurfaceContainerHigh = "#485585";
        theme.SurfaceContainerHighest = "#7681AB";
        theme.OnSurface = "#F6F8FD";
        theme.InverseSurface = "#F6F8FD";
        theme.InverseOnSurface = "#141726";
        theme.InversePrimary = "#A18BFF";
    }
}
