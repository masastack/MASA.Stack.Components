namespace Masa.Stack.Components.Options;

/// <summary>Options for the <c>Masa.Stack.Skins</c> catalog. See <see cref="MasaStackComponentOptions.UseSkinCatalog"/>.</summary>
public sealed class SkinCatalogOptions
{
    /// <summary>
    /// Where the catalog is served, relative to the host's asset root. The default is correct when the host
    /// references the <c>Masa.Stack.Skins</c> package.
    /// </summary>
    public string CatalogPath { get; set; } = "_content/Masa.Stack.Skins/skins.json";

    /// <summary>
    /// The skin used until the user picks one. <c>null</c>, empty or the legacy value <c>"default"</c> mean the
    /// catalog's default (<see cref="MasaStackSkin.DefaultId"/>).
    /// </summary>
    public string? DefaultSkinId { get; set; }

    /// <summary>
    /// Whether the optional component layer of a skin (its component-level tweaks: font stacks, link colour, picker contrast, ...)
    /// is on until the user or host code decides otherwise (<see cref="Configs.SkinManager.SetComponentLayerAsync"/>; the choice is
    /// remembered). Off by default: the shared geometry of the catalog is the whole skin, and the layer stays unloaded.
    /// </summary>
    public bool ComponentLayer { get; set; }

    /// <summary>Limits the skins offered to the user. <c>null</c> offers the whole catalog; the default skin is always offered.</summary>
    public ISet<string>? Include { get; set; }
}
