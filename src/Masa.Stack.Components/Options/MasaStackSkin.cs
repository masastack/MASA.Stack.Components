namespace Masa.Stack.Components.Options;

public sealed class MasaStackSkin
{
    public static MasaStackSkin Default { get; } = new()
    {
        Id = "default",
        DisplayName = "MASA \u9ed8\u8ba4",
        LightTheme = "light",
        DarkTheme = "dark",
        CssHref = string.Empty
    };

    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public required string LightTheme { get; init; }

    public required string DarkTheme { get; init; }

    public required string CssHref { get; init; }

    public string PrimaryColor { get; init; } = "#4318FF";
}
