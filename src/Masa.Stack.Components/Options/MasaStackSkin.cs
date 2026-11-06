namespace Masa.Stack.Components.Options;

/// <summary>
/// A selectable skin. Skins come from the <c>Masa.Skins</c> catalog (see
/// <see cref="MasaStackComponentOptions.UseSkinCatalog"/>); without the catalog only <see cref="Default"/> exists.
/// </summary>
public sealed class MasaStackSkin
{
    /// <summary>The id of the default Stack look (the identity skin of the catalog).</summary>
    public const string DefaultId = "masa-design";

    /// <summary>The look <c>Masa.Stack.Components</c> ships with. Needs no stylesheet of its own.</summary>
    public static MasaStackSkin Default { get; } = new()
    {
        Id = DefaultId,
        DisplayName = "MASA \u9ed8\u8ba4",
        Profile = SkinProfile.Identity,
        PrimaryColor = "#4318FF",
    };

    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    /// <summary>
    /// <see cref="SkinProfile.Identity"/> skins only supply colour values; <see cref="SkinProfile.Themed"/> skins also
    /// switch the shared geometry (radius, popup shadows) of the catalog.
    /// </summary>
    public SkinProfile Profile { get; init; } = SkinProfile.Themed;

    /// <summary>The light-scheme primary colour, for swatches. Not a theme input.</summary>
    public string PrimaryColor { get; init; } = "#4318FF";

    /// <summary>The skin stylesheet relative to the host's asset root; empty when the skin needs none.</summary>
    public string CssHref { get; init; } = string.Empty;

    /// <summary>
    /// The skin ships an optional component layer (<c>components.css</c>, scoped to <c>html[data-skin][data-skin-components]</c>).
    /// It is loaded only while <see cref="Configs.SkinManager.ComponentLayerEnabled"/> is on.
    /// </summary>
    public bool HasComponentLayer { get; init; }

    public bool IsDefault => Id == DefaultId;
}

public enum SkinProfile
{
    /// <summary>The Stack default look: values only, no geometry rules.</summary>
    Identity,

    /// <summary>A design system's palette, radius, shadow and font on top of the Stack structure.</summary>
    Themed,
}
