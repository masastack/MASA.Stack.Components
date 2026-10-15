namespace Masa.Stack.Components.Options;

public sealed class MasaStackSkin
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public required string LightTheme { get; init; }

    public required string DarkTheme { get; init; }

    public required string CssHref { get; init; }

    public string PrimaryColor { get; init; } = "#4318FF";
}
